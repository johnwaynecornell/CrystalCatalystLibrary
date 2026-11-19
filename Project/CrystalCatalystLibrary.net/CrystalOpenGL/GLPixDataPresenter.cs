using System;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;

namespace CrystalOpenGL;

/// <summary>
/// Texture filtering mode for the OpenGL presenter.
/// </summary>
public enum TextureFilter
{
    /// <summary>Nearest-neighbor filtering for pixel-accurate presentation.</summary>
    Nearest,
    /// <summary>Linear filtering for smooth scaling.</summary>
    Linear
}

/// <summary>
/// Lightweight diagnostics collected per frame by <see cref="GLPixDataPresenter"/>.
/// </summary>
public struct FrameDiagnostics
{
    /// <summary>Time spent uploading texture pixels (CPU-side, does not include GPU completion).</summary>
    public double TextureUploadMs;

    /// <summary>Time spent submitting GL draw commands (CPU-side).</summary>
    public double DrawSubmissionMs;

    /// <summary>Whether a texture reallocation occurred this frame.</summary>
    public bool TextureReallocated;

    /// <summary>Current texture dimensions.</summary>
    public int TextureWidth;
    public int TextureHeight;

    /// <summary>Current presentation viewport dimensions.</summary>
    public int ViewportWidth;
    public int ViewportHeight;
}

/// <summary>
/// Reusable OpenGL presenter that displays a CPU-side <see cref="PixData"/> buffer
/// through an OpenGL texture rendered as a screen-aligned quad.
/// 
/// The presenter owns its GPU resources (texture, shaders, buffers) but does not
/// take ownership of the PixData buffer — the caller retains ownership and lifetime.
/// 
/// Based on the patterns established by OpenGLCrystalTest_Texture.
/// </summary>
public class GLPixDataPresenter : IDisposable
{
    private readonly GL _gl;
    private uint _texture = 0;
    private uint _program = 0;
    private uint _vbo = 0;
    private uint _vao = 0;
    private uint _ebo = 0;
    private bool _disposed = false;

    private int _textureWidth = 0;
    private int _textureHeight = 0;
    private string? _lastFormat;

    private int _viewportWidth = 0;
    private int _viewportHeight = 0;

    /// <summary>Texture filtering mode; defaults to Nearest for pixel-accurate presentation.</summary>
    public TextureFilter Filter { get; set; } = TextureFilter.Nearest;

    /// <summary>Background clear color when ClearBeforeDraw is true.</summary>
    public (float R, float G, float B, float A) ClearColor { get; set; } = (0.1f, 0.1f, 0.1f, 1.0f);

    /// <summary>If true, <see cref="Present"/> clears the framebuffer before drawing the texture quad.</summary>
    public bool ClearBeforeDraw { get; set; } = true;

    /// <summary>
    /// If true, diagnostics are collected on each frame. Enabling this adds minimal
    /// overhead (Stopwatch calls) but provides useful performance data.
    /// </summary>
    public bool CollectDiagnostics { get; set; } = false;

    /// <summary>Most recent frame diagnostics (populated when CollectDiagnostics is true).</summary>
    public FrameDiagnostics LastFrameDiagnostics { get; private set; }

    /// <summary>Last upload error, if any.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Creates a new GLPixDataPresenter using the provided GL context.
    /// The GL context must be current on the calling thread.
    /// </summary>
    /// <param name="gl">Silk.NET GL instance obtained via CrystalWindow.GLGetProcAddress.</param>
    public GLPixDataPresenter(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        InitShaders();
    }

    /// <summary>
    /// Presents the given PixData buffer by uploading it to the OpenGL texture
    /// and drawing a full-screen textured quad.
    /// </summary>
    /// <param name="pixData">The pixel buffer to present. Caller retains ownership.</param>
    public void Present(ref PixData pixData)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GLPixDataPresenter));
        if (!pixData) throw new ArgumentException("PixData is not valid.");

        LastError = null;
        FrameDiagnostics diag = default;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Ensure texture exists and matches dimensions/format
        EnsureTexture(ref pixData);
        sw.Stop();
        diag.TextureReallocated = (_textureWidth != pixData.width || _textureHeight != pixData.height);

        // Upload pixel data
        sw.Restart();
        try
        {
            GLTextureHelper.WritePixels(_gl, _texture, pixData);
        }
        catch (Exception ex)
        {
            LastError = $"Texture upload failed: {ex.Message}";
            return;
        }
        sw.Stop();
        diag.TextureUploadMs = sw.Elapsed.TotalMilliseconds;

        // Set viewport
        _gl.Viewport(0, 0, (uint)_viewportWidth, (uint)_viewportHeight);

        // Clear if requested
        if (ClearBeforeDraw)
        {
            _gl.ClearColor(ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        // Draw textured quad
        sw.Restart();
        DrawQuad();
        sw.Stop();
        diag.DrawSubmissionMs = sw.Elapsed.TotalMilliseconds;

        diag.TextureWidth = _textureWidth;
        diag.TextureHeight = _textureHeight;
        diag.ViewportWidth = _viewportWidth;
        diag.ViewportHeight = _viewportHeight;

        if (CollectDiagnostics)
        {
            LastFrameDiagnostics = diag;
        }
    }

    /// <summary>
    /// Updates the presentation viewport dimensions. Call this on window resize.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Viewport dimensions must be positive.");

        _viewportWidth = width;
        _viewportHeight = height;
    }

    /// <summary>
    /// Returns the current viewport dimensions.
    /// </summary>
    public (int Width, int Height) GetViewport() => (_viewportWidth, _viewportHeight);

    /// <summary>
    /// Returns the current texture dimensions (0, 0) if no texture has been allocated.
    /// </summary>
    public (int Width, int Height) GetTextureSize() => (_textureWidth, _textureHeight);

    private void InitShaders()
    {
        // Simple fullscreen quad shaders — no rotation or effects, just present the texture.
        const string vertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTex;
            out vec2 vTex;
            void main()
            {
                gl_Position = vec4(aPos, 1.0);
                vTex = aTex;
            }
        ";

        const string fragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;
            in vec2 vTex;
            uniform sampler2D uTexture;
            void main()
            {
                FragColor = texture(uTexture, vTex);
            }
        ";

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);
        CheckShaderCompileStatus(vertexShader, "Vertex Shader");

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        CheckShaderCompileStatus(fragmentShader, "Fragment Shader");

        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);
        CheckProgramLinkStatus(_program);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        // Fullscreen quad vertices: positions + UVs
        // Positions span full OpenGL clip space (-1 to +1).
        // UVs: (0,0) at bottom-left to match OpenGL texture coordinate convention.
        // PixData row 0 is top, so we flip V coordinates: top row -> V=1, bottom row -> V=0.
        float[] vertices =
        {
             1.0f,  1.0f, 0.0f,   1.0f, 0.0f,   // top-right
             1.0f, -1.0f, 0.0f,   1.0f, 1.0f,   // bottom-right
            -1.0f, -1.0f, 0.0f,   0.0f, 1.0f,   // bottom-left
            -1.0f,  1.0f, 0.0f,   0.0f, 0.0f    // top-left
        };

        uint[] indices = { 0, 1, 3, 1, 2, 3 };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

        _gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 5 * sizeof(float), IntPtr.Zero);
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 5 * sizeof(float), (IntPtr)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        _gl.BindVertexArray(0);

        // Set texture uniform
        _gl.UseProgram(_program);
        int textureLocation = _gl.GetUniformLocation(_program, "uTexture");
        if (textureLocation != -1)
        {
            _gl.Uniform1(textureLocation, 0);
        }
        _gl.UseProgram(0);
    }

    private void EnsureTexture(ref PixData pixData)
    {
        bool needRealloc = _texture == 0 ||
                           _textureWidth != pixData.width ||
                           _textureHeight != pixData.height ||
                           _lastFormat != pixData.pix_format;

        if (needRealloc)
        {
            // Delete old texture
            if (_texture != 0)
            {
                _gl.DeleteTexture(_texture);
                _texture = 0;
            }

            // Create new texture from PixData
            _texture = GLTextureHelper.CreateTexture2DFromPixData(_gl, pixData, false, false);
            _textureWidth = pixData.width;
            _textureHeight = pixData.height;
            _lastFormat = pixData.pix_format;

            // Apply filtering
            UpdateFilter();
        }
    }

    private void UpdateFilter()
    {
        if (_texture == 0) return;

        int filter = Filter == TextureFilter.Nearest
            ? (int)TextureMinFilter.Nearest
            : (int)TextureMinFilter.Linear;

        int magFilter = Filter == TextureFilter.Nearest
            ? (int)TextureMagFilter.Nearest
            : (int)TextureMagFilter.Linear;

        if (GLHelper.HasDSA(_gl))
        {
            _gl.TextureParameter(_texture, TextureParameterName.TextureMinFilter, filter);
            _gl.TextureParameter(_texture, TextureParameterName.TextureMagFilter, magFilter);
        }
        else
        {
            int prev = GLHelper.GetInteger(_gl, GetPName.TextureBinding2D);
            try
            {
                _gl.BindTexture(TextureTarget.Texture2D, _texture);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, filter);
                _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, magFilter);
            }
            finally
            {
                _gl.BindTexture(TextureTarget.Texture2D, (uint)prev);
            }
        }
    }

    /// <summary>
    /// Called when the Filter property changes to update the GL texture parameters.
    /// Override or call manually if needed.
    /// </summary>
    protected virtual void OnFilterChanged()
    {
        UpdateFilter();
    }

    private void DrawQuad()
    {
        if (_program == 0 || _texture == 0) return;

        _gl.UseProgram(_program);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        _gl.BindVertexArray(_vao);
        GLHelper.DrawElements(_gl, PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero);

        // Restore state
        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.UseProgram(0);
    }

    private void CheckShaderCompileStatus(uint shader, string name)
    {
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == (int)GLEnum.False)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            throw new Exception($"Error compiling {name}: {infoLog}");
        }
    }

    private void CheckProgramLinkStatus(uint program)
    {
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == (int)GLEnum.False)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            throw new Exception($"Error linking program: {infoLog}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_texture != 0) { _gl.DeleteTexture(_texture); _texture = 0; }
            if (_vbo != 0) { _gl.DeleteBuffer(_vbo); _vbo = 0; }
            if (_ebo != 0) { _gl.DeleteBuffer(_ebo); _ebo = 0; }
            if (_vao != 0) { _gl.DeleteVertexArray(_vao); _vao = 0; }
            if (_program != 0) { _gl.DeleteProgram(_program); _program = 0; }
        }
        catch
        {
            // Ignore GL errors during disposal
        }
    }
}

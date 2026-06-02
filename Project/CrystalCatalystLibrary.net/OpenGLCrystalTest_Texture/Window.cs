using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using CrystalOpenGL;
using Silk.NET.OpenGL;
using SkiaSharp;
using CrystalSkia.net;

namespace OpenGLCrystalTest_Texture;

/// <summary>
/// This example demonstrates how to integrate CrystalSkia, PixData, and OpenGL (via Silk.NET).
/// It shows:
/// 1. Creating a PixData-backed texture using Skia.
/// 2. Converting PixData between formats using Skia.
/// 3. Uploading PixData to an OpenGL texture.
/// 4. Efficiently updating an OpenGL texture by drawing on its source PixData using Skia views.
/// </summary>
public class Window
{
    private CrystalWindow wnd;
    private GL _gl;
    
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    private int _mouseX;
    private int _mouseY;

    public int MouseX => _mouseX;
    public int MouseY => _mouseY;

    private uint _program;
    private uint _vbo;
    private uint _vao;
    private uint _ebo;
    private uint _texture;
    private PixData _pixData;

    private const int DemoTextureWidth = 256;
    private const int DemoTextureHeight = 256;
    private const double DemoTextureUpdateFps = 30.0;

    private int _lastTextureUpdateFrame = -1;
    
    private bool _initialized = false;

    private const string DemoPixFormat = "rgba:float32";
    private const bool DemoStrictFormat = true;
    
    // Pre-create once:
    private readonly SvgSkiaRenderer _overlayRenderer = new()
    {
        Size = new SKSize(64, 64),
        Crop = false
    };

    private Svg.Skia.SKSvg _overlaySvg;

    private const string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec2 aTex;

        out vec2 vTex;
        uniform float uTime;

        void main()
        {
            float s = sin(uTime * 0.25);
            float c = cos(uTime * 0.25);
            mat2 rot = mat2(c, -s, s, c);
            vec2 pos = rot * aPos.xy;

            gl_Position = vec4(pos, aPos.z, 1.0);
            vTex = aTex;
        }
    ";

    private const string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        in vec2 vTex;

        uniform sampler2D uTexture;
        uniform float uTime;

        void main()
        {
            vec4 tex = texture(uTexture, vTex);
            float pulse = 0.85 + 0.15 * sin(uTime);
            FragColor = vec4(tex.rgb * pulse, tex.a);
        }
    ";

    private const string svgCrystalReactorGlyph =
        @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""96"" height=""96"" viewBox=""0 0 96 96"">
  <defs>
    <radialGradient id=""coreGlow"" cx=""50%"" cy=""50%"" r=""55%"">
      <stop offset=""0%"" stop-color=""#ffffff"" stop-opacity=""0.95""/>
      <stop offset=""22%"" stop-color=""#7df9ff"" stop-opacity=""0.85""/>
      <stop offset=""58%"" stop-color=""#5b6cff"" stop-opacity=""0.32""/>
      <stop offset=""100%"" stop-color=""#000000"" stop-opacity=""0""/>
    </radialGradient>

    <linearGradient id=""crystalStroke"" x1=""10"" y1=""10"" x2=""86"" y2=""86"">
      <stop offset=""0%"" stop-color=""#00f5ff""/>
      <stop offset=""45%"" stop-color=""#ffffff""/>
      <stop offset=""100%"" stop-color=""#ff4fd8""/>
    </linearGradient>

    <linearGradient id=""bladeFill"" x1=""0"" y1=""0"" x2=""96"" y2=""96"">
      <stop offset=""0%"" stop-color=""#00eaff"" stop-opacity=""0.82""/>
      <stop offset=""52%"" stop-color=""#7b61ff"" stop-opacity=""0.55""/>
      <stop offset=""100%"" stop-color=""#ff3bd4"" stop-opacity=""0.72""/>
    </linearGradient>
  </defs>

  <circle cx=""48"" cy=""48"" r=""43"" fill=""url(#coreGlow)""/>

  <g opacity=""0.96"">
    <path d=""M48 7 L61 35 L89 48 L61 61 L48 89 L35 61 L7 48 L35 35 Z""
          fill=""url(#bladeFill)""
          stroke=""url(#crystalStroke)""
          stroke-width=""2.4""
          stroke-linejoin=""round""/>

    <path d=""M48 14 L56 39 L82 48 L56 57 L48 82 L40 57 L14 48 L40 39 Z""
          fill=""#06102b""
          fill-opacity=""0.42""
          stroke=""#ffffff""
          stroke-opacity=""0.48""
          stroke-width=""1.2""
          stroke-linejoin=""round""/>

    <circle cx=""48"" cy=""48"" r=""14""
            fill=""#071331""
            fill-opacity=""0.78""
            stroke=""#7df9ff""
            stroke-width=""2.2""/>

    <circle cx=""48"" cy=""48"" r=""7""
            fill=""#ffffff""
            fill-opacity=""0.96""/>

    <path d=""M48 25 C56 34 56 62 48 71 C40 62 40 34 48 25 Z""
          fill=""#ffffff""
          fill-opacity=""0.20""/>

    <path d=""M25 48 C34 40 62 40 71 48 C62 56 34 56 25 48 Z""
          fill=""#ffffff""
          fill-opacity=""0.16""/>

    <path d=""M31 20 L39 33 M65 63 L76 75 M20 65 L33 57 M63 33 L75 21""
          fill=""none""
          stroke=""#ffffff""
          stroke-opacity=""0.72""
          stroke-width=""2""
          stroke-linecap=""round""/>

    <circle cx=""31"" cy=""20"" r=""2.4"" fill=""#00f5ff""/>
    <circle cx=""76"" cy=""75"" r=""2.4"" fill=""#ff4fd8""/>
    <circle cx=""20"" cy=""65"" r=""2.1"" fill=""#7df9ff""/>
    <circle cx=""75"" cy=""21"" r=""2.1"" fill=""#ffffff""/>
  </g>
</svg>";
    
    public Window()
    {
        Console.WriteLine("[DEBUG_LOG] Window Constructor started");
        _width = 800;
        _height = 600;
        wnd = CrystalWindow.Create(800, 600, "OpenGLTextureTest");
        wnd.ApplicationRetain();

        Console.WriteLine("[DEBUG_LOG] Calling GLInitVersioned(3, 3)");
        bool success = wnd.GLInitVersioned(3, 3);
        Console.WriteLine($"[DEBUG_LOG] GLInitVersioned result: {success}");

        wnd.GLGetVersion(out int major, out int minor);
        Console.WriteLine($"[DEBUG_LOG] GLGetVersion reported: {major}.{minor}");

        Console.WriteLine("[DEBUG_LOG] Getting GL API");
        _gl = GL.GetApi(wnd.GLGetProcAddress);

        wnd.OnDraw = OnDraw;
        wnd.OnResize = OnResize;
        wnd.OnClose = OnClose;
        wnd.OnIdle = OnIdle;
        wnd.OnMouseDown = OnMouseDown;
        wnd.OnMouseMove = OnMouseMove;
        
        Console.WriteLine("[DEBUG_LOG] Window Constructor finished");
        
        _overlaySvg = new Svg.Skia.SKSvg();
        _overlaySvg = Crystal.SKSvgFromText(svgCrystalReactorGlyph);
    }
    

    /// <summary>
    /// Initialize OpenGL resources, shaders, and the initial texture.
    /// </summary>
    private void InitGLResources()
    {
        try
        {
            var version = _gl.GetStringS(GLEnum.Version);
            var vendor = _gl.GetStringS(GLEnum.Vendor);
            var renderer = _gl.GetStringS(GLEnum.Renderer);
            Console.WriteLine($"[DEBUG_LOG] OpenGL Version: {version}");
            Console.WriteLine($"[DEBUG_LOG] OpenGL Vendor: {vendor}");
            Console.WriteLine($"[DEBUG_LOG] OpenGL Renderer: {renderer}");

            // Guard against legacy/software OpenGL
            bool isSoftware = renderer.Contains("GDI Generic", StringComparison.OrdinalIgnoreCase) ||
                              renderer.Contains("Software Rasterizer", StringComparison.OrdinalIgnoreCase) ||
                              renderer.Contains("swrast", StringComparison.OrdinalIgnoreCase);
            bool versionOk = false;
            if (version != null && Version.TryParse(version.Split(' ')[0], out var v))
            {
                if (v >= new Version(3, 3)) versionOk = true;
            }

            if (isSoftware || !versionOk)
            {
                Console.WriteLine("-----------------------------------------------------------");
                Console.WriteLine("CRITICAL WARNING: Modern OpenGL (3.3+) not detected!");
                Console.WriteLine($"Detected Renderer: {renderer}");
                Console.WriteLine($"Detected Version: {version}");
                Console.WriteLine("Shader setup will likely fail. Skipping shader initialization.");
                Console.WriteLine("Please ensure your GPU drivers are installed and support OpenGL 3.3.");
                Console.WriteLine("-----------------------------------------------------------");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG_LOG] Failed to get GL info: {ex.Message}");
        }

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, VertexShaderSource);
        _gl.CompileShader(vertexShader);
        CheckShaderCompileStatus(vertexShader, "Vertex Shader");

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, FragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        CheckShaderCompileStatus(fragmentShader, "Fragment Shader");

        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);
        CheckProgramLinkStatus(_program);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        Console.WriteLine($"[DEBUG_LOG] Program ID: {_program}");

        float[] vertices =
        {
            // positions           // uv
             0.5f,  0.5f, 0.0f,   1.0f, 0.0f,
             0.5f, -0.5f, 0.0f,   1.0f, 1.0f,
            -0.5f, -0.5f, 0.0f,   0.0f, 1.0f,
            -0.5f,  0.5f, 0.0f,   0.0f, 0.0f
        };

        uint[] indices = {
            0, 1, 3,
            1, 2, 3
        };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        Console.WriteLine($"[DEBUG_LOG] VAO: {_vao}, VBO: {_vbo}, EBO: {_ebo}");

        _gl.BindVertexArray(_vao);
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
        
        _gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 5 * sizeof(float), IntPtr.Zero);
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 5 * sizeof(float), (IntPtr)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        
        // Demonstrates high-level PixData creation and conversion:
        // 1. Create a demo texture in rgba:int8 format using FixedPixDataRenderer.
        using PixData sourcePixData = FixedPixDataRenderer.CreateDemoTexture(
            DemoTextureWidth,
            DemoTextureHeight,
            "rgba:int8",
            DemoStrictFormat);

        // 2. Convert the source PixData to a different format (rgba:float32) using Skia.
        //    This uses Skia's internal rendering engine to remaster the pixels into the target format.
        _pixData = PixDataSkia.ConvertBySkia(
            sourcePixData,
            SkiaPixFormatMap.GetImageInfo(
                DemoPixFormat,
                sourcePixData.width,
                sourcePixData.height));
        
        // 3. Create an OpenGL texture directly from the resulting PixData.
        _texture = GLTextureHelper.CreateTexture2DFromPixData(
            _gl,
            _pixData,
            false,
            DemoStrictFormat);
        
        _gl.UseProgram(_program);
        int textureLocation = _gl.GetUniformLocation(_program, "uTexture");
        if (textureLocation != -1)
        {
            _gl.Uniform1(textureLocation, 0);
        }

        var err = _gl.GetError();
        if (err != GLEnum.NoError)
        {
            Console.WriteLine($"[DEBUG_LOG] GL Error during Init: {err}");
        }
    }
    
    /// <summary>
    /// Updates the animated part of the texture.
    /// This demonstrates how to use Skia to draw onto a PixData buffer and then
    /// update only the modified parts of an OpenGL texture.
    /// </summary>
    private void UpdateAnimatedTexture(double time)
    {
        if (!_pixData)
            return;

        int frame = (int)(time * DemoTextureUpdateFps);
        if (frame == _lastTextureUpdateFrame)
            return;

        _lastTextureUpdateFrame = frame;

        // Create a new temporary PixData with the same format as our main texture buffer.
        using PixData framePixData = FixedPixDataRenderer.CreateFixed(
            _pixData.width,
            _pixData.height,
            (canvas, info) =>
            {
                // Create a temporary SKBitmap 'view' into our persistent _pixData.
                // This does NOT copy the pixels; it wraps the existing memory.
                using var sourceBitmap = PixDataSkia.CreateBitmapView(_pixData);
                
                // Draw the static background from our base buffer.
                canvas.DrawBitmap(sourceBitmap, 0, 0);
                
                // Draw the dynamic/animated elements on top using Skia.
                // 1. In texture svg
                canvas.Save();

                float t = (float)time;
                float size = 64.0f;
                float margin = 10.0f;

                float x = info.Width - size - margin;
                float y = margin;

                canvas.Translate(x , y );

                if (true)
                {
                    canvas.RotateDegrees(t * 55.0f, size * 0.5f, size * 0.5f);
                    canvas.Scale(size / 96.0f, size / 96.0f);

                    canvas.DrawPicture(_overlaySvg.Picture);
                }
                else
                {
                    /* The PixData path. Any PixData can be rendered to a bitmap, which can be drawn to the canvas.
                       This may often be optimized out so this serves as a more generic illustration or model to add post proc to  
                    */
                    
                    _overlayRenderer.Size = new SKSize(96, 96);
                    SKMatrix mat = SKMatrix.Identity;
                    mat = mat.PreConcat(SKMatrix.CreateRotationDegrees(t * 55.0f, size * 0.5f, size * 0.5f));
                    mat = mat.PreConcat(SKMatrix.CreateScale(size / 96.0f, size / 96.0f));
                    _overlayRenderer.Matrix = mat;
                        
                    PixData pd = _overlayRenderer.RenderPix(_overlaySvg);
                    PixDataSkia.WithBitmapView(pd, (bitmap) => canvas.DrawBitmap(bitmap, 0, 0));
                }
                
                canvas.Restore();
                
                // 2. Skia drawing
                DrawAnimatedCornerSquare(canvas, info, time);
            },
            DemoPixFormat,
            DemoStrictFormat);

        // Upload the newly rendered frame to the existing OpenGL texture.
        GLTextureHelper.WritePixels(_gl, _texture, framePixData, DemoStrictFormat);
    }
    
    /// <summary>
    /// Draws a small animated square in the corner of the Skia canvas.
    /// </summary>
    private static void DrawAnimatedCornerSquare(SKCanvas canvas, SKImageInfo info, double time)
    {
        const float margin = 12.0f;
        const float travel = 48.0f;
        const float size = 34.0f;

        float pulse = 0.5f + 0.5f * MathF.Sin((float)time * 4.0f);
        float x = margin + travel * pulse;
        float y = margin;

        using var fill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(
                (byte)(80 + 175 * pulse),
                (byte)(220 - 120 * pulse),
                255,
                230)
        };

        using var stroke = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            Color = SKColors.White
        };

        var rect = new SKRect(x, y, x + size, y + size);
        canvas.DrawRoundRect(rect, 6, 6, fill);
        canvas.DrawRoundRect(rect, 6, 6, stroke);
    }

    private void CheckShaderCompileStatus(uint shader, string name)
    {
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == (int)GLEnum.False)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            Console.WriteLine($"Error compiling {name}: {infoLog}");
        }
    }

    private void CheckProgramLinkStatus(uint program)
    {
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == (int)GLEnum.False)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            Console.WriteLine($"Error linking program: {infoLog}");
        }
    }

    
    /// <summary>
    /// Main entry point for the window loop.
    /// </summary>
    public void Run()
    {
        wnd.Show(true);
        Application.Run();
    }

    private void OnClose(CrystalWindow windowHandle)
    {
        if (_initialized)
        {
            _gl.DeleteTexture(_texture);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteProgram(_program);
            _pixData.Dispose();
        }
        wnd.ApplicationRelease();
    }

    private void OnResize(CrystalWindow windowHandle, int width, int height)
    {
        Console.WriteLine($"[DEBUG_LOG] Resize: {width}x{height}");
        _width = width;
        _height = height;
        if (_initialized)
        {
            _gl.Viewport(0, 0, (uint)width, (uint)height);
        }
    }

    
    private void OnIdle(CrystalWindow windowHandle)
    {
        wnd.QueueRedraw();
    }

    private void OnMouseMove(CrystalWindow windowHandle, int x, int y)
    {
        _mouseX = x;
        _mouseY = y;
    }

    private void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
    }

    /// <summary>
    /// The main draw loop. Called by the CrystalWindow when a redraw is requested.
    /// </summary>
    private void OnDraw(CrystalWindow windowHandle)
    {
        wnd.GLMakeCurrent();
        if (!_initialized)
        {
            InitGLResources();
            _gl.Viewport(0, 0, (uint)_width, (uint)_height);
            _initialized = true;
        }

        double time = wnd.uptimeSeconds();
        UpdateAnimatedTexture(time);
        
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_program != 0)
        {
            _gl.UseProgram(_program);
            int timeLocation = _gl.GetUniformLocation(_program, "uTime");
            if (timeLocation != -1)
            {
                _gl.Uniform1(timeLocation, (float)time);
            }

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _texture);

            _gl.BindVertexArray(_vao);
            GLHelper.DrawElements(_gl, PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero);
        }
        
        var err = _gl.GetError();
        if (err != GLEnum.NoError)
        {
            Console.WriteLine($"[DEBUG_LOG] GL Error: {err}");
        }

        wnd.GLPresent();
    }
}

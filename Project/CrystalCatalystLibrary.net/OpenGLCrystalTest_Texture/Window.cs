using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using CrystalOpenGL;
using Silk.NET.OpenGL;
using SkiaSharp;
using CrystalSkia.net;

namespace OpenGLCrystalTest_Texture;

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

    private const string DemoPixFormat = "rgba:int8";
    private const bool DemoStrictFormat = false;

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
    }
    

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

        _pixData = FixedPixDataRenderer.CreateDemoTexture(
            DemoTextureWidth,
            DemoTextureHeight,
            DemoPixFormat,
            DemoStrictFormat);

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
    
    private void UpdateAnimatedTexture(double time)
    {
        if (!_pixData)
            return;

        int frame = (int)(time * DemoTextureUpdateFps);
        if (frame == _lastTextureUpdateFrame)
            return;

        _lastTextureUpdateFrame = frame;

        using PixData framePixData = FixedPixDataRenderer.CreateFixed(
            _pixData.width,
            _pixData.height,
            (canvas, info) =>
            {
                using var sourceBitmap = PixDataSkia.CreateBitmapView(_pixData);
                canvas.DrawBitmap(sourceBitmap, 0, 0);
                DrawAnimatedCornerSquare(canvas, info, time);
            },
            DemoPixFormat,
            DemoStrictFormat);

        GLTextureHelper.WritePixels(_gl, _texture, framePixData, DemoStrictFormat);
    }
    
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

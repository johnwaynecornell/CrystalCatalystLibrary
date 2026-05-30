using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using CrystalOpenGL;
using Silk.NET.OpenGL;
using SkiaSharp;
using CrystalSkia.net;

namespace OpenGLCrystalTest;

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

    private bool _initialized = false;

    private const string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aColor;
        out vec4 vertexColor;
        uniform float uTime;
        void main()
        {
            float s = sin(uTime);
            float c = cos(uTime);
            mat2 rot = mat2(c, -s, s, c);
            vec2 pos = rot * aPos.xy;
            gl_Position = vec4(pos, aPos.z, 1.0);
            vertexColor = vec4(aColor, 1.0);
        }
    ";

    private const string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        in vec4 vertexColor;
        uniform float uTime;
        void main()
        {
            float r = vertexColor.r * (0.5 + 0.5 * sin(uTime));
            float g = vertexColor.g * (0.5 + 0.5 * sin(uTime + 2.0));
            float b = vertexColor.b * (0.5 + 0.5 * sin(uTime + 4.0));
            FragColor = vec4(r, g, b, 1.0);
        }
    ";
    
    public Window()
    {
        Console.WriteLine("[DEBUG_LOG] Window Constructor started");
        _width = 800;
        _height = 600;
        wnd = CrystalWindow.Create(800, 600, "OpenGLTest");
        wnd.ApplicationRetain();

        Console.WriteLine("[DEBUG_LOG] Calling GLInit");
        wnd.GLInit();
        Console.WriteLine("[DEBUG_LOG] GLInit called, getting GL API");
        _gl = GL.GetApi(wnd.GLGetProcAddress);

        wnd.OnDraw = OnDraw;
        wnd.OnResize = OnResize;
        wnd.OnClose = OnClose;
        wnd.OnIdle = OnIdle;
        wnd.OnMouseDown = OnMouseDown;
        wnd.OnMouseMove = OnMouseMove;
        
        Console.WriteLine("[DEBUG_LOG] Initializing GL Resources deferred to OnDraw");
        // InitGLResources();
        
        // OnResize(wnd, 800, 600);
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
            bool isGdi = renderer.Contains("GDI Generic", StringComparison.OrdinalIgnoreCase);
            bool versionOk = false;
            if (version != null && Version.TryParse(version.Split(' ')[0], out var v))
            {
                if (v >= new Version(3, 3)) versionOk = true;
            }

            if (isGdi || !versionOk)
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

        float[] vertices = {
            // positions         // colors
             0.5f,  0.5f, 0.0f,  1.0f, 0.0f, 0.0f,
             0.5f, -0.5f, 0.0f,  0.0f, 1.0f, 0.0f,
            -0.5f, -0.5f, 0.0f,  0.0f, 0.0f, 1.0f,
            -0.5f,  0.5f, 0.0f,  1.0f, 1.0f, 0.0f
        };

        uint[] indices = {
            0, 1, 3,
            1, 2, 3
        };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        uint ebo = _gl.GenBuffer();

        Console.WriteLine($"[DEBUG_LOG] VAO: {_vao}, VBO: {_vbo}, EBO: {ebo}");

        _gl.BindVertexArray(_vao);
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
        
        _gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 6 * sizeof(float), IntPtr.Zero);
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(1, 3, GLEnum.Float, false, 6 * sizeof(float), (IntPtr)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        var err = _gl.GetError();
        if (err != GLEnum.NoError)
        {
            Console.WriteLine($"[DEBUG_LOG] GL Error during Init: {err}");
        }
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
        if ((CrystalMouseButton)button == CrystalMouseButton.Left)
        {
        }
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
        
        // Clear with a darker color like proto
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

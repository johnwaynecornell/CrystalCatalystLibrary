using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CrystalCatalystLibrary.net;
using CrystalOpenGL;
using Silk.NET.OpenGL;
using SkiaSharp;
using CrystalSkia.net;

namespace OpenGLCrystalTest_Ribbon;

public class Window
{
    private CrystalWindow wnd;
    private GL _gl;
    
    private int _width;
    private int _height;

    private uint _program;
    private uint _vbo;
    private uint _vao;
    private uint _ebo;
    private uint _texture;
    
    private bool _initialized = false;
    private GLRenderer _glRenderer = new();

    private const int NumSegments = 64;
    private const int NumRotations = 16;
    private float[] _ribbonVertices = new float[(NumSegments + 1) * 2 * 5];
    private uint[] _ribbonIndices = new uint[NumSegments * 6];

    private const string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec2 aTex;
        out vec2 vTex;
        uniform mat4 uModel;
        uniform mat4 uProjection;
        void main()
        {
            gl_Position = uProjection * uModel * vec4(aPos, 1.0);
            vTex = aTex;
        }
    ";

    private const string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        in vec2 vTex;
        uniform sampler2D uTexture;
        void main()
        {
            FragColor = texture(uTexture, vTex);
        }
    ";

    public Window()
    {
        _width = 800;
        _height = 600;
        wnd = CrystalWindow.Create(800, 600, "Ribbon Art");
        wnd.ApplicationRetain();

        // OpenGLCrystalTest_Ribbon requests OpenGL 4.5 intentionally.
        // This validates the modern OpenGL path. The render also works with 3.3
        
        GLOptions opts = GLOptions.Default;
        opts.major = 4;
        opts.minor = 5;
        opts.strict = false;
        
        if (!wnd.GLInitAdvanced(opts)) throw new Exception("Failed to initialize OpenGL with advanced options");
        
        _gl = GL.GetApi(wnd.GLGetProcAddress);

        wnd.OnDraw = OnDraw;
        wnd.OnResize = OnResize;
        wnd.OnClose = OnClose;
        wnd.OnIdle = OnIdle;
    }

    private void InitGLResources()
    {
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, VertexShaderSource);
        _gl.CompileShader(vertexShader);

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, FragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        for (int i = 0; i < NumSegments; i++)
        {
            uint startVertex = (uint)(i * 2);
            _ribbonIndices[i * 6 + 0] = startVertex;
            _ribbonIndices[i * 6 + 1] = startVertex + 1;
            _ribbonIndices[i * 6 + 2] = startVertex + 2;
            _ribbonIndices[i * 6 + 3] = startVertex + 1;
            _ribbonIndices[i * 6 + 4] = startVertex + 3;
            _ribbonIndices[i * 6 + 5] = startVertex + 2;
        }

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, _ribbonVertices, BufferUsageARB.DynamicDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, _ribbonIndices, BufferUsageARB.StaticDraw);

        _gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 5 * sizeof(float), IntPtr.Zero);
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(1, 2, GLEnum.Float, false, 5 * sizeof(float), (IntPtr)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        // Create a texture using Skia
        CreateTexture();
    }
    
    private void CreateTexture()
{
    int size = 256;
    using var bitmap = new SKBitmap(size, size);
    using var canvas = new SKCanvas(bitmap);

    canvas.Clear(SKColors.Transparent);

    using var paint = new SKPaint
    {
        IsAntialias = true
    };

    // 1. Width profile: transparent edges, luminous center
    paint.Shader = SKShader.CreateLinearGradient(
        new SKPoint(0, 0),
        new SKPoint(0, size),
        new[]
        {
            SKColors.Transparent,
            SKColors.Cyan.WithAlpha(80),
            SKColors.White.WithAlpha(210),
            SKColors.Magenta.WithAlpha(110),
            SKColors.Transparent
        },
        new[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f },
        SKShaderTileMode.Clamp);

    canvas.DrawRect(0, 0, size, size, paint);

    // 2. Directional energy bands along the ribbon
    paint.Shader = null;
    paint.StrokeWidth = 5;
    paint.Color = SKColors.White.WithAlpha(70);

    for (int x = -size; x < size * 2; x += 32)
    {
        canvas.DrawLine(
            x, size,
            x + size / 2, 0,
            paint);
    }

    // 3. Soft center glow line
    paint.StrokeWidth = 18;
    paint.Color = SKColors.White.WithAlpha(50);
    canvas.DrawLine(0, size / 2, size, size / 2, paint);

    paint.StrokeWidth = 3;
    paint.Color = SKColors.White.WithAlpha(180);
    canvas.DrawLine(0, size / 2, size, size / 2, paint);

    // 4. Small flecks / stars
    paint.Style = SKPaintStyle.Fill;
    paint.Color = SKColors.White.WithAlpha(160);

    var rand = new Random(12345);
    for (int i = 0; i < 40; i++)
    {
        float x = rand.NextSingle() * size;
        float y = (float)(size * 0.35 + rand.NextDouble() * size * 0.30);
        float r = 0.75f + rand.NextSingle() * 1.5f;
        canvas.DrawCircle(x, y, r, paint);
    }

    PixData pix = new PixData
    {
        width = size,
        height = size,
        pix_format = "rgba:int8",
        pix_data = bitmap.GetPixels(),
        pix_data_length = (IntPtr)bitmap.ByteCount,
        pix_data_free = (IntPtr p) => true
    };

    _texture = GLTextureHelper.CreateTexture2DFromPixData(_gl, pix);
}

    private void CreateTexture_orig()
    {
        int size = 256;
        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(size, size),
                new[] { SKColors.Cyan, SKColors.Magenta, SKColors.Yellow },
                null,
                SKShaderTileMode.Repeat)
        };
        canvas.DrawRect(0, 0, size, size, paint);

        // Draw some circles for detail
        paint.Shader = null;
        paint.Color = SKColors.White.WithAlpha(128);
        paint.IsAntialias = true;
        canvas.DrawCircle(size / 2, size / 2, size / 4, paint);
        
        PixData pix = new PixData();
        pix.width = size;
        pix.height = size;
        pix.pix_format = "rgba:int8";
        pix.pix_data = bitmap.GetPixels();
        pix.pix_data_length = (IntPtr)bitmap.ByteCount;
        pix.pix_data_free = (IntPtr p) => true;

        _texture = GLTextureHelper.CreateTexture2DFromPixData(_gl, pix);
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

    private void OnDraw(CrystalWindow windowHandle)
    {
        wnd.GLMakeCurrent();
        if (!_initialized)
        {
            InitGLResources();
            _initialized = true;
        }

        double time = wnd.uptimeSeconds();
        
        using var pix = _glRenderer.RenderPix(_gl, _width, _height, (gl, texture) =>
        {
            gl.ClearColor(0.05f, 0.05f, 0.1f, 1.0f);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            gl.UseProgram(_program);
            gl.BindTexture(TextureTarget.Texture2D, _texture);
            gl.BindVertexArray(_vao);

            // Animate vertex data of the ribbon
            Matrix4x4 cumulative = Matrix4x4.Identity;
            float ribbonWidth = 1.2f;
            for (int i = 0; i <= NumSegments; i++)
            {
                Vector3 pLeft = Vector3.Transform(new Vector3(0, -ribbonWidth / 2, 0), cumulative);
                Vector3 pRight = Vector3.Transform(new Vector3(0, ribbonWidth / 2, 0), cumulative);

                int baseIdx = i * 10;
                // Left vertex
                _ribbonVertices[baseIdx + 0] = pLeft.X;
                _ribbonVertices[baseIdx + 1] = pLeft.Y;
                _ribbonVertices[baseIdx + 2] = pLeft.Z;
                _ribbonVertices[baseIdx + 3] = 0.0f; // U
                _ribbonVertices[baseIdx + 4] = (float)i / NumSegments; // V

                // Right vertex
                _ribbonVertices[baseIdx + 5] = pRight.X;
                _ribbonVertices[baseIdx + 6] = pRight.Y;
                _ribbonVertices[baseIdx + 7] = pRight.Z;
                _ribbonVertices[baseIdx + 8] = 1.0f; // U
                _ribbonVertices[baseIdx + 9] = (float)i / NumSegments; // V

                if (i < NumSegments)
                {
                    float sTime = (float)time + i * 0.05f;
                    float rotX = MathF.Sin(sTime * 0.5f) * 0.15f;
                    float rotY = MathF.Cos(sTime * 0.7f) * 0.15f;
                    float rotZ = MathF.Sin(sTime * 0.3f) * 0.25f;

                    Matrix4x4 step = Matrix4x4.CreateTranslation(0.5f, 0, 0) *
                                     Matrix4x4.CreateRotationX(rotX) *
                                     Matrix4x4.CreateRotationY(rotY) *
                                     Matrix4x4.CreateRotationZ(rotZ);
                    cumulative = step * cumulative;
                }
            }

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            gl.BufferSubData<float>(BufferTargetARB.ArrayBuffer, 0, _ribbonVertices);

            int modelLoc = gl.GetUniformLocation(_program, "uModel");
            int projLoc = gl.GetUniformLocation(_program, "uProjection");

            float aspect = (float)_width / _height;
            Matrix4x4 projection = Matrix4x4.CreateOrthographic(40.0f * aspect, 40.0f, -100.0f, 100.0f);
            gl.UniformMatrix4(projLoc, 1, false, GetMatrixValues(projection));

            for (int r = 0; r < NumRotations; r++)
            {
                float angleOffset = (float)(r * 2.0 * Math.PI / NumRotations);
                Matrix4x4 rotation = Matrix4x4.CreateRotationZ(angleOffset + (float)time * 0.2f);

                gl.UniformMatrix4(modelLoc, 1, false, GetMatrixValues(rotation));
                GLBridges.DrawElements(gl, PrimitiveType.Triangles, (uint)(NumSegments * 6), DrawElementsType.UnsignedInt, IntPtr.Zero);
            }
        }, "rgba:int8");

        SKImageInfo info = PixDataSkia.GetImageInfo(pix);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        
        //Draw pix
        PixDataSkia.WithBitmapView(pix, (bitmap) => canvas.DrawBitmap(bitmap, 0, 0));
        
        // Overlay text
        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(200),
            TextSize = 32,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        canvas.DrawText("OpenGL Ribbon Art", 30, 50, paint);
        
        paint.TextSize = 18;
        canvas.DrawText($"Time: {time:F2}s | Segments: {NumSegments} | Rotations: {NumRotations}", 30, 80, paint);

        canvas.Flush();
        // Present to window
        var skPixmap = surface.PeekPixels();
        wnd.PresentImage("rgba:int8", skPixmap.GetPixels(), (IntPtr)(info.RowBytes * info.Height), info.Width, info.Height);
    }

    private float[] GetMatrixValues(Matrix4x4 m)
    {
        return new[] {
            m.M11, m.M21, m.M31, m.M41,
            m.M12, m.M22, m.M32, m.M42,
            m.M13, m.M23, m.M33, m.M43,
            m.M14, m.M24, m.M34, m.M44
        };
    }
}

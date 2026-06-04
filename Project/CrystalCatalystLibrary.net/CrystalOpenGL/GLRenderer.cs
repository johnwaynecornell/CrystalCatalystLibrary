using System;
using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;
using System.Runtime.InteropServices;

namespace CrystalOpenGL;

/// <summary>
/// Provides utilities for rendering to OpenGL textures and capturing the result as <see cref="PixData"/>.
/// </summary>
public class GLRenderer
{
    /// <summary>
    /// Renders to a specified OpenGL texture using a provided action.
    /// </summary>
    public static void RenderToTexture(GL gl, uint texture, Action<GL, uint> action)
    {
        // Bind the texture to query its dimensions
        gl.BindTexture(TextureTarget.Texture2D, texture);

        GLTextureHelper.GetTextureDimensions(gl, texture, out int width, out int height);
            
        // Generate and bind FBO
        uint fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        // Attach the existing texture
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);

        // Generate and setup RBO for depth and stencil
        uint rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)width, (uint)height);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);

        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            gl.DeleteFramebuffer(fbo);
            gl.DeleteRenderbuffer(rbo);
            gl.DeleteTexture(texture);
            throw new Exception("Framebuffer is not complete");
        }

        // Set viewport for off-screen rendering
        gl.Viewport(0, 0, (uint)width, (uint)height);
        
        // Execute the render action
        action(gl, texture);
        gl.Flush();
        
        // Cleanup GL resources
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(fbo);
        gl.DeleteRenderbuffer(rbo);
    }
    
    /// <summary>
    /// Renders to a temporary OpenGL texture and returns the result as <see cref="PixData"/>.
    /// </summary>
    public PixData RenderPix(GL gl, int width, int height, Action<GL, uint> action, string? pixFormatDest = null)
    {
    
        // Generate and setup texture
        uint texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);
        
        InternalFormat uploadFormat;
        PixelFormat pixelFormat;
        PixelType pixelType;

        CrystalOpenGL.GLPixFormatMap.TryGetUploadFormat(pixFormatDest, out uploadFormat, out pixelFormat,
            out pixelType);
        GLBridges.TexImage2D(gl, TextureTarget.Texture2D, 0, uploadFormat, (uint)width, (uint)height, 0, pixelFormat, pixelType, IntPtr.Zero);
    
        RenderToTexture(gl, texture, action);
        
        // Read the pixels back into PixData
        // We read from the texture we just rendered into
        PixData pix = GLTextureHelper.ReadPixels(gl, texture, pixFormatDest);

        // Cleanup GL resources
        gl.DeleteTexture(texture);

        return pix;
    }
}

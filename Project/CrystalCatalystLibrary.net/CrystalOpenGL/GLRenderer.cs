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
        GLTextureHelper.GetTextureDimensions(gl, texture, out int width, out int height);
            
        uint fbo;
        uint rbo;

        if (GLHelper.VersionCompare(gl, 4, 5) >= 0)
        {
            gl.CreateFramebuffers(1, out fbo);
            gl.NamedFramebufferTexture(fbo, FramebufferAttachment.ColorAttachment0, texture, 0);

            gl.CreateRenderbuffers(1, out rbo);
            gl.NamedRenderbufferStorage(rbo, InternalFormat.Depth24Stencil8, (uint)width, (uint)height);
            gl.NamedFramebufferRenderbuffer(fbo, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);

            if (gl.CheckNamedFramebufferStatus(fbo, FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                gl.DeleteFramebuffer(fbo);
                gl.DeleteRenderbuffer(rbo);
                gl.DeleteTexture(texture);
                throw new Exception("Framebuffer is not complete");
            }
        }
        else
        {
            // Generate and bind FBO
            fbo = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            // Attach the existing texture
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);

            // Generate and setup RBO for depth and stencil
            rbo = gl.GenRenderbuffer();
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
        }

        // Set viewport and execute render action
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
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
        InternalFormat uploadFormat;
        PixelFormat pixelFormat;
        PixelType pixelType;

        CrystalOpenGL.GLPixFormatMap.TryGetUploadFormat(pixFormatDest ?? "", out uploadFormat, out pixelFormat,
            out pixelType);

        uint texture;
        if (GLHelper.VersionCompare(gl, 4, 5) >= 0)
        {
            gl.CreateTextures(TextureTarget.Texture2D, 1, out texture);
            gl.TextureStorage2D(texture, 1, (GLEnum)uploadFormat, (uint)width, (uint)height);
        }
        else
        {
            // Generate and setup texture
            texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);
            GLBridges.TexImage2D(gl, TextureTarget.Texture2D, 0, uploadFormat, (uint)width, (uint)height, 0, pixelFormat, pixelType, IntPtr.Zero);
        }
    
        RenderToTexture(gl, texture, action);
        
        // Read the pixels back into PixData
        // We read from the texture we just rendered into
        PixData pix = GLTextureHelper.ReadPixels(gl, texture, pixFormatDest);

        // Cleanup GL resources
        gl.DeleteTexture(texture);

        return pix;
    }
}

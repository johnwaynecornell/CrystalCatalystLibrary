using System;
using Silk.NET.OpenGL;
using CrystalCatalystLibrary.net;
using System.Runtime.InteropServices;

namespace CrystalOpenGL;

public class GLRenderer
{
    public void RenderToTexture(GL gl, uint texture, Action<GL> action)
    {
        // Bind the texture to query its dimensions
        gl.BindTexture(TextureTarget.Texture2D, texture);

        int width = (int) gl.GetTextureLevelParameter(texture, 0, GetTextureParameter.TextureWidth);
        int height =(int)  gl.GetTextureLevelParameter(texture, 0, GetTextureParameter.TextureHeight);


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
        action(gl);
        gl.Flush();
        
        // Cleanup GL resources
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(fbo);
        gl.DeleteRenderbuffer(rbo);
    }
    
    public PixData RenderPix(GL gl, int width, int height, Action<GL> action, string? pixFormatDest = null)
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
    /*public PixData RenderPix(GL gl, int width, int height, Action<GL> action, string? pixFormatDest = null)
       {
           // Generate and bind FBO
           uint fbo = gl.GenFramebuffer();
           gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

           // Generate and setup texture
           uint texture = gl.GenTexture();
           gl.BindTexture(TextureTarget.Texture2D, texture);
           GLBridges.TexImage2D(gl, TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
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
           action(gl);

           // Read the pixels back into PixData
           // We read from the texture we just rendered into
           PixData pix = GLTextureHelper.ReadPixels(gl, texture, pixFormatDest);

           // Cleanup GL resources
           gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
           gl.DeleteFramebuffer(fbo);
           gl.DeleteRenderbuffer(rbo);
           gl.DeleteTexture(texture);

           return pix;
       }*/
}

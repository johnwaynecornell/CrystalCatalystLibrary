using System.Data.SqlTypes;
using JWCEssentials.net;

namespace CrystalCatalystLibrary.net;

/// <summary>
/// Represents raw pixel data for image presentation, including format specification, dimensions, and memory management.
/// This structure is used to pass image data between managed code and native CrystalCatalystLibrary for window presentation.
/// </summary>
public struct PixData : IDisposable
{
    /// <summary>
    /// Pixel format specification string describing channel order and data type.
    /// Format: <CHANNELS>:<TYPE>
    /// Channels are from the set [RGBA] in any order (e.g., "RGBA", "BGRA", "rgb").
    /// Types are int8, float32, or float64.
    /// Examples: "rgba:int8", "bgra:int8", "RGBA:float32", "rgb:float64"
    /// </summary>
    public utf8_string_struct pix_format;

    /// <summary>
    /// Pointer to the raw pixel data buffer in memory.
    /// The buffer contains pixel values in the format specified by pix_format.
    /// </summary>
    public IntPtr pix_data;

    /// <summary>
    /// The size of the pixel data buffer in bytes.
    /// This should equal: width * height * channels * bytes_per_channel
    /// </summary>
    public IntPtr pix_data_length;

    /// <summary>
    /// The width of the image in pixels.
    /// </summary>
    public int width;

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    public int height;

    /// <summary>
    /// Delegate for releasing pixel data memory.
    /// Called by the native library when the pixel data is no longer needed.
    /// </summary>
    /// <param name="pixdata">Reference to the PixData structure being freed.</param>
    /// <returns>True if the memory was successfully released, false otherwise.</returns>
    public delegate bool Pix_data_free(IntPtr pixdata);

    /// <summary>
    /// Function pointer for the cleanup callback.
    /// This callback is invoked by the native library to release the pixel data buffer when it is no longer needed.
    /// </summary>
    public Pix_data_free? pix_data_free;

    /// <summary>
    /// Frees the pixel data buffer if a cleanup callback was provided and resets the structure.
    /// </summary>
    public void Dispose()
    {
        if (pix_data != IntPtr.Zero)
        {
            if (pix_data_free != null)
            {
                pix_data_free(pix_data);
                pix_data_free = null;
            }
            
            pix_data = IntPtr.Zero;
        }

        pix_format.Dispose();
    }
    
    /// <summary>
    /// Implicitly converts PixData to a boolean, returning true if the pixel data pointer is not null.
    /// </summary>
    /// <param name="pixdata">The PixData to check.</param>
    public static implicit operator bool(PixData pixdata)
    {
        return pixdata.pix_data != IntPtr.Zero;
    }
}
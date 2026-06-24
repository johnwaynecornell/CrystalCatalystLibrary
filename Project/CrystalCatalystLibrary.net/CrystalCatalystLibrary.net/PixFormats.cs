using System;

namespace CrystalCatalystLibrary.net;

/// <summary>
/// Contains detailed information about a pixel format.
/// </summary>
/// <param name="PixFormat">The normalized format string (e.g., "rgba:int8").</param>
/// <param name="Channels">The channel sequence (e.g., "rgba").</param>
/// <param name="ElementType">The data type of each channel (e.g., "int8").</param>
/// <param name="ChannelCount">Number of channels.</param>
/// <param name="BytesPerChannel">Bytes used by a single channel.</param>
/// <param name="BytesPerPixel">Total bytes per pixel.</param>
public readonly record struct PixFormatInfo(
    string PixFormat,
    string Channels,
    string ElementType,
    int ChannelCount,
    int BytesPerChannel,
    int BytesPerPixel);

/// <summary>
/// Utility class for parsing and querying pixel format strings.
/// </summary>
public static class PixFormats
{
    /// <summary>
    /// Parses a pixel format string. Throws <see cref="ArgumentException"/> if invalid.
    /// </summary>
    /// <param name="pixFormat">The pixel format string to parse (e.g., "rgba:int8").</param>
    /// <returns>A <see cref="PixFormatInfo"/> object containing the parsed details.</returns>
    public static PixFormatInfo Parse(string pixFormat)
    {
        if (TryParse(pixFormat, out var info))
        {
            return info;
        }
        throw new ArgumentException($"Invalid pixel format: {pixFormat}");
    }

    /// <summary>
    /// Attempts to parse a pixel format string.
    /// </summary>
    /// <param name="pixFormat">The pixel format string to parse.</param>
    /// <param name="info">When this method returns, contains the parsed info if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string pixFormat, out PixFormatInfo info)
    {
        info = default;
        if (string.IsNullOrWhiteSpace(pixFormat)) return false;

        string normalized = pixFormat.ToLowerInvariant();
        int colonIndex = normalized.IndexOf(':');
        if (colonIndex <= 0 || colonIndex == normalized.Length - 1) return false;

        string channels = normalized.Substring(0, colonIndex);
        string type = normalized.Substring(colonIndex + 1);

        foreach (char c in channels)
        {
            if (c != 'r' && c != 'g' && c != 'b' && c != 'a') return false;
        }

        int channelCount = channels.Length;
        int bytesPerChannel = 0;

        switch (type)
        {
            case "int8":
                bytesPerChannel = 1;
                break;
            case "int16":
                bytesPerChannel = 2;
                break;
            /*
            case "float16":
                bytesPerChannel = 2;
                break;*/
            case "float32":
                bytesPerChannel = 4;
                break;
            case "float64":
                bytesPerChannel = 8;
                break;
            default:
                return false;
        }

        info = new PixFormatInfo(
            normalized,
            channels,
            type,
            channelCount,
            bytesPerChannel,
            channelCount * bytesPerChannel);

        return true;
    }

    /// <summary>
    /// Gets the number of channels in the specified format.
    /// </summary>
    public static int GetChannelCount(string pixFormat) => TryParse(pixFormat, out var info) ? info.ChannelCount : 0;

    /// <summary>
    /// Gets the total bytes per pixel for the specified format.
    /// </summary>
    public static int GetBytesPerPixel(string pixFormat) => TryParse(pixFormat, out var info) ? info.BytesPerPixel : 0;

    /// <summary>
    /// Calculates the expected buffer size in bytes for the given dimensions and format.
    /// </summary>
    public static int GetExpectedByteLength(string pixFormat, int width, int height) => GetBytesPerPixel(pixFormat) * width * height;

    /// <summary>
    /// Checks if the format uses 8-bit integer elements.
    /// </summary>
    public static bool IsByteFormat(string pixFormat) => TryParse(pixFormat, out var info) && info.ElementType == "int8";

    /// <summary>
    /// Checks if the format uses floating-point elements.
    /// </summary>
    public static bool IsFloatFormat(string pixFormat) => TryParse(pixFormat, out var info) && (/*info.ElementType == "float16" || */ info.ElementType == "float32" || info.ElementType == "float64");
    
    /// <summary>
    /// Checks if the format uses intger elements.
    /// </summary>
    public static bool IsIntegerFormat(string pixFormat) => !IsFloatFormat(pixFormat);
}

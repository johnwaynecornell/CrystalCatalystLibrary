using System;

namespace CrystalCatalystLibrary.net;

public readonly record struct PixFormatInfo(
    string PixFormat,
    string Channels,
    string ElementType,
    int ChannelCount,
    int BytesPerChannel,
    int BytesPerPixel);

public static class PixFormats
{
    public static PixFormatInfo Parse(string pixFormat)
    {
        if (TryParse(pixFormat, out var info))
        {
            return info;
        }
        throw new ArgumentException($"Invalid pixel format: {pixFormat}");
    }

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

    public static int GetChannelCount(string pixFormat) => TryParse(pixFormat, out var info) ? info.ChannelCount : 0;
    public static int GetBytesPerPixel(string pixFormat) => TryParse(pixFormat, out var info) ? info.BytesPerPixel : 0;
    public static int GetExpectedByteLength(string pixFormat, int width, int height) => GetBytesPerPixel(pixFormat) * width * height;

    public static bool IsByteFormat(string pixFormat) => TryParse(pixFormat, out var info) && info.ElementType == "int8";
    public static bool IsFloatFormat(string pixFormat) => TryParse(pixFormat, out var info) && (info.ElementType == "float32" || info.ElementType == "float64");
}

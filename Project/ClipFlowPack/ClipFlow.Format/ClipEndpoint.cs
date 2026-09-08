using System.Diagnostics;
using FluentCommandLine;
using SkiaSharp;

namespace ClipFlow.Format;

[KV_FA(FluentAttribute.Help, "a copy/paste endpoint")]
public abstract class ClipEndpoint
{
    [FluentMethod]
    public static ClipEndpoint file(string path)
    {
        return new File(path);
    }

    [FluentMethod]
    public static ClipEndpoint console()
    {
        return new Console();
    }

    [FluentMethod]
    public static ClipEndpoint directory(string path)
    {
        return new Directory(path);
    }

    [FluentMethod("string")]
    public static ClipEndpoint stringVal(string value)
    {
        return new String(value);
    }

    public abstract void Write(ClipContext context, ClipType type);
    public abstract void Read(ClipContext context, ClipType type);

    public class File : ClipEndpoint
    {
        public File(string path)
        {
            this.path = path;
        }

        public string path;

        public override void Write(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    if (text.Identity != null)
                        System.IO.File.WriteAllText(path, text.Identity);
                    break;

                case ClipType.Html html:
                    if (html.Identity != null)
                        System.IO.File.WriteAllText(path, html.Identity);
                    break;

                case ClipType.Files files:
                    if (files.Identity != null)
                    {
                        System.IO.File.WriteAllLines(path, files.Identity);
                    }

                    break;

                case ClipType.Image image:
                    WriteImage(context, image, path);
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }

        public override void Read(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    text.Identity = System.IO.File.ReadAllText(path);
                    break;

                case ClipType.Html html:
                    html.Identity = System.IO.File.ReadAllText(path);
                    break;

                case ClipType.Files files:
                    if (!System.IO.File.Exists(path))
                    {
                        context.ErrorOutput.WriteLine($"File not found: {path}");
                        context.Status = 1;
                        return;
                    }

                    string[] rawLines = System.IO.File.ReadAllLines(path);
                    List<string> normalizedPaths = new();
                    foreach (string line in rawLines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string fullPath = Path.GetFullPath(line);
                        if (!System.IO.File.Exists(fullPath) && !System.IO.Directory.Exists(fullPath))
                        {
                            context.ErrorOutput.WriteLine($"Path not found: {line}");
                            context.Status = 1;
                            return;
                        }
                        normalizedPaths.Add(fullPath);
                    }
                    files.Identity = normalizedPaths;
                    break;

                case ClipType.Image image:
                    ReadImage(context, image, path);
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }

        private static SKEncodedImageFormat GetImageFormatFromPath(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                ".png" => SKEncodedImageFormat.Png,
                ".webp" => SKEncodedImageFormat.Webp,
                ".bmp" => SKEncodedImageFormat.Bmp,
                ".gif" => SKEncodedImageFormat.Gif,
                ".ico" => SKEncodedImageFormat.Ico,
                ".wbmp" => SKEncodedImageFormat.Wbmp,
                ".pkm" => SKEncodedImageFormat.Pkm,
                ".ktx" => SKEncodedImageFormat.Ktx,
                ".astc" => SKEncodedImageFormat.Astc,
                ".dng" => SKEncodedImageFormat.Dng,
                ".heif" => SKEncodedImageFormat.Heif,
                _ => SKEncodedImageFormat.Png
            };
        }

        private static void WriteImage(
            ClipContext context,
            ClipType.Image image, string path)
        {
            if (image.Identity == null)
            {
                context.ErrorOutput.WriteLine("Image data is not available.");
                context.Status = 1;
                return;
            }

            SKEncodedImageFormat format = GetImageFormatFromPath(path);

            byte[]? dataBytes = null;
            if (format == SKEncodedImageFormat.Bmp)
            {
                dataBytes = BmpEncoder.EncodeToBmp(image.Identity);
            }
            else
            {
                using SKData? data = image.Identity.Encode(format, 100);
                if (data != null)
                {
                    dataBytes = data.ToArray();
                }
            }

            if (dataBytes == null)
            {
                context.ErrorOutput.WriteLine($"Failed to encode image for: {path}");
                context.Status = 1;
                return;
            }

            System.IO.File.WriteAllBytes(path, dataBytes);
        }

        private static void ReadImage(
            ClipContext context,
            ClipType.Image image, string path)
        {
            if (!System.IO.File.Exists(path))
            {
                context.ErrorOutput.WriteLine($"Image file not found: {path}");
                context.Status = 1;
                return;
            }

            image.Identity = SKImage.FromEncodedData(path);

            if (image.Identity == null)
            {
                context.ErrorOutput.WriteLine("Failed to load image from file.");
                context.Status = 1;
            }
        }
    }

    public class String : ClipEndpoint
    {
        public String(string value)
        {
            this.value = value;
        }

        public string value;

        public override void Write(ClipContext context, ClipType type)
        {
            context.ErrorOutput.WriteLine("String Endpoint is input only");
            context.Status = 1;
            return;
        }
        
        public override void Read(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    text.Identity = value;
                    break;

                case ClipType.Html html:
                    html.Identity = value;
                    break;

                case ClipType.Files files:
                    context.ErrorOutput.WriteLine("String endpoint does not support Files");
                    context.Status = 1;
                    return;

                case ClipType.Image image:
                    context.ErrorOutput.WriteLine("String endpoint does not support image");
                    context.Status = 1;
                    return;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }
    }

    public class Console : ClipEndpoint
    {
        public Console()
        {

        }

        public override void Write(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    if (text.Identity != null)
                        context.Output.Write(text.Identity);
                    break;

                case ClipType.Html html:
                    if (html.Identity != null)
                        context.Output.Write(html.Identity);
                    break;

                case ClipType.Files files:
                    if (files.Identity != null)
                    {
                        foreach (string file in files.Identity)
                            context.Output.WriteLine(file);
                    }

                    break;

                case ClipType.Image image:
                    WriteImage(context, image);
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }
        
        public override void Read(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    text.Identity = context.Input.ReadToEnd();
                    break;

                case ClipType.Html html:
                    html.Identity = context.Input.ReadToEnd();
                    break;

                case ClipType.Files files:
                    List<string> lines = new();
                    string? line;
                    while ((line = context.Input.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        lines.Add(line);
                    }

                    List<string> normalizedPaths = new();
                    foreach (string raw in lines)
                    {
                        string fullPath = Path.GetFullPath(raw);
                        if (!System.IO.File.Exists(fullPath) && !System.IO.Directory.Exists(fullPath))
                        {
                            context.ErrorOutput.WriteLine($"Path not found: {raw}");
                            context.Status = 1;
                            return;
                        }
                        normalizedPaths.Add(fullPath);
                    }
                    files.Identity = normalizedPaths;
                    break;

                case ClipType.Image image:
                    context.ErrorOutput.WriteLine("Image is not compatible with Console.");
                    context.Status = 1;
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }

        private static void WriteImage(
            ClipContext context,
            ClipType.Image image)
        {
            if (image.Identity == null)
            {
                context.ErrorOutput.WriteLine("Image data is not available.");
                context.Status = 1;
                return;
            }

            string path = Path.Combine(
                Path.GetTempPath(),
                $"clipflow-{Guid.NewGuid():N}.png");

            using (SKData data =
                   image.Identity.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream stream = System.IO.File.Create(path))
            {
                data.SaveTo(stream);
            }

            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            Thread.Sleep(1000);
        }
    }

    public class Directory : ClipEndpoint
    {
        public Directory(string path)
        {
            this.path = path;
        }

        public string path;

        public void CopyHelper(string src, string dst)
        {
            if (System.IO.Directory.Exists(src))
            {
                if (System.IO.File.Exists(dst))
                    System.IO.File.Delete(dst);

                if (!System.IO.Directory.Exists(dst))
                    System.IO.Directory.CreateDirectory(dst);

                foreach (string directory in System.IO.Directory.GetDirectories(src))
                {
                    string newDst = Path.Combine(
                        dst,
                        Path.GetFileName(directory));

                    CopyHelper(directory, newDst);
                }

                foreach (string file in System.IO.Directory.GetFiles(src))
                {
                    string newDst = Path.Combine(
                        dst,
                        Path.GetFileName(file));

                    CopyHelper(file, newDst);
                }
            }
            else
            {
                if (System.IO.Directory.Exists(dst))
                    System.IO.Directory.Delete(dst, recursive: true);

                System.IO.File.Copy(src, dst, overwrite: true);
            }
        }
        
        
        public override void Write(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    context.ErrorOutput.WriteLine("text is not writable at a directory endpoint.");
                    context.Status = 1;
                    break;

                case ClipType.Html html:
                    context.ErrorOutput.WriteLine("html is not writable at a directory endpoint.");
                    context.Status = 1;
                    break;

                case ClipType.Files files:
                    if (files.Identity != null)
                    {
                        System.IO.Directory.CreateDirectory(path);
                        foreach (string file in files.Identity)
                        {
                            string fileName = Path.GetFileName(file);
                            string destPath = Path.Combine(path, fileName);

                            try
                            {
                                CopyHelper(file, destPath);
                            }
                            catch (Exception ex)
                            {
                                context.ErrorOutput.WriteLine($"Error copying {file}: {ex.Message}");
                                context.Status = 1;
                                return;
                            }
                        }
                    }

                    break;

                case ClipType.Image image:
                    context.ErrorOutput.WriteLine("image is not writable at a directory endpoint.");
                    context.Status = 1;
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }
        
        public override void Read(ClipContext context, ClipType type)
        {
            switch (type)
            {
                case ClipType.Text text:
                    context.ErrorOutput.WriteLine("text is not writable at a directory endpoint.");
                    context.Status = 1;
                    break;

                case ClipType.Html html:
                    context.ErrorOutput.WriteLine("html is not writable at a directory endpoint.");
                    context.Status = 1;
                    break;

                case ClipType.Files files:
                    string searchPattern = "*";
                    string directoryPath = path;

                    // Check if path contains wildcard pattern
                    string fileName = Path.GetFileName(path);
                    if (fileName.Contains('*') || fileName.Contains('?'))
                    {
                        searchPattern = fileName;
                        directoryPath = Path.GetDirectoryName(path) ?? path;
                        if (string.IsNullOrEmpty(directoryPath))
                            directoryPath = ".";
                    }

                    if (System.IO.Directory.Exists(directoryPath))
                    {
                        string[] rawFiles = System.IO.Directory.GetFiles(directoryPath, searchPattern);
                        List<string> normalized = new(rawFiles.Length);
                        foreach (string f in rawFiles)
                        {
                            normalized.Add(Path.GetFullPath(f));
                        }
                        files.Identity = normalized;
                    }
                    else
                    {
                        context.ErrorOutput.WriteLine($"Directory not found: {directoryPath}");
                        context.Status = 1;
                    }

                    break;

                case ClipType.Image image:
                    context.ErrorOutput.WriteLine("image is not readable at a directory endpoint.");
                    context.Status = 1;
                    break;

                default:
                    context.ErrorOutput.WriteLine(
                        $"{GetType().Name} endpoint does not support {type.GetType().Name}");
                    context.Status = 1;
                    break;
            }
        }
    }
}
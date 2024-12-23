using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.IO;

namespace MolenApplicatie.Server.Utils
{
    public static class GetDateTakenOfImage
    {
        public static DateTime? GetDateTaken(string path)
        {
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

                var directories = ImageMetadataReader.ReadMetadata(path);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (subIfdDirectory != null)
                {
                    var dateTaken = subIfdDirectory.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
                    if (dateTaken != null)
                    {
                        return dateTaken;
                    }
                }

                // If no EXIF data found, fallback to file's last write time
                return new FileInfo(path).LastWriteTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image '{path}': {ex.Message}");
            }

            return null;
        }
    }
}

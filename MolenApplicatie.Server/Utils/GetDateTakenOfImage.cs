using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MolenApplicatie.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                return new FileInfo(path).CreationTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image '{path}': {ex.Message}");
            }

            return null;
        }

        public async static Task<IFormFile> SaveMolenImage(string FileDirectory, IFormFile file)
        {
            string? dateTaken = null;
            using (var inputStream = file.OpenReadStream())
            {
                var directories = ImageMetadataReader.ReadMetadata(inputStream);
                var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exifSubIfdDirectory != null)
                {
                    dateTaken = exifSubIfdDirectory.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                }
            }

            using var image = await Image.LoadAsync(file.OpenReadStream());
            if (dateTaken != null)
            {
                image.Metadata.ExifProfile = new SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifProfile();
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.DateTimeOriginal, dateTaken);
            }

            await image.SaveAsync(FileDirectory);

            return file;
        }
    }
}

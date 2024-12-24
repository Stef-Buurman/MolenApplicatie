using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SixLabors.ImageSharp;

namespace MolenApplicatie.Server.Utils
{
    public static class GetDateTakenOfImage
    {
        public static DateTime? GetDateTaken(string path)
        {
            try
            {
                if (!File.Exists(path) && !File.Exists(CreateCleanPath.CreatePathToWWWROOT(path)))
                {
                    throw new FileNotFoundException("File not found.", path);
                }
                else if(File.Exists(CreateCleanPath.CreatePathToWWWROOT(path)))
                {
                    path = CreateCleanPath.CreatePathToWWWROOT(path);
                }

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

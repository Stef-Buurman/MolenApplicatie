using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

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

                return new FileInfo(path).CreationTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image '{path}': {ex.Message}");
            }

            return null;
        }
    }
}

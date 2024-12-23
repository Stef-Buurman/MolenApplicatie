using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Text;
using System.Drawing;


namespace MolenApplicatie.Server.Utils
{
    public static class GetDateTakenOfImage
    {
        private static readonly Regex dateRegex = new Regex(":");

        public static DateTime? GetDateTaken(string path)
        {
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    PropertyItem propItem = null;
                    try
                    {
                        propItem = myImage.GetPropertyItem(36867);
                    }
                    catch { }
                    if (propItem != null)
                    {
                        string dateTaken = dateRegex.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                        return DateTime.Parse(dateTaken);
                    }
                    else
                        return new FileInfo(path).LastWriteTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image '{path}': {ex.Message}");
            }

            return null;
        }
    }
}

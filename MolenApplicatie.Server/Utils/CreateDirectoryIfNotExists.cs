namespace MolenApplicatie.Server.Utils
{
    public class CreateDirectoryIfNotExists
    {
        private static string? GetTBNOfPath(string path)
        {
            string? secondPart = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
            if (secondPart == null)
            {
                return path;
            }
            return secondPart;
        }

        public static string? GetMolenImagesFolder(string path)
        {
            string? tbn = GetTBNOfPath(path);
            if (tbn == null) return null;
            return Globals.MolenImagesFolder + "/" + tbn;
        }

        public static string? GetAddedMolenImagesFolder(string path)
        {
            string? tbn = GetTBNOfPath(path);
            if (tbn == null) return null;
            return Globals.MolenAddedImagesFolder + "/" + tbn;
        }

        public static void CreateMolenImagesFolderDirectory(string Ten_Brugge_Nr)
        {
            if (!Directory.Exists(Globals.MolenImagesFolder))
            {
                Directory.CreateDirectory(Globals.MolenImagesFolder);
            }
            if (!Directory.Exists(Globals.MolenImagesFolder + "/" + Ten_Brugge_Nr))
            {
                Directory.CreateDirectory(Globals.MolenImagesFolder + "/" + Ten_Brugge_Nr);
            }
        }

        public static void CreateMolenAddedImagesFolderDirectory(string Ten_Brugge_Nr)
        {
            if (!Directory.Exists(Globals.MolenAddedImagesFolder))
            {
                Directory.CreateDirectory(Globals.MolenAddedImagesFolder);
            }
            if (!Directory.Exists(Globals.MolenAddedImagesFolder + "/" + Ten_Brugge_Nr))
            {
                Directory.CreateDirectory(Globals.MolenAddedImagesFolder + "/" + Ten_Brugge_Nr);
            }
        }
    }
}

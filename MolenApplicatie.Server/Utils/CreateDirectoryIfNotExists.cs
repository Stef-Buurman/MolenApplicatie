namespace MolenApplicatie.Server.Utils
{
    public class CreateDirectoryIfNotExists
    {
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

namespace MolenApplicatie.Server.Utils
{
    public static class Globals
    {
        public static readonly string DBBestaandeMolens = "Database/BestaandeMolens.db";
        public static readonly string MolenImagesFolder = "wwwroot/MolenImages";
        public static readonly string MolenAddedImagesFolder = "wwwroot/MolenAddedImages";
        public static readonly List<string> AllowedMolenTypes = new List<string>
        {
            "beltmolen",
            "grondzeiler",
            "paltrokmolen",
            "spinnenkop",
            "standerdmolen",
            "stellingmolen",
            "weidemolen",
            "kleine molen"
        };
    }
}

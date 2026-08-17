namespace MolenApplicatie.Server.Utils
{
    public static class Globals
    {
        public static readonly string DBAlleMolens = "Database/AlleMolens.db";
        public static readonly string DBBestaandeMolens = "Database/BestaandeMolens.db";
        public static readonly string MolenImagesFolder = "wwwroot/MolenImages";
        public static readonly string MolenAddedImagesFolder = "wwwroot/MolenAddedImages";
        public static readonly string WWWROOTPath = "wwwroot";
        public static readonly int MaxNormalImagesCount = 2;

        public static readonly List<string> AllowedMolenTypes =
        [
            "beltmolen",
            "grondzeiler",
            "paltrokmolen",
            "spinnenkop",
            "standerdmolen",
            "stellingmolen",
            "weidemolen",
            "kleine molen",
            "wipmolen",
            "torenmolen",
            "windmolen",
            "watermolen",
            "ronde molen",
            "kantige molen"
        ];

        public static readonly List<string> MillDatabaseRemoteMolenTypes =
        [
            "hollow post mill",
            "smock mill",
            "tower mill",
            "combined wind and watermill",
            "composite mill",
            "inverted windmill",
            "paltrok mill",
            "post mill"
        ];

        public static readonly List<string> AllowedMillDatabaseSourceTypes =
        [
            .. MillDatabaseRemoteMolenTypes,
            "kantige molen",
            "holländermühle",
            "hollandermuhle"
        ];
    }
}

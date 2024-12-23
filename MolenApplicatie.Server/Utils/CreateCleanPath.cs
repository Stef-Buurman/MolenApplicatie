namespace MolenApplicatie.Server.Utils
{
    public class CreateCleanPath
    {
        public static string CreatePathToWWWROOT(string path)
        {
            path = CreatePathWithoutWWWROOT(path);
            if (path.StartsWith("/"))
            {
                return Globals.WWWROOTPath + path;
            }
            return Globals.WWWROOTPath + "/" + path;
        }

        public static string CreatePath(string path)
        {
            return path.Replace("\\", "/");
        }

        public static string CreatePathWithoutWWWROOT(string path)
        {
            path = CreatePath(path);
            path = path.Replace("wwwroot", "");
            path = path.TrimStart('/');
            return "/" + path;
        }
    }
}

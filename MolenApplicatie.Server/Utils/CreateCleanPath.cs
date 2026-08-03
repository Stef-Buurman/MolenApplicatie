namespace MolenApplicatie.Server.Utils
{
    public class CreateCleanPath
    {
        public static string CreatePathToWWWROOT(string path)
        {
            var relativePath = CreatePathWithoutWWWROOT(path)
                .TrimStart('/');

            return Path.Combine(
                Globals.WWWROOTPath,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }

        public static string CreatePath(string path)
        {
            return path.Replace("\\", "/");
        }

        public static string CreatePathWithoutWWWROOT(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            var normalizedPath = CreatePath(path).Trim();

            const string webRootMarker = "/wwwroot/";

            var webRootIndex = normalizedPath.IndexOf(
                webRootMarker,
                StringComparison.OrdinalIgnoreCase);

            if (webRootIndex >= 0)
            {
                normalizedPath = normalizedPath[
                    (webRootIndex + webRootMarker.Length)..];
            }
            else if (normalizedPath.StartsWith(
                         "wwwroot/",
                         StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath =
                    normalizedPath["wwwroot/".Length..];
            }
            else if (normalizedPath.Equals(
                         "wwwroot",
                         StringComparison.OrdinalIgnoreCase))
            {
                return "/";
            }

            return "/" + normalizedPath.TrimStart('/');
        }
    }
}
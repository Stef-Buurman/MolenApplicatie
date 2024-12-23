namespace MolenApplicatie.Server.Utils
{
    public class GetFileNameForImage
    {
        public static string GetFileName()
        {
            return Guid.NewGuid().ToString();
        }
    }
}

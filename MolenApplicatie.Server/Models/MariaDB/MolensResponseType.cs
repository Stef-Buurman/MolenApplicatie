namespace MolenApplicatie.Server.Models.MariaDB
{
    public class MolensResponseType<T>
    {
        public int ActiveMolensWithImage { get; set; }
        public int RemainderMolensWithImage { get; set; }
        public int TotalMolensWithImage { get; set; }
        public int TotalCountActiveMolens { get; set; }
        public int TotalCountRemainderMolens { get; set; }
        public List<CountDisappearedMolens> TotalCountDisappearedMolens { get; set; }
        public int TotalCountExistingMolens { get; set; }
        public int TotalCountMolens { get; set; }
        public List<T> Molens { get; set; }
        public List<RecentAddedImages>? RecentAddedImages { get; set; }
    }

    public class CountDisappearedMolens
    {
        public string Provincie { get; set; }
        public int Count { get; set; }
    }

    public class RecentAddedImages
    {
        public MolenData molen { get; set; }
        public List<AddedImage> Images { get; set; }
    }
}

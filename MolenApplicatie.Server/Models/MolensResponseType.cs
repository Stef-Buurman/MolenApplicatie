namespace MolenApplicatie.Server.Models
{
    public class MolensResponseType
    {
        public int ActiveMolensWithImage { get; set; }
        public int RemainderMolensWithImage { get; set; }
        public int TotalMolensWithImage { get; set; }
        public int TotalCountActiveMolens { get; set; }
        public int TotalCountRemainderMolens { get; set; }
        public List<CountDisappearedMolens> TotalCountDisappearedMolens { get; set; }
        public int TotalCountExistingMolens { get; set; }
        public int TotalCountMolens { get; set; }
        public List<MolenData> Molens { get; set; }
    }

    public class CountDisappearedMolens
    {
        public string Provincie { get; set; }
        public int Count { get; set; }
    }
}

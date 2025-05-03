namespace MolenApplicatie.Server.Models
{
    public class MolensResponseTypeOld
    {
        public int ActiveMolensWithImage { get; set; }
        public int RemainderMolensWithImage { get; set; }
        public int TotalMolensWithImage { get; set; }
        public int TotalCountActiveMolens { get; set; }
        public int TotalCountRemainderMolens { get; set; }
        public List<CountDisappearedMolensOld> TotalCountDisappearedMolens { get; set; }
        public int TotalCountExistingMolens { get; set; }
        public int TotalCountMolens { get; set; }
        public List<MolenDataOld> Molens { get; set; }
    }

    public class CountDisappearedMolensOld
    {
        public string Provincie { get; set; }
        public int Count { get; set; }
    }
}

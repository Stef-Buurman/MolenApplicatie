using SQLite;
using MolenApplicatie.Models;

namespace MolenApplicatie.Server.Models
{
    public class MolenData2
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Bouwjaar { get; set; }
        public string? Herbouwd_jaar { get; set; }
        public int? Bouwjaar_start { get; set; }
        public int? Bouwjaar_einde { get; set; }
        public string Functie { get; set; }
        public string Toestand { get; set; }
        public string Opvolger { get; set; }
        public string Voorganger { get; set; }
        public string VerplaatstNaar { get; set; }
        public string AfkomstigVan { get; set; }
        public string Krachtbron { get; set; }
        public string Website { get; set; }
        public bool OpenVoorPubliek { get; set; }
        public string Ten_Brugge_Nr { get; set; }
        public string Plaats { get; set; }
        public string Adres { get; set; }
        [Ignore]
        public MolenImage Image { get; set; }
        [Ignore]
        public List<MolenType> ModelType { get; set; } = new List<MolenType>();

        [Ignore]
        public List<MolenImage> AddedImages { get; set; } = new List<MolenImage>();

        [Ignore]
        public List<MolenYearInfo> AddedDisappearedYears { get; set; } = new List<MolenYearInfo>();

        [Ignore]
        public bool HasImage { get; set; }
        public double North { get; set; }
        public double East { get; set; }
        public DateTime LastUpdated { get; set; }

        [Ignore]
        public string GetBouwjaar
        {
            get
            {
                if (Bouwjaar.HasValue)
                {
                    return Bouwjaar.ToString();
                }
                else if (Bouwjaar_start.HasValue && Bouwjaar_einde.HasValue)
                {
                    return $"{Bouwjaar_start} - {Bouwjaar_einde}";
                }
                else if (Bouwjaar_start.HasValue)
                {
                    return Bouwjaar_start.ToString();
                }
                else if (Bouwjaar_einde.HasValue)
                {
                    return Bouwjaar_einde.ToString();
                }
                else
                {
                    return "Onbekend";
                }
            }
        }
    }
}

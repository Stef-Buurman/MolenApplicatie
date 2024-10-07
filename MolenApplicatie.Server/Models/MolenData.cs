using SQLite;
using Reinforced.Typings.Attributes;

namespace MolenApplicatie.Server.Models
{
    [TsInterface]
    public class MolenData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Bouwjaar { get; set; }
        public string? Herbouwd_jaar { get; set; }
        public int? Bouwjaar_start { get; set; }
        public int? Bouwjaar_einde { get; set; }
        public string Functie { get; set; }
        public string Ten_Brugge_Nr { get; set; }
        public string Plaats { get; set; }
        public string Adres { get; set; }
        public byte[] Image { get; set; }
        [Ignore]
        public List<MolenType> ModelType { get; set; } = new List<MolenType>();
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

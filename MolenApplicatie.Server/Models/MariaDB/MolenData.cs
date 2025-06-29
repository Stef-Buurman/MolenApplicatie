using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_data")]
    public class MolenData : DefaultModel, IEquatable<MolenData>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public virtual MolenTBN MolenTBN { get; set; }
        public required string Ten_Brugge_Nr { get; set; }
        public required string Name { get; set; }
        public string? ToelichtingNaam { get; set; }
        public int? Bouwjaar { get; set; }
        public string? HerbouwdJaar { get; set; }
        public int? BouwjaarStart { get; set; }
        public int? BouwjaarEinde { get; set; }
        public string? Functie { get; set; }
        public string? Doel { get; set; }
        public string? Toestand { get; set; }
        public string? Bedrijfsvaardigheid { get; set; }
        public string? Plaats { get; set; }
        public string? Adres { get; set; }
        public string? Provincie { get; set; }
        public string? Gemeente { get; set; }
        public string? Streek { get; set; }
        public string? Plaatsaanduiding { get; set; }
        public string? Opvolger { get; set; }
        public string? Voorganger { get; set; }
        public string? VerplaatstNaar { get; set; }
        public string? AfkomstigVan { get; set; }
        public string? Literatuur { get; set; }
        public string? PlaatsenVoorheen { get; set; }
        public string? Wiekvorm { get; set; }
        public string? WiekVerbeteringen { get; set; }
        public string? Monument { get; set; }
        public string? PlaatsBediening { get; set; }
        public string? BedieningKruiwerk { get; set; }
        public string? PlaatsKruiwerk { get; set; }
        public string? Kruiwerk { get; set; }
        public string? Vlucht { get; set; }
        public string? Openingstijden { get; set; }
        public bool OpenVoorPubliek { get; set; }
        public bool OpenOpZaterdag { get; set; }
        public bool OpenOpZondag { get; set; }
        public bool OpenOpAfspraak { get; set; }
        public string? Krachtbron { get; set; }
        public string? Website { get; set; }
        public string? WinkelInformatie { get; set; }
        public string? Bouwbestek { get; set; }
        public string? Bijzonderheden { get; set; }
        public string? Museuminformatie { get; set; }
        public string? Molenaar { get; set; }
        public string? Eigendomshistorie { get; set; }
        public string? Molenerf { get; set; }
        public string? Trivia { get; set; }
        public string? Geschiedenis { get; set; }
        public string? Wetenswaardigheden { get; set; }
        public string? Wederopbouw { get; set; }
        public string? As { get; set; }
        public string? Wieken { get; set; }
        public string? Toegangsprijzen { get; set; }
        public string? UniekeEigenschap { get; set; }
        public string? LandschappelijkeWaarde { get; set; }
        public string? KadastraleAanduiding { get; set; }
        public bool CanAddImages { get; set; }
        public virtual List<MolenImage> Images { get; set; } = null!;
        public virtual List<MolenTypeAssociation> MolenTypeAssociations { get; set; } = null!;
        public virtual List<AddedImage> AddedImages { get; set; } = null!;
        public virtual List<MolenMaker> MolenMakers { get; set; } = null!;
        public virtual List<DisappearedYearInfo> DisappearedYearInfos { get; set; } = null!;
        [NotMapped]
        public bool HasImage { get; set; }
        public string? Eigenaar { get; set; }
        public string? RecenteWerkzaamheden { get; set; }
        public string? Rad { get; set; }
        public string? RadDiameter { get; set; }
        public string? Wateras { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }

        public bool Equals(MolenData? other)
        {
            if (other == null) return false;
            return Ten_Brugge_Nr == other.Ten_Brugge_Nr;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MolenData other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ten_Brugge_Nr);
        }
    }
}

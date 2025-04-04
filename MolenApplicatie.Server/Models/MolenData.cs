﻿using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Ten_Brugge_Nr { get; set; }
        public string Name { get; set; }
        public string ToelichtingNaam { get; set; }
        public int? Bouwjaar { get; set; }
        public string? HerbouwdJaar { get; set; }
        public int? BouwjaarStart { get; set; }
        public int? BouwjaarEinde { get; set; }
        public string Functie { get; set; }
        public string Doel { get; set; }
        public string Toestand { get; set; }
        public string Bedrijfsvaardigheid { get; set; }
        public string Plaats { get; set; }
        public string Adres { get; set; }
        public string Provincie { get; set; }
        public string Gemeente { get; set; }
        public string Streek { get; set; }
        public string Plaatsaanduiding { get; set; }
        public string Opvolger { get; set; }
        public string Voorganger { get; set; }
        public string VerplaatstNaar { get; set; }
        public string AfkomstigVan { get; set; }
        public string Literatuur { get; set; }
        public string PlaatsenVoorheen { get; set; }
        public string Wiekvorm { get; set; }
        public string WiekVerbeteringen { get; set; }
        public string Monument { get; set; }
        public string PlaatsBediening { get; set; }
        public string BedieningKruiwerk { get; set; }
        public string PlaatsKruiwerk { get; set; }
        public string Kruiwerk { get; set; }
        public string Vlucht { get; set; }
        public string Openingstijden { get; set; }
        public bool OpenVoorPubliek { get; set; }
        public bool OpenOpZaterdag { get; set; }
        public bool OpenOpZondag { get; set; }
        public bool OpenOpAfspraak { get; set; }
        public string Krachtbron { get; set; }
        public string Website { get; set; }
        public string WinkelInformatie { get; set; }
        public string Bouwbestek { get; set; }
        public string Bijzonderheden { get; set; }
        public string Museuminformatie { get; set; }
        public string Molenaar { get; set; }
        public string Eigendomshistorie { get; set; }
        public string Molenerf { get; set; }
        public string Trivia { get; set; }
        public string Geschiedenis { get; set; }
        public string Wetenswaardigheden { get; set; }
        public string Wederopbouw { get; set; }
        public string As { get; set; }
        public string Wieken { get; set; }
        public string Toegangsprijzen { get; set; }
        public string UniekeEigenschap { get; set; }
        public string LandschappelijkeWaarde { get; set; }
        public string KadastraleAanduiding { get; set; }
        public bool CanAddImages { get; set; }

        [Ignore]
        public List<MolenImage> Images { get; set; }
        [Ignore]
        public List<MolenType> ModelType { get; set; } = new List<MolenType>();

        [Ignore]
        public List<AddedImage> AddedImages { get; set; } = new List<AddedImage>();

        [Ignore]
        public List<VerdwenenYearInfo> DisappearedYears { get; set; } = new List<VerdwenenYearInfo>();

        [Ignore]
        public List<MolenMaker> MolenMakers { get; set; } = new List<MolenMaker>();

        [Ignore]
        public bool HasImage { get; set; }
        public string Eigenaar { get; set; }
        public string RecenteWerkzaamheden { get; set; }
        public string Rad { get; set; }
        public string RadDiameter { get; set; }
        public string Wateras { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}

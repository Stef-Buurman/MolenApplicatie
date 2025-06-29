using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Services.Database;
using MolenApplicatie.Server.Utils;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MolenApplicatie.Server.Services
{
    public class NewMolenDataService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        readonly string baseUrl = "https://www.molendatabase.nl/molens/ten-bruggencate-nr-";
        private readonly HttpClient _client;
        private readonly MolenService _molenService;
        private readonly PlacesService _placesService;
        private List<Dictionary<string, string>> strings = new List<Dictionary<string, string>>();
        private List<string> allverdwenenMolens = new List<string>();
        private readonly MolenDbContext _dbContext;
        private readonly DBMolenDataService _dBMolenDataService;
        private readonly DBMolenTypeService _dBMolenTypeService;

        public NewMolenDataService(MolenService molenService, PlacesService placesService, MolenDbContext dbContext, DBMolenDataService dBMolenDataService, DBMolenTypeService dBMolenTypeService)
        {
            _client = new HttpClient();
            _molenService = molenService;
            _placesService = placesService;
            _dbContext = dbContext;
            _dBMolenDataService = dBMolenDataService;
            _dBMolenTypeService = dBMolenTypeService;
        }

        public async Task<List<MolenData>?> AddMolensToMariaDb(List<MolenData> molens)
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            if (molens == null || molens.Count == 0) return null;

            await _dbContext.Database.EnsureCreatedAsync();

            foreach (var molen in molens)
            {
                if (molen == null) continue;

                var existingMolen = await _dbContext.MolenData
                    .FirstOrDefaultAsync(m => m.Ten_Brugge_Nr == molen.Ten_Brugge_Nr);

                var existingTBN = await _dbContext.MolenTBNs
                    .FirstOrDefaultAsync(m => m.Ten_Brugge_Nr == molen.Ten_Brugge_Nr);

                if (existingMolen != null && existingTBN != null) continue;

                var molenData = existingMolen ?? new MolenData
                {
                    Ten_Brugge_Nr = molen.Ten_Brugge_Nr,
                    Name = molen.Name,
                    ToelichtingNaam = molen.ToelichtingNaam,
                    Bouwjaar = molen.Bouwjaar,
                    HerbouwdJaar = molen.HerbouwdJaar,
                    BouwjaarStart = molen.BouwjaarStart,
                    BouwjaarEinde = molen.BouwjaarEinde,
                    Functie = molen.Functie,
                    Doel = molen.Doel,
                    Toestand = molen.Toestand,
                    Bedrijfsvaardigheid = molen.Bedrijfsvaardigheid,
                    Plaats = molen.Plaats,
                    Adres = molen.Adres,
                    Provincie = molen.Provincie,
                    Gemeente = molen.Gemeente,
                    Streek = molen.Streek,
                    Plaatsaanduiding = molen.Plaatsaanduiding,
                    Opvolger = molen.Opvolger,
                    Voorganger = molen.Voorganger,
                    VerplaatstNaar = molen.VerplaatstNaar,
                    AfkomstigVan = molen.AfkomstigVan,
                    Literatuur = molen.Literatuur,
                    PlaatsenVoorheen = molen.PlaatsenVoorheen,
                    Wiekvorm = molen.Wiekvorm,
                    WiekVerbeteringen = molen.WiekVerbeteringen,
                    Monument = molen.Monument,
                    PlaatsBediening = molen.PlaatsBediening,
                    BedieningKruiwerk = molen.BedieningKruiwerk,
                    PlaatsKruiwerk = molen.PlaatsKruiwerk,
                    Kruiwerk = molen.Kruiwerk,
                    Vlucht = molen.Vlucht,
                    Openingstijden = molen.Openingstijden,
                    OpenVoorPubliek = molen.OpenVoorPubliek,
                    OpenOpZaterdag = molen.OpenOpZaterdag,
                    OpenOpZondag = molen.OpenOpZondag,
                    OpenOpAfspraak = molen.OpenOpAfspraak,
                    Krachtbron = molen.Krachtbron,
                    Website = molen.Website,
                    WinkelInformatie = molen.WinkelInformatie,
                    Bouwbestek = molen.Bouwbestek,
                    Bijzonderheden = molen.Bijzonderheden,
                    Museuminformatie = molen.Museuminformatie,
                    Molenaar = molen.Molenaar,
                    Eigendomshistorie = molen.Eigendomshistorie,
                    Molenerf = molen.Molenerf,
                    Trivia = molen.Trivia,
                    Geschiedenis = molen.Geschiedenis,
                    Wetenswaardigheden = molen.Wetenswaardigheden,
                    Wederopbouw = molen.Wederopbouw,
                    As = molen.As,
                    Wieken = molen.Wieken,
                    Toegangsprijzen = molen.Toegangsprijzen,
                    UniekeEigenschap = molen.UniekeEigenschap,
                    LandschappelijkeWaarde = molen.LandschappelijkeWaarde,
                    KadastraleAanduiding = molen.KadastraleAanduiding,
                    CanAddImages = molen.CanAddImages,
                    Eigenaar = molen.Eigenaar,
                    RecenteWerkzaamheden = molen.RecenteWerkzaamheden,
                    Rad = molen.Rad,
                    RadDiameter = molen.RadDiameter,
                    Wateras = molen.Wateras,
                    Latitude = molen.Latitude,
                    Longitude = molen.Longitude,
                    LastUpdated = molen.LastUpdated
                };

                await _dBMolenDataService.AddOrUpdate(molenData);
            }
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            return null;
        }

        public async Task<(MolenData, Dictionary<string, object>)> GetMolenDataByTBNumber(string Ten_Brugge_Nr, string? url = null)
        {
            List<DisappearedYearInfo> MolenRemovedYears = new List<DisappearedYearInfo>();
            //List<MolenMaker> molenmakers = await _dbContext.MolenMakers.ToListAsync();
            //List<MolenTypeAssociation> MolenTypeAssociations = await _dbContext.MolenTypeAssociations.ToListAsync();
            //List<DisappearedYearInfo> AllMolenRemovedYears = await _dbContext.DisappearedYearInfos.ToListAsync();
            MolenData? oldMolenData = await _molenService.GetMolenByTBN(Ten_Brugge_Nr);
            try
            {
                HttpResponseMessage response;
                if (url == null)
                {
                    response = await _client.GetAsync(baseUrl + Ten_Brugge_Nr);
                }
                else
                {
                    response = await _client.GetAsync(url);
                }
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);

                var divs = doc.DocumentNode.SelectNodes("//div[contains(@class, 'attrib')]");
                var ModelTypeDiv = doc.DocumentNode.SelectNodes("//div[@class='attrib model_type']");
                var Image = doc.DocumentNode.SelectNodes("//img[@class='figure-img img-fluid large portrait']");
                var ArticleAbout = doc.DocumentNode.SelectNodes("//article[@class='mill-about']");
                var ArticleFotos = doc.DocumentNode.SelectNodes("//article[@class='mill-photos']");
                bool isNogWaarneembaarPrevious = false;
                bool isGeschiedenisPrevious = false;
                if (divs != null)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    MolenData newMolenData = new MolenData()
                    {
                        Name = "",
                        Ten_Brugge_Nr = Ten_Brugge_Nr,
                        DisappearedYearInfos = oldMolenData?.DisappearedYearInfos ?? new List<DisappearedYearInfo>(),
                        Images = oldMolenData?.Images ?? new List<MolenImage>(),
                        AddedImages = oldMolenData?.AddedImages ?? new List<AddedImage>(),
                        MolenMakers = oldMolenData?.MolenMakers ?? new List<MolenMaker>(),
                        //ModelTypes = oldMolenData?.ModelTypes ?? new List<MolenType>(),
                        MolenTypeAssociations = oldMolenData?.MolenTypeAssociations ?? new List<MolenTypeAssociation>(),
                    };
                    foreach (var div in divs)
                    {
                        var dt = div.SelectSingleNode(".//dt")?.InnerText?.Trim();
                        var dd = div.SelectSingleNode(".//dd")?.InnerText?.Trim();
                        var dd2 = div.SelectSingleNode(".//dd");
                        if (!string.IsNullOrEmpty(dt) && !string.IsNullOrEmpty(dd))
                        {
                            if (strings.Find(x => x.ContainsKey(dt)) == null) strings.Add(new Dictionary<string, string> { { dt, Ten_Brugge_Nr } });
                            switch (dt.ToLower())
                            {
                                case "geo positie":
                                    string pattern = @"N:\s*([0-9.-]+),\s*O:\s*([0-9.-]+)";
                                    var match = Regex.Match(dd, pattern);
                                    if (Ten_Brugge_Nr == "12170")
                                    {
                                        newMolenData.Latitude = 51.91575984198239;
                                        newMolenData.Longitude = 6.577599354094867;
                                    }
                                    else
                                    {
                                        if (match.Success)
                                        {
                                            if (float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float NorthValue))
                                            {
                                                newMolenData.Latitude = NorthValue;
                                            }
                                            if (double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double EastValue))
                                            {
                                                newMolenData.Longitude = EastValue;
                                            }
                                        }
                                    }
                                    break;
                                case "naam":
                                    newMolenData.Name = dd;
                                    break;
                                case "toelichting naam":
                                    newMolenData.ToelichtingNaam = dd;
                                    break;
                                case "bouwjaar":
                                    if (dd.All(char.IsDigit)) newMolenData.Bouwjaar = Convert.ToInt32(dd);
                                    else
                                    {
                                        Regex regexSingleYear = new Regex(@"\b\d{4}\b");
                                        Match matchSingleYear = regexSingleYear.Match(dd);
                                        Regex regexMultiYear = new Regex(@"\b(\d{4})\s*[-–]\s*(\d{4})\b");
                                        Match matchMultiYear = regexMultiYear.Match(dd);

                                        if (matchMultiYear.Success)
                                        {
                                            int startYear = int.Parse(matchMultiYear.Groups[1].Value);
                                            int endYear = int.Parse(matchMultiYear.Groups[2].Value);

                                            if (startYear <= endYear)
                                            {
                                                newMolenData.BouwjaarStart = startYear;
                                                newMolenData.BouwjaarEinde = endYear;
                                            }
                                        }
                                        else if (matchSingleYear.Success)
                                        {
                                            newMolenData.Bouwjaar = Convert.ToInt32(matchSingleYear.Value);
                                        }
                                    }
                                    break;
                                case "herbouwd":
                                    newMolenData.HerbouwdJaar = dd;
                                    break;
                                case "bedrijfsvaardigheid":
                                    if (string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Bedrijfsvaardigheid = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "functie":
                                    if ((newMolenData.Functie == null || newMolenData.Functie == "Onbekend") && string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Functie = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "molenmaker":
                                    string molenmakerPattern = @"^(.*?),.*\((\d{4})\)$";
                                    if (dd != null && dd != "")
                                    {
                                        foreach (var line in dd.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                                        {
                                            Match molenmakerMatch = Regex.Match(line, molenmakerPattern);
                                            if (molenmakerMatch.Success)
                                            {
                                                string name = molenmakerMatch.Groups[1].Value;
                                                string year = molenmakerMatch.Groups[2].Value;
                                                if (!newMolenData.MolenMakers.Any(x => x.Name.ToLower() == name.ToLower() && x.Year == year))
                                                {
                                                    newMolenData.MolenMakers.Add(new MolenMaker()
                                                    {
                                                        Name = name,
                                                        Year = year
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case "bestemming":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Doel = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "ten bruggencate-nr.":
                                    newMolenData.Ten_Brugge_Nr = dd.Replace(" ", "-");
                                    break;
                                case "monument":
                                    newMolenData.Monument = GetUrlFromATag(dd2);
                                    break;
                                case "plaats":
                                    newMolenData.Plaats = dd;
                                    break;
                                case "adres":
                                    newMolenData.Adres = dd;
                                    break;
                                case "gemeente":
                                    var gemeente = dd.Split(",");
                                    string provincie = null;
                                    string completeGemeente = null;
                                    if (gemeente.Length >= 2 && gemeente[gemeente.Length - 1] != null)
                                    {
                                        if (newMolenData.Ten_Brugge_Nr == "17563")
                                        {
                                            Place? place = await _placesService.GetPlaceByName(gemeente[0]);
                                            if (place == null)
                                            {
                                                place = await _placesService.GetPlaceByName(newMolenData.Plaats);
                                            }
                                            if (place != null)
                                            {
                                                provincie = place.Province;
                                            }
                                        }
                                        else
                                        {
                                            provincie = gemeente[gemeente.Length - 1];
                                        }
                                    }
                                    if (gemeente.Length >= 1 && gemeente[0] != null)
                                    {
                                        completeGemeente = gemeente[0];
                                    }
                                    newMolenData.Provincie = provincie != null ? provincie.Trim() : null;
                                    newMolenData.Gemeente = completeGemeente != null ? completeGemeente.Trim() : null;
                                    break;
                                case "streek":
                                    newMolenData.Streek = dd;
                                    break;
                                case "plaatsaanduiding":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Plaatsaanduiding = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "plaats bediening":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.PlaatsBediening = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "bediening kruiwerk":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.BedieningKruiwerk = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "plaats kruiwerk":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.PlaatsKruiwerk = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "kruiwerk":
                                    newMolenData.Kruiwerk = dd;
                                    break;
                                case "vlucht":
                                    newMolenData.Vlucht = dd;
                                    break;
                                case "openingstijden":
                                    newMolenData.Openingstijden = dd;
                                    break;
                                case "open voor publiek":
                                    newMolenData.OpenVoorPubliek = dd.ToLower().Contains("ja");
                                    break;
                                case "open op zaterdag":
                                    newMolenData.OpenOpZaterdag = dd.ToLower().Contains("ja");
                                    break;
                                case "open op zondag":
                                    newMolenData.OpenOpZondag = dd.ToLower().Contains("ja");
                                    break;
                                case "open op afspraak":
                                    newMolenData.OpenOpAfspraak = dd.ToLower().Contains("ja");
                                    break;
                                case "molenaar":
                                    newMolenData.Molenaar = dd;
                                    break;
                                case "winkelinformatie":
                                    newMolenData.WinkelInformatie = dd;
                                    break;
                                case "toestand":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Toestand = char.ToUpper(dd[0]) + dd.Substring(1);
                                        newMolenData.CanAddImages = newMolenData.Toestand.ToLower() != "verdwenen";
                                    }
                                    break;
                                case "krachtbron":
                                    newMolenData.Krachtbron = dd.ToLower();
                                    break;
                                case "website":
                                    newMolenData.Website = GetUrlFromATag(dd2);
                                    break;
                                case "eigenaar":
                                    newMolenData.Eigenaar = dd;
                                    break;
                                case "wiekvorm":
                                    newMolenData.Wiekvorm = dd;
                                    break;
                                case "wiekverbeteringen":
                                    newMolenData.WiekVerbeteringen = dd;
                                    break;
                                case "literatuur":
                                    newMolenData.Literatuur = dd;
                                    break;
                                case "bouwbestek":
                                    newMolenData.Bouwbestek = dd;
                                    break;
                                case "bijzonderheden":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Bijzonderheden = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "museuminformatie":
                                    newMolenData.Museuminformatie = dd;
                                    break;
                                case "wederopbouw":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Wederopbouw = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "over de as":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.As = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "over de wieken":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Wieken = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "unieke eigenschap":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.UniekeEigenschap = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "landschappelijke waarde":
                                    newMolenData.LandschappelijkeWaarde = dd;
                                    break;
                                case "kadastrale aanduiding":
                                    newMolenData.KadastraleAanduiding = dd;
                                    break;
                                case "opvolger":
                                    newMolenData.Opvolger = GetTBNFromUrl(GetUrlFromATag(dd2));
                                    break;
                                case "voorganger":
                                    newMolenData.Voorganger = GetTBNFromUrl(GetUrlFromATag(dd2));
                                    break;
                                case "verplaatst naar":
                                    newMolenData.VerplaatstNaar = GetTBNFromUrl(GetUrlFromATag(dd2));
                                    break;
                                case "afkomstig van":
                                    newMolenData.AfkomstigVan = GetTBNFromUrl(GetUrlFromATag(dd2));
                                    break;
                                case "plaats(en) voorheen":
                                    newMolenData.PlaatsenVoorheen = dd;
                                    break;
                                case "molenerf":
                                    newMolenData.Molenerf = dd;
                                    break;
                                case "trivia":
                                    newMolenData.Trivia = dd;
                                    break;
                                case "geschiedenis":
                                    isGeschiedenisPrevious = true;
                                    newMolenData.Geschiedenis = dd;
                                    var imageInGeschiedenis = await GetImageFromHtmlNode(dd2, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder, "Geschiedenis", true);
                                    if (imageInGeschiedenis != null)
                                    {
                                        newMolenData.Images.Add(imageInGeschiedenis);
                                    }
                                    break;
                                case "eigendomshistorie":
                                    newMolenData.Eigendomshistorie = dd;
                                    break;
                                case "wetenswaardigheden":
                                    newMolenData.Wetenswaardigheden = dd;
                                    break;
                                case "recente werkzaamheden":
                                    newMolenData.RecenteWerkzaamheden = dd;
                                    break;
                                case "rad":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Rad = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "rad diameter":
                                    newMolenData.RadDiameter = dd;
                                    break;
                                case "wateras":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Wateras = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "toegangsprijzen":
                                    if (!string.IsNullOrEmpty(dd))
                                    {
                                        newMolenData.Toegangsprijzen = char.ToUpper(dd[0]) + dd.Substring(1);
                                    }
                                    break;
                                case "nog waarneembaar":
                                    isNogWaarneembaarPrevious = true;
                                    var nogWaarneembareImage = await GetImageFromHtmlNode(dd2, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder, "Nog waarneembaar");
                                    if (nogWaarneembareImage != null)
                                    {
                                        newMolenData.Images.Add(nogWaarneembareImage);
                                    }
                                    break;
                                case "verdwenen":
                                    allverdwenenMolens.Add(newMolenData.Ten_Brugge_Nr);
                                    var lines = dd.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (lines.Length < MolenRemovedYears.Count) break;
                                    MolenRemovedYears.Clear();
                                    for (int i = 0; i < lines.Length; i++)
                                    {
                                        string line = lines[i].Trim();

                                        // Case 1: Single year with no status on the same line
                                        if (Regex.IsMatch(line, @"^\d{4}$") && lines.Length == 1)
                                        {
                                            string year = line;
                                            string status = "gesloopt";

                                            if (i + 1 < lines.Length && !Regex.IsMatch(lines[i + 1], @"^\d{4}$"))
                                            {
                                                status = lines[i + 1].Trim();
                                                i++;
                                            }

                                            DisappearedYearInfo newMolenYearInfo = new DisappearedYearInfo
                                            {
                                                Year = Convert.ToInt32(year),
                                                Status_after = status
                                            };

                                            if (newMolenData.DisappearedYearInfos.Find(info => info.Year == Convert.ToInt32(year) && info.Status_after == newMolenYearInfo.Status_after) == null)
                                            {
                                                newMolenData.DisappearedYearInfos.Add(newMolenYearInfo);
                                            }
                                        }
                                        // Case 2: Line with a prefix (like "na", "voor", etc.) before the year
                                        else if (Regex.IsMatch(line, @"^\w+ \d{4}$"))
                                        {
                                            var match1 = Regex.Match(line, @"^(\w+) (\d{4})$");
                                            string prefix = match1.Groups[1].Value.Trim();
                                            string year = match1.Groups[2].Value;

                                            DisappearedYearInfo newMolenYearInfo = new DisappearedYearInfo
                                            {
                                                Status_before = prefix,
                                                Year = Convert.ToInt32(year)
                                            };

                                            if (newMolenData.DisappearedYearInfos.Find(info => info.Year == Convert.ToInt32(year) && info.Status_before == newMolenYearInfo.Status_before) == null)
                                            {
                                                newMolenData.DisappearedYearInfos.Add(newMolenYearInfo);
                                            }
                                        }
                                        // Case 3: Line with both a year and a status in the same line
                                        else if (!Regex.IsMatch(line, @"^\d{4}$"))
                                        {
                                            var match1 = Regex.Match(line, @"(.*?)\b(\d{4})\b\s*(.*)");
                                            if (match1.Success)
                                            {
                                                string prefixStatus = match1.Groups[1].Value.Trim();
                                                string year = match1.Groups[2].Value;
                                                string suffixStatus = match1.Groups[3].Value.Trim();

                                                if (prefixStatus == "") prefixStatus = null;
                                                if (suffixStatus == "") suffixStatus = null;

                                                DisappearedYearInfo newMolenYearInfo = new DisappearedYearInfo
                                                {
                                                    Status_before = prefixStatus,
                                                    Year = Convert.ToInt32(year),
                                                    Status_after = suffixStatus
                                                };

                                                if (newMolenData.DisappearedYearInfos.Find(info => info.Year == newMolenYearInfo.Year
                                                && info.Status_before == newMolenYearInfo.Status_before
                                                && info.Status_after == newMolenYearInfo.Status_after) == null)
                                                {
                                                    newMolenData.DisappearedYearInfos.Add(newMolenYearInfo);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                            if (data.ContainsKey(dt)) data[dt] = dd;
                            else data.Add(dt, dd);
                        }
                        else if (dt?.ToLower() == "nog waarneembaar")
                        {
                            isNogWaarneembaarPrevious = true;
                        }
                        else if (isNogWaarneembaarPrevious)
                        {
                            var nogWaarneembareImage = await GetImageFromHtmlNode(dd2, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder, "Nog waarneembaar");
                            if (nogWaarneembareImage != null)
                            {
                                newMolenData.Images.Add(nogWaarneembareImage);
                            }
                            isNogWaarneembaarPrevious = false;
                        }
                        else if (isGeschiedenisPrevious && string.IsNullOrEmpty(dt))
                        {
                            var imageInGeschiedenis = await GetImageFromHtmlNode(dd2, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder, "Geschiedenis", true);
                            if (imageInGeschiedenis != null)
                            {
                                newMolenData.Images.Add(imageInGeschiedenis);
                            }
                        }

                        if (dt?.ToLower() != "geschiedenis" && isGeschiedenisPrevious)
                        {
                            isGeschiedenisPrevious = !isGeschiedenisPrevious;
                        }
                        if (dt?.ToLower() != "nog waarneembaar" && isNogWaarneembaarPrevious)
                        {
                            isNogWaarneembaarPrevious = !isNogWaarneembaarPrevious;
                        }
                    }

                    if (oldMolenData != null) newMolenData.Id = oldMolenData.Id;

                    if (ModelTypeDiv != null)
                    {
                        string name = "";
                        foreach (var modelDiv in ModelTypeDiv)
                        {
                            var dt = modelDiv.SelectSingleNode(".//dt")?.InnerText?.Trim();
                            name = dt ?? "";
                            var dd = modelDiv.SelectSingleNode(".//dd")?.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(dt) && !string.IsNullOrEmpty(dd))
                            {
                                foreach (string type in dd.Split(','))
                                {
                                    string trimmedType = type.Trim();
                                    var molenType = new MolenType() { Name = char.ToUpper(trimmedType[0]) + trimmedType.Substring(1) };
                                    //Console.WriteLine("============="+ molenType.Name);
                                    if (!_dBMolenTypeService.Exists(molenType, out var found)) 
                                    {
                                        found = molenType;
                                    }
                                    //Console.WriteLine(JsonSerializer.Serialize(molenType));
                                    //Console.WriteLine(char.ToUpper(trimmedType[0]) + trimmedType.Substring(1));
                                    if (newMolenData.MolenTypeAssociations.Where(x => x.MolenType.Name.ToLower() == molenType.Name.ToLower()).Count() == 0)
                                    {
                                        if (found != null)
                                        {
                                            //Console.WriteLine("=============================");
                                            var y = new MolenTypeAssociation()
                                            {
                                                MolenDataId = newMolenData.Id,
                                                MolenType = found
                                            };
                                            newMolenData.MolenTypeAssociations.Add(y);
                                        }
                                        else
                                        {
                                            //Console.WriteLine("=============================");
                                            var y = new MolenTypeAssociation()
                                            {
                                                MolenDataId = newMolenData.Id,
                                                MolenType = molenType
                                            };
                                            newMolenData.MolenTypeAssociations.Add(y);
                                        }
                                        //Console.WriteLine(JsonSerializer.Serialize(y));
                                    }
                                }
                            }
                        }
                        if (data.ContainsKey(name)) data[name] = newMolenData.MolenTypeAssociations;
                        else data.Add(name, newMolenData.MolenTypeAssociations);
                    }

                    if (Image != null)
                    {
                        var src = Image.First().GetAttributeValue("src", string.Empty);
                        if (!string.IsNullOrEmpty(src) && Uri.IsWellFormedUriString(src, UriKind.Absolute))
                        {
                            var imageResponse = await _client.GetAsync(src);
                            byte[] image = await imageResponse.Content.ReadAsByteArrayAsync();
                            try
                            {
                                if (newMolenData.Images.Find(x => x.Name == "base-image") == null)
                                {
                                    newMolenData.Images.Add(new MolenImage()
                                    {
                                        FilePath = CreateCleanPath.CreatePathWithoutWWWROOT($"{Globals.MolenImagesFolder}/{newMolenData.Ten_Brugge_Nr}/base-image.jpg"),
                                        Name = "base-image",
                                        MolenDataId = newMolenData.Id,
                                        ExternalUrl = src
                                    });
                                }
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine("Error: " + err.Message);
                            }
                        }
                    }

                    if (newMolenData.Images.Count < Globals.MaxNormalImagesCount)
                    {
                        bool isImageAdded = false;
                        if (ArticleAbout != null)
                        {
                            var firstArticle = ArticleAbout.First();
                            var images = firstArticle.SelectNodes(".//img");
                            if (images != null)
                            {
                                var imageToAdd = images.First();
                                var molenImage = await GetImageFromHtmlNode(imageToAdd, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder);
                                if (molenImage != null)
                                {
                                    newMolenData.Images.Add(molenImage);
                                    isImageAdded = true;
                                }
                            }
                        }
                        if (((isImageAdded && newMolenData.Images.Count < Globals.MaxNormalImagesCount) || !isImageAdded) && ArticleFotos != null)
                        {
                            string pattern = @"\((\d{1,2}-\d{1,2}-(\d{4}))\)";
                            var firstArticle = ArticleFotos.First();
                            var turboFrames = firstArticle.SelectNodes(".//turbo-frame");
                            List<(string, HtmlNode)> foundTurboFrames = new List<(string, HtmlNode)>();
                            for (int i = 0; i < turboFrames.Count; i++)
                            {
                                var divsInTurboFrame = turboFrames[i].SelectNodes(".//div");
                                foreach (var div in divsInTurboFrame)
                                {
                                    Match match = Regex.Match(div.InnerText.Trim(), pattern);
                                    if (match.Success)
                                    {
                                        foundTurboFrames.Add((match.Groups[2].Value, turboFrames[i]));
                                    }
                                }
                                if (i == 2)
                                {
                                    break;
                                }
                            }

                            foundTurboFrames = foundTurboFrames
                                .OrderByDescending(frame => int.Parse(frame.Item1))
                                .ToList();

                            List<HtmlNode> imagesPortraits = new List<HtmlNode>();
                            foreach (var turboFrame in foundTurboFrames)
                            {
                                var imageContainer = turboFrame.Item2;
                                var images = imageContainer.SelectNodes(".//img[@class='figure-img img-fluid large portrait']");
                                if (images != null && images.Count > 0)
                                {
                                    imagesPortraits.Add(imageContainer);
                                }
                            }
                            if (imagesPortraits.Count > 0)
                            {
                                var imageToAdd = imagesPortraits.First();
                                var molenImage = await GetImageFromHtmlNode(imageToAdd, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder, canBeDeleted: true);
                                if (molenImage != null)
                                {
                                    newMolenData.Images.Add(molenImage);
                                }

                            }
                            else if (foundTurboFrames.Count > 0)
                            {
                                var imageContainer = foundTurboFrames.First().Item2;
                                var images = imageContainer.SelectNodes(".//img");
                                if (images != null)
                                {
                                    var imageToAdd = images.First();
                                    var molenImage = await GetImageFromHtmlNode(imageContainer, newMolenData.Ten_Brugge_Nr, Globals.MolenImagesFolder, canBeDeleted: true);
                                    if (molenImage != null)
                                    {
                                        newMolenData.Images.Add(molenImage);
                                    }
                                }
                            }
                        }
                    }

                    newMolenData.LastUpdated = DateTime.Now;
                    for (int i = 0; i < newMolenData.Images.Count; i++)
                    {
                        var imageInOldMolen = oldMolenData?.Images.Find(x => x.Name == newMolenData.Images[i].Name && x.FilePath == newMolenData.Images[i].FilePath);
                        bool isImageInOldMolen = oldMolenData != null && imageInOldMolen != null;
                        newMolenData.Images[i].MolenDataId = newMolenData.Id;
                        if (!isImageInOldMolen)
                        {
                            CreateDirectoryIfNotExists.CreateMolenImagesFolderDirectory(newMolenData.Ten_Brugge_Nr);
                            if (!File.Exists(Globals.WWWROOTPath + newMolenData.Images[i].FilePath) && Uri.IsWellFormedUriString(newMolenData.Images[i].ExternalUrl, UriKind.Absolute))
                            {
                                File.WriteAllBytes(Globals.WWWROOTPath + newMolenData.Images[i].FilePath, await _client.GetByteArrayAsync(newMolenData.Images[i].ExternalUrl));
                            }
                        }
                    }
                    for (int i = 0; i < oldMolenData?.Images.Count; i++)
                    {
                        if (newMolenData.Images.Find(x => x.Id == oldMolenData.Images[i].Id) == null && File.Exists(Globals.WWWROOTPath + oldMolenData.Images[i].FilePath))
                        {
                            newMolenData.Images.Add(oldMolenData.Images[i]);
                        }
                    }

                    if (newMolenData != null && oldMolenData != null && oldMolenData.AddedImages != null)
                    {
                        newMolenData.AddedImages = oldMolenData.AddedImages;
                    }

                    if (Directory.Exists(Globals.MolenAddedImagesFolder) && Directory.Exists(Globals.MolenAddedImagesFolder + "/" + Ten_Brugge_Nr))
                    {
                        var allImages = Directory.GetFiles(Globals.MolenAddedImagesFolder + "/" + Ten_Brugge_Nr, "*.jpg");
                        foreach (var image in allImages)
                        {
                            string imageName = Path.GetFileNameWithoutExtension(image);
                            string imagePath = CreateCleanPath.CreatePathWithoutWWWROOT(image);
                            if (newMolenData != null && newMolenData.AddedImages.Find(x => x.Name == imageName && x.FilePath == imagePath) == null)
                            {
                                var newAddedMolenImage = new AddedImage
                                {
                                    FilePath = imagePath,
                                    Name = imageName,
                                    CanBeDeleted = true,
                                    Description = "",
                                    MolenDataId = newMolenData.Id
                                };
                                newMolenData.AddedImages.Add(newAddedMolenImage);
                            }
                        }
                    }
                    if (newMolenData!= null)
                    {
                        newMolenData.MolenTBN = new MolenTBN()
                        {
                            Ten_Brugge_Nr = newMolenData.Ten_Brugge_Nr
                        };
                    }
                    Console.WriteLine("newMolenData.MolenMakers: " + newMolenData.MolenMakers.Count);
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    };
                    //Console.WriteLine(JsonSerializer.Serialize(newMolenData, options));
                    //_dbContext.SaveChanges();
                    await _dBMolenDataService.AddOrUpdate(newMolenData);
                    return (newMolenData, data);
                }
            }

            catch (HttpRequestException e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            return (null, null);
        }

        public async Task<MolenImage?> GetImageFromHtmlNode(HtmlNode dd2, string Ten_Brugge_Nr, string filePath, string? description = null, bool canBeDeleted = false)
        {
            var nogWaarneembareImages = dd2.SelectNodes(".//img");
            if (nogWaarneembareImages != null && nogWaarneembareImages.Count > 0)
            {
                var nogWaarneembareImage = nogWaarneembareImages.First();
                var nogWaarneembareImageSrc = nogWaarneembareImage.GetAttributeValue("src", string.Empty);
                if (!Uri.IsWellFormedUriString(nogWaarneembareImageSrc, UriKind.Absolute))
                {
                    if (Uri.IsWellFormedUriString("https://www.molendatabase.nl/" + nogWaarneembareImageSrc, UriKind.Absolute))
                    {
                        nogWaarneembareImageSrc = "https://www.molendatabase.nl/" + nogWaarneembareImageSrc;
                    }
                }

                if (!string.IsNullOrEmpty(nogWaarneembareImageSrc) && Uri.IsWellFormedUriString(nogWaarneembareImageSrc, UriKind.Absolute))
                {

                    var imageResponse = await _client.GetAsync(nogWaarneembareImageSrc);
                    byte[] nogWaarneembaarImage = await imageResponse.Content.ReadAsByteArrayAsync();
                    string ImageName = GetFileNameForImage.GetFileName();
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    if (!Directory.Exists(filePath + "/" + Ten_Brugge_Nr))
                    {
                        Directory.CreateDirectory(filePath + "/" + Ten_Brugge_Nr);
                    }

                    string imagePath = CreateCleanPath.CreatePathToWWWROOT($"{filePath}/{Ten_Brugge_Nr}/{ImageName}.jpg");

                    using (var md5 = MD5.Create())
                    {
                        byte[] newImageHash = md5.ComputeHash(nogWaarneembaarImage);

                        string? existingPath = null;
                        foreach (var existingImagePath in Directory.GetFiles($"{Globals.MolenImagesFolder}/{Ten_Brugge_Nr}", "*.jpg"))
                        {
                            byte[] existingImage = File.ReadAllBytes(existingImagePath);
                            byte[] existingImageHash = md5.ComputeHash(existingImage);

                            if (newImageHash.SequenceEqual(existingImageHash))
                            {
                                existingPath = existingImagePath;
                                break;
                            }
                        }

                        if (existingPath == null)
                        {
                            File.WriteAllBytes(imagePath, nogWaarneembaarImage);
                            return (new MolenImage
                            {
                                FilePath = CreateCleanPath.CreatePathWithoutWWWROOT(imagePath),
                                Name = ImageName.ToString(),
                                Description = description ?? string.Empty,
                                CanBeDeleted = canBeDeleted,
                                ExternalUrl = nogWaarneembareImageSrc
                            });
                        }
                        else if (existingPath != null)
                        {
                            string pathWithoutRoot = CreateCleanPath.CreatePathWithoutWWWROOT(existingPath);
                            var foundImagesInDB = _dbContext.MolenImages
                                .FirstOrDefault(x => x.FilePath == pathWithoutRoot);
                            if (foundImagesInDB != null)
                            {
                                return new MolenImage
                                {
                                    Id = foundImagesInDB.Id,
                                    FilePath = pathWithoutRoot,
                                    Name = foundImagesInDB.Name,
                                    Description = description ?? string.Empty,
                                    CanBeDeleted = canBeDeleted,
                                    ExternalUrl = nogWaarneembareImageSrc,
                                    MolenDataId = foundImagesInDB.MolenDataId
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        public string GetTBNFromUrl(string url)
        {
            if (url != null && !string.IsNullOrEmpty(url))
            {
                int IndexOfQuest = url.IndexOf('?');
                if (IndexOfQuest > 0)
                {
                    url = url.Substring(0, IndexOfQuest);
                }
                string pattern_voorganger = @"ten-bruggencate-nr-(\d+(-[a-zA-Z0-9]+)?)";

                Match match_voorganger = Regex.Match(url, pattern_voorganger);

                if (match_voorganger.Success)
                {
                    url = match_voorganger.Groups[1].Value;
                    return url;
                }
            }
            return null;
        }

        public string GetUrlFromATag(HtmlNode url)
        {
            HtmlNode aTag = url.SelectSingleNode(".//a");
            if (aTag != null)
            {
                return aTag.GetAttributeValue("href", string.Empty);
            }
            return null;
        }

        public async Task<List<MolenTBN>> AddMolenTBNToDB(List<MolenTBNOld> tBNsOld)
        {
            List<MolenTBN> alleMolenTBNR = await _dbContext.MolenTBNs.ToListAsync();
            List<MolenTBN> molenList = new List<MolenTBN>();
            foreach (var tbn in tBNsOld)
            {
                var molen = new MolenTBN()
                {
                    Ten_Brugge_Nr = tbn.Ten_Brugge_Nr
                };
                molenList.Add(molen);
            }
            foreach (var molentbn in molenList)
            {
                if (alleMolenTBNR.Where(x => x.Ten_Brugge_Nr == molentbn.Ten_Brugge_Nr).Count() == 0)
                {
                    await _dbContext.MolenTBNs.AddAsync(molentbn);
                    alleMolenTBNR.Add(molentbn);
                }
            }
            return alleMolenTBNR;
        }

        public async Task<List<MolenTBN>> AddMolenTBNToDB()
        {
            List<MolenTBN> alleMolenTBNR = await _dbContext.MolenTBNs.ToListAsync();
            List<MolenTBN> molenList = await ReadAllActiveMolenTBN();
            foreach (var molentbn in molenList)
            {
                if (alleMolenTBNR.Where(x => x.Ten_Brugge_Nr == molentbn.Ten_Brugge_Nr).Count() == 0)
                {
                    await _dbContext.MolenTBNs.AddAsync(molentbn);
                    alleMolenTBNR.Add(molentbn);
                }
            }
            return alleMolenTBNR;
        }

        public async Task<(List<MolenData>? MolenData, bool isDone, TimeSpan? timeToWait)> UpdateDataOfLastUpdatedMolens()
        {
            List<MolenData>? oldestUpdateTimesMolens = await _dbContext.MolenData.OrderByDescending(x => x.LastUpdated).Take(50).ToListAsync();
            if (oldestUpdateTimesMolens == null) return (null, false, null);
            if (oldestUpdateTimesMolens[0].LastUpdated >= DateTime.Now.AddMinutes(-30))
            {
                return (null, false, oldestUpdateTimesMolens[0].LastUpdated.AddMinutes(30) - DateTime.Now);
            }
            List<MolenData> updatedMolens = new List<MolenData>();

            foreach (var molen in oldestUpdateTimesMolens)
            {
                var result = await GetMolenDataByTBNumber(molen.Ten_Brugge_Nr);
                updatedMolens.Add(result.Item1);
            }
            return (oldestUpdateTimesMolens, true, TimeSpan.FromMinutes(30));
        }

        public async Task<List<Dictionary<string, object>>> GetAllMolenData()
        {
            List<Dictionary<string, object>> keyValuePairs = new List<Dictionary<string, object>>();
            //List<MolenData> currentData = await _dbContext.MolenData.ToListAsync();
            List<MolenTBN> Data = await ReadAllMolenTBN();
            //List<MolenTBN> Data = await _dbContext.MolenTBNs.ToListAsync();
            int count = 0;
            foreach (MolenTBN tbn in Data)
            {
                //if (currentData.Find(mol => mol.Ten_Brugge_Nr == tbn.Ten_Brugge_Nr) != null) continue;
                count++;
                Thread.Sleep(1500);
                Console.WriteLine("nr-" + count);
                (MolenData, Dictionary<string, object>) molen = await GetMolenDataByTBNumber(tbn.Ten_Brugge_Nr);
                //await _dbContext.MolenTBNs.AddAsync(tbn);
                keyValuePairs.Add(molen.Item2);
                //if (count == 1)
                //{
                //    break;
                //}
                if (count % 1 == 0)
                {
                    await _dbContext.SaveChangesAsync();
                }
            }

            //File.WriteAllText("Json/AlleKeysMolens.json", JsonSerializer.Serialize(strings, new JsonSerializerOptions
            //{
            //    WriteIndented = true
            //}));

            //File.WriteAllText("Json/DaadwerkelijkAlleMolenData.json", JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions
            //{
            //    WriteIndented = true
            //}));

            //File.WriteAllText("Json/allverdwenenMolens.json", JsonSerializer.Serialize(allverdwenenMolens, new JsonSerializerOptions
            //{
            //    WriteIndented = true
            //}));

            await _dbContext.SaveChangesAsync();

            return keyValuePairs;
        }

        public async Task<List<MolenTBN>> SaveAndReadMolenTBN(List<MolenTBNOld> tBNOlds)
        {
            List<MolenTBN> tbns = tBNOlds.Select(tbn => new MolenTBN { Ten_Brugge_Nr = tbn.Ten_Brugge_Nr }).ToList();
            await _dbContext.MolenTBNs.AddRangeAsync(tbns);
            await _dbContext.SaveChangesAsync();
            return tbns;
        }

        public async Task<List<MolenTBN>> ReadAllMolenTBN()
        {
            List<MolenTBN> alleMolenTBNR = new List<MolenTBN>();

            int countDivsAtNull = 0;
            int i = 1;

            while (countDivsAtNull <= 1)
            {
                Thread.Sleep(1500);
                HttpResponseMessage response = await _client.GetAsync("https://www.molendatabase.nl/molens?page=" + i);
                string responseBody = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);
                var divs = doc.DocumentNode.SelectNodes("//div[@class='mill_link']");
                if (divs == null || divs.Count() == 0)
                {
                    countDivsAtNull++;
                }
                if (divs != null)
                {
                    foreach (var div in divs)
                    {
                        var url = div.SelectSingleNode(".//a").GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(url))
                        {
                            url = url.Substring(0, url.IndexOf('?'));
                            string pattern = @"ten-bruggencate-nr-(\d+(-[a-zA-Z0-9]+)?)";

                            Match match = Regex.Match(url, pattern);

                            if (match.Success)
                            {
                                url = match.Groups[1].Value;
                                if (alleMolenTBNR.Where(x => x.Ten_Brugge_Nr == url).Count() == 0)
                                {
                                    alleMolenTBNR.Add(new MolenTBN() { Ten_Brugge_Nr = url });
                                    Console.WriteLine(url);
                                }
                            }
                        }
                    }
                }
                i++;
        }
        Console.WriteLine(alleMolenTBNR.Count);
            return alleMolenTBNR;
        }

        public async Task<List<MolenTBN>> ReadAllActiveMolenTBN()
        {
            List<MolenTBN> alleMolenTBNR = new List<MolenTBN>();

            int countDivsAtNull = 0;
            int i = 1;

            while (countDivsAtNull <= 1)
            {
                HttpResponseMessage response = await _client.GetAsync("https://www.molendatabase.nl/molens?mill_state%5Bstate%5D=existing&page=" + i);
                string responseBody = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);
                var divs = doc.DocumentNode.SelectNodes("//div[@class='mill_link']");
                if (divs == null || divs.Count() == 0)
                {
                    countDivsAtNull++;
                }
                if (divs != null)
                {
                    foreach (var div in divs)
                    {
                        var url = div.SelectSingleNode(".//a").GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(url))
                        {
                            url = url.Substring(0, url.IndexOf('?'));
                            string pattern = @"ten-bruggencate-nr-(\d+(-[a-zA-Z0-9]+)?)";

                            Match match = Regex.Match(url, pattern);

                            if (match.Success)
                            {
                                url = match.Groups[1].Value;
                                if (alleMolenTBNR.Where(x => x.Ten_Brugge_Nr == url).Count() == 0)
                                {
                                    alleMolenTBNR.Add(new MolenTBN() { Ten_Brugge_Nr = url });
                                }
                            }
                        }
                    }
                }
                i++;
            }

            return alleMolenTBNR;
        }

        private readonly TimeSpan cooldownTime = TimeSpan.FromHours(1);

        public async Task<(List<MolenData>? MolenData, TimeSpan? timeToWait)> SearchForNewMolens()
        {
            var newestSearch = await _dbContext.LastSearchedForNewDatas.OrderByDescending(x => x.LastSearched).FirstOrDefaultAsync();
            if (newestSearch.LastSearched >= DateTime.Now.Add(cooldownTime * -1))
            {
                return (null, cooldownTime - (DateTime.Now - newestSearch.LastSearched));
            }

            List<MolenData> allAddedMolens = new List<MolenData>();
            List<MolenTBN> allAddedMolenTBN = new List<MolenTBN>();
            List<MolenTBN> allFoundMolenTBN = await ReadAllActiveMolenTBN();

            foreach (MolenTBN readMolenTBN in allFoundMolenTBN)
            {
                if (allAddedMolens.Count == 50) break;
                var results = await _dbContext.MolenTBNs.Where(x => x.Ten_Brugge_Nr == readMolenTBN.Ten_Brugge_Nr).ToListAsync();
                if (results?.Count == 0)
                {
                    await _dbContext.MolenTBNs.AddAsync(readMolenTBN);
                    var molen = await GetMolenDataByTBNumber(readMolenTBN.Ten_Brugge_Nr);
                    if (molen.Item1 != null)
                    {
                        allAddedMolens.Add(molen.Item1);
                    }
                }
                else if (await _molenService.GetMolenByTBN(readMolenTBN.Ten_Brugge_Nr) == null)
                {
                    var molen = await GetMolenDataByTBNumber(readMolenTBN.Ten_Brugge_Nr);
                    if (molen.Item1 != null)
                    {
                        allAddedMolens.Add(molen.Item1);
                    }
                }
            }

            await AddDateTimeFromSearches();
            return (allAddedMolens, cooldownTime);
        }

        public async Task<bool> AddDateTimeFromSearches()
        {
            var searchesLongerThanOneDay = await _dbContext.LastSearchedForNewDatas
                .Where(x => x.LastSearched < DateTime.Now.AddDays(-1))
                .ToListAsync();

            foreach (var search in searchesLongerThanOneDay)
            {
                _dbContext.Remove(search);
            }
            await _dbContext.AddAsync(new LastSearchedForNewData() { LastSearched = DateTime.Now });
            return true;
        }
    }
}
using MolenApplicatie.Server.Models;
using SQLite;
using MolenApplicatie.Server.Utils;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Globalization;
using HtmlAgilityPack;
using MolenApplicatie.Models;

namespace MolenApplicatie.Server.Services
{
    public class NewMolenDataService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        readonly string baseUrl = "https://www.molendatabase.nl/molens/ten-bruggencate-nr-";
        private readonly HttpClient _client;
        private readonly MolenService _molenService;
        private List<string> allowedTypes = new List<string>();

        private readonly DbConnection _db;

        public NewMolenDataService()
        {
            _client = new HttpClient();
            _db = new DbConnection();
            _molenService = new MolenService();
        }

        public async Task<List<MolenData>> GetAllMolenData()
        {
            await AddMolenTBNToDBFromJson();
            List<Dictionary<string, object>> keyValuePairs = new List<Dictionary<string, object>>();
            List<MolenData> Data = new List<MolenData>();
            List<MolenTBN> MolenNumbers = await _db.Table<MolenTBN>();
            allowedTypes = JsonSerializer.Deserialize<List<string>>(File.ReadAllText("Json/CorrectMolenTypes.json"));
            foreach (MolenTBN Ten_Brugge_Nr in MolenNumbers)
            {
                Thread.Sleep(1000);
                var x = await GetMolenDataByTBNumber(Ten_Brugge_Nr.Ten_Brugge_Nr);
                if (x.Item1 == null) continue;
                Data.Add(x.Item1);
                keyValuePairs.Add(x.Item2);
            }

            File.WriteAllText(PathAlleInformatieMolens, JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            return Data;
        }

        public async Task<(MolenData, Dictionary<string, object>)> GetMolenDataByTBNumber(string Ten_Brugge_Nr)
        {
            List<MolenType> MolenTypes = await _db.Table<MolenType>();
            List<MolenType> NewAddedTypes = new List<MolenType>();
            try
            {
                HttpResponseMessage response = await _client.GetAsync(baseUrl + Ten_Brugge_Nr);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);

                var divs = doc.DocumentNode.SelectNodes("//div[@class='attrib']");
                var ModelTypeDiv = doc.DocumentNode.SelectNodes("//div[@class='attrib model_type']");
                var Image = doc.DocumentNode.SelectNodes("//img[@class='figure-img img-fluid large portrait']");
                if (divs != null)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    MolenData newMolenData = new MolenData() { Id = -1 };
                    foreach (var div in divs)
                    {
                        var dt = div.SelectSingleNode(".//dt")?.InnerText?.Trim();
                        var dd = div.SelectSingleNode(".//dd")?.InnerText?.Trim();
                        if (!string.IsNullOrEmpty(dt) && !string.IsNullOrEmpty(dd))
                        {
                            switch (dt.ToLower())
                            {
                                case "geo positie":
                                    string pattern = @"N:\s*([0-9.-]+),\s*O:\s*([0-9.-]+)";
                                    var match = Regex.Match(dd, pattern);
                                    if (Ten_Brugge_Nr == "12170")
                                    {
                                        newMolenData.North = 51.91575984198239;
                                        newMolenData.East = 6.577599354094867;
                                    }
                                    else
                                    {
                                        if (match.Success)
                                        {
                                            if (float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float NorthValue))
                                            {
                                                newMolenData.North = NorthValue;
                                            }
                                            if (double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double EastValue))
                                            {
                                                newMolenData.East = EastValue;
                                            }
                                        }
                                    }
                                    break;
                                case "naam":
                                    newMolenData.Name = dd;
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
                                                newMolenData.Bouwjaar_start = startYear;
                                                newMolenData.Bouwjaar_einde = endYear;
                                            }
                                        }
                                        else if (matchSingleYear.Success)
                                        {
                                            newMolenData.Bouwjaar = Convert.ToInt32(matchSingleYear.Value);
                                        }
                                    }
                                    break;
                                case "herbouwd":
                                    newMolenData.Herbouwd_jaar = dd;
                                    break;
                                case "functie":
                                    newMolenData.Functie = dd;
                                    break;
                                case "ten bruggencate-nr.":
                                    newMolenData.Ten_Brugge_Nr = dd.Replace(" ", "-");
                                    break;
                                case "plaats":
                                    newMolenData.Plaats = dd;
                                    break;
                                case "adres":
                                    newMolenData.Adres = dd;
                                    break;
                                default:
                                    break;
                            }
                            if (data.ContainsKey(dt)) data[dt] = dd;
                            else data.Add(dt, dd);
                        }
                    }

                    MolenData oldMolenData = await _db.FindWithQueryAsync<MolenData>($"SELECT * FROM MolenData WHERE Ten_Brugge_Nr = '{newMolenData.Ten_Brugge_Nr}'");
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
                                    var molenType = new MolenType() { Name = type.Trim() };
                                    if (MolenTypes.Concat(NewAddedTypes).Where(x => x.Name == molenType.Name).Count() == 0 && allowedTypes.Contains(molenType.Name.ToLower()))
                                    {
                                        await _db.InsertAsync(molenType);
                                        molenType.Id = await _db.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
                                        NewAddedTypes.Add(molenType);
                                        newMolenData.ModelType.Add(molenType);
                                    }
                                    else
                                    {
                                        var existingType = MolenTypes.Concat(NewAddedTypes).FirstOrDefault(x => x.Name == molenType.Name);
                                        if (existingType != null && newMolenData.ModelType.Find(x => x.Name == existingType.Name) == null)
                                        {
                                            newMolenData.ModelType.Add(existingType);
                                        }
                                    }
                                }
                                NewAddedTypes.AddRange(newMolenData.ModelType);
                            }
                        }
                        if (data.ContainsKey(name)) data[name] = NewAddedTypes;
                        else data.Add(name, NewAddedTypes);
                    }

                    if (!allowedTypes.Any(x => newMolenData.ModelType.Any(y => y.Name.ToLower() == x.ToLower()) && newMolenData.ModelType.Count() > 0))
                    {
                        return (null, null);
                    }

                    if (Image != null)
                    {
                        var src = Image.First().GetAttributeValue("src", string.Empty);
                        if (!string.IsNullOrEmpty(src))
                        {
                            var imageResponse = await _client.GetAsync(src);
                            byte[] image = await imageResponse.Content.ReadAsByteArrayAsync();
                            newMolenData.Image = new MolenImage(image, newMolenData.Ten_Brugge_Nr, false);
                            if (!Directory.Exists(Globals.MolenImagesFolder))
                            {
                                Directory.CreateDirectory(Globals.MolenImagesFolder);
                            }
                            if (!File.Exists($"{Globals.MolenImagesFolder}/{newMolenData.Ten_Brugge_Nr}"))
                            {
                                File.WriteAllBytes($"{Globals.MolenImagesFolder}/{newMolenData.Ten_Brugge_Nr}.jpg", newMolenData.Image.Content);
                            }
                            else if (File.ReadAllBytes($"{Globals.MolenImagesFolder}/{newMolenData.Ten_Brugge_Nr}.jpg").Length != newMolenData.Image.Content.Length)
                            {
                                File.WriteAllBytes($"{Globals.MolenImagesFolder}/{newMolenData.Ten_Brugge_Nr}.jpg", newMolenData.Image.Content);
                            }
                        }
                    }

                    newMolenData.LastUpdated = DateTime.Now;
                    if (oldMolenData == null)
                    {
                        await _db.InsertAsync(newMolenData);
                    }
                    else
                    {
                        await _db.UpdateAsync(newMolenData);
                    }


                    var allExistingTypes = await _db.QueryAsync<MolenTypeAssociation>(
                        "SELECT * FROM MolenTypeAssociation WHERE MolenDataId = ?", new object[] { newMolenData.Id }
                    );
                    if (allExistingTypes != null)
                    {
                        foreach (var type in allExistingTypes)
                        {
                            if (newMolenData.ModelType.Find(x => x.Id == type.MolenTypeId) == null)
                            {
                                await _db.DeleteAsync<MolenTypeAssociation>(type);
                            }
                        }
                    }
                    foreach (var type in newMolenData.ModelType)
                    {
                        if ((allExistingTypes != null && allExistingTypes.Find(x => x.MolenTypeId == type.Id) == null) || allExistingTypes == null)
                        {
                            await _db.InsertAsync(new MolenTypeAssociation() { MolenDataId = newMolenData.Id, MolenTypeId = type.Id });
                        }
                    }

                    return (newMolenData, data);
                }
            }

            catch (HttpRequestException e)
            {
                throw new HttpRequestException("Error: " + e.Message);
            }
            return (null, null);
        }

        public async Task<List<MolenData>> GetAllMolenDataDB()
        {
            List<MolenData> MolenData = await _db.Table<MolenData>();
            List<MolenData> MolenData_ = new List<MolenData>();
            string jsonString = File.ReadAllText("Json/CorrectMolenTypes.json");
            List<string> CorrectMolenTypes = JsonSerializer.Deserialize<List<string>>(jsonString);
            foreach (var molen in MolenData)
            {
                MolenData CompleteMolen = await _molenService.GetFullDataOfMolen(molen);
                if (CorrectMolenTypes.Any(correct => molen.ModelType.Any(complete => complete.Name.ToLower() == correct.ToLower())))
                {
                    MolenData_.Add(CompleteMolen);
                }
            }
            return MolenData_;
        }

        public async Task<List<MolenTBN>> AddMolenTBNToDBFromJson()
        {
            List<MolenTBN> alleMolenTBNR = await _db.Table<MolenTBN>();
            string jsonString = File.ReadAllText("Json/AlleActieveMolens.json");
            List<MolenTBN> molenList = JsonSerializer.Deserialize<List<MolenTBN>>(jsonString);
            foreach (var molen in molenList)
            {
                if (alleMolenTBNR.Where(x => x.Ten_Brugge_Nr == molen.Ten_Brugge_Nr).Count() == 0)
                {
                    await _db.InsertAsync(molen);
                }
            }
            return await _db.Table<MolenTBN>();
        }

        public async Task<(List<MolenData> MolenData, bool isDone, TimeSpan timeToWait)> UpdateDataOfLastUpdatedMolens()
        {
            MolenData newestUpdateTimeMolen = await _db.FindWithQueryAsync<MolenData>("SELECT * FROM MolenData ORDER BY LastUpdated DESC LIMIT 1");
            if (newestUpdateTimeMolen != null && newestUpdateTimeMolen.LastUpdated >= DateTime.Now.AddMinutes(-30))
            {
                return (null, false, newestUpdateTimeMolen.LastUpdated.AddMinutes(30) - DateTime.Now);
            }
            List<MolenData> oldestUpdateTimesMolens = await _db.QueryAsync<MolenData>("SELECT * FROM MolenData ORDER BY LastUpdated ASC LIMIT 50");
            List<MolenData> updatedMolens = new List<MolenData>();
            foreach (var molen in oldestUpdateTimesMolens)
            {
                var result = await GetMolenDataByTBNumber(molen.Ten_Brugge_Nr);
                updatedMolens.Add(result.Item1);
            }
            return (oldestUpdateTimesMolens, true, TimeSpan.FromMinutes(30));
        }

        public async Task<List<MolenTBN>> ReadAllMolenTBN()
        {
            List<MolenTBN> alleMolenTBNR = new List<MolenTBN>();

            int amountDivsAtNull = 0;
            int i = 1;

            while (amountDivsAtNull <= 1)
            {
                HttpResponseMessage response = await _client.GetAsync("https://www.molendatabase.nl/molens?mill_state%5Bstate%5D=existing&page=" + i);
                string responseBody = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);
                var divs = doc.DocumentNode.SelectNodes("//div[@class='mill_link']");
                if (divs == null || divs.Count() == 0)
                {
                    amountDivsAtNull++;
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

        public async Task<(List<MolenData> MolenData, TimeSpan timeToWait)> SearchForNewMolens()
        {
            var newestSearch = await _db.QueryAsync<LastSearchedForNewData>("SELECT * FROM LastSearchedForNewData ORDER BY LastSearched DESC LIMIT 1");
            if (newestSearch.Count > 0 && newestSearch[0].LastSearched >= DateTime.Now.Add(cooldownTime * -1))
            {
                return (null, cooldownTime - (DateTime.Now - newestSearch[0].LastSearched));
            }

            List<MolenData> allAddedMolens = new List<MolenData>();
            List<MolenTBN> allAddedMolenTBN = new List<MolenTBN>();
            List<MolenTBN> allFoundMolenTBN = await ReadAllMolenTBN();

            foreach (MolenTBN readMolenTBN in allFoundMolenTBN)
            {
                if (allAddedMolens.Count == 50) break;
                if (await _db.FindWithQueryAsync<MolenTBN>("SELECT * FROM MolenTBN WHERE Ten_Brugge_Nr = ?", readMolenTBN.Ten_Brugge_Nr) == null)
                {
                    await _db.InsertAsync(readMolenTBN);
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
            var searchesLongerThanOneDay = await _db.QueryAsync<LastSearchedForNewData>("SELECT * FROM LastSearchedForNewData WHERE LastSearched < ?", new object[] { DateTime.Now.AddDays(-1) });
            foreach (var search in searchesLongerThanOneDay)
            {
                await _db.DeleteAsync(search);
            }
            await _db.InsertAsync(new LastSearchedForNewData() { LastSearched = DateTime.Now });
            return true;
        }
    }
}
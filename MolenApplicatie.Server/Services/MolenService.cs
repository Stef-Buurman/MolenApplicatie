using MolenApplicatie.Server.Models;
using SQLite;
using MolenApplicatie.Server.Utils;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Globalization;
using HtmlAgilityPack;

namespace MolenApplicatie.Server.Services
{
    public class MolenService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        readonly string baseUrl = "https://www.molendatabase.nl/molens/ten-bruggencate-nr-";
        private readonly HttpClient _client;

        private readonly SQLiteAsyncConnection _db;

        public MolenService()
        {
            _client = new HttpClient();
            _db = new SQLiteAsyncConnection(Globals.DBBestaandeMolens);
            InitializeDB();
        }

        public async Task<int> InitializeDB()
        {
            await _db.CreateTableAsync<MolenTBN>();
            await _db.CreateTableAsync<MolenData>();
            await _db.CreateTableAsync<MolenType>();
            await _db.CreateTableAsync<MolenTypeAssociation>();
            return 1;
        }

        public async Task<List<MolenData>> GetAllMolenData()
        {
            await InitializeDB();
            await AddMolenTBNToDBFromJson();
            List<Dictionary<string, object>> keyValuePairs = new List<Dictionary<string, object>>();
            List<MolenData> Data = new List<MolenData>();
            List<MolenTBN> MolenNumbers = await _db.Table<MolenTBN>().ToListAsync();
            foreach (MolenTBN Ten_Brugge_Nr in MolenNumbers)
            {
                var x = await GetMolenDataByTBNumber(Ten_Brugge_Nr.Ten_Brugge_Nr);
                Data.Add(x.Item1);
                keyValuePairs.Add(x.Item2);
                Console.WriteLine(Ten_Brugge_Nr.Id);
            }

            File.WriteAllText(PathAlleInformatieMolens, JsonSerializer.Serialize(keyValuePairs, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            return Data;
        }

        public async Task<(MolenData, Dictionary<string, object>)> GetMolenDataByTBNumber(string Ten_Brugge_Nr)
        {
            List<MolenType> MolenTypes = await _db.Table<MolenType>().ToListAsync();
            List<MolenType> NewAddedTypes = new List<MolenType>();
            try
            {
                Thread.Sleep(1000);
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
                            //Console.WriteLine($"{dt}: {dd}");
                            switch (dt.ToLower())
                            {
                                case "geo positie":
                                    string pattern = @"N:\s*([0-9.-]+),\s*O:\s*([0-9.-]+)";
                                    var match = Regex.Match(dd, pattern);
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
                                    if (MolenTypes.Concat(NewAddedTypes).Where(x => x.Name == molenType.Name).Count() == 0)
                                    {
                                        await _db.InsertAsync(molenType);
                                        molenType.Id = await _db.ExecuteScalarAsync<int>("SELECT last_insert_rowid()");
                                        NewAddedTypes.Add(molenType);
                                        newMolenData.ModelType.Add(molenType);
                                    }
                                    else if (newMolenData.ModelType.Find(x => x.Name == molenType.Name) == null)
                                    {
                                        molenType = MolenTypes.Concat(NewAddedTypes).Where(x => x.Name == molenType.Name).First();
                                        newMolenData.ModelType.Add(molenType);
                                    }
                                }
                                NewAddedTypes.AddRange(newMolenData.ModelType);
                            }
                        }
                        if (data.ContainsKey(name)) data[name] = NewAddedTypes;
                        else data.Add(name, NewAddedTypes);
                    }

                    if (Image != null)
                    {
                        var src = Image.First().GetAttributeValue("src", string.Empty);
                        if (!string.IsNullOrEmpty(src))
                        {
                            var imageResponse = await _client.GetAsync(src);
                            newMolenData.Image = await imageResponse.Content.ReadAsByteArrayAsync();
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

        public async Task<List<MolenTBN>> GetAllActiveTBNR()
        {
            await InitializeDB();
            List<MolenTBN> alleMolenTBNR = await _db.Table<MolenTBN>().ToListAsync();

            for (int i = 1; i <= 13; i++)
            {
                HttpResponseMessage response = await _client.GetAsync("https://www.molendatabase.nl/molens?mill_state%5Bstate%5D=existing&page=" + i);
                string responseBody = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);
                var divs = doc.DocumentNode.SelectNodes("//div[@class='mill_link']");
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
                                    await _db.InsertAsync(new MolenTBN() { Ten_Brugge_Nr = url });
                                }
                            }
                        }
                    }
                }
            }
            return await _db.Table<MolenTBN>().ToListAsync();
        }

        public async Task<List<MolenData>> GetAllMolenDataDB()
        {
            await InitializeDB();
            List<MolenData> MolenData = await _db.Table<MolenData>().ToListAsync();
            List<MolenData> MolenData_ = new List<MolenData>();
            string jsonString = File.ReadAllText("Json/CorrectMolenTypes.json");
            List<string> CorrectMolenTypes = JsonSerializer.Deserialize<List<string>>(jsonString);
            foreach (var molen in MolenData)
            {
                MolenData CompleteMolen = await GetFullDataOfMolen(molen);
                if (CorrectMolenTypes.Any(correct => molen.ModelType.Any(complete => complete.Name.ToLower() == correct.ToLower())))
                {
                    MolenData_.Add(CompleteMolen);
                }
            }
            return MolenData_;
        }

        public async Task<MolenData> GetFullDataOfMolen(MolenData molen)
        {
            molen.ModelType = await _db.QueryAsync<MolenType>("SELECT * FROM MolenType WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociation WHERE MolenDataId = ?)", new object[] { molen.Id });
            return molen;
        }

        public async Task<List<MolenTBN>> AddMolenTBNToDBFromJson()
        {
            await InitializeDB();
            List<MolenTBN> alleMolenTBNR = await _db.Table<MolenTBN>().ToListAsync();
            string jsonString = File.ReadAllText("Json/AlleActieveMolens.json");
            List<MolenTBN> molenList = JsonSerializer.Deserialize<List<MolenTBN>>(jsonString);
            foreach (var molen in molenList)
            {
                if (alleMolenTBNR.Where(x => x.Ten_Brugge_Nr == molen.Ten_Brugge_Nr).Count() == 0)
                {
                    await _db.InsertAsync(molen);
                }
            }
            return await _db.Table<MolenTBN>().ToListAsync();
        }

        public async Task<List<MolenData>> GetAllMolenLatLon()
        {
            List<MolenData> MolenData = await _db.QueryAsync<MolenData>("SELECT Ten_Brugge_Nr, North, East FROM MolenData");
            return MolenData;
        }

        public async Task<List<MolenType>> GetAllMolenTypes()
        {
            List<MolenType> MolenTypes = await _db.Table<MolenType>().ToListAsync();
            return MolenTypes;
        }

        public async Task<MolenData> GetMolenByTBN(string TBN)
        {
            MolenData MolenData = await _db.FindWithQueryAsync<MolenData>($"SELECT * FROM MolenData WHERE Ten_Brugge_Nr = '{TBN}'");
            return await GetFullDataOfMolen(MolenData);
        }

        public async Task<List<MolenData>> GetMolenDataByType(string type)
        {
            List<MolenData> MolenData = await _db.QueryAsync<MolenData>("SELECT * FROM MolenData WHERE Id IN " +
                "(SELECT MolenDataId FROM MolenTypeAssociation WHERE MolenTypeId IN " +
                "(SELECT Id FROM MolenType WHERE Name = ?))", new object[] { type.ToLower() });
            MolenData.ForEach(async x => await GetFullDataOfMolen(x));
            return MolenData;
        }
    }
}
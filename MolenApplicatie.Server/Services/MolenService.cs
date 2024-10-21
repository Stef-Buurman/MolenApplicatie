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
    public class MolenService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        readonly string baseUrl = "https://www.molendatabase.nl/molens/ten-bruggencate-nr-";
        private readonly HttpClient _client;
        private List<string> allowedTypes = new List<string>();

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

        public async Task<MolenData> GetFullDataOfMolen(MolenData molen)
        {
            if (molen == null) return null;
            molen.ModelType = await _db.QueryAsync<MolenType>("SELECT * FROM MolenType WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociation WHERE MolenDataId = ?)", new object[] { molen.Id });

            if (File.Exists($"{Globals.MolenImagesFolder}/{molen.Ten_Brugge_Nr}.jpg"))
            {
                byte[] image = await File.ReadAllBytesAsync($"{Globals.MolenImagesFolder}/{molen.Ten_Brugge_Nr}.jpg");
                molen.Image = new MolenImage(image, molen.Ten_Brugge_Nr);
            }

            if (Directory.Exists($"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}"))
            {
                string[] imageFiles = Directory.GetFiles($"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}", "*.*", SearchOption.TopDirectoryOnly)
                                               .Where(file => file.ToLower().EndsWith(".jpg") ||
                                                              file.ToLower().EndsWith(".jpeg") ||
                                                              file.ToLower().EndsWith(".png"))
                                               .ToArray();

                foreach (string imageFile in imageFiles)
                {
                    byte[] imageBytes = File.ReadAllBytes(imageFile);
                    molen.AddedImages.Add(new MolenImage(imageBytes, Path.GetFileName(imageFile), true));
                }
            }
            return molen;
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

        public async Task<IFormFile> SaveMolenImage(int id, string TBN, IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                var fileExtension = Path.GetExtension(file.FileName);
                if (fileExtension == null ||
                    (fileExtension.ToLower() != ".jpg"
                    && fileExtension.ToLower() != ".jpeg"
                    && fileExtension.ToLower() != ".png")) return null;
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                string folderName = $"MolenAddedImages/{TBN}";

                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                var FileDirectory = $"{folderName}/{Guid.NewGuid()}{fileExtension}";
                while (File.Exists(FileDirectory))
                {
                    FileDirectory = $"{folderName}/{Guid.NewGuid()}{fileExtension}";
                }
                File.WriteAllBytes(FileDirectory, imageBytes);
            }
            return file;
        }

        public async Task<(bool status, string message)> DeleteImageFromMolen(string tbNummer, string imgName)
        {
            MolenData molen = await GetMolenByTBN(tbNummer);
            if (molen == null) return (false, "Molen not found");
            if (molen.AddedImages.Find(x => x.Name == imgName) == null) return (false, "Image not found");
            if (File.Exists($"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}/{imgName}"))
            {
                File.Delete($"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}/{imgName}");
            }
            return (true, "Image deleted");
        }
    }
}
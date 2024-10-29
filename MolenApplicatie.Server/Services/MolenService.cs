using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Utils;
using MolenApplicatie.Models;

namespace MolenApplicatie.Server.Services
{
    public class MolenService : DbConnection
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        readonly string baseUrl = "https://www.molendatabase.nl/molens/ten-bruggencate-nr-";
        private readonly HttpClient _client;
        private readonly string folderNameMolenImages = $"wwwroot/MolenAddedImages";
        private List<string> allowedTypes = new List<string>();

        private DbConnection _db;

        public MolenService()
        {
            _client = new HttpClient();
            _db = new DbConnection();
        }

        public async Task<MolenData> GetFullDataOfMolen(MolenData molen)
        {
            if (molen == null) return null;

            molen.ModelType = await _db.QueryAsync<MolenType>("SELECT * FROM MolenType WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociation WHERE MolenDataId = ?)", new object[] { molen.Id });

            var mainImagePath = $"wwwroot/{Globals.MolenImagesFolder}/{molen.Ten_Brugge_Nr}.jpg";
            if (File.Exists(mainImagePath))
            {
                molen.Image = new MolenImage(mainImagePath, molen.Ten_Brugge_Nr);
            }

            var addedImagesFolder = $"wwwroot/{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}";
            if (Directory.Exists(addedImagesFolder))
            {
                string[] imageFiles = Directory.GetFiles(addedImagesFolder, "*.*", SearchOption.TopDirectoryOnly)
                                               .Where(file => file.ToLower().EndsWith(".jpg") ||
                                                              file.ToLower().EndsWith(".jpeg") ||
                                                              file.ToLower().EndsWith(".png"))
                                               .ToArray();

                foreach (string imageFile in imageFiles)
                {
                    molen.AddedImages.Add(new MolenImage(imageFile, Path.GetFileName(imageFile), true));
                }
            }
            return molen;
        }

        public async Task<List<MolenData>> GetAllMolenLatLon()
        {
            List<MolenData> MolenData = await _db.QueryAsync<MolenData>("SELECT Id, Ten_Brugge_Nr, North, East FROM MolenData");
            for (int i = 0; i < MolenData.Count; i++)
            {
                MolenData[i].ModelType = await _db.QueryAsync<MolenType>("SELECT * FROM MolenType WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociation WHERE MolenDataId = ?)", new object[] { MolenData[i].Id });
                if (Directory.Exists(folderNameMolenImages) && Directory.Exists(folderNameMolenImages + "/" + MolenData[i].Ten_Brugge_Nr) 
                    && Directory.EnumerateFiles(folderNameMolenImages + "/" + MolenData[i].Ten_Brugge_Nr).Any())
                {
                    MolenData[i].HasImage = true;
                }
                else
                {
                    MolenData[i].HasImage = false;
                }
            }
            return MolenData;
        }

        public async Task<List<MolenType>> GetAllMolenTypes()
        {
            List<MolenType> MolenTypes = await _db.Table<MolenType>();
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

        public async Task<(IFormFile file, string errorMessage)> SaveMolenImage(int id, string TBN, IFormFile file)
        {
            var maxSavedFilesAmount = 5;
            using (var memoryStream = new MemoryStream())
            {
                string folderName = folderNameMolenImages;

                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                folderName += "/" + TBN;

                if (Directory.Exists(folderName) && Directory.GetFiles(folderName).Length >= maxSavedFilesAmount)
                {
                    return (null, "Er zijn al te veel foto's opgeslagen voor de molen met ten bruggencate nummer: " + TBN);
                }

                var fileExtension = Path.GetExtension(file.FileName);
                if (fileExtension == null ||
                    (fileExtension.ToLower() != ".jpg"
                    && fileExtension.ToLower() != ".jpeg"
                    && fileExtension.ToLower() != ".png")) return (null, "Dit soort bestand wordt niet ondersteund!");
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

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
            return (file, "");
        }

        public async Task<(bool status, string message)> DeleteImageFromMolen(string tbNummer, string imgName)
        {
            MolenData molen = await GetMolenByTBN(tbNummer);
            if (molen == null) return (false, "Molen not found");
            if (molen.AddedImages.Find(x => x.Name == imgName) == null) return (false, "Image not found");
            if (File.Exists($"wwwroot/{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}/{imgName}"))
            {
                File.Delete($"wwwroot/{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}/{imgName}");
            }
            return (true, "Image deleted");
        }
    }
}
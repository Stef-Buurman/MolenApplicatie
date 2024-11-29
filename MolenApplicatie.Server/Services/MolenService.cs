using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Utils;
using MolenApplicatie.Models;

namespace MolenApplicatie.Server.Services
{
    public class MolenService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        private readonly HttpClient _client;
        private readonly string folderNameMolenImages = $"wwwroot/MolenAddedImages";
        private List<string> allowedTypes = new List<string>();

        private DbConnection _db;

        public MolenService()
        {
            _client = new HttpClient();
            _db = new DbConnection(Globals.DBAlleMolens);
        }

        public async Task<List<MolenData>> GetAllMolenData()
        {
            List<MolenData> MolenData = await _db.Table<MolenData>();
            List<MolenType> MolenTypes = await _db.Table<MolenType>();
            List<MolenTypeAssociation> MolenTypeAssociations = await _db.Table<MolenTypeAssociation>();
            List<MolenYearInfo> MolenYearInfos = await _db.Table<MolenYearInfo>();

            return GetFullDataOfAllMolens(MolenData, MolenTypes, MolenTypeAssociations, MolenYearInfos);
        }

        public async Task<List<MolenData>> GetAllActiveMolenData()
        {
            List<MolenData> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "werkend").ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenData>> GetAllMolenDataCorrectTypes()
        {
            List<MolenData> alleMolenData = await GetAllMolenData();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => Globals.AllowedMolenTypes.Any(x => molen.ModelType.Count() > 0 && molen.ModelType.Any(y => y.Name.ToLower() == x.ToLower()))).ToList();
            return GefilterdeMolenData;
        }


        public async Task<List<MolenData>> GetAllExistingMolens()
        {
            List<MolenData> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() != "verdwenen").ToList();
            return GefilterdeMolenData;
        }


        public async Task<List<MolenData>> GetAllDisappearedMolens(string provincie)
        {
            List<MolenData> alleMolenData = await GetAllMolenData();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "verdwenen" && molen.Provincie == provincie).ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenData>> GetAllRemainderMolens()
        {
            List<MolenData> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "restant").ToList();
            return GefilterdeMolenData;
        }

        public List<MolenData> GetFullDataOfAllMolens(List<MolenData> MolenData, List<MolenType> MolenTypes, List<MolenTypeAssociation> MolenTypeAssociations, List<MolenYearInfo> MolenYearInfos)
        {
            for (int i = 0; i < MolenData.Count; i++)
            {
                //MolenData[i] = await GetFullDataOfMolen(MolenData[i]);
                var associations = MolenTypeAssociations.FindAll(type => type.MolenDataId == MolenData[i].Id);
                MolenData[i].ModelType = MolenTypes.FindAll(type => associations.Find(assoc => assoc.MolenTypeId == type.Id) != null);
                var mainImagePath = $"{Globals.MolenImagesFolder}/{MolenData[i].Ten_Brugge_Nr}.jpg";
                if (File.Exists(mainImagePath))
                {
                    MolenData[i].Image = new MolenImage(mainImagePath.Replace("wwwroot/", ""), MolenData[i].Ten_Brugge_Nr);
                }

                var addedImagesFolder = $"{Globals.MolenAddedImagesFolder}/{MolenData[i].Ten_Brugge_Nr}";
                if (MolenData[i].CanAddImages && Directory.Exists(addedImagesFolder))
                {
                    string[] imageFiles = Directory.GetFiles(addedImagesFolder, "*.*", SearchOption.TopDirectoryOnly)
                                                   .Where(file => file.ToLower().EndsWith(".jpg") ||
                                                                  file.ToLower().EndsWith(".jpeg") ||
                                                                  file.ToLower().EndsWith(".png"))
                                                   .ToArray();

                    foreach (string imageFile in imageFiles)
                    {
                        var gottenDate = GetDateTakenOfImage.GetDateTaken(imageFile);
                        MolenData[i].AddedImages.Add(new MolenImage(imageFile.Replace("wwwroot/", ""), Path.GetFileName(imageFile), true, gottenDate));
                    }

                    if (imageFiles.Count() > 0)
                    {
                        MolenData[i].HasImage = true;
                    }
                    else
                    {
                        MolenData[i].HasImage = false;
                    }
                }

                MolenData[i].AddedDisappearedYears = MolenYearInfos.FindAll(info => info.MolenDataId == MolenData[i].Id);

            }
            return MolenData;
        }

        public async Task<MolenData> GetFullDataOfMolen(MolenData molen)
        {
            if (molen == null) return null;

            molen.ModelType = await _db.QueryAsync<MolenType>("SELECT * FROM MolenType WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociation WHERE MolenDataId = ?)", new object[] { molen.Id });

            var mainImagePath = $"{Globals.MolenImagesFolder}/{molen.Ten_Brugge_Nr}.jpg";
            if (File.Exists(mainImagePath))
            {
                molen.Image = new MolenImage(mainImagePath.Replace("wwwroot/", ""), molen.Ten_Brugge_Nr);
            }

            var addedImagesFolder = $"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}";
            if (molen.CanAddImages && Directory.Exists(addedImagesFolder))
            {
                string[] imageFiles = Directory.GetFiles(addedImagesFolder, "*.*", SearchOption.TopDirectoryOnly)
                                               .Where(file => file.ToLower().EndsWith(".jpg") ||
                                                              file.ToLower().EndsWith(".jpeg") ||
                                                              file.ToLower().EndsWith(".png"))
                                               .ToArray();

                foreach (string imageFile in imageFiles)
                {
                    var gottenDate = GetDateTakenOfImage.GetDateTaken(imageFile);
                    molen.AddedImages.Add(new MolenImage(imageFile.Replace("wwwroot/", ""), Path.GetFileName(imageFile), true, gottenDate));
                }

                if (imageFiles.Count() > 0)
                {
                    molen.HasImage = true;
                }
                else
                {
                    molen.HasImage = false;
                }
            }

            molen.AddedDisappearedYears = await _db.QueryAsync<MolenYearInfo>("SELECT * FROM MolenYearInfo WHERE MolenDataId = ?", new object[] { molen.Id });

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

        public bool CanMolenHaveAddedImages(MolenData molen)
        {
            return molen.Toestand.ToLower() != "verdwenen";
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
            if (File.Exists($"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}/{imgName}"))
            {
                File.Delete($"{Globals.MolenAddedImagesFolder}/{molen.Ten_Brugge_Nr}/{imgName}");
            }
            return (true, "Image deleted");
        }
    }
}
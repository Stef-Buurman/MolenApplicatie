using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Utils;

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

        public async Task<List<string>> GetAllMolenProvincies()
        {
            var provincies = await _db.QueryAsync<Provincie>("SELECT DISTINCT Provincie AS Name FROM MolenData");
            return provincies.Select(p => p.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
        }

        public async Task<List<MolenData>> GetAllMolenDataByProvincie(string provincie)
        {
            List<MolenData> alleMolenData = await GetAllMolenData();
            List<MolenData> MolenDataByProvincie = alleMolenData.Where(molen => molen.Provincie != null && molen.Provincie.ToLower() == provincie.ToLower()).ToList();
            return MolenDataByProvincie;
        }

        public async Task<List<MolenData>> GetAllMolenData()
        {
            List<MolenData> MolenData = await _db.Table<MolenData>();

            return await GetAllMolenData(MolenData);
        }

        public async Task<List<MolenData>> GetAllMolenData(List<MolenData> MolenData)
        {
            var molenIds = MolenData.Select(x => x.Id).ToList();
            List<MolenTypeAssociation> MolenTypeAssociations = await _db.QueryAsync<MolenTypeAssociation>(
                    $"SELECT * FROM MolenTypeAssociation WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            var molenTypeIds = MolenData.Select(x => x.Id).ToList();
            List<MolenType> MolenTypes = await _db.QueryAsync<MolenType>(
                    $"SELECT * FROM MolenType WHERE Id IN ({string.Join(',', molenTypeIds)})");

            List<VerdwenenYearInfo> MolenYearInfos = await _db.QueryAsync<VerdwenenYearInfo>(
                    $"SELECT * FROM VerdwenenYearInfo WHERE Id IN ({string.Join(',', molenIds)})");

            List<MolenImage> AllMolenImages = await _db.QueryAsync<MolenImage>(
                    $"SELECT * FROM MolenImage WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            List<AddedImage> AllAddedMolenImages = await _db.QueryAsync<AddedImage>(
                    $"SELECT * FROM AddedImage WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            List<MolenMaker> AllMolenMakers = await _db.QueryAsync<MolenMaker>(
                    $"SELECT * FROM MolenMaker WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            return await GetFullDataOfAllMolens(MolenData, MolenTypes, MolenTypeAssociations, MolenYearInfos, AllMolenImages, AllAddedMolenImages, AllMolenMakers);
        }

        public async Task<List<MolenData>> GetAllActiveMolenData()
        {
            List<MolenData> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "werkend").ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenData>> GetAllMolenDataCorrectTypes()
        {
            string allowedMolenTypes = string.Join(", ", Globals.AllowedMolenTypes.Select(x => $"'{x}'"));

            List<MolenData> alleMolenData = await _db.QueryAsync<MolenData>(
                @"SELECT * FROM MolenData WHERE Id IN 
                (SELECT MolenDataId FROM MolenTypeAssociation WHERE MolenTypeId IN 
                    (SELECT Id FROM MolenType WHERE LOWER(Name) IN 
                        (" + allowedMolenTypes + ")))");

            return await GetAllMolenData(alleMolenData);
        }


        public async Task<List<MolenData>> GetAllExistingMolens()
        {
            List<MolenData> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() != "verdwenen").ToList();
            return GefilterdeMolenData;
        }


        public async Task<List<MolenData>> GetAllDisappearedMolens(string provincie)
        {
            List<MolenData> alleMolenData = await _db.QueryAsync<MolenData>(
                @"SELECT * FROM MolenData WHERE LOWER(Toestand) == 'verdwenen' AND LOWER(Provincie) = ?", new object[] { provincie.ToLower() });
            return await GetAllMolenData(alleMolenData);
        }

        public async Task<List<MolenData>> GetAllRemainderMolens()
        {
            List<MolenData> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "restant").ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenData>> GetFullDataOfAllMolens(List<MolenData> MolenData, List<MolenType> MolenTypes, List<MolenTypeAssociation> MolenTypeAssociations, List<VerdwenenYearInfo> VerdwenenYearInfo, List<MolenImage> AllMolenImages, List<AddedImage> AllAddedMolenImages, List<MolenMaker> AllMolenMakers)
        {
            for (int i = 0; i < MolenData.Count; i++)
            {
                var associations = MolenTypeAssociations.FindAll(type => type.MolenDataId == MolenData[i].Id);
                MolenData[i].ModelType = MolenTypes.FindAll(type => associations.Find(assoc => assoc.MolenTypeId == type.Id) != null);
                MolenData[i].Images = AllMolenImages.FindAll(image => image.MolenDataId == MolenData[i].Id);
                MolenData[i].AddedImages = AllAddedMolenImages.FindAll(image => image.MolenDataId == MolenData[i].Id);
                MolenData[i].MolenMakers = AllMolenMakers.FindAll(maker => maker.MolenDataId == MolenData[i].Id);
                var imagePath = $"{Globals.MolenImagesFolder}/{MolenData[i].Ten_Brugge_Nr}";
                if (Directory.Exists(imagePath))
                {
                    var foundFiles = Directory.GetFiles(imagePath);
                    if (foundFiles != null)
                    {
                        for (int j = 0; j < foundFiles.Length; j++)
                        {
                            if (MolenData[i].Images == null)
                            {
                                MolenData[i].Images = new List<MolenImage>();
                            }
                            if (File.Exists(foundFiles[j]))
                            {
                                var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(foundFiles[j]));
                                var filePath = CreateCleanPath.CreatePathWithoutWWWROOT(foundFiles[j]);

                                if (MolenData[i].Images.Find(x => x.Name == fileName && x.FilePath == filePath) == null)
                                {
                                    var addedMolenImage = new MolenImage
                                    {
                                        FilePath = filePath,
                                        Name = fileName,
                                        CanBeDeleted = false,
                                        MolenDataId = MolenData[i].Id
                                    };
                                    await _db.InsertAsync(addedMolenImage);
                                    MolenData[i].Images.Add(addedMolenImage);
                                }
                            }
                            else if (MolenData[i].Images != null && MolenData[i].Images.Count >= 0)
                            {
                                foreach (var image in MolenData[i].Images)
                                {
                                    await _db.DeleteAsync(image);
                                }
                            }
                        }
                    }
                    else if ((foundFiles == null || foundFiles.Length == 0) && MolenData[i].Images.Count >= 0)
                    {
                        foreach (var image in MolenData[i].Images)
                        {
                            await _db.DeleteAsync(image);
                        }
                    }
                }

                foreach(AddedImage addedImg in MolenData[i].AddedImages)
                {
                    if(!File.Exists(CreateCleanPath.CreatePathToWWWROOT(addedImg.FilePath)))
                    {
                        await _db.DeleteAsync(addedImg);
                    }
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
                        string imageFilePath = CreateCleanPath.CreatePathWithoutWWWROOT(imageFile);
                        string imageFileName = Path.GetFileNameWithoutExtension(imageFile);
                        if (MolenData[i].AddedImages.Find(img=> img.FilePath == imageFilePath && img.Name == imageFileName) == null)
                        {
                            AddedImage AddedMolenImage = new AddedImage
                            {
                                FilePath = imageFilePath,
                                Name = imageFileName,
                                CanBeDeleted = true,
                                DateTaken = gottenDate,
                                MolenDataId = MolenData[i].Id
                            };
                            await _db.InsertAsync(AddedMolenImage);
                            MolenData[i].AddedImages.Add(AddedMolenImage);
                        }
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

                MolenData[i].DisappearedYears = VerdwenenYearInfo.FindAll(info => info.MolenDataId == MolenData[i].Id);

            }
            return MolenData;
        }

        public async Task<MolenData> GetFullDataOfMolen(MolenData molen)
        {
            if (molen == null) return null;

            molen.ModelType = await _db.QueryAsync<MolenType>("SELECT * FROM MolenType WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociation WHERE MolenDataId = ?)", new object[] { molen.Id });

            molen.Images = await _db.QueryAsync<MolenImage>("SELECT * FROM MolenImage WHERE MolenDataId = ?", new object[] { molen.Id });
            molen.AddedImages = await _db.QueryAsync<AddedImage>("SELECT * FROM AddedImage WHERE MolenDataId = ?", new object[] { molen.Id });
            molen.MolenMakers = await _db.QueryAsync<MolenMaker>("SELECT * FROM MolenMaker WHERE MolenDataId = ?", new object[] { molen.Id });

            var imagePath = $"{Globals.MolenImagesFolder}/{molen.Ten_Brugge_Nr}";
            if (Directory.Exists(imagePath))
            {
                var foundFiles = Directory.GetFiles(imagePath);
                if (foundFiles != null)
                {
                    for (int j = 0; j < foundFiles.Length; j++)
                    {
                        if (molen.Images == null)
                        {
                            molen.Images = new List<MolenImage>();
                        }
                        if (File.Exists(foundFiles[j]))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(foundFiles[j]));
                            var filePath = CreateCleanPath.CreatePathWithoutWWWROOT(foundFiles[j]);

                            if (molen.Images.Find(x => x.Name == fileName && x.FilePath == filePath) == null)
                            {
                                var addedMolenImage = new MolenImage
                                {
                                    FilePath = filePath,
                                    Name = fileName,
                                    CanBeDeleted = false,
                                    MolenDataId = molen.Id
                                };
                                await _db.InsertAsync(addedMolenImage);
                                molen.Images.Add(addedMolenImage);
                            }
                        }
                        else if (molen.Images != null && molen.Images.Count >= 0)
                        {
                            foreach (var image in molen.Images)
                            {
                                await _db.DeleteAsync(image);
                            }
                        }
                    }
                }
                else if ((foundFiles == null || foundFiles.Length == 0) && molen.Images.Count >= 0)
                {
                    foreach (var image in molen.Images)
                    {
                        await _db.DeleteAsync(image);
                    }
                }
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
                    string imageFilePath = CreateCleanPath.CreatePathWithoutWWWROOT(imageFile);
                    string imageFileName = Path.GetFileNameWithoutExtension(imageFile);
                    if (molen.AddedImages.Find(img => img.FilePath == imageFilePath && img.Name == imageFileName) == null)
                    {
                        AddedImage AddedMolenImage = new AddedImage
                        {
                            FilePath = imageFilePath,
                            Name = imageFileName,
                            CanBeDeleted = true,
                            DateTaken = gottenDate
                        };
                        await _db.InsertAsync(AddedMolenImage);
                        molen.AddedImages.Add(AddedMolenImage);
                    }
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

            molen.DisappearedYears = await _db.QueryAsync<VerdwenenYearInfo>("SELECT * FROM VerdwenenYearInfo WHERE MolenDataId = ?", new object[] { molen.Id });

            molen.MolenMakers = await _db.QueryAsync<MolenMaker>("SELECT * FROM MolenMaker WHERE MolenDataId = ?", new object[] { molen.Id });

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
                var FileDirectory = $"{folderName}/{GetFileNameForImage.GetFileName()}{fileExtension}";
                while (File.Exists(FileDirectory))
                {
                    FileDirectory = $"{folderName}/{GetFileNameForImage.GetFileName()}{fileExtension}";
                }
                File.WriteAllBytes(FileDirectory, imageBytes);
                await _db.InsertAsync(new AddedImage
                {
                    FilePath = CreateCleanPath.CreatePathWithoutWWWROOT(FileDirectory),
                    Name = Path.GetFileNameWithoutExtension(FileDirectory),
                    DateTaken = GetDateTakenOfImage.GetDateTaken(FileDirectory),
                    CanBeDeleted = true,
                    MolenDataId = id
                });
            }
            return (file, "");
        }

        public async Task<(bool status, string message)> DeleteImageFromMolen(string tbNummer, string imgName)
        {
            MolenData molen = await GetMolenByTBN(tbNummer);
            if (molen == null) return (false, "Molen not found");
            var molenImageToDelete = molen.Images.Find(x => x.Name == imgName);
            var molenAddedImageToDelete = molen.AddedImages.Find(x => x.Name == imgName);
            if (molenImageToDelete == null && molenAddedImageToDelete == null) return (false, "Images not found");
            if (molenImageToDelete != null && File.Exists(CreateCleanPath.CreatePathToWWWROOT(molenImageToDelete.FilePath)))
            {
                File.Delete(CreateCleanPath.CreatePathToWWWROOT(molenImageToDelete.FilePath));
                await _db.DeleteAsync(molenImageToDelete);
            }else if (molenAddedImageToDelete != null && File.Exists(CreateCleanPath.CreatePathToWWWROOT(molenAddedImageToDelete.FilePath)))
            {
                File.Delete(CreateCleanPath.CreatePathToWWWROOT(molenAddedImageToDelete.FilePath));
                await _db.DeleteAsync(molenAddedImageToDelete);
            }
            return (true, "Images deleted");
        }
    }
}
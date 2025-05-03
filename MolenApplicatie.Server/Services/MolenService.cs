using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services
{
    public class MolenService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        private readonly HttpClient _client;
        private readonly string folderNameMolenImages = $"wwwroot/MolenAddedImages";
        private List<string> allowedTypes = new List<string>();
        private readonly MolenDbContext _dbContext;

        private DbConnection _db;

        public MolenService(MolenDbContext dbContext)
        {
            _client = new HttpClient();
            _db = new DbConnection(Globals.DBAlleMolens);
            _dbContext = dbContext;
        }

        public MolenData GetMolenData(MolenData molen)
        {
            molen.ModelTypes = molen.MolenTypeAssociations
                .Where(a => a.MolenType != null)
                .Select(a =>
                {
                    a.MolenType.MolenTypeAssociations = null;
                    return a.MolenType;
                }).ToList();
            return molen;
        }

        public async Task<List<string>> GetAllMolenProvincies()
        {
            var provincies = await _db.QueryAsync<Provincie>("SELECT DISTINCT Provincie AS Name FROM MolenDataOld");
            return provincies.Select(p => p.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
        }

        public async Task<List<MolenDataOld>> GetAllMolenDataByProvincie(string provincie)
        {
            List<MolenDataOld> alleMolenData = await GetAllMolenData();
            List<MolenDataOld> MolenDataByProvincie = alleMolenData.Where(molen => molen.Provincie != null && molen.Provincie.ToLower() == provincie.ToLower()).ToList();
            return MolenDataByProvincie;
        }

        public async Task<List<MolenDataOld>> GetAllMolenData()
        {
            List<MolenDataOld> MolenData = await _db.Table<MolenDataOld>();

            return await GetAllMolenData(MolenData);
            //return _dbContext.MolenData.ToList();
        }

        public async Task<List<MolenDataOld>> GetAllMolenData(List<MolenDataOld> MolenData)
        {
            var molenIds = MolenData.Select(x => x.Id).ToList();
            List<MolenTypeAssociationOld> MolenTypeAssociations = await _db.QueryAsync<MolenTypeAssociationOld>(
                    $"SELECT * FROM MolenTypeAssociationOld WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            var molenTypeIds = MolenData.Select(x => x.Id).ToList();
            List<MolenTypeOld> MolenTypes = await _db.QueryAsync<MolenTypeOld>(
                    $"SELECT * FROM MolenTypeOld WHERE Id IN ({string.Join(',', molenTypeIds)})");

            List<VerdwenenYearInfoOld> MolenYearInfos = await _db.QueryAsync<VerdwenenYearInfoOld>(
                    $"SELECT * FROM VerdwenenYearInfoOld WHERE Id IN ({string.Join(',', molenIds)})");

            List<MolenImageOld> AllMolenImages = await _db.QueryAsync<MolenImageOld>(
                    $"SELECT * FROM MolenImageOld WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            List<AddedImageOld> AllAddedMolenImages = await _db.QueryAsync<AddedImageOld>(
                    $"SELECT * FROM AddedImageOld WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            List<MolenMakerOld> AllMolenMakers = await _db.QueryAsync<MolenMakerOld>(
                    $"SELECT * FROM MolenMakerOld WHERE MolenDataId IN ({string.Join(',', molenIds)})");

            return await GetFullDataOfAllMolens(MolenData, MolenTypes, MolenTypeAssociations, MolenYearInfos, AllMolenImages, AllAddedMolenImages, AllMolenMakers);
        }

        public async Task<List<MolenDataOld>> GetAllActiveMolenData()
        {
            List<MolenDataOld> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenDataOld> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "werkend").ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenDataOld>> GetAllMolenDataCorrectTypes()
        {
            string allowedMolenTypes = string.Join(", ", Globals.AllowedMolenTypes.Select(x => $"'{x}'"));

            List<MolenDataOld> alleMolenData = await _db.QueryAsync<MolenDataOld>(
                @"SELECT * FROM MolenDataOld WHERE Id IN 
                (SELECT MolenDataId FROM MolenTypeAssociationOld WHERE MolenTypeId IN 
                    (SELECT Id FROM MolenTypeOld WHERE LOWER(Name) IN 
                        (" + allowedMolenTypes + ")))");

            return await GetAllMolenData(alleMolenData);
        }


        public async Task<List<MolenDataOld>> GetAllExistingMolens()
        {
            List<MolenDataOld> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenDataOld> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() != "verdwenen").ToList();
            return GefilterdeMolenData;
        }


        public async Task<List<MolenDataOld>> GetAllDisappearedMolens(string provincie)
        {
            List<MolenDataOld> alleMolenData = await _db.QueryAsync<MolenDataOld>(
                @"SELECT * FROM MolenDataOld WHERE LOWER(Toestand) == 'verdwenen' AND LOWER(Provincie) = ?", new object[] { provincie.ToLower() });
            return await GetAllMolenData(alleMolenData);
        }

        public async Task<List<MolenDataOld>> GetAllRemainderMolens()
        {
            List<MolenDataOld> alleMolenData = await GetAllMolenDataCorrectTypes();
            List<MolenDataOld> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == "restant").ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenDataOld>> GetFullDataOfAllMolens(List<MolenDataOld> MolenData, List<MolenTypeOld> MolenTypes, List<MolenTypeAssociationOld> MolenTypeAssociations, List<VerdwenenYearInfoOld> VerdwenenYearInfo, List<MolenImageOld> AllMolenImages, List<AddedImageOld> AllAddedMolenImages, List<MolenMakerOld> AllMolenMakers)
        {
            for (int i = 0; i < MolenData.Count; i++)
            {
                var associations = MolenTypeAssociations.FindAll(type => type.MolenDataId == MolenData[i].Id);
                MolenData[i].ModelType = MolenTypes.FindAll(type => associations.Find(assoc => assoc.MolenTypeId == type.Id) != null);
                MolenData[i].Images = AllMolenImages.FindAll(image => image.MolenDataId == MolenData[i].Id);
                var duplicateImages = MolenData[i].Images.GroupBy(x => x.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToList();
                foreach (var duplicateImage in duplicateImages)
                {
                    await _db.DeleteAsync(duplicateImage);
                }
                MolenData[i].AddedImages = AllAddedMolenImages.FindAll(image => image.MolenDataId == MolenData[i].Id);
                var duplicateAddedImages = MolenData[i].AddedImages.GroupBy(x => x.Name).Where(g => g.Count() > 1).SelectMany(g => g).ToList();
                foreach (var duplicateImage in duplicateAddedImages)
                {
                    await _db.DeleteAsync(duplicateImage);
                }
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
                                MolenData[i].Images = new List<MolenImageOld>();
                            }
                            if (File.Exists(foundFiles[j]))
                            {
                                var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(foundFiles[j]));
                                var filePath = CreateCleanPath.CreatePathWithoutWWWROOT(foundFiles[j]);

                                if (MolenData[i].Images.Find(x => x.Name == fileName && x.FilePath == filePath) == null)
                                {
                                    var addedMolenImage = new MolenImageOld
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

                foreach (MolenImage IMG in MolenData[i].Images)
                {
                    if (!File.Exists(CreateCleanPath.CreatePathToWWWROOT(IMG.FilePath)))
                    {
                        await _db.DeleteAsync(IMG);
                    }
                }

                foreach (AddedImageOld addedImg in MolenData[i].AddedImages)
                {
                    if (!File.Exists(CreateCleanPath.CreatePathToWWWROOT(addedImg.FilePath)))
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
                        if (MolenData[i].AddedImages.Find(img => img.FilePath == imageFilePath && img.Name == imageFileName) == null)
                        {
                            AddedImageOld AddedMolenImage = new AddedImageOld
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

                if (MolenData[i].AddedImages.Count > 0)
                {
                    foreach (AddedImageOld addedImg in MolenData[i].AddedImages)
                    {
                        if (addedImg.DateTaken == null)
                        {
                            addedImg.DateTaken = GetDateTakenOfImage.GetDateTaken(addedImg.FilePath);
                            await _db.UpdateAsync(addedImg);
                        }
                    }
                }

                MolenData[i].DisappearedYears = VerdwenenYearInfo.FindAll(info => info.MolenDataId == MolenData[i].Id);

            }
            return MolenData;
        }

        public async Task<MolenDataOld> GetFullDataOfMolen(MolenDataOld molen)
        {
            if (molen == null) return null;

            molen.ModelType = await _db.QueryAsync<MolenTypeOld>("SELECT * FROM MolenTypeOld WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociationOld WHERE MolenDataId = ?)", new object[] { molen.Id });

            molen.Images = await _db.QueryAsync<MolenImageOld>("SELECT * FROM MolenImageOld WHERE MolenDataId = ?", new object[] { molen.Id });
            molen.AddedImages = await _db.QueryAsync<AddedImageOld>("SELECT * FROM AddedImageOld WHERE MolenDataId = ?", new object[] { molen.Id });
            molen.MolenMakers = await _db.QueryAsync<MolenMakerOld>("SELECT * FROM MolenMakerOld WHERE MolenDataId = ?", new object[] { molen.Id });

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
                            molen.Images = new List<MolenImageOld>();
                        }
                        if (File.Exists(foundFiles[j]))
                        {
                            var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(foundFiles[j]));
                            var filePath = CreateCleanPath.CreatePathWithoutWWWROOT(foundFiles[j]);

                            if (molen.Images.Find(x => x.Name == fileName && x.FilePath == filePath) == null)
                            {
                                var addedMolenImage = new MolenImageOld
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
                        AddedImageOld AddedMolenImage = new AddedImageOld
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

            molen.DisappearedYears = await _db.QueryAsync<VerdwenenYearInfoOld>("SELECT * FROM DisappearedYearInfo WHERE MolenDataId = ?", new object[] { molen.Id });

            molen.MolenMakers = await _db.QueryAsync<MolenMakerOld>("SELECT * FROM MolenMakerOld WHERE MolenDataId = ?", new object[] { molen.Id });

            return molen;
        }

        public async Task<List<MolenDataOld>> GetAllMolenLatLon()
        {
            List<MolenDataOld> MolenData = await _db.QueryAsync<MolenDataOld>("SELECT Id, Ten_Brugge_Nr, North, East FROM MolenDataOld");
            for (int i = 0; i < MolenData.Count; i++)
            {
                MolenData[i].ModelType = await _db.QueryAsync<MolenTypeOld>("SELECT * FROM MolenTypeOld WHERE Id IN " +
                "(SELECT MolenTypeId FROM MolenTypeAssociationOld WHERE MolenDataId = ?)", new object[] { MolenData[i].Id });
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

        public async Task<List<MolenTypeOld>> GetAllMolenTypes()
        {
            List<MolenTypeOld> MolenTypes = await _db.Table<MolenTypeOld>();
            return MolenTypes;
        }

        public async Task<MolenDataOld> GetMolenByTBN(string TBN)
        {
            MolenDataOld MolenData = await _db.FindWithQueryAsync<MolenDataOld>($"SELECT * FROM MolenDataOld WHERE Ten_Brugge_Nr = '{TBN}'");
            return await GetFullDataOfMolen(MolenData);
        }

        public async Task<List<MolenDataOld>> GetMolenDataByType(string type)
        {
            List<MolenDataOld> MolenData = await _db.QueryAsync<MolenDataOld>("SELECT * FROM MolenDataOld WHERE Id IN " +
                "(SELECT MolenDataId FROM MolenTypeAssociationOld WHERE MolenTypeId IN " +
                "(SELECT Id FROM MolenTypeOld WHERE Name = ?))", new object[] { type.ToLower() });
            MolenData.ForEach(async x => await GetFullDataOfMolen(x));
            return MolenData;
        }

        public bool CanMolenHaveAddedImages(MolenDataOld molen)
        {
            return molen.Toestand.ToLower() != "verdwenen";
        }

        public async Task<(IFormFile file, string errorMessage)> SaveMolenImage(int id, string TBN, IFormFile file)
        {
            var maxSavedFilesCount = 5;
            using (var memoryStream = new MemoryStream())
            {
                string folderName = folderNameMolenImages;

                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                folderName += "/" + TBN;

                if (Directory.Exists(folderName) && Directory.GetFiles(folderName).Length >= maxSavedFilesCount)
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
                await _db.InsertAsync(new AddedImageOld
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
            MolenDataOld molen = await GetMolenByTBN(tbNummer);
            if (molen == null) return (false, "Molen not found");
            var molenImageToDelete = molen.Images.Find(x => x.Name == imgName);
            var molenAddedImageToDelete = molen.AddedImages.Find(x => x.Name == imgName);
            if (molenImageToDelete == null && molenAddedImageToDelete == null) return (false, "Images not found");
            if (molenImageToDelete != null && File.Exists(CreateCleanPath.CreatePathToWWWROOT(molenImageToDelete.FilePath)))
            {
                File.Delete(CreateCleanPath.CreatePathToWWWROOT(molenImageToDelete.FilePath));
                await _db.DeleteAsync(molenImageToDelete);
            }
            else if (molenAddedImageToDelete != null && File.Exists(CreateCleanPath.CreatePathToWWWROOT(molenAddedImageToDelete.FilePath)))
            {
                File.Delete(CreateCleanPath.CreatePathToWWWROOT(molenAddedImageToDelete.FilePath));
                await _db.DeleteAsync(molenAddedImageToDelete);
            }
            return (true, "Images deleted");
        }

        private async Task<int> GetCountOfActiveMolensWithImages() => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MolenDataOld WHERE Toestand = 'Werkend' AND Id IN " +"(SELECT MolenDataId FROM MolenImageOld)");

        private async Task<int> GetCountOfRemainderMolensWithImage() => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MolenDataOld WHERE Toestand = 'Restant' AND Id IN " +"(SELECT MolenDataId FROM MolenImageOld)");

        private async Task<int> GetCountOfActiveMolens() => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MolenDataOld WHERE Toestand = 'Werkend'");

        private async Task<int> GetCountOfRemainderMolens() => await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MolenDataOld WHERE Toestand = 'Restant'");

        private async Task<List<CountDisappearedMolensOld>> GetCountOfDisappearedMolens()
        {
            List<string> provincies = await GetAllMolenProvincies();
            List<CountDisappearedMolensOld> disappearedMolens = new List<CountDisappearedMolensOld>();
            foreach (string provincie in provincies)
            {
                Console.WriteLine(provincie);
                int count = await _db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM MolenDataOld WHERE Toestand = 'Verdwenen' AND Provincie = ?", new object[] { provincie });
                disappearedMolens.Add(new CountDisappearedMolensOld
                {
                    Provincie = provincie,
                    Count = count
                });
            }
            return disappearedMolens;
        }


        public async Task<MolensResponseTypeOld> MolensResponse(List<MolenDataOld> molens)
        {
            int activeMolensWithImage = await GetCountOfActiveMolensWithImages();
            int remainderMolensWithImage = await GetCountOfRemainderMolensWithImage();
            int totalMolensWithImage = activeMolensWithImage + remainderMolensWithImage;

            int totalActiveMolens = await GetCountOfActiveMolens();
            int totalRemainderMolens = await GetCountOfRemainderMolens();
            int totalExistingMolens = totalActiveMolens + totalRemainderMolens;

            List<CountDisappearedMolensOld> totalDisappearedMolens = await GetCountOfDisappearedMolens();

            int totalMolens = totalMolensWithImage + totalExistingMolens;
            totalDisappearedMolens.ForEach(x => totalMolens += x.Count);
            return new MolensResponseTypeOld
            {
                Molens = molens,
                ActiveMolensWithImage = activeMolensWithImage,
                RemainderMolensWithImage = remainderMolensWithImage,
                TotalMolensWithImage = totalMolensWithImage,
                TotalCountActiveMolens = totalActiveMolens,
                TotalCountRemainderMolens = totalRemainderMolens,
                TotalCountExistingMolens = totalExistingMolens,
                TotalCountDisappearedMolens = totalDisappearedMolens,
                TotalCountMolens = totalMolens
            };
        }
    }
}
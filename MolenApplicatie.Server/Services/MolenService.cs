using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;
using System.Linq;

namespace MolenApplicatie.Server.Services
{
    public class MolenService
    {
        private readonly string folderNameMolenImages = $"wwwroot/MolenAddedImages";
        private readonly MolenDbContext _dbContext;

        public MolenService(MolenDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public MolenData GetMolenData(MolenData molen)
        {
            molen.HasImage = molen.AddedImages.Count > 0;
            return molen;
        }

        public async Task<List<string>> GetAllMolenProvincies()
        {
            var provincies = await _dbContext.MolenData
                    .Select(m => m.Provincie)
                    .Distinct().ToListAsync();
            return provincies
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!)
                .ToList();
        }

        public List<MolenData> GetAllMolenDataByProvincie(string provincie)
        {
            List<MolenData> alleMolenData = GetAllMolenData();
            List<MolenData> MolenDataByProvincie = alleMolenData.Where(molen => molen.Provincie != null && molen.Provincie.ToLower() == provincie.ToLower()).ToList();
            return MolenDataByProvincie;
        }

        public List<MolenData> GetAllMolenData()
        {
            return _dbContext.MolenData
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .ToList()
                .Select(GetMolenData).ToList();
        }

        public List<MolenData> GetAllActiveMolenData()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            Console.WriteLine(alleMolenData.Count());
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand == MolenToestand.Werkend).ToList();
            return GefilterdeMolenData;
        }

        public IQueryable<MolenData> GetAllMolenDataCorrectTypes()
        {
            List<string> allowedMolenTypes = Globals.AllowedMolenTypes.Select(t => t.ToLower()).ToList();

            return _dbContext.MolenData
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .Where(m => m.MolenTypeAssociations.Any(mta => allowedMolenTypes.Contains(mta.MolenType.Name.ToLower())))
                .AsQueryable();
        }


        public List<MolenData> GetAllExistingMolens()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand != MolenToestand.Verdwenen).ToList();
            return GefilterdeMolenData;
        }


        public async Task<List<MolenData>> GetAllDisappearedMolens(string provincie)
        {
            return await _dbContext.MolenData
                .Where(m => m.Toestand != null && m.Toestand == MolenToestand.Verdwenen && m.Provincie != null && m.Provincie.ToLower() == provincie.ToLower())
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .Select(m => GetMolenData(m)).ToListAsync();
        }

        public List<MolenData> GetAllRemainderMolens()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand == MolenToestand.Restant).ToList();
            return GefilterdeMolenData;
        }

        public async Task<List<MolenLatLongReturn>> GetAllMolenLatLon()
        {
            List<MolenLatLongReturn> MolenData = await _dbContext.MolenData
                .Include(m => m.AddedImages)
                .Select(m => new MolenLatLongReturn
                {
                    MolenID = m.Id,
                    MolenTBN = m.Ten_Brugge_Nr,
                    Latitude = m.Latitude,
                    Longitude = m.Longitude,
                    HasImage = m.AddedImages.Count > 0,
                }).ToListAsync();
            return MolenData;
        }

        public async Task<List<MolenType>> GetAllMolenTypes()
        {
            return await _dbContext.MolenTypes.ToListAsync();
        }

        public async Task<MolenData?> GetMolenByTBN(string tbn)
        {
            var molen = await _dbContext.MolenData
                .Include(m => m.MolenTBN)
                    .Where(m => m.MolenTBN.Ten_Brugge_Nr.ToLower() == tbn.ToLower())
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .FirstOrDefaultAsync();

            if (molen == null) return null;

            return GetMolenData(molen);
        }


        public async Task<List<MolenData>> GetMolenDataByType(string type)
        {
            List<MolenData> MolenData = await _dbContext.MolenData
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos).ToListAsync();
            return MolenData;
        }

        public bool CanMolenHaveAddedImages(MolenData molen)
        {
            return molen.Toestand != MolenToestand.Verdwenen;
        }

        public async Task<(IFormFile? file, string errorMessage)> SaveMolenImage(int id, string TBN, IFormFile file)
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
                await _dbContext.AddedImages.AddAsync(new AddedImage
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
            MolenData? molen = await GetMolenByTBN(tbNummer);
            if (molen == null) return (false, "Molen not found");
            var molenImageToDelete = molen.Images.Find(x => x.Name == imgName);
            var molenAddedImageToDelete = molen.AddedImages.Find(x => x.Name == imgName);
            if (molenImageToDelete == null && molenAddedImageToDelete == null) return (false, "Images not found");
            if (molenImageToDelete != null && File.Exists(CreateCleanPath.CreatePathToWWWROOT(molenImageToDelete.FilePath)))
            {
                File.Delete(CreateCleanPath.CreatePathToWWWROOT(molenImageToDelete.FilePath));
                _dbContext.Remove(molenImageToDelete);
            }
            else if (molenAddedImageToDelete != null && File.Exists(CreateCleanPath.CreatePathToWWWROOT(molenAddedImageToDelete.FilePath)))
            {
                File.Delete(CreateCleanPath.CreatePathToWWWROOT(molenAddedImageToDelete.FilePath));
                _dbContext.Remove(molenAddedImageToDelete);
            }
            return (true, "Images deleted");
        }

        private async Task<int> GetCountOfActiveMolensWithImages() => await _dbContext.MolenData
            .Include(m => m.AddedImages)
            .Where(m => m.Toestand == MolenToestand.Werkend && (m.AddedImages.Count > 0))
            .CountAsync();

        private async Task<int> GetCountOfRemainderMolensWithImage() => await _dbContext.MolenData
            .Include(m => m.AddedImages)
            .Where(m => m.Toestand == MolenToestand.Restant && (m.AddedImages.Count > 0))
            .CountAsync();

        private async Task<int> GetCountOfActiveMolens() => await _dbContext.MolenData
            .Where(m => m.Toestand == MolenToestand.Werkend)
            .CountAsync();

        private async Task<int> GetCountOfRemainderMolens() => await _dbContext.MolenData
            .Where(m => m.Toestand == MolenToestand.Restant)
            .CountAsync();

        private async Task<int> GetCountMolens() => await _dbContext.MolenData.CountAsync();

        private async Task<List<CountDisappearedMolens>> GetCountOfDisappearedMolens()
        {
            List<string> provincies = await GetAllMolenProvincies();
            List<CountDisappearedMolens> disappearedMolens = new List<CountDisappearedMolens>();
            foreach (string provincie in provincies)
            {
                int count = await _dbContext.MolenData
                    .Where(m => m.Toestand == MolenToestand.Verdwenen &&
                                m.Provincie != null &&
                                EF.Functions.Like(m.Provincie.ToLower(), provincie.ToLower()))
                    .CountAsync();

                disappearedMolens.Add(new CountDisappearedMolens
                {
                    Provincie = provincie,
                    Count = count
                });
            }
            return disappearedMolens;
        }

        public async Task<MolensResponseType> MolensResponse(List<MolenData> molens)
        {
            int activeMolensWithImage = await GetCountOfActiveMolensWithImages();
            int remainderMolensWithImage = await GetCountOfRemainderMolensWithImage();
            int totalMolensWithImage = activeMolensWithImage + remainderMolensWithImage;

            int totalActiveMolens = await GetCountOfActiveMolens();
            int totalRemainderMolens = await GetCountOfRemainderMolens();
            int totalExistingMolens = totalActiveMolens + totalRemainderMolens;

            List<CountDisappearedMolens> totalDisappearedMolens = await GetCountOfDisappearedMolens();

            int totalMolens = await GetCountMolens();
            return new MolensResponseType
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
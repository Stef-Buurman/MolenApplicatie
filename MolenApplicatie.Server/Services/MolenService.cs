using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Services.Database;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services
{
    public class MolenService
    {
        private readonly string folderNameMolenImages = $"wwwroot/MolenAddedImages";
        private readonly MolenDbContext _dbContext;
        private readonly DBMolenDataService _dBMolenDataService;

        public MolenService(MolenDbContext dbContext, DBMolenDataService dBMolenDataService)
        {
            _dbContext = dbContext;
            _dBMolenDataService = dBMolenDataService;
        }

        public static MolenData GetMolenData(MolenData molen)
        {
            molen.HasImage = molen.AddedImages.Count > 0;
            return molen;
        }

        public static List<MolenData>? RemoveCircularDependencyAll(List<MolenData>? molens)
        {
            if (molens == null) return null;
            molens.ForEach(molen => RemoveCircularDependency(molen));
            return molens;
        }

        public static MolenData? RemoveCircularDependency(MolenData? molen)
        {
            if (molen == null) return null;
            if (molen.MolenTBN != null)
            {
                molen.MolenTBN.MolenData = null;
            }
            if (molen.MolenTypeAssociations != null)
            {
                molen.MolenTypeAssociations.ForEach(mak =>
                {
                    mak.MolenData = null;
                    if (mak.MolenType != null) mak.MolenType.MolenTypeAssociations = null;
                });
            }
            if (molen.MolenMakers != null)
            {
                molen.MolenMakers.ForEach(mak =>
                {
                    mak.MolenData = null;
                });
            }
            if (molen.AddedImages != null)
            {
                molen.AddedImages.ForEach(mak =>
                {
                    mak.MolenData = null;
                });
            }
            if (molen.Images != null)
            {
                molen.Images.ForEach(mak =>
                {
                    mak.MolenData = null;
                });
            }
            if (molen.DisappearedYearInfos != null)
            {
                molen.DisappearedYearInfos.ForEach(mak =>
                {
                    mak.MolenData = null;
                });
            }
            return molen;
        }

        public List<MapData> GetMapData(
            string? MolenType,
            string? Provincie,
            string? MolenState = null)
        {
            List<string> allowedMolenTypes = Globals.AllowedMolenTypes.Select(t => t.ToLower()).ToList();

            IQueryable<MolenData> mapData = _dbContext.MolenData
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType);

            if (!string.IsNullOrWhiteSpace(MolenState))
            {
                MolenState = MolenToestand.From(MolenState);
            }
            else
            {
                MolenState = null;
            }

            if (MolenState == null)
            {
                MolenState = MolenToestand.Werkend;
            }

            if (!string.IsNullOrWhiteSpace(MolenState))
            {
                if (MolenToestand.Equals(MolenState, MolenToestand.Bestaande))
                {
                    mapData = mapData.Where(m => m.Toestand != null && m.Toestand != MolenToestand.Verdwenen);
                }
                else if (MolenToestand.Equals(MolenState, MolenToestand.Verdwenen))
                {
                    mapData = mapData.Where(m => m.Toestand != null && m.Toestand == MolenToestand.Verdwenen);
                    if (!string.IsNullOrWhiteSpace(Provincie) && _dbContext.MolenData.Any(m => m.Provincie != null && m.Provincie.ToLower() == Provincie.ToLower()))
                    {
                        mapData = mapData.Where(m => m.Provincie != null && m.Provincie.ToLower() == Provincie.ToLower());
                    }
                    else
                    {
                        mapData = mapData.Where(m => m.Provincie != null && m.Provincie.ToLower() == "zuid-holland");
                    }
                }
                else
                {
                    mapData = mapData.Where(m => m.Toestand != null && m.Toestand.ToLower() == MolenState.ToLower());
                }
            }

            if (!string.IsNullOrWhiteSpace(MolenType))
            {
                mapData = mapData.Where(m => m.MolenTypeAssociations.Any(mt => mt.MolenType.Name.ToLower() == MolenType.ToLower()));
            }
            else
            {
                mapData = mapData.Where(m => m.MolenTypeAssociations.Any(mt => allowedMolenTypes.Contains(mt.MolenType.Name.ToLower())));
            }

            if (!string.IsNullOrWhiteSpace(Provincie))
            {
                mapData = mapData.Where(m => m.Provincie != null && m.Provincie.ToLower() == Provincie.ToLower());
            }

            return mapData.Select(m => new MapData
            {
                Reference = m.Ten_Brugge_Nr,
                Latitude = m.Latitude,
                Longitude = m.Longitude,
                HasImage = m.AddedImages.Count > 0,
                Toestand = m.Toestand,
                Type = "Molens",
                Types = m.MolenTypeAssociations.Select(mt => mt.MolenType.Name).ToList(),
            }).ToList();
        }

        public async Task<List<ValueName>> GetAllMolenProvincies()
        {
            var provincies = await _dbContext.MolenData
                    .Where(m => !string.IsNullOrWhiteSpace(m.Provincie))
                    .GroupBy(m => m.Provincie)
                    .Select(g => new ValueName
                    {
                        Name = g.Key ?? string.Empty,
                        Count = g.Count()
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            return provincies.ToList();
        }

        public async Task<List<ValueName>> GetAllMolenTypes()
        {
            var types = await _dbContext.MolenData
                    .SelectMany(m => m.MolenTypeAssociations.Select(mt => mt.MolenType.Name))
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .GroupBy(t => t)
                    .Select(g => new ValueName
                    {
                        Name = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            return types.ToList();
        }

        public async Task<List<ValueName>> GetAllMolenConditions()
        {
            var conditions = await _dbContext.MolenData
                    .Where(m => !string.IsNullOrWhiteSpace(m.Toestand))
                    .GroupBy(m => m.Toestand)
                    .Select(g => new ValueName
                    {
                        Name = g.Key ?? string.Empty,
                        Count = g.Count()
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            return conditions.ToList();
        }

        public async Task<MolenFilters> GetMolenFilters()
        {
            return new MolenFilters
            {
                Provincies = await GetAllMolenProvincies(),
                Toestanden = await GetAllMolenConditions(),
                Types = await GetAllMolenTypes()
            };
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
                .Select(GetMolenData).ToList();
        }

        public List<MolenData> GetAllActiveMolenData()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            Console.WriteLine(alleMolenData.Count());
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand == MolenToestand.Werkend).Select(GetMolenData).ToList();
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


        public List<MolenData> GetAllDisappearedMolens(string provincie)
        {
            return _dbContext.MolenData
                .Where(m => m.Toestand != null && m.Toestand == MolenToestand.Verdwenen && m.Provincie != null && m.Provincie.ToLower() == provincie.ToLower())
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos).ToList();
        }

        public List<MolenData> GetAllRemainderMolens()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand == MolenToestand.Restant).ToList();
            return GefilterdeMolenData;
        }

        public List<MolenData> GetMolensByTBN(List<string> tbns)
        {
            return _dbContext.MolenData
                .AsNoTracking()
                .Include(m => m.MolenTBN)
                    .Where(m => tbns.Contains(m.MolenTBN.Ten_Brugge_Nr.ToLower()))
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .Select(GetMolenData).ToList();
        }

        public async Task<MolenData?> GetMolenByTBN(string tbn)
        {
            _dBMolenDataService._cache.Exists(p => p.Ten_Brugge_Nr.ToLower() == tbn.ToLower(), out var molen);

            if (molen != null) return molen;

            molen = await _dbContext.MolenData
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

        public async Task<(IFormFile? file, string errorMessage)> SaveMolenImage(Guid id, string TBN, IFormFile file)
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
            List<ValueName> provincies = await GetAllMolenProvincies();
            List<CountDisappearedMolens> disappearedMolens = new List<CountDisappearedMolens>();
            foreach (ValueName provincie in provincies)
            {
                int count = await _dbContext.MolenData
                    .Where(m => m.Toestand == MolenToestand.Verdwenen &&
                                m.Provincie != null &&
                                EF.Functions.Like(m.Provincie.ToLower(), provincie.Name.ToLower()))
                    .CountAsync();

                disappearedMolens.Add(new CountDisappearedMolens
                {
                    Provincie = provincie.Name,
                    Count = count
                });
            }
            return disappearedMolens;
        }

        public async Task<MolensResponseType<T>> MolensResponse<T>(List<T> molens)
        {
            int activeMolensWithImage = await GetCountOfActiveMolensWithImages();
            int remainderMolensWithImage = await GetCountOfRemainderMolensWithImage();
            int totalMolensWithImage = activeMolensWithImage + remainderMolensWithImage;

            int totalActiveMolens = await GetCountOfActiveMolens();
            int totalRemainderMolens = await GetCountOfRemainderMolens();
            int totalExistingMolens = totalActiveMolens + totalRemainderMolens;

            List<CountDisappearedMolens> totalDisappearedMolens = await GetCountOfDisappearedMolens();

            int totalMolens = await GetCountMolens();
            return new MolensResponseType<T>
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
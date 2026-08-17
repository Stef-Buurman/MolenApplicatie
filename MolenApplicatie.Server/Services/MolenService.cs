using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Records;
using MolenApplicatie.Server.Services.Database;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services
{
    public class MolenService
    {
        private const string MillDatabaseReferencePrefix = "MDB-";
        private const string LegacyDefaultCountry = "Nederland";

        private readonly string _molenAddedImagesPath;
        private readonly MolenDbContext _dbContext;
        private readonly DBMolenDataService _dBMolenDataService;
        private readonly DBMolenAddedImageService _dBMolenAddedImageService;
        private readonly DBMolenImageService _dBMolenImageService;
        private readonly MapClusterService _mapClusterService;

        public MolenService(MolenDbContext dbContext, DBMolenDataService dBMolenDataService, DBMolenAddedImageService dBMolenAddedImageService, DBMolenImageService dBMolenImageService, MapClusterService mapClusterService, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _dBMolenDataService = dBMolenDataService;
            _dBMolenAddedImageService = dBMolenAddedImageService;
            _dBMolenImageService = dBMolenImageService;
            _mapClusterService = mapClusterService;

            _molenAddedImagesPath =
                Path.Combine(environment.WebRootPath, "MolenAddedImages");
        }

        public static MolenData GetMolenData(MolenData molen)
        {
            molen.HasImage = (molen.AddedImages?.Count ?? 0) > 0;
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
            string? molenType,
            string? provincie,
            string? molenState = null)
        {
            var mapData = ApplyMapFilters(
                GetAllMolenDataCorrectTypes().AsNoTracking(),
                new MolenMapFilter
                {
                    MolenType = molenType,
                    Provincie = provincie,
                    MolenState = molenState
                });

            return mapData.Select(molen => new MapData
            {
                Reference = molen.Ten_Brugge_Nr,
                Latitude = molen.Latitude,
                Longitude = molen.Longitude,
                HasImage = molen.AddedImages.Any(),
                Toestand = molen.Toestand,
                Type = "Molens",
                Types = molen.MolenTypeAssociations.Select(association => association.MolenType.Name).ToList(),
            }).ToList();
        }

        public async Task<List<ValueName>> GetAllMolenProvincies()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(MolenCoordinateQuery.HasUsableCoordinates)
                .Where(molen => molen.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .Where(molen => !string.IsNullOrWhiteSpace(molen.Provincie))
                .GroupBy(molen => molen.Provincie)
                .Select(group => new ValueName
                {
                    Name = group.Key ?? string.Empty,
                    Count = group.Count()
                })
                .OrderBy(province => province.Name)
                .ToListAsync();
        }

        private async Task<List<ValueName>> GetAllMolenFilterProvincies()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(MolenCoordinateQuery.HasUsableCoordinates)
                .Where(molen => molen.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .Where(molen => !string.IsNullOrWhiteSpace(molen.Provincie))
                .GroupBy(molen => new
                {
                    molen.Provincie,
                    Land = molen.Land != null && molen.Land != string.Empty
                        ? molen.Land
                        : !molen.Ten_Brugge_Nr.StartsWith(
                            MillDatabaseReferencePrefix)
                            ? LegacyDefaultCountry
                            : null
                })
                .Select(group => new ValueName
                {
                    Name = group.Key.Provincie ?? string.Empty,
                    Parent = group.Key.Land,
                    Count = group.Count()
                })
                .OrderBy(province => province.Parent)
                .ThenBy(province => province.Name)
                .ToListAsync();
        }

        public async Task<List<ValueName>> GetAllMolenCountries()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(MolenCoordinateQuery.HasUsableCoordinates)
                .Where(molen => molen.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .Select(molen => new
                {
                    Land = molen.Land != null && molen.Land != string.Empty
                        ? molen.Land
                        : !molen.Ten_Brugge_Nr.StartsWith(
                            MillDatabaseReferencePrefix)
                            ? LegacyDefaultCountry
                            : null
                })
                .Where(molen => molen.Land != null)
                .GroupBy(molen => molen.Land)
                .Select(group => new ValueName
                {
                    Name = group.Key ?? string.Empty,
                    Count = group.Count()
                })
                .OrderBy(country => country.Name)
                .ToListAsync();
        }

        public async Task<List<ValueName>> GetAllMolenTypes()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            var types = await _dbContext.MolenData
                    .Where(MolenCoordinateQuery.HasUsableCoordinates)
                    .SelectMany(m => m.MolenTypeAssociations.Select(mt => mt.MolenType.Name))
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Where(t => allowedMolenTypes.Contains(t.ToLower()))
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
            var allowedMolenTypes = GetAllowedMolenTypes();

            var conditionValues = await _dbContext.MolenData
                .AsNoTracking()
                .Where(MolenCoordinateQuery.HasUsableCoordinates)
                .Where(molen => molen.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .Where(molen => !string.IsNullOrWhiteSpace(molen.Toestand))
                .Select(molen => molen.Toestand!)
                .ToListAsync();

            var normalizedConditions = conditionValues
                .Select(value => MolenToestand.From(value) ?? value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            var conditions = normalizedConditions
                .GroupBy(
                    value => value,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => new ValueName
                {
                    Name = group.First(),
                    Count = group.Count()
                })
                .ToList();

            var existingStates = new HashSet<string>(
                [
                    MolenToestand.Bestaande,
                    MolenToestand.InAanbouw,
                    MolenToestand.Restant,
                    MolenToestand.Werkend,
                    MolenToestand.NietWerkend
                ],
                StringComparer.OrdinalIgnoreCase);

            var existingCount = normalizedConditions.Count(existingStates.Contains);

            var existingCondition = conditions.FirstOrDefault(
                condition => string.Equals(
                    condition.Name,
                    MolenToestand.Bestaande,
                    StringComparison.OrdinalIgnoreCase));

            if (existingCondition != null)
            {
                existingCondition.Count = existingCount;
            }
            else if (existingCount > 0)
            {
                conditions.Add(new ValueName
                {
                    Name = MolenToestand.Bestaande,
                    Count = existingCount
                });
            }

            return conditions.OrderBy(condition => condition.Name).ToList();
        }

        public async Task<MolenFilters> GetMolenFilters()
        {
            return new MolenFilters
            {
                Landen = await GetAllMolenCountries(),
                Provincies = await GetAllMolenFilterProvincies(),
                Toestanden = await GetAllMolenConditions(),
                Types = await GetAllMolenTypes()
            };
        }

        public List<MolenData> GetAllMolenDataByProvincie(string provincie)
        {
            return GetAllMolenDataCorrectTypes()
                .Where(molen => molen.Provincie != null &&
                    molen.Provincie.ToLower() == provincie.ToLower())
                .AsEnumerable()
                .Select(GetMolenData)
                .ToList();
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
            List<MolenData> GefilterdeMolenData = alleMolenData.Where(molen => molen.Toestand != null && molen.Toestand == MolenToestand.Werkend).Select(GetMolenData).ToList();
            return GefilterdeMolenData;
        }

        public IQueryable<MolenData> GetAllMolenDataCorrectTypes()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return _dbContext.MolenData
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .Where(m => m.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .AsQueryable();
        }


        public List<MolenData> GetAllExistingMolens()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData
                .Where(molen => molen.Toestand != null && molen.Toestand != MolenToestand.Verdwenen)
                .AsEnumerable()
                .Select(GetMolenData)
                .ToList();
            return GefilterdeMolenData;
        }


        public List<MolenData> GetAllDisappearedMolens(string provincie)
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return _dbContext.MolenData
                .Where(m => m.Toestand != null &&
                    m.Toestand == MolenToestand.Verdwenen &&
                    m.Provincie != null &&
                    m.Provincie.ToLower() == provincie.ToLower() &&
                    m.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .AsEnumerable()
                .Select(GetMolenData)
                .ToList();
        }

        public List<MolenData> GetAllRemainderMolens()
        {
            IQueryable<MolenData> alleMolenData = GetAllMolenDataCorrectTypes();
            List<MolenData> GefilterdeMolenData = alleMolenData
                .Where(molen => molen.Toestand != null && molen.Toestand == MolenToestand.Restant)
                .AsEnumerable()
                .Select(GetMolenData)
                .ToList();
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
            if (string.IsNullOrWhiteSpace(tbn)) return null;

            var normalizedTbn = tbn.Trim().ToLower();

            var molen = await _dbContext.MolenData.AsNoTracking()
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .FirstOrDefaultAsync(m => m.Ten_Brugge_Nr != null && m.Ten_Brugge_Nr.ToLower() == normalizedTbn);

            if (molen == null) return null;

            await PopulateLinkedMolenIdsAsync(molen);
            return GetMolenData(molen);
        }

        public async Task<MolenData?> GetMolenByReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;

            if (Guid.TryParse(reference, out var molenId))
            {
                var molenById = await GetMolenById(molenId);
                if (molenById != null) return molenById;
            }

            return await GetMolenByTBN(reference);
        }

        public async Task<MolenData?> GetMolenById(Guid id)
        {
            var molen = await _dbContext.MolenData.AsNoTracking()
                .Include(m => m.MolenTBN)
                .Include(m => m.Images)
                .Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations)
                    .ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers)
                .Include(m => m.DisappearedYearInfos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (molen == null) return null;

            await PopulateLinkedMolenIdsAsync(molen);
            return GetMolenData(molen);
        }

        private async Task PopulateLinkedMolenIdsAsync(MolenData molen)
        {
            var references = new[] { molen.Voorganger, molen.Opvolger }
                .Where(reference => !string.IsNullOrWhiteSpace(reference))
                .Select(reference => reference!.Trim().ToLower())
                .Distinct()
                .ToList();

            if (references.Count == 0) return;

            var linkedMolens = await _dbContext.MolenData
                .AsNoTracking()
                .Where(linkedMolen => references.Contains(linkedMolen.Ten_Brugge_Nr.ToLower()))
                .Select(linkedMolen => new
                {
                    Reference = linkedMolen.Ten_Brugge_Nr,
                    linkedMolen.Id
                })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(molen.Voorganger))
            {
                molen.VoorgangerMolenId = linkedMolens
                    .FirstOrDefault(linkedMolen => linkedMolen.Reference.Equals(molen.Voorganger.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?.Id;
            }

            if (!string.IsNullOrWhiteSpace(molen.Opvolger))
            {
                molen.OpvolgerMolenId = linkedMolens
                    .FirstOrDefault(linkedMolen => linkedMolen.Reference.Equals(molen.Opvolger.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?.Id;
            }
        }

        public async Task<MapData?> GetMapDataByTBN(string tbn)
        {
            var molen = await GetMolenByTBN(tbn);
            return molen == null ? null : CreateMapData(molen);
        }

        public async Task<MapData?> GetMapDataByReference(string reference)
        {
            var molen = await GetMolenByReference(reference);
            return molen == null ? null : CreateMapData(molen);
        }

        private static MapData CreateMapData(MolenData molen)
        {
            return new MapData
            {
                Reference = string.IsNullOrWhiteSpace(molen.Ten_Brugge_Nr) ? molen.Id.ToString() : molen.Ten_Brugge_Nr,
                Latitude = molen.Latitude,
                Longitude = molen.Longitude,
                HasImage = molen.AddedImages.Count > 0,
                Toestand = molen.Toestand,
                Type = "Molens",
                Types = molen.MolenTypeAssociations.Select(mt => mt.MolenType.Name).ToList(),
            };
        }

        public async Task<(IFormFile? file, string errorMessage, HttpStatusCode statusCode)> SaveMolenImage(Guid id, string imageFolderKey, IFormFile file)
        {
            const int maxSavedFilesCount = 5;
            var folderName = Path.Combine(_molenAddedImagesPath, imageFolderKey);
            Directory.CreateDirectory(folderName);
            var existingFiles = Directory.GetFiles(folderName);
            if (existingFiles.Length >= maxSavedFilesCount) return (null, "Er zijn al te veel foto's opgeslagen voor deze molen.", HttpStatusCode.BadRequest);
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                return (null, "Dit soort bestand wordt niet ondersteund!", HttpStatusCode.UnsupportedMediaType);

            byte[] imageBytes;

            await using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }

            var uploadedHash =
                ComputeSha256Hash(imageBytes);

            foreach (var existingFile in existingFiles)
            {
                var existingBytes = await File.ReadAllBytesAsync(existingFile);
                var existingHash = ComputeSha256Hash(existingBytes);
                if (uploadedHash != existingHash) continue;
                var existingRelativePath = CreateCleanPath.CreatePathWithoutWWWROOT(existingFile);
                var alreadyRegistered = await _dbContext.AddedImages.AsNoTracking().AnyAsync(addedImage => addedImage.MolenDataId == id && addedImage.FilePath == existingRelativePath);
                if (alreadyRegistered) return (null, "Deze afbeelding is al opgeslagen.", HttpStatusCode.Conflict);


                await _dBMolenAddedImageService.AddOrUpdate(
                    new AddedImage
                    {
                        FilePath = existingRelativePath,
                        Name = Path.GetFileNameWithoutExtension(
                            existingFile),
                        DateTaken =
                            GetDateTakenOfImage.GetDateTaken(
                                existingFile),
                        CanBeDeleted = true,
                        MolenDataId = id
                    });

                await _dbContext.SaveChangesAsync();

                return (file, string.Empty, HttpStatusCode.OK);
            }

            string fileDirectory;

            do
            {
                fileDirectory = Path.Combine(folderName, GetFileNameForImage.GetFileName() + fileExtension);
            }
            while (File.Exists(fileDirectory));

            await File.WriteAllBytesAsync(
                fileDirectory,
                imageBytes);

            try
            {
                var relativeFilePath =
                    CreateCleanPath.CreatePathWithoutWWWROOT(
                        fileDirectory);

                await _dBMolenAddedImageService.AddOrUpdate(
                    new AddedImage
                    {
                        FilePath = relativeFilePath,
                        Name = Path.GetFileNameWithoutExtension(
                            fileDirectory),
                        DateTaken =
                            GetDateTakenOfImage.GetDateTaken(
                                fileDirectory),
                        CanBeDeleted = true,
                        MolenDataId = id
                    });

                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                if (File.Exists(fileDirectory))
                {
                    File.Delete(fileDirectory);
                }

                throw;
            }

            return (
                file,
                string.Empty,
                HttpStatusCode.OK);
        }
        private string ComputeSha256Hash(byte[] bytes)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public async Task<(bool status, string message)> DeleteImageFromMolen(string molenReference, string imgName)
        {
            MolenData? molen = await GetMolenByReference(molenReference);
            if (molen == null) return (false, "Molen not found");
            var molenImageToDelete = molen.Images.Find(x => x.Name == imgName);
            var molenAddedImageToDelete = molen.AddedImages.Find(x => x.Name == imgName);
            if (molenImageToDelete == null && molenAddedImageToDelete == null) return (false, "Images not found");
            if (molenImageToDelete != null)
            {
                await _dBMolenImageService.Delete(molenImageToDelete);
            }
            else if (molenAddedImageToDelete != null)
            {
                await _dBMolenAddedImageService.Delete(molenAddedImageToDelete);
            }
            await _dbContext.SaveChangesAsync();
            return (true, "Images deleted");
        }

        private async Task<int> GetCountOfActiveMolensWithImages()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(m => m.Toestand == MolenToestand.Werkend &&
                    m.AddedImages.Any() &&
                    m.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .CountAsync();
        }

        private async Task<int> GetCountOfRemainderMolensWithImage()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(m => m.Toestand == MolenToestand.Restant &&
                    m.AddedImages.Any() &&
                    m.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .CountAsync();
        }

        public Task<int> GetMolensWithImageCountAsync(
            CancellationToken token = default)
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return _dbContext.MolenData
                .AsNoTracking()
                .CountAsync(molen =>
                    molen.AddedImages.Any() &&
                    molen.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())),
                    token);
        }

        private async Task<int> GetCountOfActiveMolens()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(m => m.Toestand == MolenToestand.Werkend &&
                    m.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .CountAsync();
        }

        private async Task<int> GetCountOfRemainderMolens()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(m => m.Toestand == MolenToestand.Restant &&
                    m.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .CountAsync();
        }

        private async Task<int> GetCountMolens()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            return await _dbContext.MolenData
                .Where(m => m.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .CountAsync();
        }

        private async Task<List<CountDisappearedMolens>> GetCountOfDisappearedMolens()
        {
            var allowedMolenTypes = GetAllowedMolenTypes();
            List<ValueName> provincies = await GetAllMolenProvincies();
            List<CountDisappearedMolens> disappearedMolens = new List<CountDisappearedMolens>();
            foreach (ValueName provincie in provincies)
            {
                int count = await _dbContext.MolenData
                    .Where(m => m.Toestand == MolenToestand.Verdwenen &&
                                m.Provincie != null &&
                                EF.Functions.Like(m.Provincie.ToLower(), provincie.Name.ToLower()) &&
                                m.MolenTypeAssociations.Any(association =>
                                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
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
            int totalMolensWithImage = await GetMolensWithImageCountAsync();

            int totalActiveMolens = await GetCountOfActiveMolens();
            int totalRemainderMolens = await GetCountOfRemainderMolens();
            int totalExistingMolens = totalActiveMolens + totalRemainderMolens;

            List<CountDisappearedMolens> totalDisappearedMolens = await GetCountOfDisappearedMolens();

            int totalMolens = await GetCountMolens();

            var allowedMolenTypes = GetAllowedMolenTypes();

            var recentImages = _dbContext.AddedImages
                .Where(ai =>
                    ai.DateTaken >= DateTime.Now.AddDays(-7) &&
                    ai.DateTaken <= DateTime.Now &&
                    ai.MolenData.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .GroupBy(ai => ai.MolenDataId)
                .Select(g => new
                {
                    MolenDataId = g.Key,
                    Images = g.OrderByDescending(i => i.DateTaken).ToList()
                })
                .ToList();

            var molenDataDict = _dbContext.MolenData
                .Where(m => recentImages.Select(r => r.MolenDataId).Contains(m.Id))
                .ToDictionary(m => m.Id);

            var recentAddedImages = recentImages
                .Select(g => new RecentAddedImages
                {
                    molen = (g.MolenDataId != null && molenDataDict.ContainsKey(g.MolenDataId)) ? molenDataDict[g.MolenDataId] : null,
                    Images = g.Images
                })
                .OrderByDescending(r => r.Images.Count)
                .ToList();


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
                TotalCountMolens = totalMolens,
                RecentAddedImages = recentAddedImages
            };
        }

        public async Task<MolenMapSummaryResponse> GetMapSummaryAsync(CancellationToken token)
        {
            var totalMolensWithImage = await GetMolensWithImageCountAsync(token);

            var now = DateTime.Now;
            var recentImageStart = now.AddDays(-7);

            var allowedMolenTypes = GetAllowedMolenTypes();

            var recentImages = await _dbContext.AddedImages
                .AsNoTracking()
                .Where(image =>
                    image.DateTaken >= recentImageStart &&
                    image.DateTaken <= now &&
                    image.MolenData.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                .OrderByDescending(image => image.DateTaken)
                .ToListAsync(token);

            var molenIds = recentImages
                .Select(image => image.MolenDataId)
                .Distinct()
                .ToList();

            List<MolenData> molens = molenIds.Count == 0
                ? []
                : await _dbContext.MolenData
                    .AsNoTracking()
                    .Include(molen => molen.Images)
                    .Include(molen => molen.AddedImages)
                    .Include(molen => molen.MolenTypeAssociations)
                        .ThenInclude(association => association.MolenType)
                    .Where(molen => molenIds.Contains(molen.Id))
                    .Where(molen => molen.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())))
                    .ToListAsync(token);

            var molensById = molens
                .Select(GetMolenData)
                .ToDictionary(molen => molen.Id);

            var recentAddedImages = recentImages
                .GroupBy(image => image.MolenDataId)
                .Where(group => molensById.ContainsKey(group.Key))
                .Select(group => new RecentAddedImages
                {
                    molen = molensById[group.Key],
                    Images = group.ToList()
                })
                .OrderByDescending(group => group.Images.Max(image => image.DateTaken))
                .ToList();

            return new MolenMapSummaryResponse
            {
                TotalMolensWithImage = totalMolensWithImage,
                RecentAddedImages = recentAddedImages
            };
        }

        public async Task<IReadOnlyList<MapItemResponse>> GetMapItemsAsync(MolenMapFilter filter, CancellationToken token)
        {
            var allowedMolenTypes = GetAllowedMolenTypes();

            var query = _dbContext.MolenData
                .AsNoTracking()
                .Where(MolenCoordinateQuery.HasUsableCoordinates)
                .Where(molen => molen.MolenTypeAssociations.Any(association =>
                    allowedMolenTypes.Contains(association.MolenType.Name.ToLower())));

            query = ApplyMapFilters(query, filter);

            return await _mapClusterService.GetMapItemsAsync(
                query,
                (queryWest, querySouth, queryEast, queryNorth) => molen =>
                    molen.Latitude >= querySouth &&
                    molen.Latitude <= queryNorth &&
                    molen.Longitude >= queryWest &&
                    molen.Longitude <= queryEast,
                filteredQuery => filteredQuery.Select(molen => molen.Id),
                filteredQuery => filteredQuery.Select(molen => new MapCoordinateQueryPoint
                {
                    Latitude = molen.Latitude,
                    Longitude = molen.Longitude
                }),
                filteredQuery => MapQueryBuilder.CreateGridQuery(
                    filteredQuery,
                    filter.Zoom,
                    molen => molen.Id,
                    molen => molen.Latitude,
                    molen => molen.Longitude,
                    molen => molen.MercatorY),
                filteredQuery => MapQueryBuilder.CreateIndividualPointQuery(
                    filteredQuery,
                    molen => molen.Id,
                    molen => molen.Name,
                    molen => molen.Latitude,
                    molen => molen.Longitude,
                    molen => molen.Toestand,
                    molen => molen.MolenTypeAssociations.Select(association => association.MolenType.Name),
                    molen => molen.AddedImages.Any()),
                (filteredQuery, pointIds) => MapQueryBuilder.CreateIndividualPointQuery(
                    filteredQuery.Where(molen => pointIds.Contains(molen.Id)),
                    molen => molen.Id,
                    molen => molen.Name,
                    molen => molen.Latitude,
                    molen => molen.Longitude,
                    molen => molen.Toestand,
                    molen => molen.MolenTypeAssociations.Select(association => association.MolenType.Name),
                    molen => molen.AddedImages.Any()),
                new MapViewport(filter.West, filter.South, filter.East, filter.North, filter.Zoom),
                point => $"/map/{point.Id}",
                point => point.FriendlyView,
                points => $"{points.Count:N0} molens op deze locatie",
                token);
        }

        private static List<string> GetAllowedMolenTypes()
        {
            return Globals.AllowedMolenTypes
                .Select(type => type.ToLowerInvariant())
                .ToList();
        }

        private IQueryable<MolenData> ApplyMapFilters(IQueryable<MolenData> query, MolenMapFilter filter)
        {
            var molenState = MolenToestand.From(filter.MolenState);

            if (!string.IsNullOrWhiteSpace(molenState))
            {
                var stateAliases = MolenToestand.GetDatabaseAliases(molenState);
                query = query.Where(molen => molen.Toestand != null && stateAliases.Contains(molen.Toestand.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.MolenType))
            {
                var molenType = filter.MolenType.Trim().ToLower();
                query = query.Where(molen => molen.MolenTypeAssociations.Any(association => association.MolenType.Name.ToLower() == molenType));
            }

            if (!string.IsNullOrWhiteSpace(filter.Land))
            {
                var land = filter.Land.Trim().ToLower();
                var legacyDefaultCountry = LegacyDefaultCountry.ToLower();

                query = land == legacyDefaultCountry
                    ? query.Where(molen =>
                        (molen.Land != null && molen.Land.ToLower() == land) ||
                        ((molen.Land == null || molen.Land == string.Empty) &&
                         !molen.Ten_Brugge_Nr.StartsWith(
                             MillDatabaseReferencePrefix)))
                    : query.Where(molen =>
                        molen.Land != null && molen.Land.ToLower() == land);
            }

            if (!string.IsNullOrWhiteSpace(filter.Provincie))
            {
                var provincie = filter.Provincie.Trim().ToLower();
                query = query.Where(molen => molen.Provincie != null && molen.Provincie.ToLower() == provincie);
            }

            if (filter.HasImage.HasValue)
            {
                query = filter.HasImage.Value
                    ? query.Where(molen => molen.AddedImages.Any())
                    : query.Where(molen => !molen.AddedImages.Any());
            }

            return query;
        }
    }
}

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
            var provincies = await _dbContext.MolenData
                    .Where(m => !string.IsNullOrWhiteSpace(m.Provincie) && m.Latitude != 0 && m.Longitude != 0)
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
                    .Where(m => m.Latitude != 0 && m.Longitude != 0)
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
                    .Where(m => !string.IsNullOrWhiteSpace(m.Toestand) && m.Latitude != 0 && m.Longitude != 0)
                    .GroupBy(m => m.Toestand)
                    .Select(g => new ValueName
                    {
                        Name = g.Key ?? string.Empty,
                        Count = g.Count()
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            conditions.Add(new ValueName { Name = MolenToestand.Bestaande, Count = conditions.Where(c => !MolenToestand.Equals(c.Name, MolenToestand.Verdwenen)).Sum(c => c.Count) });
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
            List<MolenData> GefilterdeMolenData = alleMolenData
                .Where(molen => molen.Toestand != null && molen.Toestand != MolenToestand.Verdwenen)
                .AsEnumerable()
                .Select(GetMolenData)
                .ToList();
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
            var maxSavedFilesCount = 5;
            using (var memoryStream = new MemoryStream())
            {
                string folderName = _molenAddedImagesPath;

                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }

                folderName = Path.Combine(folderName, imageFolderKey);

                if (Directory.Exists(folderName) && Directory.GetFiles(folderName).Length >= maxSavedFilesCount)
                {
                    return (null, "Er zijn al te veel foto's opgeslagen voor deze molen.", HttpStatusCode.BadRequest);
                }

                var fileExtension = Path.GetExtension(file.FileName);
                if (fileExtension == null ||
                    (fileExtension.ToLower() != ".jpg"
                    && fileExtension.ToLower() != ".jpeg"
                    && fileExtension.ToLower() != ".png"))
                {
                    return (null, "Dit soort bestand wordt niet ondersteund!", HttpStatusCode.UnsupportedMediaType);
                }

                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                var uploadedHash = ComputeSha256Hash(imageBytes);

                if (Directory.Exists(folderName))
                {
                    foreach (var existingFile in Directory.GetFiles(folderName))
                    {
                        var existingBytes = await File.ReadAllBytesAsync(existingFile);
                        var existingHash = ComputeSha256Hash(existingBytes);
                        if (uploadedHash == existingHash)
                        {
                            return (null, "Deze afbeelding is al opgeslagen.", HttpStatusCode.Conflict);
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(folderName);
                }

                var fileDirectory = Path.Combine(folderName, GetFileNameForImage.GetFileName() + fileExtension);
                while (File.Exists(fileDirectory))
                {
                    fileDirectory = Path.Combine(folderName, GetFileNameForImage.GetFileName() + fileExtension);
                }

                await File.WriteAllBytesAsync(fileDirectory, imageBytes);

                await _dBMolenAddedImageService.AddOrUpdate(new AddedImage
                {
                    FilePath = CreateCleanPath.CreatePathWithoutWWWROOT(fileDirectory),
                    Name = Path.GetFileNameWithoutExtension(fileDirectory),
                    DateTaken = GetDateTakenOfImage.GetDateTaken(fileDirectory),
                    CanBeDeleted = true,
                    MolenDataId = id
                });

                await _dbContext.SaveChangesAsync();
            }

            return (file, "", HttpStatusCode.OK);
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

        private async Task<int> GetCountOfActiveMolensWithImages() => await _dbContext.MolenData
            .Where(m => m.Toestand == MolenToestand.Werkend && m.AddedImages.Any())
            .CountAsync();

        private async Task<int> GetCountOfRemainderMolensWithImage() => await _dbContext.MolenData
            .Where(m => m.Toestand == MolenToestand.Restant && m.AddedImages.Any())
            .CountAsync();

        private async Task<int> GetCountOfMolensWithAddedImage(CancellationToken token = default) => await _dbContext.AddedImages
            .AsNoTracking()
            .Select(image => image.MolenDataId)
            .Distinct()
            .CountAsync(token);

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
            int totalMolensWithImage = await GetCountOfMolensWithAddedImage();

            int totalActiveMolens = await GetCountOfActiveMolens();
            int totalRemainderMolens = await GetCountOfRemainderMolens();
            int totalExistingMolens = totalActiveMolens + totalRemainderMolens;

            List<CountDisappearedMolens> totalDisappearedMolens = await GetCountOfDisappearedMolens();

            int totalMolens = await GetCountMolens();

            var recentImages = _dbContext.AddedImages
                .Where(ai => ai.DateTaken >= DateTime.Now.AddDays(-7) && ai.DateTaken <= DateTime.Now)
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
            var totalMolensWithImage = await GetCountOfMolensWithAddedImage(token);

            var now = DateTime.Now;
            var recentImageStart = now.AddDays(-7);

            var recentImages = await _dbContext.AddedImages
                .AsNoTracking()
                .Where(image => image.DateTaken >= recentImageStart && image.DateTaken <= now)
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
            var allowedMolenTypes = Globals.AllowedMolenTypes
                .Select(type => type.ToLower())
                .ToList();

            var query = _dbContext.MolenData
                .AsNoTracking()
                .Where(molen =>
                    molen.Latitude >= -90d &&
                    molen.Latitude <= 90d &&
                    molen.Longitude >= -180d &&
                    molen.Longitude <= 180d &&
                    molen.MolenTypeAssociations.Any(association =>
                        allowedMolenTypes.Contains(association.MolenType.Name.ToLower())));

            query = ApplyMapFilters(query, filter);

            return await _mapClusterService.GetMapItemsAsync(
                query,
                (queryWest, querySouth, queryEast, queryNorth) => molen =>
                    molen.Latitude >= querySouth &&
                    molen.Latitude <= queryNorth &&
                    molen.Longitude >= queryWest &&
                    molen.Longitude <= queryEast,
                filteredQuery => MapQueryBuilder.Create(
                    filteredQuery,
                    filter.Zoom,
                    molen => molen.Id,
                    molen => molen.Name,
                    molen => molen.Latitude,
                    molen => molen.Longitude,
                    molen => molen.MercatorY,
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

        private IQueryable<MolenData> ApplyMapFilters(IQueryable<MolenData> query, MolenMapFilter filter)
        {
            var molenState = MolenToestand.From(filter.MolenState);

            if (MolenToestand.Equals(molenState, MolenToestand.Bestaande))
            {
                query = query.Where(molen => molen.Toestand != null && molen.Toestand != MolenToestand.Verdwenen);
            }
            else if (!string.IsNullOrWhiteSpace(molenState))
            {
                query = query.Where(molen => molen.Toestand != null && molen.Toestand.ToLower() == molenState.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.MolenType))
            {
                var molenType = filter.MolenType.Trim().ToLower();
                query = query.Where(molen => molen.MolenTypeAssociations.Any(association => association.MolenType.Name.ToLower() == molenType));
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

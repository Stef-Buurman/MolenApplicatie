using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenDataService : DBDefaultService<MolenData>
    {
        private readonly DBMolenAddedImageService _dBMolenAddedImageService;
        private readonly DBMolenDissappearedYearsService _dBMolenDissappearedYearsService;
        private readonly DBMolenImageService _dBMolenImageService;
        private readonly DBMolenMakerService _dBMolenMakerService;
        private readonly DBMolenTBNService _dBMolenTBNService;
        private readonly DBMolenTypeAssociationService _dBMolenTypeAssociationService;
        public DBMolenDataService(MolenDbContext context,
            DBMolenAddedImageService dBMolenAddedImageService,
            DBMolenDissappearedYearsService dBMolenDissappearedYearsService,
            DBMolenImageService dBMolenImageService,
            DBMolenMakerService dBMolenMakerService,
            DBMolenTBNService dBMolenTBNService,
            DBMolenTypeAssociationService dBMolenTypeAssociationService) : base(context)
        {
            _dBMolenAddedImageService = dBMolenAddedImageService;
            _dBMolenDissappearedYearsService = dBMolenDissappearedYearsService;
            _dBMolenImageService = dBMolenImageService;
            _dBMolenMakerService = dBMolenMakerService;
            _dBMolenTBNService = dBMolenTBNService;
            _dBMolenTypeAssociationService = dBMolenTypeAssociationService;
        }

        public override async Task<List<MolenData>> GetAllAsync()
        {
            return await _dbSet.Include(e => e.MolenTBN)
                .Include(e => e.MolenTypeAssociations)
                    .ThenInclude(e => e.MolenType)
                .Include(e => e.Images)
                .Include(e => e.MolenMakers)
                .Include(e => e.AddedImages)
                .Include(e => e.DisappearedYearInfos)
                .ToListAsync();
        }

        public override bool Exists(MolenData molenData, out MolenData? existing)
        {
            return Exists(e => e.Ten_Brugge_Nr == molenData.Ten_Brugge_Nr, out existing);
        }

        public override bool ExistsRange(List<MolenData> entities,
            out List<MolenData> matchingEntities,
            out List<MolenData> newEntities,
            out List<MolenData> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => e.Ten_Brugge_Nr,
                y => e => e.Ten_Brugge_Nr == y.Ten_Brugge_Nr,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat
            );
        }

        public override async Task<MolenData> Add(MolenData entity, CancellationToken token = default)
        {
            if (entity == null)
                return null;

            if (entity.MolenTBN != null)
            {
                var tbn = await _dBMolenTBNService.AddOrUpdate(entity.MolenTBN, token);
                entity.MolenTBNId = tbn.Id;
                entity.MolenTBN = null;
            }

            var relTypes = entity.MolenTypeAssociations?.ToList() ?? new();
            var relImages = entity.Images?.ToList() ?? new();
            var relMakers = entity.MolenMakers?.ToList() ?? new();
            var relAddedImages = entity.AddedImages?.ToList() ?? new();
            var relDisappearedYears = entity.DisappearedYearInfos?.ToList() ?? new();

            entity.MolenTypeAssociations = null;
            entity.Images = null;
            entity.MolenMakers = null;
            entity.AddedImages = null;
            entity.DisappearedYearInfos = null;

            entity = await base.Add(entity, token);

            if (relTypes?.Any() == true)
            {
                foreach (var association in relTypes)
                    association.MolenDataId = entity.Id;

                await _dBMolenTypeAssociationService.AddOrUpdateRange(relTypes, token);
            }

            if (relImages?.Any() == true)
            {
                foreach (var image in relImages)
                    image.MolenDataId = entity.Id;

                await _dBMolenImageService.AddOrUpdateRange(relImages, token);
            }

            if (relMakers?.Any() == true)
            {
                foreach (var maker in relMakers)
                    maker.MolenDataId = entity.Id;

                await _dBMolenMakerService.AddOrUpdateRange(relMakers, token);
            }

            if (relAddedImages?.Any() == true)
            {
                foreach (var addedImage in relAddedImages)
                    addedImage.MolenDataId = entity.Id;

                await _dBMolenAddedImageService.AddOrUpdateRange(relAddedImages, token);
            }

            if (relDisappearedYears?.Any() == true)
            {
                foreach (var year in relDisappearedYears)
                    year.MolenDataId = entity.Id;

                await _dBMolenDissappearedYearsService.AddOrUpdateRange(relDisappearedYears, token);
            }

            return entity;
        }


        public override async Task<MolenData?> Update(MolenData entity, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entity == null) return null;

            token.ThrowIfCancellationRequested();

            if (entity.MolenTBN != null)
            {
                entity.MolenTBN.MolenData = null;
                var tbn = await _dBMolenTBNService.AddOrUpdate(entity.MolenTBN, token, strat);
                entity.MolenTBNId = tbn.Id;
                entity.MolenTBN = null;
            }

            var relTypes = entity.MolenTypeAssociations?.ToList() ?? new();
            var relImages = entity.Images?.ToList() ?? new();
            var relMakers = entity.MolenMakers?.ToList() ?? new();
            var relAddedImages = entity.AddedImages?.ToList() ?? new();
            var relDisappearedYears = entity.DisappearedYearInfos?.ToList() ?? new();

            entity.MolenTypeAssociations = null;
            entity.Images = null;
            entity.MolenMakers = null;
            entity.AddedImages = null;
            entity.DisappearedYearInfos = null;

            var tracked = await _dbSet.FirstOrDefaultAsync(e => e.Id == entity.Id, token);
            if (tracked != null)
            {
                CopyScalarsFrom(tracked, entity);
            }

            void SetForeignKeys<T>(List<T> list, Action<T, Guid> setter)
            {
                foreach (var item in list)
                {
                    setter(item, entity.Id);
                }
            }

            SetForeignKeys(relTypes, (item, id) =>
            {
                if (item is MolenTypeAssociation mta)
                {
                    mta.MolenDataId = id;
                    mta.MolenData = null;
                }
            });

            SetForeignKeys(relImages, (item, id) =>
            {
                if (item is MolenImage img)
                {
                    img.MolenDataId = id;
                    img.MolenData = null;
                }
            });

            SetForeignKeys(relMakers, (item, id) =>
            {
                if (item is MolenMaker maker)
                {
                    maker.MolenDataId = id;
                    maker.MolenData = null;
                }
            });

            SetForeignKeys(relAddedImages, (item, id) =>
            {
                if (item is AddedImage ai)
                {
                    ai.MolenDataId = id;
                    ai.MolenData = null;
                }
            });

            SetForeignKeys(relDisappearedYears, (item, id) =>
            {
                if (item is DisappearedYearInfo dy)
                {
                    dy.MolenDataId = id;
                    dy.MolenData = null;
                }
            });

            await _dBMolenTypeAssociationService.AddOrUpdateRange(relTypes, token, strat);
            await _dBMolenImageService.AddOrUpdateRange(relImages, token, strat);
            await _dBMolenMakerService.AddOrUpdateRange(relMakers, token, strat);
            await _dBMolenAddedImageService.AddOrUpdateRange(relAddedImages, token, strat);
            await _dBMolenDissappearedYearsService.AddOrUpdateRange(relDisappearedYears, token, strat);

            _cache.Update(entity);

            return entity;
        }

        public override async Task<List<MolenData>> UpdateRange(List<MolenData> entities, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entities == null || entities.Count == 0) return entities;
            entities = entities.Where(e => e != null).ToList();

            var entityIds = entities.Select(e => e.Id).ToList();
            var allTBNs = await _dBMolenTBNService
                .AddOrUpdateRange(entities.Where(e => e.MolenTBN != null).Select(e => e.MolenTBN!).ToList(), token, strat);

            var existingMolens = await _dbSet
                .Where(e => entityIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, token);

            var relTypes = new Dictionary<Guid, List<MolenTypeAssociation>>();
            var relImages = new Dictionary<Guid, List<MolenImage>>();
            var relMakers = new Dictionary<Guid, List<MolenMaker>>();
            var relAddedImages = new Dictionary<Guid, List<AddedImage>>();
            var relDisappearedYears = new Dictionary<Guid, List<DisappearedYearInfo>>();

            foreach (var entity in entities)
            {
                token.ThrowIfCancellationRequested();

                var matchedTBN = allTBNs.FirstOrDefault(t => t.Equals(entity.MolenTBN));
                if (matchedTBN != null)
                    entity.MolenTBNId = matchedTBN.Id;
                entity.MolenTBN = null;

                relTypes[entity.Id] = entity.MolenTypeAssociations?.ToList() ?? new();
                relImages[entity.Id] = entity.Images?.ToList() ?? new();
                relMakers[entity.Id] = entity.MolenMakers?.ToList() ?? new();
                relAddedImages[entity.Id] = entity.AddedImages?.ToList() ?? new();
                relDisappearedYears[entity.Id] = entity.DisappearedYearInfos?.ToList() ?? new();

                entity.MolenTypeAssociations = null;
                entity.Images = null;
                entity.MolenMakers = null;
                entity.AddedImages = null;
                entity.DisappearedYearInfos = null;
                token.ThrowIfCancellationRequested();
                if (existingMolens.TryGetValue(entity.Id, out var tracked)) CopyScalarsFrom(tracked, entity);
            }

            void SetForeignKeys<T>(Dictionary<Guid, List<T>> map, Action<T, Guid> setter)
            {
                foreach (var (id, items) in map)
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var item in items)
                    {
                        setter(item, id);
                    }
                }
            }

            SetForeignKeys(relTypes, (item, id) =>
            {
                if (item is MolenTypeAssociation mta)
                {
                    mta.MolenDataId = id;
                    mta.MolenData = null;
                }
            });

            SetForeignKeys(relImages, (item, id) =>
            {
                if (item is MolenImage img)
                {
                    img.MolenDataId = id;
                    img.MolenData = null;
                }
            });

            SetForeignKeys(relMakers, (item, id) =>
            {
                if (item is MolenMaker maker)
                {
                    maker.MolenDataId = id;
                    maker.MolenData = null;
                }
            });

            SetForeignKeys(relAddedImages, (item, id) =>
            {
                if (item is AddedImage ai)
                {
                    ai.MolenDataId = id;
                    ai.MolenData = null;
                }
            });

            SetForeignKeys(relDisappearedYears, (item, id) =>
            {
                if (item is DisappearedYearInfo dy)
                {
                    dy.MolenDataId = id;
                    dy.MolenData = null;
                }
            });

            var allTypeAss = relTypes.SelectMany(x => x.Value).ToList();
            var allImages = relImages.SelectMany(x => x.Value).ToList();
            var allMakers = relMakers.SelectMany(x => x.Value).ToList();
            var allAddedImages = relAddedImages.SelectMany(x => x.Value).ToList();
            var allDisappearedYears = relDisappearedYears.SelectMany(x => x.Value).ToList();
            await _dBMolenTypeAssociationService.AddOrUpdateRange(allTypeAss, token, strat);
            await _dBMolenImageService.AddOrUpdateRange(allImages, token, strat);
            await _dBMolenMakerService.AddOrUpdateRange(allMakers, token, strat);
            await _dBMolenAddedImageService.AddOrUpdateRange(allAddedImages, token, strat);
            await _dBMolenDissappearedYearsService.AddOrUpdateRange(allDisappearedYears, token, strat);

            _cache.UpdateRange(entities);

            //await CheckToDeleteProperties(entitiesCopy);

            return entities;
        }

        public override async Task AddRangeAsync(List<MolenData> entities, CancellationToken token = default)
        {
            if (entities == null || entities.Count() == 0) return;
            entities = entities.Where(e => e != null).ToList();
            var allTBNs = entities.Where(x => x != null).Select(e => e.MolenTBN).Where(x => x != null).ToList();


            Dictionary<string, List<MolenTypeAssociation>> allTypeAssMolens = new Dictionary<string, List<MolenTypeAssociation>>();
            Dictionary<string, List<MolenImage>> allImagesMolens = new Dictionary<string, List<MolenImage>>();
            Dictionary<string, List<MolenMaker>> allMakersMolens = new Dictionary<string, List<MolenMaker>>();
            Dictionary<string, List<AddedImage>> allAddedImagesMolens = new Dictionary<string, List<AddedImage>>();
            Dictionary<string, List<DisappearedYearInfo>> allDisappearedYearsMolens = new Dictionary<string, List<DisappearedYearInfo>>();

            allTBNs = await _dBMolenTBNService.AddOrUpdateRange(allTBNs, token);
            List<MolenData> entitiesToAdd = new List<MolenData>();

            foreach (var entity in entities)
            {
                if (entity.MolenTBN != null)
                {
                    var matchedTbn = allTBNs.FirstOrDefault(t => t.Equals(entity.MolenTBN));
                    if (matchedTbn != null)
                    {
                        entity.MolenTBNId = matchedTbn.Id;
                        var entity3 = entity;
                        entity3.MolenTBN = null;
                        matchedTbn.MolenData = entity3;
                        entity.MolenTBN = null;
                    }
                }

                if (entity.MolenTypeAssociations != null)
                {
                    allTypeAssMolens[entity.Ten_Brugge_Nr] = entity.MolenTypeAssociations;
                }
                if (entity.Images != null)
                {
                    allImagesMolens[entity.Ten_Brugge_Nr] = entity.Images;
                }
                if (entity.MolenMakers != null)
                {
                    allMakersMolens[entity.Ten_Brugge_Nr] = entity.MolenMakers;
                }
                if (entity.AddedImages != null)
                {
                    allAddedImagesMolens[entity.Ten_Brugge_Nr] = entity.AddedImages;
                }
                if (entity.DisappearedYearInfos != null)
                {
                    allDisappearedYearsMolens[entity.Ten_Brugge_Nr] = entity.DisappearedYearInfos;
                }

                entity.MolenTypeAssociations = null;
                entity.Images = null;
                entity.MolenMakers = null;
                entity.AddedImages = null;
                entity.DisappearedYearInfos = null;
                entitiesToAdd.Add(entity);
            }
            await _dbSet.AddRangeAsync(entitiesToAdd, token);
            _cache.AddRange(entitiesToAdd);

            void Attach<T>(Dictionary<string, List<T>> all, Func<string, Guid?> getForeignKey, Action<T, Guid> setForeignKey)
            {
                foreach (var entity in all)
                {
                    var foreignKey = getForeignKey(entity.Key);
                    if (foreignKey.HasValue)
                    {
                        foreach (var item in entity.Value)
                        {
                            setForeignKey(item, foreignKey.Value);
                        }
                    }
                }
            }

            var getMolenIdByTBN = new Func<string, Guid?>(tbn =>
            {
                return entitiesToAdd.FirstOrDefault(e => e.Ten_Brugge_Nr == tbn)?.Id;
            });

            Attach(allTypeAssMolens,
                    getMolenIdByTBN,
                    (x, id) =>
                    {
                        (x as MolenTypeAssociation)!.MolenDataId = id;
                        (x as MolenTypeAssociation)!.MolenData = null;
                    });
            Attach(allImagesMolens,
                    getMolenIdByTBN,
                    (x, id) =>
                    {
                        (x as MolenImage)!.MolenDataId = id;
                        (x as MolenImage)!.MolenData = null;
                    });
            Attach(allMakersMolens,
                    getMolenIdByTBN,
                    (x, id) =>
                    {
                        (x as MolenMaker)!.MolenDataId = id;
                        (x as MolenMaker)!.MolenData = null;
                    });
            Attach(allAddedImagesMolens,
                    getMolenIdByTBN,
                    (x, id) =>
                    {
                        (x as AddedImage)!.MolenDataId = id;
                        (x as AddedImage)!.MolenData = null;
                    });
            Attach(allDisappearedYearsMolens,
                    getMolenIdByTBN,
                    (x, id) =>
                    {
                        (x as DisappearedYearInfo)!.MolenDataId = id;
                        (x as DisappearedYearInfo)!.MolenData = null;
                    }
                );

            List<MolenTypeAssociation> allTypeAssToAdd = allTypeAssMolens
                .SelectMany(x => x.Value)
                .Where(x => x != null)
                .ToList();
            List<MolenImage> allImagesToAdd = allImagesMolens
                .SelectMany(x => x.Value)
                .Where(x => x != null)
                .ToList();
            List<MolenMaker> allMakersToAdd = allMakersMolens
                .SelectMany(x => x.Value)
                .Where(x => x != null)
                .ToList();
            List<AddedImage> allAddedImagesToAdd = allAddedImagesMolens
                .SelectMany(x => x.Value)
                .Where(x => x != null)
                .ToList();
            List<DisappearedYearInfo> allDisappearedYearsToAdd = allDisappearedYearsMolens
                .SelectMany(x => x.Value)
                .Where(x => x != null)
                .ToList();

            await _dBMolenTypeAssociationService.AddOrUpdateRange(allTypeAssToAdd, token);
            await _dBMolenImageService.AddOrUpdateRange(allImagesToAdd, token);
            await _dBMolenMakerService.AddOrUpdateRange(allMakersToAdd, token);
            await _dBMolenAddedImageService.AddOrUpdateRange(allAddedImagesToAdd, token);
            await _dBMolenDissappearedYearsService.AddOrUpdateRange(allDisappearedYearsToAdd, token);
        }

        public async Task CheckToDeleteProperties(List<MolenData> molens)
        {
            var molenIds = molens.Select(m => m.Id).ToList();
            var images = await _dBMolenAddedImageService.GetImagesOfMolens(molenIds);
            var addedImages = await _dBMolenImageService.GetImagesOfMolens(molenIds);
            var makers = await _dBMolenMakerService.GetMakersOfMolens(molenIds);
            var typeAssociations = await _dBMolenTypeAssociationService.GetMolenTypeAssociationsOfMolens(molenIds);
            var dissappearedYears = await _dBMolenDissappearedYearsService.GetDissappearedYearsOfMolens(molenIds);
            foreach (var molenData in molens)
            {
                if (molenData == null)
                    return;

                if (images.TryGetValue(molenData.Id, out var molenImages))
                {
                    await CheckAndDeleteOrphanedItems(
                        molenImages,
                        molenData.AddedImages,
                        x => x.FilePath,
                        _dBMolenAddedImageService.Delete
                    );
                }
                if (addedImages.TryGetValue(molenData.Id, out var molenAddedImages))
                {
                    await CheckAndDeleteOrphanedItems(
                        molenAddedImages,
                        molenData.Images,
                        x => x.FilePath,
                        _dBMolenImageService.Delete
                    );
                }
                if (makers.TryGetValue(molenData.Id, out var molenMakers))
                {
                    await CheckAndDeleteOrphanedItems(
                        molenMakers,
                        molenData.MolenMakers,
                        x => x.Name,
                        _dBMolenMakerService.Delete
                    );
                }
                if (typeAssociations.TryGetValue(molenData.Id, out var molenTypeAssociations))
                {
                    await CheckAndDeleteOrphanedItems(
                        molenTypeAssociations,
                        molenData.MolenTypeAssociations,
                        x => x.MolenTypeId.ToString(),
                        _dBMolenTypeAssociationService.Delete
                    );
                }
                if (dissappearedYears.TryGetValue(molenData.Id, out var molenDissappearedYears))
                {
                    await CheckAndDeleteOrphanedItems(
                        molenDissappearedYears,
                        molenData.DisappearedYearInfos,
                        x => x.Year.ToString(),
                        _dBMolenDissappearedYearsService.Delete
                    );
                }
            }
        }

        private async Task CheckAndDeleteOrphanedItems<T>(
            List<T> dbItems,
            List<T>? currentItems,
            Func<T, string> keySelector,
            Func<T, Task> deleteFunc)
        {
            var currentKeys = new HashSet<string>(currentItems?.Select(keySelector) ?? Enumerable.Empty<string>());

            var duplicateKeys = dbItems
                .GroupBy(keySelector)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            foreach (var item in dbItems)
            {
                var key = keySelector(item);
                var isNotInCurrent = !currentKeys.Contains(key);
                var isDuplicate = duplicateKeys.Contains(key);

                if (isNotInCurrent || isDuplicate)
                {
                    await deleteFunc(item);
                    duplicateKeys.Remove(key);
                }
            }
        }

        public override async Task Delete(MolenData molenData)
        {
            MolenData? molenDataToDelete = await GetById(molenData.Id);
            if (molenDataToDelete == null)
            {
                return;
            }

            await _dBMolenAddedImageService.DeleteRange(molenDataToDelete.AddedImages);
            await _dBMolenDissappearedYearsService.DeleteRange(molenDataToDelete.DisappearedYearInfos);
            await _dBMolenImageService.DeleteRange(molenDataToDelete.Images);
            await _dBMolenMakerService.DeleteRange(molenDataToDelete.MolenMakers);
            await _dBMolenTBNService.Delete(molenDataToDelete.MolenTBN);
            await _dBMolenTypeAssociationService.DeleteRange(molenDataToDelete.MolenTypeAssociations);
            _context.MolenData.Remove(molenDataToDelete);
        }

        public void CopyScalarsFrom(MolenData target, MolenData source)
        {
            target.MolenTBNId = source.MolenTBNId;
            target.Ten_Brugge_Nr = source.Ten_Brugge_Nr;
            target.Name = source.Name;
            target.ToelichtingNaam = source.ToelichtingNaam;
            target.Bouwjaar = source.Bouwjaar;
            target.HerbouwdJaar = source.HerbouwdJaar;
            target.BouwjaarStart = source.BouwjaarStart;
            target.BouwjaarEinde = source.BouwjaarEinde;
            target.Functie = source.Functie;
            target.Doel = source.Doel;
            target.Toestand = source.Toestand;
            target.Bedrijfsvaardigheid = source.Bedrijfsvaardigheid;
            target.Plaats = source.Plaats;
            target.Adres = source.Adres;
            target.Provincie = source.Provincie;
            target.Gemeente = source.Gemeente;
            target.Streek = source.Streek;
            target.Plaatsaanduiding = source.Plaatsaanduiding;
            target.Opvolger = source.Opvolger;
            target.Voorganger = source.Voorganger;
            target.VerplaatstNaar = source.VerplaatstNaar;
            target.AfkomstigVan = source.AfkomstigVan;
            target.Literatuur = source.Literatuur;
            target.PlaatsenVoorheen = source.PlaatsenVoorheen;
            target.Wiekvorm = source.Wiekvorm;
            target.WiekVerbeteringen = source.WiekVerbeteringen;
            target.Monument = source.Monument;
            target.PlaatsBediening = source.PlaatsBediening;
            target.BedieningKruiwerk = source.BedieningKruiwerk;
            target.PlaatsKruiwerk = source.PlaatsKruiwerk;
            target.Kruiwerk = source.Kruiwerk;
            target.Vlucht = source.Vlucht;
            target.Openingstijden = source.Openingstijden;
            target.OpenVoorPubliek = source.OpenVoorPubliek;
            target.OpenOpZaterdag = source.OpenOpZaterdag;
            target.OpenOpZondag = source.OpenOpZondag;
            target.OpenOpAfspraak = source.OpenOpAfspraak;
            target.Krachtbron = source.Krachtbron;
            target.Website = source.Website;
            target.WinkelInformatie = source.WinkelInformatie;
            target.Bouwbestek = source.Bouwbestek;
            target.Bijzonderheden = source.Bijzonderheden;
            target.Museuminformatie = source.Museuminformatie;
            target.Molenaar = source.Molenaar;
            target.Eigendomshistorie = source.Eigendomshistorie;
            target.Molenerf = source.Molenerf;
            target.Trivia = source.Trivia;
            target.Geschiedenis = source.Geschiedenis;
            target.Wetenswaardigheden = source.Wetenswaardigheden;
            target.Wederopbouw = source.Wederopbouw;
            target.As = source.As;
            target.Wieken = source.Wieken;
            target.Toegangsprijzen = source.Toegangsprijzen;
            target.UniekeEigenschap = source.UniekeEigenschap;
            target.LandschappelijkeWaarde = source.LandschappelijkeWaarde;
            target.KadastraleAanduiding = source.KadastraleAanduiding;
            target.CanAddImages = source.CanAddImages;
            target.Eigenaar = source.Eigenaar;
            target.RecenteWerkzaamheden = source.RecenteWerkzaamheden;
            target.Rad = source.Rad;
            target.RadDiameter = source.RadDiameter;
            target.Wateras = source.Wateras;
            target.Latitude = source.Latitude;
            target.Longitude = source.Longitude;
            target.LastUpdated = source.LastUpdated;
        }
    }
}

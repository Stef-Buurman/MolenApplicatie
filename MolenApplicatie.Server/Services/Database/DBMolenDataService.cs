using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
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

        public override bool Exists(MolenData molenData, out MolenData? existing)
        {
            return Exists(e => e.Ten_Brugge_Nr == molenData.Ten_Brugge_Nr, out existing);
        }

        public override bool ExistsRange(List<MolenData> entities, out List<MolenData> matchingEntities, out List<MolenData> newEntities, out List<MolenData> updatedEntities)
        {
            return ExistsRange(
                entities,
                e => e.Ten_Brugge_Nr,
                y => e => e.Ten_Brugge_Nr == y.Ten_Brugge_Nr,
                out matchingEntities,
                out newEntities,
                out updatedEntities
            );
        }

        public override async Task<MolenData> Add(MolenData molenData)
        {
            var TypeAss = molenData.MolenTypeAssociations.ToList();
            var images = molenData.Images.ToList();
            var makers = molenData.MolenMakers.ToList();
            var addedImages = molenData.AddedImages.ToList();
            var dissappearedYears = molenData.DisappearedYearInfos.ToList();
            var tbn = molenData.MolenTBN;
            addedImages = await _dBMolenAddedImageService.AddOrUpdateRange(addedImages);
            dissappearedYears = await _dBMolenDissappearedYearsService.AddOrUpdateRange(dissappearedYears);
            images = await _dBMolenImageService.AddOrUpdateRange(images);
            makers = await _dBMolenMakerService.AddOrUpdateRange(makers);
            tbn = await _dBMolenTBNService.AddOrUpdate(tbn);
            TypeAss = await _dBMolenTypeAssociationService.AddOrUpdateRange(TypeAss);

            molenData.AddedImages.Clear();
            molenData.DisappearedYearInfos.Clear();
            molenData.Images.Clear();
            molenData.MolenMakers.Clear();
            molenData.MolenTypeAssociations.Clear();
            molenData.MolenTBN = null!;

            await base.Add(molenData);

            molenData.AddedImages = addedImages;
            molenData.DisappearedYearInfos = dissappearedYears;
            molenData.Images = images;
            molenData.MolenMakers = makers;
            molenData.MolenTypeAssociations = TypeAss;
            molenData.MolenTBN = tbn;

            return molenData;
        }

        public override async Task<MolenData> Update(MolenData molenData)
        {
            var TypeAss = molenData.MolenTypeAssociations.ToList();
            var images = molenData.Images.ToList();
            var makers = molenData.MolenMakers.ToList();
            var addedImages = molenData.AddedImages.ToList();
            var dissappearedYears = molenData.DisappearedYearInfos.ToList();
            var tbn = molenData.MolenTBN;

            molenData.AddedImages.Clear();
            molenData.DisappearedYearInfos.Clear();
            molenData.Images.Clear();
            molenData.MolenMakers.Clear();
            molenData.MolenTypeAssociations.Clear();
            molenData.MolenTBN = null!;

            tbn = await _dBMolenTBNService.AddOrUpdate(tbn);
            molenData.MolenTBNId = tbn.Id;

            molenData = await base.Update(molenData);

            images.ForEach(mak =>
            {
                mak.MolenDataId = molenData.Id;
                mak.MolenData = mak.MolenData;
            });
            makers.ForEach(mak =>
            {
                mak.MolenDataId = molenData.Id;
                mak.MolenData = mak.MolenData;
            });
            addedImages.ForEach(mak =>
            {
                mak.MolenDataId = molenData.Id;
                mak.MolenData = mak.MolenData;
            });
            dissappearedYears.ForEach(mak =>
            {
                mak.MolenDataId = molenData.Id;
                mak.MolenData = mak.MolenData;
            });
            TypeAss.ForEach(mak =>
            {
                mak.MolenData = molenData;
            });
            TypeAss = await _dBMolenTypeAssociationService.AddOrUpdateRange(TypeAss);
            images = await _dBMolenImageService.AddOrUpdateRange(images);
            makers = await _dBMolenMakerService.AddOrUpdateRange(makers);
            addedImages = await _dBMolenAddedImageService.AddOrUpdateRange(addedImages);
            dissappearedYears = await _dBMolenDissappearedYearsService.AddOrUpdateRange(dissappearedYears);
            molenData.AddedImages = addedImages;
            molenData.DisappearedYearInfos = dissappearedYears;
            molenData.Images = images;
            molenData.MolenMakers = makers;
            molenData.MolenTypeAssociations = TypeAss;
            molenData.MolenTBN = tbn;
            return molenData;
        }

        public override async Task<List<MolenData>> AddOrUpdateRange(List<MolenData> entities)
        {
            if (entities == null || entities.Count == 0)
                return entities;

            var existing = await _cache.GetAllAsync();
            var toAdd = new List<MolenData>();
            var toUpdate = new List<MolenData>();

            foreach (var entity in entities)
            {
                var existingEntity = existing.FirstOrDefault(e => e.Equals(entity));
                if (existingEntity != null)
                {
                    if (entity.Id == Guid.Empty && existingEntity.Id != Guid.Empty)
                        entity.Id = existingEntity.Id;

                    toUpdate.Add(entity);
                }
                else
                {
                    toAdd.Add(entity);
                }
            }

            await UpdateRange(toUpdate);
            await AddRangeAsync(toAdd);

            return toUpdate.Concat(toAdd).ToList();
        }

        private void DetachEntity(object entity)
        {
            var entry = _context.Entry(entity);
            if (entry != null)
                entry.State = EntityState.Detached;
        }

        public virtual async Task<List<MolenData>> UpdateRange(List<MolenData> entities)
        {
            if (entities == null || entities.Count == 0)
                return entities;

            var allTBNs = entities.Select(e => e.MolenTBN).Where(x => x != null).ToList();

            allTBNs = await _dBMolenTBNService.AddOrUpdateRange(allTBNs);

            var allTypeAssMolens = new Dictionary<string, List<MolenTypeAssociation>>();
            var allImagesMolens = new Dictionary<string, List<MolenImage>>();
            var allMakersMolens = new Dictionary<string, List<MolenMaker>>();
            var allAddedImagesMolens = new Dictionary<string, List<AddedImage>>();
            var allDisappearedYearsMolens = new Dictionary<string, List<DisappearedYearInfo>>();

            foreach (var entity in entities)
            {
                var matchedTBN = allTBNs.FirstOrDefault(t => t.Equals(entity.MolenTBN));
                if (matchedTBN != null)
                {
                    entity.MolenTBNId = matchedTBN.Id;
                }
                entity.MolenTBN = null;

                var trackedEntity = _context.ChangeTracker.Entries<MolenData>()
                    .FirstOrDefault(e => e.Entity.Id == entity.Id);

                if (trackedEntity != null)
                {
                    trackedEntity.State = EntityState.Detached;
                }

                _context.Entry(entity).State = EntityState.Modified;

                //if (entity.MolenTypeAssociations != null)
                //{
                //    allTypeAssMolens[entity.Ten_Brugge_Nr] = entity.MolenTypeAssociations;
                //    foreach (var obj in entity.MolenTypeAssociations.ToList())
                //        DetachEntity(obj);
                //}

                //if (entity.Images != null)
                //{
                //    allImagesMolens[entity.Ten_Brugge_Nr] = entity.Images;
                //    foreach (var obj in entity.Images.ToList())
                //        DetachEntity(obj);
                //}

                //if (entity.MolenMakers != null)
                //{
                //    allMakersMolens[entity.Ten_Brugge_Nr] = entity.MolenMakers;
                //    foreach (var obj in entity.MolenMakers.ToList())
                //        DetachEntity(obj);
                //}

                //if (entity.AddedImages != null)
                //{
                //    allAddedImagesMolens[entity.Ten_Brugge_Nr] = entity.AddedImages;
                //    foreach (var obj in entity.AddedImages.ToList())
                //        DetachEntity(obj);
                //}

                //if (entity.DisappearedYearInfos != null)
                //{
                //    allDisappearedYearsMolens[entity.Ten_Brugge_Nr] = entity.DisappearedYearInfos;
                //    foreach (var obj in entity.DisappearedYearInfos.ToList())
                //        DetachEntity(obj);
                //}

                entity.MolenTypeAssociations = null;
                entity.Images = null;
                entity.MolenMakers = null;
                entity.AddedImages = null;
                entity.DisappearedYearInfos = null;
            }

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
                entities.FirstOrDefault(e => e.Ten_Brugge_Nr == tbn)?.Id
            );

            Attach(allTypeAssMolens, getMolenIdByTBN, (x, id) =>
            {
                var obj = x as MolenTypeAssociation;
                if (obj != null)
                {
                    obj.MolenDataId = id;
                    obj.MolenData = null;
                }
            });

            Attach(allImagesMolens, getMolenIdByTBN, (x, id) =>
            {
                var obj = x as MolenImage;
                if (obj != null)
                {
                    obj.MolenDataId = id;
                    obj.MolenData = null;
                }
            });

            Attach(allMakersMolens, getMolenIdByTBN, (x, id) =>
            {
                var obj = x as MolenMaker;
                if (obj != null)
                {
                    obj.MolenDataId = id;
                    obj.MolenData = null;
                }
            });

            Attach(allAddedImagesMolens, getMolenIdByTBN, (x, id) =>
            {
                var obj = x as AddedImage;
                if (obj != null)
                {
                    obj.MolenDataId = id;
                    obj.MolenData = null;
                }
            });

            Attach(allDisappearedYearsMolens, getMolenIdByTBN, (x, id) =>
            {
                var obj = x as DisappearedYearInfo;
                if (obj != null)
                {
                    obj.MolenDataId = id;
                    obj.MolenData = null;
                }
            });

            var allTypeAss = allTypeAssMolens.SelectMany(x => x.Value).ToList();
            var allImages = allImagesMolens.SelectMany(x => x.Value).ToList();
            var allMakers = allMakersMolens.SelectMany(x => x.Value).ToList();
            var allAddedImages = allAddedImagesMolens.SelectMany(x => x.Value).ToList();
            var allDisappearedYears = allDisappearedYearsMolens.SelectMany(x => x.Value).ToList();

            await _dBMolenTypeAssociationService.AddOrUpdateRange(allTypeAss);
            await _dBMolenImageService.AddOrUpdateRange(allImages);
            await _dBMolenMakerService.AddOrUpdateRange(allMakers);
            await _dBMolenAddedImageService.AddOrUpdateRange(allAddedImages);
            await _dBMolenDissappearedYearsService.AddOrUpdateRange(allDisappearedYears);

            _dbSet.UpdateRange(entities);
            _cache.UpdateRange(entities);

            return entities;
        }


        public override async Task AddRangeAsync(List<MolenData> entities)
        {
            if (entities == null || entities.Count() == 0) return;
            var allTBNs = entities.Select(e => e.MolenTBN).Where(x => x != null).ToList();


            Dictionary<string, List<MolenTypeAssociation>> allTypeAssMolens = new Dictionary<string, List<MolenTypeAssociation>>();
            Dictionary<string, List<MolenImage>> allImagesMolens = new Dictionary<string, List<MolenImage>>();
            Dictionary<string, List<MolenMaker>> allMakersMolens = new Dictionary<string, List<MolenMaker>>();
            Dictionary<string, List<AddedImage>> allAddedImagesMolens = new Dictionary<string, List<AddedImage>>();
            Dictionary<string, List<DisappearedYearInfo>> allDisappearedYearsMolens = new Dictionary<string, List<DisappearedYearInfo>>();

            allTBNs = await _dBMolenTBNService.AddOrUpdateRange(allTBNs);
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
            await _dbSet.AddRangeAsync(entitiesToAdd);
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

            await _dBMolenTypeAssociationService.AddOrUpdateRange(allTypeAssToAdd);
            await _dBMolenImageService.AddOrUpdateRange(allImagesToAdd);
            await _dBMolenMakerService.AddOrUpdateRange(allMakersToAdd);
            await _dBMolenAddedImageService.AddOrUpdateRange(allAddedImagesToAdd);
            await _dBMolenDissappearedYearsService.AddOrUpdateRange(allDisappearedYearsToAdd);
        }

        public async Task CheckToDeleteProperties(MolenData molenData)
        {
            if (molenData == null)
            {
                return;
            }

            List<AddedImage> foundAddedImages = _dBMolenAddedImageService.GetImagesOfMolen(molenData.Id).Result;
            var duplicateFoundAddedImages = foundAddedImages.GroupBy(x => x.FilePath)
                .Where(g => g.Count() > 1)
                .Select(g => g.First())
                .ToList();
            foreach (var image in foundAddedImages)
            {
                if (molenData.AddedImages == null || molenData.AddedImages.Find(img => img.FilePath == image.FilePath) == null)
                {
                    await _dBMolenAddedImageService.Delete(image);
                }
                else
                {
                    var duplicate = duplicateFoundAddedImages.Find(dupe => dupe.FilePath == image.FilePath);
                    if (duplicate != null)
                    {
                        await _dBMolenAddedImageService.Delete(image);
                        duplicateFoundAddedImages.Remove(duplicate);
                    }
                }
            }

            List<MolenImage> FoundImages = _dBMolenImageService.GetImagesOfMolen(molenData.Id).Result;
            var duplicateFoundImages = FoundImages.GroupBy(x => x.FilePath)
                .Where(g => g.Count() > 1)
                .Select(g => g.First())
                .ToList();
            foreach (var image in FoundImages)
            {
                if (molenData.Images == null || molenData.Images.Find(img => img.FilePath == image.FilePath) == null)
                {
                    await _dBMolenImageService.Delete(image);
                }
                else
                {
                    var duplicate = duplicateFoundImages.Find(dupe => dupe.FilePath == image.FilePath);
                    if (duplicate != null)
                    {
                        await _dBMolenImageService.Delete(image);
                        duplicateFoundImages.Remove(duplicate);
                    }
                }
            }

            List<MolenMaker> FoundMakers = _dBMolenMakerService.GetMakersOfMolen(molenData.Id).Result;
            var duplicateFoundMakers = FoundMakers.GroupBy(x => x.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.First())
                .ToList();
            foreach (var maker in FoundMakers)
            {
                if (molenData.MolenMakers == null || molenData.MolenMakers.Find(mk => mk.Name == maker.Name) == null)
                {
                    await _dBMolenMakerService.Delete(maker);
                }
                else
                {
                    var duplicate = duplicateFoundMakers.Find(dupe => dupe.Name == maker.Name);
                    if (duplicate != null)
                    {
                        await _dBMolenMakerService.Delete(maker);
                        duplicateFoundMakers.Remove(duplicate);
                    }
                }
            }

            List<MolenTypeAssociation> FoundAssociations = _dBMolenTypeAssociationService.GetMolenTypeAssociationsOfMolen(molenData.Id).Result;
            var duplicateFoundAssociations = FoundAssociations.GroupBy(x => x.MolenTypeId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First())
                .ToList();

            foreach (var association in FoundAssociations)
            {
                if (molenData.MolenTypeAssociations == null || molenData.MolenTypeAssociations.Find(asoc => asoc.MolenTypeId == association.MolenTypeId) == null)
                {
                    await _dBMolenTypeAssociationService.Delete(association);
                }
                else
                {
                    var duplicate = duplicateFoundAssociations.Find(dupe => dupe.MolenTypeId == association.MolenTypeId);
                    if (duplicate != null)
                    {
                        await _dBMolenTypeAssociationService.Delete(association);
                        duplicateFoundAssociations.Remove(duplicate);
                    }
                }
            }

            List<DisappearedYearInfo> FoundDissappearedYears = _dBMolenDissappearedYearsService.GetDissappearedYearsOfMolen(molenData.Id).Result;
            var duplicateFoundDissappearedYears = FoundDissappearedYears.GroupBy(x => x.Year)
                .Where(g => g.Count() > 1)
                .Select(g => g.First())
                .ToList();
            foreach (var year in FoundDissappearedYears)
            {
                if (molenData.DisappearedYearInfos == null || molenData.DisappearedYearInfos.Find(yr => yr.Year == year.Year) == null)
                {
                    await _dBMolenDissappearedYearsService.Delete(year);
                }
                else
                {
                    var duplicate = duplicateFoundDissappearedYears.Find(dupe => dupe.Year == year.Year);
                    if (duplicate != null)
                    {
                        await _dBMolenDissappearedYearsService.Delete(year);
                        duplicateFoundDissappearedYears.Remove(duplicate);
                    }
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
    }
}

using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenDataService : DBDefaultService<MolenData>
    {
        private readonly MolenDbContext _context;
        private readonly DBMolenAddedImageService _dBMolenAddedImageService;
        private readonly DBMolenDissappearedYearsService _dBMolenDissappearedYearsService;
        private readonly DBMolenImageService _dBMolenImageService;
        private readonly DBMolenMakerService _dBMolenMakerService;
        private readonly DBMolenTypeService _dBMolenTypeService;
        private readonly DBMolenTBNService _dBMolenTBNService;
        private readonly DBMolenTypeAssociationService _dBMolenTypeAssociationService;

        public DBMolenDataService(MolenDbContext context,
            DBMolenAddedImageService dBMolenAddedImageService,
            DBMolenDissappearedYearsService dBMolenDissappearedYearsService,
            DBMolenImageService dBMolenImageService,
            DBMolenMakerService dBMolenMakerService,
            DBMolenTypeService dBMolenTypeService,
            DBMolenTBNService dBMolenTBNService,
            DBMolenTypeAssociationService dBMolenTypeAssociationService) : base(context)
        {
            _context = context;
            _dBMolenAddedImageService = dBMolenAddedImageService;
            _dBMolenDissappearedYearsService = dBMolenDissappearedYearsService;
            _dBMolenImageService = dBMolenImageService;
            _dBMolenMakerService = dBMolenMakerService;
            _dBMolenTypeService = dBMolenTypeService;
            _dBMolenTBNService = dBMolenTBNService;
            _dBMolenTypeAssociationService = dBMolenTypeAssociationService;
        }
        public override bool Exists(MolenData molenData, out MolenData? existing)
        {
            existing = _context.MolenData.FirstOrDefault(e => e.Ten_Brugge_Nr == molenData.Ten_Brugge_Nr);
            return existing != null;
        }

        public override async Task<MolenData> Add(MolenData molenData)
        {
            molenData.AddedImages = await _dBMolenAddedImageService.AddRange(molenData.AddedImages);
            molenData.DisappearedYearInfos = await _dBMolenDissappearedYearsService.AddRange(molenData.DisappearedYearInfos);
            molenData.Images = await _dBMolenImageService.AddRange(molenData.Images);
            molenData.MolenMakers = await _dBMolenMakerService.AddRange(molenData.MolenMakers);
            molenData.ModelTypes = await _dBMolenTypeService.AddRange(molenData.ModelTypes);
            molenData.MolenTBN = await _dBMolenTBNService.Add(molenData.MolenTBN);
            molenData.MolenTypeAssociations = await _dBMolenTypeAssociationService.AddRange(molenData.MolenTypeAssociations);

            await _context.MolenData.AddAsync(molenData);
            return molenData;
        }

        public override MolenData Update(MolenData molenData)
        {
            molenData.AddedImages = _dBMolenAddedImageService.UpdateRange(molenData.AddedImages);
            molenData.DisappearedYearInfos = _dBMolenDissappearedYearsService.UpdateRange(molenData.DisappearedYearInfos);
            molenData.Images = _dBMolenImageService.UpdateRange(molenData.Images);
            molenData.MolenMakers = _dBMolenMakerService.UpdateRange(molenData.MolenMakers);
            molenData.ModelTypes = _dBMolenTypeService.UpdateRange(molenData.ModelTypes);
            molenData.MolenTBN = _dBMolenTBNService.Update(molenData.MolenTBN);
            molenData.MolenTypeAssociations = _dBMolenTypeAssociationService.UpdateRange(molenData.MolenTypeAssociations);
            _context.MolenData.Update(molenData);
            CheckToDeleteProperties(molenData).Wait();
            return molenData;
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
                if (molenData.AddedImages.Find(img => img.FilePath == image.FilePath) == null)
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
                if (molenData.Images.Find(img => img.FilePath == image.FilePath) == null)
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
                if (molenData.MolenMakers.Find(mk => mk.Name == maker.Name) == null)
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
                if (molenData.MolenTypeAssociations.Find(asoc => asoc.MolenTypeId == association.MolenTypeId) == null)
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
                if (molenData.DisappearedYearInfos.Find(yr => yr.Year == year.Year) == null)
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

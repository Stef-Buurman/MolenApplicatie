using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;
using System.Linq;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenAddedImageService : DBDefaultService<AddedImage>
    {
        public DBMolenAddedImageService(MolenDbContext context) : base(context)
        { }

        public override bool Exists(AddedImage addedImage, out AddedImage? existing)
        {
            return Exists(e => e.FilePath == addedImage.FilePath, out existing);
        }
        public override bool ExistsRange(List<AddedImage> entities,
            out List<AddedImage> matchingEntities,
            out List<AddedImage> newEntities,
            out List<AddedImage> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => e.FilePath,
                y => e => e.FilePath == y.FilePath,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat
            );
        }

        public override async Task<AddedImage> Add(AddedImage addedImage, CancellationToken token = default)
        {
            if (!File.Exists(Globals.WWWROOTPath + addedImage.FilePath)) return addedImage;
            return await base.Add(addedImage);
        }

        public async Task<List<AddedImage>> GetImagesOfMolen(Guid MolenId)
        {
            var images = await _context.AddedImages
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return images;
        }

        public async Task<Dictionary<Guid, List<AddedImage>>> GetImagesOfMolens(List<Guid> molens)
        {
            var images = await _context.AddedImages
                .Where(e => molens.Contains(e.MolenDataId))
                .GroupBy(e => e.MolenDataId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            return images;
        }


        public override async Task Delete(AddedImage image)
        {
            AddedImage? addedImageToDelete = await GetById(image.Id);
            if (addedImageToDelete != null)
            {
                if (File.Exists(CreateCleanPath.CreatePathToWWWROOT(addedImageToDelete.FilePath)))
                {
                    File.Delete(CreateCleanPath.CreatePathToWWWROOT(addedImageToDelete.FilePath));
                }
                _context.AddedImages.Remove(addedImageToDelete);
            }
        }
    }
}

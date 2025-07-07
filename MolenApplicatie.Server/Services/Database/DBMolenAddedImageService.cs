using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenAddedImageService : DBDefaultService<AddedImage>
    {
        public DBMolenAddedImageService(MolenDbContext context) : base(context)
        {}

        public override bool Exists(AddedImage addedImage, out AddedImage? existing)
        {
            return Exists(e => e.FilePath == addedImage.FilePath, out existing);
        }

        public override async Task<AddedImage> Add(AddedImage addedImage)
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

        public override async Task Delete(AddedImage image)
        {
            AddedImage? addedImageToDelete = await GetById(image.Id);
            if (addedImageToDelete != null)
            {
                if (!File.Exists(Globals.WWWROOTPath + addedImageToDelete.FilePath))
                {
                    File.Delete(Globals.WWWROOTPath + addedImageToDelete.FilePath);
                }
                _context.AddedImages.Remove(addedImageToDelete);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenAddedImageService : DBDefaultService<AddedImage>
    {
        private readonly MolenDbContext _context;
        public DBMolenAddedImageService(MolenDbContext context) : base(context)
        {
            _context = context;
        }

        public override bool Exists(AddedImage entity, out AddedImage? existing)
        {
            existing = _context.AddedImages.FirstOrDefault(e => e.FilePath == entity.FilePath);
            return existing != null;
        }

        public async Task<List<AddedImage>> GetImagesOfMolen(int MolenId)
        {
            var images = await _context.AddedImages
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return images;
        }
    }
}

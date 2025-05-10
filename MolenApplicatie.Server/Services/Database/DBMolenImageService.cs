using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenImageService : DBDefaultService<MolenImage>
    {
        private readonly MolenDbContext _context;
        public DBMolenImageService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(MolenImage molenImage, out MolenImage? existing)
        {
            existing = _context.MolenImages.FirstOrDefault(e => e.FilePath == molenImage.FilePath);
            return existing != null;
        }
        public async Task<List<MolenImage>> GetImagesOfMolen(int MolenId)
        {
            var images = await _context.MolenImages
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return images;
        }
    }
}

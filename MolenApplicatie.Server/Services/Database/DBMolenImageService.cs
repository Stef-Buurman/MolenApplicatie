using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenImageService : DBDefaultService<MolenImage>
    {
        private readonly MolenDbContext _context;
        private readonly HttpClient _client;
        public DBMolenImageService(MolenDbContext context, HttpClient client) : base(context)
        {
            _context = context;
            _client = client;
        }
        public override bool Exists(MolenImage molenImage, out MolenImage? existing)
        {
            return Exists(e => e.FilePath == molenImage.FilePath, out existing);
        }

        public override async Task<MolenImage> Add(MolenImage molenImage)
        {
            bool DoesFileExist = File.Exists(Globals.WWWROOTPath + molenImage.FilePath);
            if (!DoesFileExist && Uri.IsWellFormedUriString(molenImage.ExternalUrl, UriKind.Absolute))
            {
                string? tbn = CreateDirectoryIfNotExists.GetMolenImagesFolder(molenImage.FilePath);
                if (tbn == null) return molenImage;
                CreateDirectoryIfNotExists.CreateMolenImagesFolderDirectory(tbn);
                File.WriteAllBytes(Globals.WWWROOTPath + molenImage.FilePath, await _client.GetByteArrayAsync(molenImage.ExternalUrl));
            }
            else if (!DoesFileExist) return molenImage;
            return await base.Add(molenImage);
        }

        public async Task<List<MolenImage>> GetImagesOfMolen(Guid MolenId)
        {
            var images = await _context.MolenImages
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return images;
        }

        public override async Task Delete(MolenImage image)
        {
            MolenImage? imageToDelete = await GetById(image.Id);
            if (imageToDelete != null)
            {
                if (!File.Exists(Globals.WWWROOTPath + imageToDelete.FilePath))
                {
                    File.Delete(Globals.WWWROOTPath + imageToDelete.FilePath);
                }
                _context.MolenImages.Remove(imageToDelete);
            }
        }
    }
}

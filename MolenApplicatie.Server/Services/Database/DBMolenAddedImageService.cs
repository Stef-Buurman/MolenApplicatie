using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenAddedImageService
        : DBDefaultService<AddedImage>
    {
        private readonly string _webRootPath;

        public DBMolenAddedImageService(
            MolenDbContext context,
            IWebHostEnvironment environment)
            : base(context)
        {
            _webRootPath = environment.WebRootPath;
        }

        public override bool Exists(
            AddedImage addedImage,
            out AddedImage? existing)
        {
            return Exists(
                entity => entity.FilePath == addedImage.FilePath,
                out existing);
        }

        public override bool ExistsRange(
            List<AddedImage> entities,
            out List<AddedImage> matchingEntities,
            out List<AddedImage> newEntities,
            out List<AddedImage> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                entity => entity.FilePath,
                value => entity => entity.FilePath == value.FilePath,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat);
        }

        public override async Task<AddedImage> Add(
            AddedImage addedImage,
            CancellationToken token = default)
        {
            var absolutePath = GetAbsolutePath(
                addedImage.FilePath);

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    $"The added image file does not exist: {absolutePath}",
                    absolutePath);
            }

            return await base.Add(addedImage, token);
        }

        public async Task<List<AddedImage>> GetImagesOfMolen(
            Guid molenId)
        {
            return await _context.AddedImages
                .Where(entity =>
                    entity.MolenDataId == molenId)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, List<AddedImage>>>
            GetImagesOfMolens(List<Guid> molens)
        {
            return await _context.AddedImages
                .Where(entity =>
                    molens.Contains(entity.MolenDataId))
                .GroupBy(entity => entity.MolenDataId)
                .ToDictionaryAsync(
                    group => group.Key,
                    group => group.ToList());
        }

        public override async Task Delete(AddedImage image)
        {
            var addedImageToDelete =
                await GetById(image.Id);

            if (addedImageToDelete == null)
            {
                return;
            }

            var absolutePath = GetAbsolutePath(
                addedImageToDelete.FilePath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            _context.AddedImages.Remove(
                addedImageToDelete);

            _cache.Remove(addedImageToDelete);
        }

        private string GetAbsolutePath(string filePath)
        {
            var relativePath = filePath
                .Replace("\\", "/")
                .TrimStart('/');

            return Path.Combine(
                _webRootPath,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }
    }
}
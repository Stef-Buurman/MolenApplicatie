using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api/DB")]
    public class DBController : ControllerBase
    {
        private readonly NewMolenDataService _NewMolenDataService;
        private readonly PlacesService _PlacesService;
        private readonly MolenDbContext _dbContext;
        private readonly MolenService _molenService;

        public DBController(NewMolenDataService newMolenDataService, PlacesService placesService, MolenDbContext dbContext, MolenService molenService)
        {
            _NewMolenDataService = newMolenDataService;
            _PlacesService = placesService;
            _dbContext = dbContext;
            _molenService = molenService;
        }

        [FileUploadFilter]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllDataForDB()
        {
            await _PlacesService.ReadAllNetherlandsPlaces();
            await _NewMolenDataService.ReadAllMolenTBN();
            await _NewMolenDataService.GetAllMolenData();
            return Ok();
        }

        [HttpGet("FillMariaDB")]
        public async Task<IActionResult> FillMariaDB()
        {
            List<MolenDataOld> molens = await _molenService.GetAllMolenData();
            await _NewMolenDataService.AddMolensToMariaDb(molens);
            List<PlaceOld> places = await _PlacesService.GetAllNetherlandsPlaces();
            await _PlacesService.AddPlacesToMariaDb(places);
            return Ok();
        }

        [HttpGet("GetMolens")]
        public async Task<IActionResult> GetMolens()
        {
            var molens = await _dbContext.MolenData.Include(m => m.MolenTypeAssociations)
                .ThenInclude(mt => mt.MolenType)
                .Include(m => m.AddedImages)
                .Include(m => m.Images)
                .Include(m => m.DisappearedYearInfos)
                .Include(m => m.MolenMakers)
                .ToListAsync();
            return Ok(molens);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Models;
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
            var molens2 = await _dbContext.MolenData
                .Include(m => m.MolenTBN).Include(m => m.Images).Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations).ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers).Include(m => m.DisappearedYearInfos)
                .Select(m => _molenService.GetMolenData(m)).ToListAsync();
            return Ok(molens2);
        }

        [HttpGet("GetMolens2")]
        public async Task<IActionResult> GetMolens2()
        {
            var molens = await _molenService.GetAllMolenData();
            return Ok(molens.Take(1000));
        }
    }
}

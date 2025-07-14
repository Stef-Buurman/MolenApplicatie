using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api/DB")]
    public class DBController : ControllerBase
    {
        private readonly NewMolenDataService _NewMolenDataService2_0;
        private readonly PlacesService _PlacesService2_0;
        private readonly MolenService _molenService2_0;
        private readonly MolenDbContext _dbContext;

        public DBController(NewMolenDataService newMolenDataService2_0, PlacesService placesService2_0, MolenDbContext dbContext, MolenService molenService)
        {
            _NewMolenDataService2_0 = newMolenDataService2_0;
            _PlacesService2_0 = placesService2_0;
            _dbContext = dbContext;
            _molenService2_0 = molenService;
        }

        [FileUploadFilter]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllDataForDB()
        {
            await _PlacesService2_0.ReadAllNetherlandsPlaces();
            await _NewMolenDataService2_0.ReadAllMolenTBN();
            await _NewMolenDataService2_0.GetAllMolenData();
            return Ok();
        }

        [HttpGet("GetMolens")]
        public async Task<IActionResult> GetMolens()
        {
            var molens2 = await _dbContext.MolenData
                .Include(m => m.MolenTBN).Include(m => m.Images).Include(m => m.AddedImages)
                .Include(m => m.MolenTypeAssociations).ThenInclude(a => a.MolenType)
                .Include(m => m.MolenMakers).Include(m => m.DisappearedYearInfos)
                .Select(m => MolenService.GetMolenData(m))
                .ToListAsync();
            return Ok(molens2);
        }

        [HttpGet("GetMolens2")]
        public async Task<IActionResult> GetMolens2()
        {
            var molens = _molenService2_0.GetAllMolenData();
            return Ok(molens.Take(1000));
        }

        [HttpGet("CallMolenResponses")]
        public async Task<IActionResult> CallMolenResponses()
        {
            //await _NewMolenDataService2_0.SaveAllMolenTBN();
            await _NewMolenDataService2_0.CallMolenResponses();
            return Ok();
        }

        [HttpGet("SaveMolenResponses")]
        public async Task<IActionResult> SaveMolenResponses()
        {
            await _NewMolenDataService2_0.SaveMolenResponses();
            return Ok();
        }
    }
}

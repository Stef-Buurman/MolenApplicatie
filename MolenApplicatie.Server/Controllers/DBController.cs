using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api/DB")]
    public class DBController : ControllerBase
    {
        private readonly NewMolenDataService _NewMolenDataService;
        private readonly PlacesService _PlacesService;

        public DBController(NewMolenDataService newMolenDataService, PlacesService placesService)
        {
            _NewMolenDataService = newMolenDataService;
            _PlacesService = placesService;
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
    }
}

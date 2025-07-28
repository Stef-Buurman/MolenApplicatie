using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class PlacesController : ControllerBase
    {
        private readonly PlacesService _PlacesService;

        public PlacesController(PlacesService placesService)
        {
            _PlacesService = placesService;
        }

        [FileUploadFilter]
        [HttpGet("read_all_netherlands_places")]
        public async Task<IActionResult> ReadAllNetherlandsPlaces()
        {
            await _PlacesService.ReadAllNetherlandsPlaces();
            return Ok();
        }

        [HttpGet("get_all_netherlands_places")]
        public async Task<IActionResult> GetAllNetherlandsPlaces()
        {
            await _PlacesService.GetAllNetherlandsPlaces();
            return Ok();
        }


        [HttpGet("get_places_by_input/{input}")]
        public async Task<IActionResult> GetPlacesByInput(string input)
        {
            await _PlacesService.GetPlacesByName(input);
            return Ok();
        }
    }
}
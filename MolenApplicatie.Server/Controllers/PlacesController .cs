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
            var locations = await _PlacesService.ReadAllNetherlandsPlaces();
            return Ok();
        }

        [HttpGet("get_all_netherlands_places")]
        public async Task<IActionResult> GetAllNetherlandsPlaces()
        {
            var locations = await _PlacesService.GetAllNetherlandsPlaces();
            return Ok(locations);
        }


        [HttpGet("get_places_by_input/{input}")]
        public async Task<IActionResult> GetPlacesByInput(string input)
        {
            var locations = await _PlacesService.GetPlacesByType(input);
            return Ok(locations);
        }
    }
}
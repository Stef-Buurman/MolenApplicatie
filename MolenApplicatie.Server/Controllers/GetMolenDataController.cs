using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class GetMolenDataController : ControllerBase
    {
        private readonly ReadMolenDataService _readMolenDataService;

        public GetMolenDataController(ReadMolenDataService readMolenDataService)
        {
            _readMolenDataService = readMolenDataService;
        }

        [HttpGet("all_molen_locations")] // Matches the route /api/all_molen_locations
        public async Task<IActionResult> GetAllMolenLocations()
        {
            Console.WriteLine("API endpoint hit"); // This should log if the endpoint is hit
            var locations = await _readMolenDataService.GetAllMolenLatLon();
            return Ok(locations);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Models.MariaDB;
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
            await _NewMolenDataService2_0.SaveAllMolenTBN();
            await _NewMolenDataService2_0.GetAllMolenData();
            return Ok();
        }

        [HttpGet("GetMolens")]
        public ActionResult<List<MolenData>> GetMolens()
        {
            var molens2 = _molenService2_0.GetAllMolenData();
            return Ok(molens2);
        }

        [HttpGet("CallMolenResponses")]
        public async Task<IActionResult> CallMolenResponses()
        {
            await _NewMolenDataService2_0.SaveAllMolenTBN();
            await _NewMolenDataService2_0.CallMolenResponses();
            return Ok();
        }

        [HttpGet("SaveMolenResponses")]
        public async Task<IActionResult> SaveMolenResponses()
        {
            await _NewMolenDataService2_0.SaveMolenResponses();
            return Ok();
        }

        [HttpGet("test")]
        public async Task<IActionResult> test()
        {
            var startTime = DateTime.Now;
            await _NewMolenDataService2_0.test();
            var midTime = DateTime.Now;
            await _PlacesService2_0.test();
            var midTime2 = DateTime.Now;
            var changes = await _dbContext.SaveChangesAsync();
            var endTime = DateTime.Now;
            Console.WriteLine($"Start: {startTime}, Mid1: {midTime}, Mid2: {midTime2}, End: {endTime}");
            Console.WriteLine($"Molen duration: {midTime - startTime}");
            Console.WriteLine($"Places duration: {midTime2 - midTime}");
            Console.WriteLine($"Save changes duration: {endTime - midTime2}");
            Console.WriteLine($"Total duration: {endTime - startTime}");
            Console.WriteLine($"Changes saved: {changes}");
            return Ok();
        }

        [HttpGet("test2")]
        public async Task<IActionResult> test2()
        {
            var startTime = DateTime.Now;
            await _NewMolenDataService2_0.test2();
            var midTime = DateTime.Now;
            await _PlacesService2_0.test2();
            var endTime = DateTime.Now;
            Console.WriteLine($"Start: {startTime}, Mid: {midTime}, End: {endTime}, Duration: {endTime - startTime}");
            return Ok();
        }
    }
}

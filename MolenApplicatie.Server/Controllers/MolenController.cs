using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class MolenController : ControllerBase
    {
        private readonly MolenService _readMolenDataService;

        public MolenController(MolenService readMolenDataService)
        {
            _readMolenDataService = readMolenDataService;
        }

        [HttpGet("all_molen_locations")]
        public async Task<IActionResult> GetAllMolenLocations()
        {
            var locations = await _readMolenDataService.GetAllMolenLatLon();
            return Ok(locations);
        }

        [HttpGet("molen/{tbNumber}")]
        public async Task<IActionResult> GetMolenDataByTBNumber(string tbNumber)
        {
            return Ok(await _readMolenDataService.GetMolenByTBN(tbNumber));
        }

        [HttpPost]
        [Route("upload_image/{tbNumber}")]
        public async Task<IActionResult> UploadImage(string tbNumber, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided");

            MolenData molen = await _readMolenDataService.GetMolenByTBN(tbNumber);
            if (molen == null) return NotFound("Molen not found");
            IFormFile savedImage = await _readMolenDataService.SaveMolenImage(molen.Id, tbNumber, image);

            return Ok(new { File = savedImage });
        }

        [HttpGet("get_all_molen_data")]
        public async Task<IActionResult> GetAllMolenData()
        {
            return Ok(await _readMolenDataService.GetAllMolenData());
        }
    }
}
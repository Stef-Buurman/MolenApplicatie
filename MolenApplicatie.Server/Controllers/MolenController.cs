using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class MolenController : ControllerBase
    {
        private readonly MolenService _MolenService;
        private readonly NewMolenDataService _NewMolenDataService;

        public MolenController(MolenService molenService, NewMolenDataService newMolenDataService)
        {
            _MolenService = molenService;
            _NewMolenDataService = newMolenDataService;
        }

        [HttpGet("all_molen_locations")]
        public async Task<IActionResult> GetAllMolenLocations()
        {
            var locations = await _MolenService.GetAllMolenLatLon();
            return Ok(locations);
        }

        [HttpGet("molen/{tbNumber}")]
        public async Task<IActionResult> GetMolenDataByTBNumber(string tbNumber)
        {
            return Ok(await _MolenService.GetMolenByTBN(tbNumber));
        }

        [HttpPost]
        [Route("upload_image/{tbNumber}")]
        public async Task<IActionResult> UploadImage(string tbNumber, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided");

            MolenData molen = await _MolenService.GetMolenByTBN(tbNumber);
            if (molen == null) return NotFound("Molen not found");
            IFormFile savedImage = await _MolenService.SaveMolenImage(molen.Id, tbNumber, image);
            if (savedImage == null) return BadRequest("An error occured while saving the image");

            return Ok(await _MolenService.GetMolenByTBN(tbNumber));
        }

        [HttpGet("get_all_molen_data")]
        public async Task<IActionResult> GetAllMolenData()
        {
            return Ok(await _NewMolenDataService.GetAllMolenData());
        }

        [HttpDelete("molen_image/{tbNumber}/{imageName}")]
        public async Task<IActionResult> DeleteMolenImage(string tbNumber, string imageName)
        {
            var result = await _MolenService.DeleteImageFromMolen(tbNumber, imageName);
            if (result.status)
            {
                return Ok(new { message = result.message });
            }
            else
            {
                return BadRequest(new { message = result.message });
            }
        }
    }
}
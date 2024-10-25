using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Filters;
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

        [FileUploadFilter]
        [HttpPost]
        [Route("upload_image/{tbNumber}")]
        public async Task<IActionResult> UploadImage(string tbNumber, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Geen foto meegestuurd!");

            MolenData molen = await _MolenService.GetMolenByTBN(tbNumber);
            if (molen == null) return NotFound("Molen niet gevonden!");
            var result = await _MolenService.SaveMolenImage(molen.Id, tbNumber, image);
            IFormFile savedImage = result.file;
            if (savedImage == null)
            {
                if(result.errorMessage == null || result.errorMessage == "")
                {
                    return BadRequest("Er is iets misgegaan met het opslaan van de foto!");
                }
                else
                {
                    return BadRequest(result.errorMessage);
                }
            }

            return Ok(await _MolenService.GetMolenByTBN(tbNumber));
        }

        [HttpGet("get_all_molen_data")]
        public async Task<IActionResult> GetAllMolenData()
        {
            return Ok(await _NewMolenDataService.GetAllMolenData());
        }

        [FileUploadFilter]
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

        [HttpGet("update_oldest_molens")]
        public async Task<IActionResult> UpdateOldestMolens()
        {
            var result = await _NewMolenDataService.UpdateDataOfLastUpdatedMolens();
            if (result.isDone)
            {
                return Ok(result.MolenData);
            }
            return BadRequest($"Kan dit niet uitvoeren, je kan dit na {Convert.ToInt32(result.timeToWait.TotalMinutes)} minuten nog een keer proberen!");
        }

        [HttpGet("search_for_new_molens")]
        public async Task<IActionResult> GetNewAddedMolens()
        {
            var result = await _NewMolenDataService.SearchForNewMolens();
            if (result.MolenData == null)
            {
                return BadRequest($"Kan dit niet uitvoeren, je kan dit na {Convert.ToInt32(result.timeToWait.TotalMinutes)} minuten nog een keer proberen!");
            }
            return Ok(result.MolenData);
        }
    }
}
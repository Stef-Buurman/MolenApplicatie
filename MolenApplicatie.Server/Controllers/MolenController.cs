using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Models;
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

        [HttpGet("get-image-urls")]
        public IActionResult GetImageUrls()
        {
            var imageDirectory = "wwwroot/MolenImages";
            var images = Directory.GetFiles(imageDirectory)
                .Select(file => new MolenImage2(
                    file, // Original file path
                    Path.GetFileName(file),
                    $"{Request.Scheme}://{Request.Host}/MolenImages/{Path.GetFileName(file)}", // Construct URL
                    true // or false based on your logic
                ))
                .ToList();

            return Ok(images);
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

        [HttpGet]
        [Route("get_molen_tbn")]
        public async Task<IActionResult> GetMolenTBN()
        {
            return Ok(await _NewMolenDataService.AddMolenTBNToDB());
        }

        [FileUploadFilter]
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

        [FileUploadFilter]
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

        [FileUploadFilter]
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
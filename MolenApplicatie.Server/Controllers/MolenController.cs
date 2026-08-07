using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api/molen")]
    public class MolenController : ControllerBase
    {
        private readonly MolenService _molenService;
        private readonly NewMolenDataService _newMolenDataService;

        public MolenController(MolenService molenService, NewMolenDataService newMolenDataService)
        {
            _molenService = molenService;
            _newMolenDataService = newMolenDataService;
        }

        [HttpGet("all/{provincie}")]
        public async Task<ActionResult<MolensResponseType<MolenData>>> GetAllMolensByProvincie(string provincie)
        {
            var molenData = _molenService.GetAllMolenDataByProvincie(provincie);
            return Ok(await _molenService.MolensResponse(molenData));
        }

        [FileUploadFilter]
        [HttpGet("all")]
        public ActionResult<List<MolenData>> GetAllMolens()
        {
            return Ok(_molenService.GetAllMolenData());
        }

        [HttpGet("mapdata")]
        public async Task<ActionResult<MolensResponseType<MapData>>> GetAllMolenMapData(
            [FromQuery] string? molenType,
            [FromQuery] string? provincie,
            [FromQuery] string? molenState,
            [FromQuery] string type = "molens")
        {
            var molenData = _molenService.GetMapData(molenType, provincie, molenState);
            return Ok(await _molenService.MolensResponse(molenData));
        }

        [HttpGet("active")]
        public async Task<ActionResult<MolensResponseType<MolenData>>> GetAllActiveMolens()
        {
            var molenData = _molenService.GetAllActiveMolenData();
            return Ok(await _molenService.MolensResponse(molenData));
        }

        [HttpGet("existing")]
        public async Task<ActionResult<MolensResponseType<MolenData>>> GetAllExistingMolens()
        {
            var molenData = _molenService.GetAllExistingMolens();
            return Ok(await _molenService.MolensResponse(molenData));
        }

        [HttpGet("disappeared/{provincie}")]
        public async Task<ActionResult<MolensResponseType<MolenData>>> GetAllDisappearedMolens(string provincie)
        {
            var molenData = _molenService.GetAllDisappearedMolens(provincie);
            return Ok(await _molenService.MolensResponse(molenData));
        }

        [HttpGet("remainder")]
        public async Task<ActionResult<MolensResponseType<MolenData>>> GetAllRemainderMolens()
        {
            var molenData = _molenService.GetAllRemainderMolens();
            return Ok(await _molenService.MolensResponse(molenData));
        }

        [HttpGet("provincies")]
        public async Task<ActionResult<List<ValueName>>> GetAllMolenProvincies()
        {
            return Ok(await _molenService.GetAllMolenProvincies());
        }

        [HttpGet("filters")]
        public async Task<ActionResult<MolenFilters>> GetMolenFilters()
        {
            return Ok(await _molenService.GetMolenFilters());
        }

        [HttpGet("map-summary")]
        public async Task<ActionResult<MolenMapSummaryResponse>> GetMapSummary(CancellationToken token)
        {
            return Ok(await _molenService.GetMapSummaryAsync(token));
        }

        [HttpGet("with-image-count")]
        public async Task<ActionResult<int>> GetMolensWithImageCount(CancellationToken token)
        {
            return Ok(await _molenService.GetMolensWithImageCountAsync(token));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MolenData>> GetMolenDataById(Guid id)
        {
            var molen = await _molenService.GetMolenById(id);
            return molen == null ? NotFound("Molen niet gevonden!") : Ok(molen);
        }

        [FileUploadFilter]
        [HttpPost("molen_image/{tbNumber}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<UploadDeleteImageReturnType>> UploadImage(string tbNumber, [Required] IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Geen foto meegestuurd!");

            var molen = await _molenService.GetMolenByReference(tbNumber);
            if (molen == null) return NotFound("Molen niet gevonden!");
            if (!molen.CanAddImages) return BadRequest("Voor deze molen kan geen foto worden opgeslagen!");

            var imageFolderKey = string.IsNullOrWhiteSpace(molen.Ten_Brugge_Nr) ? molen.Id.ToString() : molen.Ten_Brugge_Nr;
            var result = await _molenService.SaveMolenImage(molen.Id, imageFolderKey, image);
            if (result.file == null)
            {
                return string.IsNullOrWhiteSpace(result.errorMessage)
                    ? BadRequest("Er is iets misgegaan met het opslaan van de foto!")
                    : StatusCode((int)result.statusCode, result.errorMessage);
            }

            var updatedMolen = await _molenService.GetMolenByReference(tbNumber);
            if (updatedMolen == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "De bijgewerkte molen kon niet opnieuw worden geladen.");

            var updatedMapData = await _molenService.GetMapDataByReference(tbNumber);
            if (updatedMapData == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "De bijgewerkte kaartgegevens konden niet opnieuw worden geladen.");

            return Ok(new UploadDeleteImageReturnType
            {
                Molen = updatedMolen,
                MapData = updatedMapData,
            });
        }

        [HttpPost("uploadMolensHtml")]
        public async Task<ActionResult> UploadMolensHtml(Dictionary<string, Dictionary<string, string>> molenResponses)
        {
            if (molenResponses == null || molenResponses.Count == 0)
                return BadRequest("Geen molens meegestuurd!");

            return Ok(await _newMolenDataService.SaveMolensByResponses(molenResponses));
        }

        [FileUploadFilter]
        [HttpGet("uploadMolenHtml")]
        public async Task<ActionResult> SendMolenHtml()
        {
            await _newMolenDataService.SendMolenByResponses();
            return Ok();
        }

        [FileUploadFilter]
        [HttpDelete("molen_image/{tbNumber}/{imageName}")]
        public async Task<ActionResult<UploadDeleteImageReturnType>> DeleteMolenImage(string tbNumber, string imageName)
        {
            var result = await _molenService.DeleteImageFromMolen(tbNumber, imageName);
            if (!result.status) return BadRequest(result.message);

            var updatedMolen = await _molenService.GetMolenByReference(tbNumber);
            if (updatedMolen == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "De bijgewerkte molen kon niet opnieuw worden geladen.");

            var updatedMapData = await _molenService.GetMapDataByReference(tbNumber);
            if (updatedMapData == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "De bijgewerkte kaartgegevens konden niet opnieuw worden geladen.");

            return Ok(new UploadDeleteImageReturnType
            {
                Molen = updatedMolen,
                MapData = updatedMapData,
            });
        }

        [FileUploadFilter]
        [HttpGet("update_oldest_molens")]
        public async Task<ActionResult<List<MolenData>>> UpdateOldestMolens()
        {
            var result = await _newMolenDataService.UpdateDataOfLastUpdatedMolens();

            if (!result.isDone && result.timeToWait == null && result.MolenData == null)
                return BadRequest("Er zijn te veel aanvragen gedaan, probeer het later nog eens!");

            if (result.isDone)
                return Ok(result.MolenData ?? []);

            if (result.timeToWait.HasValue)
                return BadRequest($"Kan dit niet uitvoeren, je kan dit na {Convert.ToInt32(result.timeToWait.Value.TotalMinutes)} minuten nog een keer proberen!");

            return StatusCode(StatusCodes.Status500InternalServerError, "Er is iets misgegaan bij het updaten van de molens.");
        }

        [FileUploadFilter]
        [HttpGet("search_for_new_molens")]
        public async Task<ActionResult<List<MolenData>>> GetNewAddedMolens()
        {
            var result = await _newMolenDataService.SearchForNewMolens();

            if (result.MolenData == null && result.timeToWait.HasValue)
                return BadRequest($"Kan dit niet uitvoeren, je kan dit na {Convert.ToInt32(result.timeToWait.Value.TotalMinutes)} minuten nog een keer proberen!");

            if (result.MolenData != null)
                return Ok(result.MolenData);

            return StatusCode(StatusCodes.Status500InternalServerError, "Er is iets fout gegaan bij het zoeken naar nieuwe molens.");
        }

        [FileUploadFilter]
        [HttpGet("read_molen/{tbNumber}")]
        public async Task<ActionResult<MolenData>> GetMolenTypes(string tbNumber)
        {
            var results = await _newMolenDataService.GetMolenDataByTBNumber(tbNumber);
            return results.HasValue ? Ok(results.Value.molen) : NotFound("Molen niet gevonden!");
        }

        [FileUploadFilter]
        [HttpGet("read_all_molen")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> GetAllMolen()
        {
            return Ok(await _newMolenDataService.GetAllMolenData());
        }

        [HttpGet("map-items")]
        public async Task<ActionResult<IReadOnlyList<MapItemResponse>>> GetMapItems([FromQuery] MolenMapFilter filter, CancellationToken token)
        {
            return Ok(await _molenService.GetMapItemsAsync(filter, token));
        }
    }
}

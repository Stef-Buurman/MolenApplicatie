using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "testFolder");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var uniqueFileName = $"{tbNumber}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(directoryPath, uniqueFileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            return Ok(new { filePath = uniqueFileName });
        }


        //[HttpGet("get_molen_data")]
        //public async Task<IActionResult> GetMolenData()
        //{
        //    return Ok(await _readMolenDataService.GetAllMolenData());
        //}
    }
}
//namespace MolenApplicatie.Server.Controllers
//{
//    [ApiController]
//    [Route("api")]
//    public class GetMolenDataController : ControllerBase
//    {
//        private readonly ReadMolenDataService readMolenDataService;
//        public GetMolenDataController(ReadMolenDataService readMolenDataService)
//        {
//            this.readMolenDataService = readMolenDataService;
//        }

//        [HttpGet("get_molen_data")]
//        public async Task<IActionResult> GetMolenData()
//        {
//            return Ok(await readMolenDataService.GetAllMolenData());
//        }

//        [HttpGet("active_TBN")]
//        public async Task<IActionResult> GetAllActiveTBNR()
//        {
//            return Ok(await readMolenDataService.GetAllActiveTBNR());
//        }

//        [HttpGet("molen/{tbNumber}")]
//        public async Task<IActionResult> GetMolenDataByTBNumber(string tbNumber)
//        {
//            return Ok(await readMolenDataService.GetMolenByTBN(tbNumber));
//        }

//        [HttpGet("by_type/{type}")]
//        public async Task<IActionResult> GetMolenDataByType(string type)
//        {
//            return Ok(await readMolenDataService.GetMolenDataByType(type));
//        }

//        [HttpGet("add_molen_TBN_to_DB_from_json")]
//        public async Task<IActionResult> AddMolenTBNToDBFromJson()
//        {
//            return Ok(await readMolenDataService.AddMolenTBNToDBFromJson());
//        }

//        [HttpGet("all_molen_locations")]
//        public async Task<IActionResult> GetAllMolenLocations()
//        {
//            return Ok(await readMolenDataService.GetAllMolenLatLon());
//        }

//        [HttpGet("all_molen_types")]
//        public async Task<IActionResult> GetAllMolenTypes()
//        {
//            return Ok(await readMolenDataService.GetAllMolenTypes());
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using MolenApplicatie.Models;
using MolenApplicatie.Server.Filters;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;
using System.Text.Json;

namespace MolenApplicatie.Server.Controllers
{
    [ApiController]
    [Route("api/molen")]
    public class MolenController : ControllerBase
    {
        private readonly MolenService _MolenService;
        private readonly NewMolenDataService _NewMolenDataService;

        public MolenController(MolenService molenService, NewMolenDataService newMolenDataService)
        {
            _MolenService = molenService;
            _NewMolenDataService = newMolenDataService;
        }

        [HttpGet("all/{provincie}")]
        public async Task<IActionResult> GetAllMolens(string provincie)
        {
            var molenData = await _MolenService.GetAllMolenDataByProvincie(provincie);
            return Ok(molenData);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActiveMolens()
        {
            var molenData = await _MolenService.GetAllActiveMolenData();
            return Ok(molenData);
        }

        [HttpGet("existing")]
        public async Task<IActionResult> GetAllExistingMolens()
        {
            var molenData = await _MolenService.GetAllExistingMolens();
            return Ok(molenData);
        }
        

        [HttpGet("disappeared/{provincie}")]
        public async Task<IActionResult> GetAllDisappearedMolens(string provincie)
        {
            var molenData = await _MolenService.GetAllDisappearedMolens(provincie);
            return Ok(molenData);
        }

        [HttpGet("remainder")]
        public async Task<IActionResult> GetAllRemainderMolens()
        {
            var molenData = await _MolenService.GetAllRemainderMolens();
            return Ok(molenData);
        }

        [HttpGet("all_molen_locations")]
        public async Task<IActionResult> GetAllMolenLocations()
        {
            var locations = await _MolenService.GetAllMolenLatLon();
            return Ok(locations);
        }

        [HttpGet("provincies")]
        public async Task<IActionResult> GetAllMolenProvincies()
        {
            var provincies = await _MolenService.GetAllMolenProvincies();
            return Ok(provincies);
        }

        [HttpGet("{tbNumber}")]
        public async Task<IActionResult> GetMolenDataByTBNumber(string tbNumber)
        {
            return Ok(await _MolenService.GetMolenByTBN(tbNumber));
        }

        [FileUploadFilter]
        [HttpPost]
        [Route("molen_image/{tbNumber}")]
        public async Task<IActionResult> UploadImage(string tbNumber, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Geen foto meegestuurd!");

            MolenData molen = await _MolenService.GetMolenByTBN(tbNumber);
            if (molen == null) return NotFound("Molen niet gevonden!");
            var result = await _MolenService.SaveMolenImage(molen.Id, tbNumber, image);
            IFormFile savedImage = result.file;
            if (!molen.CanAddImages)
            {
                return BadRequest("Voor deze molen kan geen foto worden opgeslagen!");
            }
            if (savedImage == null)
            {
                if (result.errorMessage == null || result.errorMessage == "")
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
        public async Task<IActionResult> GetMolenAllActiveTBN()
        {
            return Ok(await _NewMolenDataService.AddMolenTBNToDB());
        }

        //[FileUploadFilter]
        //[HttpGet("get_all_molen_data")]
        //public async Task<IActionResult> GetAllMolenData()
        //{
        //    return Ok(await _NewMolenDataService.GetAllMolenData());
        //}

        [FileUploadFilter]
        [HttpDelete("molen_image/{tbNumber}/{imageName}")]
        public async Task<IActionResult> DeleteMolenImage(string tbNumber, string imageName)
        {
            var result = await _MolenService.DeleteImageFromMolen(tbNumber, imageName);
            if (result.status)
            {
                return Ok(await _MolenService.GetMolenByTBN(tbNumber));
            }
            else
            {
                return BadRequest(result.message);
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

        [FileUploadFilter]
        [HttpGet]
        [Route("get_all_molen_tbn")]
        public async Task<IActionResult> GetMolenTBN()
        {
            return Ok(await _NewMolenDataService.ReadAllMolenTBN());
        }

        [FileUploadFilter]
        [HttpGet]
        [Route("read_molen/{tbNumber}")]
        public async Task<IActionResult> GetMolenTypes(string tbNumber)
        {
            var results = await _NewMolenDataService.GetMolenDataByTBNumber(tbNumber);
            return Ok(results.Item1);
        }

        [FileUploadFilter]
        [HttpGet]
        [Route("read_all_molen")]
        public async Task<IActionResult> GetAllMolen()
        {
            var results = await _NewMolenDataService.GetAllMolenData();
            return Ok(results);
        }

        [HttpGet]
        [Route("provinces")]
        public async Task<IActionResult> GetAllProvinces()
        {
            var results = await _MolenService.GetAllMolenData();
            var provinces = new List<string>();
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Lat != null && results[i].Long != null)
                {
                    provinces.Add(await GetProvinceByCoordinates((double)results[i].Lat, (double)results[i].Long));
                }
                if (i == 100) break;
            }
            return Ok(provinces);
        }

        private static readonly string apiUrl = "https://nominatim.openstreetmap.org/reverse";

        public static async Task<string> GetProvinceByCoordinates(double latitude, double longitude)
        {
            using (HttpClient client = new HttpClient())
            {
                // Add a valid User-Agent header
                client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0 (email@example.com)");

                // Construct the request URL
                string url = $"{apiUrl}?lat={latitude}&lon={longitude}&format=json";

                HttpResponseMessage response = await client.GetAsync(url);

                // Ensure the response is successful (status code 2xx)
                response.EnsureSuccessStatusCode();

                // Parse the response body
                string responseBody = await response.Content.ReadAsStringAsync();
                JsonDocument jsonResponse = JsonDocument.Parse(responseBody);

                // Look for the province in the JSON response
                if (jsonResponse.RootElement.TryGetProperty("address", out JsonElement address))
                {
                    if (address.TryGetProperty("state", out JsonElement state))
                    {
                        return state.GetString(); // Return the province
                    }
                }

                return "Province not found";
            }
        }
    }
}
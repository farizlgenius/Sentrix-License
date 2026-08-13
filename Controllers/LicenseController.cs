using System.Net;
using LicenseService.Model;
using LicenseService.Service.Impl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LicenseService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController(ILicenseService service) : ControllerBase
    {

        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeAsync([FromBody] ExchangeRequest request)
        {
            var res = await service.ExchangeAsync(request);
            return Ok(res);
        }


        [HttpPost("demo")]
        public async Task<IActionResult> GenerateDemoLicenseAsync([FromBody] GenerateDemo fingerPrint)
        {
            var res = await service.CreateLicenseDemoAsync(fingerPrint);
            return Ok(res);
        }

        [HttpPost]
        public Task<IActionResult> GenerateStandardLicenseAsync()
        {
            return Task.FromResult<IActionResult>(Ok(new { message = "Standard license generated" }));
        }
    }
}

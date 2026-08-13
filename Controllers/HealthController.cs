using LicenseService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LicenseService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class Health : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult GetHealthAsync()
        {
            return Ok(new HealthDto("UP", DateTime.UtcNow));
        }
    }
}

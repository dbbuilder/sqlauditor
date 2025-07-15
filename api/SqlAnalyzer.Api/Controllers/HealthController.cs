using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SqlAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                service = "sqlanalyzer-api-v2"
            });
        }

        [HttpGet("error")]
        [AllowAnonymous]
        public IActionResult GetError()
        {
            try
            {
                throw new Exception("Test error");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, type = ex.GetType().Name });
            }
        }
    }
}
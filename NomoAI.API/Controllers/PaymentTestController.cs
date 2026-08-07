using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NomoAI.API.Infrastructure.PayMob.Models;
using NomoAI.API.Infrastructure.PayMob.Services;

namespace NomoAI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentTestController(IPayMobService payMobService) : ControllerBase
    {
        private readonly IPayMobService _payMobService = payMobService;
        [HttpPost("test-quickLink")]
        public async Task<IActionResult> TestQuickLink([FromBody] QuickLinkRequest request)
        {
            var response = await _payMobService.CreateQuickLinkAsync(request);
            return Ok(response);
        }
    }
}

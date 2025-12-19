using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Controllers._Base;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    public AuthController()
    {
    }

    // Aplica rate limiting específico para login (5 tentativas por minuto)
    [HttpPost("login")]
    [EnableRateLimiting("authentication")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Lógica de autenticação...
        return Ok(new { token = "..." });
    }

    // Usa rate limiting padrão do usuário autenticado
    [HttpPost("refresh")]
    [EnableRateLimiting("authenticated-user")]
    [Authorize]
    public IActionResult RefreshToken()
    {
        // Lógica de refresh token...
        return Ok(new { token = "..." });
    }

    // Desabilita rate limiting para endpoint específico (usar com cautela)
    [HttpGet("health")]
    [DisableRateLimiting]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy" });
    }
}
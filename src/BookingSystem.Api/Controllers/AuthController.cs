using BookingSystem.Api.Contracts.Auth;
using BookingSystem.Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request.Email, request.Password);

        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Email, request.Password);

        return result.Succeeded ? Ok(new AuthResponse(result.Token!)) : Unauthorized(result.Errors);
    }
}

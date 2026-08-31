using BookingSystem.Application.Authentication;
using BookingSystem.Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Failure(createResult.Errors.Select(e => e.Description));
        }

        await userManager.AddToRoleAsync(user, Roles.User);

        return AuthResult.Success();
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthResult.Failure(["Invalid credentials"]);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return AuthResult.Failure(["Invalid credentials"]);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user.Id, user.Email!, roles);

        return AuthResult.Success(token);
    }
}

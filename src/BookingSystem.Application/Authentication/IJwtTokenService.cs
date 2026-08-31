namespace BookingSystem.Application.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string email, IEnumerable<string> roles);
}

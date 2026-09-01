using BookingSystem.Application.Common;

namespace BookingSystem.Application.Authentication;

public interface IAuthService
{
    Task<Result> RegisterAsync(string email, string password);
    Task<Result<string>> LoginAsync(string email, string password);
}

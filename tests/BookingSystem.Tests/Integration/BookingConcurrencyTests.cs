using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Tests.Integration;

public class BookingConcurrencyTests(BookingApiFactory factory) : IClassFixture<BookingApiFactory>
{
    private record RegisterRequest(string Email, string Password);
    private record LoginRequest(string Email, string Password);
    private record AuthResponse(string Token);
    private record BookingRequest(int SlotId, DateOnly Date);

    [Fact]
    public async Task ConcurrentBookings_ForSameSlotAndDate_OnlyOneSucceeds()
    {
        var slotId = await SeedSlotAsync();
        var token = await RegisterAndLoginAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        using var clientA = CreateAuthenticatedClient(token);
        using var clientB = CreateAuthenticatedClient(token);

        var requestA = clientA.PostAsJsonAsync("/api/bookings", new BookingRequest(slotId, date));
        var requestB = clientB.PostAsJsonAsync("/api/bookings", new BookingRequest(slotId, date));

        var responses = await Task.WhenAll(requestA, requestB);
        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        statusCodes.Should().Contain(HttpStatusCode.Created).And.Contain(HttpStatusCode.Conflict);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bookingCount = await dbContext.Bookings.CountAsync(b => b.SlotId == slotId && b.Date == date);

        bookingCount.Should().Be(1);
    }

    private async Task<int> SeedSlotAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var resource = new Resource { Name = "Room A" };
        var slot = new Slot { Resource = resource, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };

        dbContext.Resources.Add(resource);
        dbContext.Slots.Add(slot);
        await dbContext.SaveChangesAsync();

        return slot.Id;
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        using var client = factory.CreateClient();
        var email = $"race-{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

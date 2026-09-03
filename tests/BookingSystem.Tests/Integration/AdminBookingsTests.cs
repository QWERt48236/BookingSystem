using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Tests.Integration;

public class AdminBookingsTests(BookingApiFactory factory) : IClassFixture<BookingApiFactory>
{
    private record RegisterRequest(string Email, string Password, bool IsAdmin);
    private record LoginRequest(string Email, string Password);
    private record AuthResponse(string Token);
    private record BookingRequest(int SlotId, DateOnly Date);

    [Fact]
    public async Task GetAll_AsNonAdmin_ReturnsForbidden()
    {
        var token = await RegisterAndLoginAsync(isAdmin: false);
        using var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsBookingsAcrossUsers()
    {
        var slotId = await SeedSlotAsync();
        var ownerToken = await RegisterAndLoginAsync(isAdmin: false);
        var adminToken = await RegisterAndLoginAsync(isAdmin: true);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        using var ownerClient = CreateAuthenticatedClient(ownerToken);
        var bookingResponse = await ownerClient.PostAsJsonAsync("/api/bookings", new BookingRequest(slotId, date));
        bookingResponse.EnsureSuccessStatusCode();

        using var adminClient = CreateAuthenticatedClient(adminToken);
        var response = await adminClient.GetAsync("/api/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(slotId.ToString());
    }

    private async Task<int> SeedSlotAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var resource = new Resource { Name = "Room Admin Test" };
        var slot = new Slot { Resource = resource, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };

        dbContext.Resources.Add(resource);
        dbContext.Slots.Add(slot);
        await dbContext.SaveChangesAsync();

        return slot.Id;
    }

    private async Task<string> RegisterAndLoginAsync(bool isAdmin)
    {
        using var client = factory.CreateClient();
        var email = $"admin-test-{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, isAdmin));
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

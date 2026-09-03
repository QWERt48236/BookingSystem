using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingSystem.Api.Hubs;
using BookingSystem.Application.Bookings;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Tests.Integration;

public class BookingHubTests(BookingApiFactory factory) : IClassFixture<BookingApiFactory>
{
    private record RegisterRequest(string Email, string Password);
    private record LoginRequest(string Email, string Password);
    private record AuthResponse(string Token);
    private record BookingRequest(int SlotId, DateOnly Date);

    [Fact]
    public async Task Create_WhenBookingSucceeds_NotifiesResourceGroup()
    {
        var (resourceId, slotId) = await SeedSlotAsync();
        var token = await RegisterAndLoginAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, BookingHubRoutes.HubPath).ToString(), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        var received = new TaskCompletionSource<SlotBookedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<SlotBookedPayload>("SlotBooked", payload => received.TrySetResult(payload));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinResourceGroup", resourceId);

        using var client = CreateAuthenticatedClient(token);
        var response = await client.PostAsJsonAsync("/api/bookings", new BookingRequest(slotId, date));
        response.EnsureSuccessStatusCode();

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().Be(received.Task, "the resource group should receive a SlotBooked notification");

        var payload = await received.Task;
        payload.ResourceId.Should().Be(resourceId);
        payload.SlotId.Should().Be(slotId);
        payload.Date.Should().Be(date);
    }

    private async Task<(int ResourceId, int SlotId)> SeedSlotAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var resource = new Resource { Name = "Room Hub Test" };
        var slot = new Slot { Resource = resource, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };

        dbContext.Resources.Add(resource);
        dbContext.Slots.Add(slot);
        await dbContext.SaveChangesAsync();

        return (resource.Id, slot.Id);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        using var client = factory.CreateClient();
        var email = $"hub-{Guid.NewGuid():N}@example.com";
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

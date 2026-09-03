using BookingSystem.Api.Common;
using BookingSystem.Api.Hubs;
using BookingSystem.Application.Bookings;
using BookingSystem.Infrastructure;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var azureSignalRConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(azureSignalRConnectionString))
{
    signalRBuilder.AddAzureSignalR(azureSignalRConnectionString);
}

builder.Services.AddScoped<IBookingNotifier, SignalRBookingNotifier>();

const string AngularDevCorsPolicy = "AngularDev";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy(AngularDevCorsPolicy, policy =>
            policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "BookingSystem API v1"));
    app.UseCors(AngularDevCorsPolicy);
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BookingsHub>(BookingHubRoutes.HubPath);

if (!app.Environment.IsDevelopment())
{
    // More specific than the catch-all fallback below, so these always win routing precedence
    // for any /api or /hubs request that isn't matched by a real controller action or the hub -
    // otherwise MapFallbackToFile would silently serve index.html for a missing API route.
    app.Map("/api/{**catchAll}", () => Results.NotFound());
    app.Map("/hubs/{**catchAll}", () => Results.NotFound());

    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program;

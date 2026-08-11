using MenuItems.API.Data;
using MenuItems.API.Data.Interfaces;
using MenuItems.API.Repositories;
using MenuItems.API.Repositories.Interfaces;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Add services to the container ---
// (In .NET 5, this used to be Startup.ConfigureServices. .NET 6+ merged
// Startup.cs into Program.cs — same two phases, one file.)

// Registered once for the app's lifetime: it just wraps a MongoClient,
// which is itself thread-safe and meant to be shared, not recreated per request.
builder.Services.AddSingleton<IMenuItemsContext, MenuItemsContext>();

// Registered per-request: cheap, stateless, and there's no reason to share
// an instance across requests.
builder.Services.AddScoped<IMenuItemsRepository, MenuItemsRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MenuItems.API", Version = "v1" });
});

var app = builder.Build();

// --- Configure the HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MenuItems.API v1"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();

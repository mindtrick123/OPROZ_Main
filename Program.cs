using OPROZ_Main;
using OPROZ_Main.Data;

var builder = WebApplication.CreateBuilder(args);

// Create Startup instance and configure services
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline using Startup
startup.Configure(app, app.Environment);

// Initialize database
try
{
    await DbInitializer.InitializeAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while initializing the database.");
}

await app.RunAsync();
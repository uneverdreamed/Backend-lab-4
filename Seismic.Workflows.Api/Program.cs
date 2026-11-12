using Seismic.Workflows.Api.Earthquakes.Services;
using Seismic.Workflows.Api.Common;
using Serilog;

Console.WriteLine("====================================================");
Console.WriteLine(" Seismic Workflows API (Temporal client)");
Console.WriteLine($" {MeInfo.Line}");
Console.WriteLine("====================================================");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddTemporalClient(builder.Configuration["Temporal:Address"]!, "default");

builder.Services.AddScoped<IEarthquakeProcessingService, EarthquakeProcessingService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();

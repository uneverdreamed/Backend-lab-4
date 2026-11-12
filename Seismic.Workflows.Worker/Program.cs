using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seismic.Workflows.Contracts;
using Seismic.Workflows.Worker.BackgroundServices.EarthquakeProcessing.Activities;
using Seismic.Workflows.Worker.BackgroundServices.EarthquakeProcessing.Workflows;
using Seismic.Workflows.Worker.Common;
using Seismic.Workflows.Worker.Options;
using Serilog;
using Temporalio.Extensions.Hosting;

Console.WriteLine("====================================================");
Console.WriteLine(" Seismic Workflows Worker (Temporal)");
Console.WriteLine($" {MeInfo.Line}");
Console.WriteLine("====================================================");

var builder = Host.CreateApplicationBuilder(args);

var workerName = builder.Configuration["Worker:Name"] ?? $"worker-{Environment.ProcessId}";

builder.Services.AddSerilog(config => config
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("Worker", workerName));

builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));
builder.Services.Configure<ClassificationOptions>(builder.Configuration.GetSection("Classification"));

builder.Services.AddTemporalClient(builder.Configuration["Temporal:Address"]!, "default");

builder.Services
    .AddHostedTemporalWorker(TemporalNames.TaskQueue)
    .AddScopedActivities<SeismicActivities>()
    .AddWorkflow<EarthquakeProcessingWorkflow>();

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>().LogInformation(
    "Worker {Worker} started, pid {Pid}, task queue {TaskQueue}", workerName, Environment.ProcessId, TemporalNames.TaskQueue);

host.Run();

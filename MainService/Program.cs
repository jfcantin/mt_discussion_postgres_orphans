using MainService;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Shared;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
    .MinimumLevel.Override("Npgsql", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();

var connectionString = builder.Configuration.GetConnectionString("Transport");

builder.Services.ConfigureMassTransitWithPostgresTransport(connectionString, configurator =>
{
    configurator.AddConsumer<MainServiceTestMessageConsumer>();
    // configurator.AddConsumer<Service_A_SecondTestMessageConsumer>();
});

// Using PublishBatch works with both consumer in same assembly and outside
builder.Services.AddHostedService<PublishBatchService>();

// builder.Services.AddHostedService<PublishSynchronousService>();

var app = builder.Build();
app.Run();
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// Build a config object, using env vars and JSON providers.
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

    
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register your services here
        services.AddSingleton<App>();
        services.AddTransient<ValidatorService>();
        services.AddTransient<SpecificationSelector>();
    })
        .ConfigureLogging((context, logging) => {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
    }).Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
// logger.LogDebug("Host created.");
host.Services.GetService<App>().Run();
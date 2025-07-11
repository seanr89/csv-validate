using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using validator;

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
        services.AddTransient<IHeaderValidator, HeaderValidator>();
        services.AddTransient<ITypeValidator, TypeValidator>();
        services.AddTransient<IValidatorService, ValidatorService>();
        services.AddTransient<ISpecificationSelector, SpecificationSelector>();

        // Bind the RunSettings section
        services.Configure<RunSettings>(config.GetSection("RunSettings"));
    })
        .ConfigureLogging((context, logging) => {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
    }).Build();

var app = host.Services.GetRequiredService<App>();
app?.Run();
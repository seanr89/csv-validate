
namespace validator;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using validator.Headless;
using validator.Models.Settings;
using validator.Services;
using validator.Services.Interfaces;

class Program
{
    static int Main(string[] args)
    {
        var headlessOptions = HeadlessOptions.Parse(args);

        // In headless mode stdout is reserved for the JSON payload consumed by frontends,
        // so route all incidental Console output (service diagnostics) to stderr.
        var stdout = Console.Out;
        if (headlessOptions.IsHeadless)
        {
            Console.SetOut(Console.Error);
        }

        // Build a config object, using env vars and JSON providers.
        // appsettings.json is resolved from the binary directory so the engine can be launched from any working directory.
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: headlessOptions.IsHeadless)
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
                if (!string.IsNullOrWhiteSpace(headlessOptions.SpecDirectory))
                {
                    services.AddTransient<ISpecificationSelector>(_ =>
                        new SpecificationSelector(SpecificationSelector.ResolveSpecificationsDirectory(headlessOptions.SpecDirectory)));
                }
                else
                {
                    services.AddTransient<ISpecificationSelector, SpecificationSelector>();
                }
                services.AddTransient<IFileWriter, FileWriter>();

                // Bind the RunSettings section
                services.Configure<RunSettings>(config.GetSection("RunSettings"));
            })
                .ConfigureLogging((context, logging) => {
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                if (headlessOptions.IsHeadless)
                {
                    // Keep stdout clean for the JSON contract.
                    logging.ClearProviders();
                }
            }).Build();

        if (headlessOptions.IsHeadless)
        {
            var runner = new HeadlessRunner(
                host.Services.GetRequiredService<IValidatorService>(),
                host.Services.GetRequiredService<ISpecificationSelector>(),
                stdout,
                Console.Error,
                SpecificationSelector.ResolveSpecificationsDirectory(headlessOptions.SpecDirectory));
            return runner.Run(headlessOptions);
        }

        var app = host.Services.GetRequiredService<App>();
        app?.Run();
        return 0;
    }
}

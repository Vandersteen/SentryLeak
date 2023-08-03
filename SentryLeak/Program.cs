using SentryLeak;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Initializer>();
        services.AddHostedService<Consumer>();
        services.AddHostedService<Producer>();
    })
    .UseSerilog((context, provider, loggerConfig) =>
    {
        loggerConfig
            .WriteTo.Console()
            .WriteTo.Sentry(sentry =>
            {
                sentry.Dsn = context.Configuration.GetValue<string>("Sentry:Dsn");
            });
    })
    .Build();

await host.RunAsync();
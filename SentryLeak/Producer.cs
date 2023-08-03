using Azure.Messaging.ServiceBus;
using Sentry;
using Sentry.Extensibility;

namespace SentryLeak;

public class Producer : BackgroundService
{
    private readonly ILogger<Producer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHub _hub;

    public Producer(ILogger<Producer> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _hub = _serviceProvider.GetService<IHub>() ?? HubAdapter.Instance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        var client = new ServiceBusClient(_configuration.GetValue<string>("ServiceBus:ConnectionString"));

        var topic = _configuration.GetValue<string>("ServiceBus:Topic");
        
        var sender = client.CreateSender(topic);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var sentryScope = _hub.PushScope();
            var message = new ServiceBusMessage("Hello world");
            await sender.SendMessageAsync(message, stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
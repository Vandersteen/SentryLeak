using Azure.Messaging.ServiceBus;
using Sentry;
using Sentry.Extensibility;

namespace SentryLeak;

public class Consumer : IHostedService
{
    private readonly ILogger<Consumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private ServiceBusProcessor _processor = null;
    private readonly IHub _hub;

    public Consumer(ILogger<Consumer> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _hub = _serviceProvider.GetService<IHub>() ?? HubAdapter.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var topic = _configuration.GetValue<string>("ServiceBus:Topic");
        var subscription = _configuration.GetValue<string>("ServiceBus:Subscription");

        using var sentryScope = _hub.PushScope();
        _hub.ConfigureScope(s => s.Clear());

        _logger.LogInformation(
            "Starting to process messages from topic {topic} on subscription {subscription}",
            topic, subscription
        );

        var client = new ServiceBusClient(_configuration.GetValue<string>("ServiceBus:ConnectionString"));
        _processor = client.CreateProcessor(topic, subscription);

        _processor.ProcessMessageAsync += ProcessorOnProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessorOnProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);
        
        
        // Comment this line to 'introduce' the leak
        _hub.ConfigureScope(s => s.Clear());
    }

    private Task ProcessorOnProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        _logger.LogError(arg.Exception, "Unexpected error in service bus consumer");
        return Task.CompletedTask;
    }

    private async Task ProcessorOnProcessMessageAsync(ProcessMessageEventArgs arg)
    {
        using var sentryScope = _hub.PushScope();
        
        _hub.ConfigureScope(s =>
        {
            if (s.Breadcrumbs.Count > 0)
            {
                _logger.LogWarning("Breadcrumbs should be empty, but they are not");
            }
            else
            {
                _logger.LogInformation("Breadcrumbs are empty");
            }
        });
        
        _logger.LogInformation("Processing message: {content}", arg.Message.Body);

        await arg.CompleteMessageAsync(arg.Message);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
        }
    }
}
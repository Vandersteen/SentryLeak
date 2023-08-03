using Azure.Messaging.ServiceBus.Administration;
using Sentry;
using Sentry.Extensibility;

namespace SentryLeak;

//Just some code to initialize the azure service bus
public class Initializer : IHostedService
{
    private readonly ILogger<Initializer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHub _hub;

    public Initializer(ILogger<Initializer> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _hub = _serviceProvider.GetService<IHub>() ?? HubAdapter.Instance;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var sentryScope = _hub.PushScope();
        _hub.ConfigureScope(s => s.Clear());
        
        _logger.LogInformation("Initializing Service Bus");
        
        var topic = _configuration.GetValue<string>("ServiceBus:Topic");
        var subscription = _configuration.GetValue<string>("ServiceBus:Subscription");
        
        var adminClient = new ServiceBusAdministrationClient(_configuration.GetValue<string>("ServiceBus:ConnectionString"));

        if (!await adminClient.TopicExistsAsync(topic, cancellationToken))
        {
            _logger.LogInformation("Creating topic {topic}", topic);
            await adminClient.CreateTopicAsync(topic, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Topic {topic} already exists", topic);
        }

        if (!await adminClient.SubscriptionExistsAsync(topic, subscription, cancellationToken))
        {
            _logger.LogInformation("Creating subscription {subscription} on topic {topic}", subscription, topic);
            await adminClient.CreateSubscriptionAsync(topic, subscription, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Subscription {subscription} on topic {topic} already exists", subscription, topic);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
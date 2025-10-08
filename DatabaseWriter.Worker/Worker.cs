// DatabaseWriter.Worker/Worker.cs
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DatabaseWriter.Worker.Models;
using DatabaseWriter.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DatabaseWriter.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDatasetRepository _repository;
    private readonly string? _rabbitMqConnectionString;
    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(ILogger<Worker> logger, IDatasetRepository repository, IConfiguration configuration)
    {
        _logger = logger;
        _repository = repository;
        _rabbitMqConnectionString = configuration["RabbitMQ:ConnectionString"];
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_rabbitMqConnectionString))
        {
            _logger.LogError("RabbitMQ connection string is not configured.");
            throw new InvalidOperationException("RabbitMQ connection string is required.");
        }

        var factory = new ConnectionFactory() 
        { 
            Uri = new Uri(_rabbitMqConnectionString), 
            DispatchConsumersAsync = true 
        };
        
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Ensure the queue exists, matching the producer's declaration
        await _channel.QueueDeclareAsync(
            queue: "dataset-queue", 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        if (_channel == null)
        {
            _logger.LogError("Channel is not initialized.");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("--> Received new message from RabbitMQ.");

            try
            {
                var datasets = JsonSerializer.Deserialize<List<Dataset>>(message);
                if (datasets != null && datasets.Any())
                {
                    await _repository.SaveDatasetsAsync(datasets);
                    // Acknowledge the message was processed successfully
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message.");
                // An option to requeue the message or move it to a dead-letter queue TBC
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(queue: "dataset-queue", autoAck: false, consumer: consumer);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
        }
        
        if (_connection != null)
        {
            await _connection.CloseAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
// DatabaseWriter.Worker/Worker.cs
using System;
using System.Collections;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DatabaseWriter.Worker.Models;
using DatabaseWriter.Worker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DatabaseWriter.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDatasetRepository _repository;
    private readonly string _rabbitMqConnectionString;
    private IConnection _connection;
    private IModel _channel;

    public Worker(ILogger<Worker> logger, IDatasetRepository repository, IConfiguration configuration)
    {
        _logger = logger;
        _repository = _repository;
        _rabbitMqConnectionString = configuration["RabbitMQ:ConnectionString"];
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory() { Uri = new Uri(_rabbitMqConnectionString), DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Ensure the queue exists, matching the produce's declaration
        _channel.QueueDeclare(queue: "dataset-queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppigToken)
    {
        stoppigToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
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
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message.");
                // An option to requeue the message or move it to a dead-letter queue TBC
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(queue: "dataset-queue", autoAck: false, consumer: consumer);

        await Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }

}

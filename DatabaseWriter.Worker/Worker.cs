// DatabaseWriter.Worker/Worker.cs
using System;
using System.Collections;
using System.Text;
using System.Json;
using System.Threading;
using System.Threading.Tasks;
using DatabaseWriter.Worker.Models;
using DatabaseWriter.Worker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDatasetRepository _repository;
    private readonly string _rabbitMqConnectionString;
    private IConnection _connection;
    private IModel _channel;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}

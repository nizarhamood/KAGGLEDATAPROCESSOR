//In Services/RabbitMqPublisher.cs
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DataIngestion.Services
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly string _connectionString;

        // The connection string is passed in when the service is created
        public RabbitMqPublisher(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Publish(string message)
        {
            // Use async method internally
            PublishAsync(message).GetAwaiter().GetResult();
        }

        private async Task PublishAsync(string message)
        {
            // 1. Create a connection to the server
            var factory = new ConnectionFactory() { Uri = new Uri(_connectionString) };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            try
            {
                // 2. Declare an exchange and a queue
                // The exchange is the "mail sorting centre"
                await channel.ExchangeDeclareAsync(
                    exchange: "data-ingestion-exchange", 
                    type: ExchangeType.Direct);

                // The queue is the "mailbox"
                await channel.QueueDeclareAsync(
                    queue: "dataset-queue",
                    durable: true, // The queue will survive a server restart
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // 3. Bind the queue to the exchange with a routing key
                // This tells the exchange where to send messages
                await channel.QueueBindAsync(
                    queue: "dataset-queue",
                    exchange: "data-ingestion-exchange",
                    routingKey: "dataset.new");

                // 4. Prepare the message
                var body = Encoding.UTF8.GetBytes(message);

                // 5. Publish the message to the exchange with the routing key
                await channel.BasicPublishAsync(
                    exchange: "data-ingestion-exchange",
                    routingKey: "dataset.new",
                    body: body);
            }
            finally
            {
                await channel.CloseAsync();
                await connection.CloseAsync();
            }
        }
    }
}
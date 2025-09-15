//In Services/RabbitMqPublisher.cs
using RabbitMQ.Client;
using System;
using System.Text;

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
            // 1. Create a connection to the server
            var factory = new ConnectionFactory() { Uri = new Uri(_connectionString) };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // 2. Declare an exchange and a queue
            // The exchange is the "mail sorting centre"
            channel.ExchangeDeclare(exchange: "data-ingestion-exchange", type: ExchangeType.Direct);

            // The queue is the "mailbox"
            channel.QueueDeclare(queue: "dataset-queue",
                                durable: true, // The queue will survive a server restart
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            // 3. Bind the queue to the exchange with a routing key
            // This tells the exchange where to send messages
            channel.QueueBind(queue: "dataset-queue",
                            exchange: "data-ingestion-exchange",
                            routingKey: "dataset.new");

            // 4. Prepare the message
            var body = Encoding.UTF8.GetBytes(message);

            // 5. Publish the message to the exchange with the routing key
            channel.BasicPublish(exchange: "data-ingestion-exchange",
                                routingKey: "dataset.new",
                                body: body);
        }
    }
}
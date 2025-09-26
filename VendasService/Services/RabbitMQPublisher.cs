using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared.Messages;

namespace VendasService.Services
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync<T>(T message, string queueName);
    }

    public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQPublisher> _logger;

        public RabbitMQPublisher(IConnectionFactory connectionFactory, ILogger<RabbitMQPublisher> logger)
        {
            _logger = logger;
            try
            {
                _connection = connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Declara a fila de vendas realizadas
                _channel.QueueDeclare(queue: "venda-realizada", 
                                    durable: true, 
                                    exclusive: false, 
                                    autoDelete: false, 
                                    arguments: null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
                throw;
            }
        }

        public Task PublishAsync<T>(T message, string queueName)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Torna a mensagem persistente

                _channel.BasicPublish(exchange: "",
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Mensagem publicada na fila {QueueName}: {Message}", queueName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem na fila {QueueName}", queueName);
                throw;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
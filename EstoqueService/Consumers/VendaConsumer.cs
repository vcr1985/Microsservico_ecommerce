using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messages;
using System.Text;
using System.Text.Json;
using EstoqueService.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Consumers
{
    public class VendaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VendaConsumer> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IModel? _channel;

        public VendaConsumer(IServiceScopeFactory scopeFactory, ILogger<VendaConsumer> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declara a fila de vendas realizadas
                _channel.QueueDeclare(
                    queue: "venda-realizada",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                _logger.LogInformation("VendaConsumer iniciado e conectado ao RabbitMQ");
                
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar VendaConsumer");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogError("Canal RabbitMQ não está inicializado");
                return Task.CompletedTask;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonSerializer.Deserialize<VendaRealizadaMessage>(json);

                        if (message != null)
                        {
                            _logger.LogInformation("Processando venda: Produto {ProdutoId}, Quantidade {Quantidade}", 
                                message.ProdutoId, message.QuantidadeVendida);

                            using var scope = _scopeFactory.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();

                            var produto = await context.Produtos.FindAsync(message.ProdutoId);
                            if (produto != null)
                            {
                                if (produto.Quantidade >= message.QuantidadeVendida)
                                {
                                    produto.Quantidade -= message.QuantidadeVendida;
                                    await context.SaveChangesAsync();
                                    
                                    _logger.LogInformation("Estoque atualizado para produto {ProdutoId}. Nova quantidade: {Quantidade}", 
                                        produto.Id, produto.Quantidade);
                                }
                                else
                                {
                                    _logger.LogWarning("Estoque insuficiente para produto {ProdutoId}. Disponível: {Disponivel}, Solicitado: {Solicitado}",
                                        produto.Id, produto.Quantidade, message.QuantidadeVendida);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Produto {ProdutoId} não encontrado", message.ProdutoId);
                            }
                        }

                        if (_channel != null)
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar mensagem de venda");
                        if (_channel != null)
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                });
            };

            _channel.BasicConsume(
                queue: "venda-realizada",
                autoAck: false,
                consumer: consumer
            );

            stoppingToken.Register(() =>
            {
                _logger.LogInformation("Parando VendaConsumer...");
                _channel?.Close();
                _connection?.Close();
            });

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("VendaConsumer sendo finalizado");
            _channel?.Close();
            _connection?.Close();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}

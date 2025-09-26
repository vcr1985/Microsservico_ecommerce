using RabbitMQ.Client;

public class RabbitMqTest
{
    public void Test()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare("test", durable: true, exclusive: false, autoDelete: false, arguments: null);
    }
}

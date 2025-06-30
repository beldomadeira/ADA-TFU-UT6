using AdaTfuUt6;
using RabbitMQ.Client;

var config = new RabbitMQConfig();

await using var connection = await new ConnectionFactory()
{
    HostName = config.HostName,
    UserName = config.UserName,
    Password = config.Password,
}.CreateConnectionAsync();

await using var channel = await connection.CreateChannelAsync();

var runtime = new Cli(args).ProcessArg()?.Run(config: config, channel: channel);

if (runtime is null)
{
    Console.WriteLine("Usage: dotnet run [produce|subscribe|consume]");
    return;
}

await runtime;

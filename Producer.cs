using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Text;

namespace AdaTfuUt6;

public class VoteProducer
{
    private readonly RabbitMQConfig Config;
    private readonly IChannel Channel;

    private VoteProducer(RabbitMQConfig config, IChannel channel) {
        Config = config;
        Channel = channel;
    }

    public static async Task<VoteProducer> Initialize(RabbitMQConfig config, IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        return new(config, channel);
    }

    public async Task CastVote(VoteMessage vote)
    {
        try
        {
            var messageBody = JsonConvert.SerializeObject(vote);
            var body = Encoding.UTF8.GetBytes(messageBody);

            await Channel.BasicPublishAsync(
                exchange: "",
                routingKey: Config.QueueName,
                body: body
            ).AsTask();

            Console.WriteLine($"[VOTER {vote.VoterId}] Vote cast for {vote.CandidateName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VOTING ERROR] Failed to cast vote: {ex.Message}");
            throw;
        }
    }
}

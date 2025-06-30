using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Concurrent;

namespace AdaTfuUt6;

public class VoteSubscriber
{
    private readonly RabbitMQConfig Config;
    private readonly IChannel Channel;
    private readonly ConcurrentDictionary<string, int> LiveTally;
    private readonly HashSet<string> SeenVoters;
    private readonly Lock LockObject = new();
    private int TotalVotes = 0;

    private VoteSubscriber(RabbitMQConfig config, IChannel channel)
    {
        Config = config;
        Channel = channel;
        LiveTally = new ConcurrentDictionary<string, int>();
        SeenVoters = [];
    }

    public static async Task<VoteSubscriber> InitializeAsync(RabbitMQConfig config, IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        return new VoteSubscriber(config, channel);
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(Channel);
        
        consumer.ReceivedAsync += async (model, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            try
            {
                var vote = JsonConvert.DeserializeObject<VoteMessage>(messageJson)!;
                
                lock (LockObject)
                {
                    if (SeenVoters.Contains(vote.VoterId))
                    {
                        Console.WriteLine($"🚨 [DUPLICATE] {vote.VoterId} attempted to vote again - REJECTED");
                    }
                    else
                    {
                        SeenVoters.Add(vote.VoterId);
                        LiveTally.AddOrUpdate(vote.CandidateName, 1, (key, oldValue) => oldValue + 1);
                        TotalVotes++;
                        
                        Console.WriteLine($"✅ [VOTE] {vote.VoterId} → {vote.CandidateName}");
                        DisplayLiveTally();
                    }
                }

                await Channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ERROR] Failed to process vote: {ex.Message}");
                await Channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
            }

            await Task.CompletedTask;
        };

        await Channel.BasicConsumeAsync(
            queue: Config.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );

        // Keep the method running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    private void DisplayLiveTally()
    {
        if (TotalVotes == 0) return;

        Console.WriteLine($"📊 Current Tally (Total: {TotalVotes}):");
        
        var sortedResults = LiveTally
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        foreach (var result in sortedResults)
        {
            var percentage = (double)result.Value / TotalVotes * 100;
            Console.WriteLine($"   {result.Key}: {result.Value} votes ({percentage:F1}%)");
        }
        Console.WriteLine();
    }

    public void DisplayCurrentTally()
    {
        Console.WriteLine();
        Console.WriteLine("📊 Final Tally Summary:");
        Console.WriteLine("=======================");
        
        if (TotalVotes == 0)
        {
            Console.WriteLine("No votes recorded.");
            return;
        }

        DisplayLiveTally();
    }
}

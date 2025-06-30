using System.Text;
using System.Threading.Channels;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AdaTfuUt6;

public class VoteConsumer
{
    private readonly RabbitMQConfig Config;
    private readonly IChannel Channel;
    private readonly HashSet<string> ProcessedVoters = [];
    private readonly List<VoteMessage> CollectedVotes = [];

    private VoteConsumer(RabbitMQConfig config, IChannel channel)
    {
        Config = config;
        Channel = channel;
    }

    public static async Task<VoteConsumer> Initialize(RabbitMQConfig config, IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: config.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        await channel.BasicQosAsync(prefetchCount: 1, prefetchSize: 0, global: false);

        return new(config, channel);
    }

    public async Task ProcessAllVotes()
    {
        var consumer = new AsyncEventingBasicConsumer(Channel);
        var processingComplete = new TaskCompletionSource<bool>();
        var noMessageTimeout = TimeSpan.FromSeconds(3);
        var lastMessageTime = DateTime.Now;

        consumer.ReceivedAsync += async (model, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            lastMessageTime = DateTime.Now;

            try
            {
                var vote = JsonConvert.DeserializeObject<VoteMessage>(messageJson)!;

                // Ensure one vote per voter (basic fraud prevention)
                if (ProcessedVoters.Contains(vote.VoterId))
                {
                    Console.WriteLine($"[PROCESSOR] Duplicate vote detected from {vote.VoterId} - REJECTED");
                }
                else
                {
                    CollectedVotes.Add(vote);
                    ProcessedVoters.Add(vote.VoterId);
                    Console.WriteLine($"[PROCESSOR] Accepted vote from {vote.VoterId} for {vote.CandidateName}");
                }

                // Acknowledge message processing
                await Channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PROCESSOR ERROR] Failed to process vote: {ex.Message}");
                await Channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await Channel.BasicConsumeAsync(
            queue: Config.QueueName,
            autoAck: false,
            consumer: consumer
        );

        Console.WriteLine("[PROCESSOR] Processing votes... Waiting for voting to complete.");
        Console.WriteLine($"[PROCESSOR] Will finish processing if no new votes arrive for {noMessageTimeout.TotalSeconds} seconds.");
        Console.WriteLine();

        // Monitor for completion (no new messages for specified timeout)
        while (true)
        {
            await Task.Delay(500);

            if (DateTime.Now - lastMessageTime > noMessageTimeout)
            {
                processingComplete.SetResult(true);
                break;
            }
        }

        await processingComplete.Task;
    }

    public void GenerateResults()
    {
        Console.WriteLine();
        Console.WriteLine("=======================================");
        Console.WriteLine("           VOTING RESULTS");
        Console.WriteLine("=======================================");
        Console.WriteLine();

        if (CollectedVotes.Count == 0)
        {
            Console.WriteLine("No valid votes were cast.");
            return;
        }

        var results = CollectedVotes
            .GroupBy(v => v.CandidateName)
            .Select(g => new VoteTally
            {
                CandidateName = g.Key,
                VoteCount = g.Count()
            })
            .OrderByDescending(r => r.VoteCount)
            .ToList();

        var totalVotes = CollectedVotes.Count;

        Console.WriteLine($"Total Valid Votes Cast: {totalVotes}");
        Console.WriteLine($"Total Unique Voters: {ProcessedVoters.Count}");
        Console.WriteLine();

        foreach (var (i, result) in results.Index())
        {
            var percentage = (double)result.VoteCount / totalVotes * 100;
            var position = i == 0 ? "🏆 WINNER" : $"#{i + 1}";

            Console.WriteLine($"{position,-12} {result.CandidateName,-20} {result.VoteCount,4} votes ({percentage:F1}%)");
        }

        Console.WriteLine();
        Console.WriteLine("=======================================");

        if (results.Count > 1 && results[0].VoteCount == results[1].VoteCount)
        {
            Console.WriteLine("🔄 RESULT: TIE - Additional voting required!");
        }
        else if (results.Count != 0)
        {
            Console.WriteLine($"🎉 WINNER: {results[0].CandidateName}");
        }

        Console.WriteLine("=======================================");
    }
}

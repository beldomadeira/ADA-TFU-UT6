using RabbitMQ.Client;

namespace AdaTfuUt6;

public interface IDemo
{
    Task Run(RabbitMQConfig config, IChannel channel);
}

public class ProducerDemo : IDemo
{
    public async Task Run(RabbitMQConfig config, IChannel channel)
    {
        var producer = await VoteProducer.Initialize(config, channel);

        Console.WriteLine("🗳️  VOTING BOOTH - Casting sample votes...");
        Console.WriteLine();

        var candidates = new[] { "Alice Johnson", "Bob Thompson", "Carol Martinez", "David Lee" };
        var random = new Random();

        // Simulate multiple voters casting votes
        var voters = new[]
        {
                "voter_001", "voter_002", "voter_003", "voter_004", "voter_005",
                "voter_006", "voter_007", "voter_008", "voter_009", "voter_010",
                "voter_011", "voter_012", "voter_013", "voter_014", "voter_015"
        };

        Console.WriteLine($"Simulating {voters.Length} voters choosing from {candidates.Length} candidates:");
        Console.WriteLine($"Candidates: {string.Join(", ", candidates)}");
        Console.WriteLine();

        foreach (var voterId in voters)
        {
            // Weighted random selection to make results more interesting
            var candidateIndex = GetWeightedRandomCandidate(random);
            var selectedCandidate = candidates[candidateIndex];

            var vote = new VoteMessage
            {
                VoterId = voterId,
                CandidateName = selectedCandidate,
                VoteTimestamp = DateTime.Now
            };

            await producer.CastVote(vote);
        }

        // Simulate a duplicate vote attempt
        Console.WriteLine();
        Console.WriteLine("🚨 Attempting duplicate vote...");
        var duplicateVote = new VoteMessage
        {
            VoterId = "voter_001", // This voter already voted
            CandidateName = candidates[random.Next(candidates.Length)],
            VoteTimestamp = DateTime.Now
        };

        await producer.CastVote(duplicateVote);

        Console.WriteLine();
        Console.WriteLine("✅ Voting session complete!");
    }

    static int GetWeightedRandomCandidate(Random random)
    {
        var roll = random.Next(100);

        // Alice Johnson: 40% chance
        if (roll < 40) return 0;
        // Bob Thompson: 30% chance  
        if (roll < 70) return 1;
        // Carol Martinez: 20% chance
        if (roll < 90) return 2;
        // David Lee: 10% chance
        return 3;
    }
}

public class ConsumerDemo : IDemo
{
    public async Task Run(RabbitMQConfig config, IChannel channel)
    {
        var consumer = await VoteConsumer.Initialize(config, channel);

        await consumer.ProcessAllVotes();
        consumer.GenerateResults();
    }
}

public class SubscriberDemo : IDemo
{
    public async Task Run(RabbitMQConfig config, IChannel channel)
    {
        var subscriber = await VoteSubscriber.InitializeAsync(config, channel);
        
        Console.WriteLine("📊 VOTE MONITOR - Real-time vote tracking");
        Console.WriteLine("=========================================");
        Console.WriteLine("Listening for votes... Press 'q' to quit.");
        Console.WriteLine();

        // Start listening for votes in background
        var cancellationTokenSource = new CancellationTokenSource();
        var subscriptionTask = subscriber.StartListeningAsync(cancellationTokenSource.Token);

        // Wait for user to press 'q' to quit
        await WaitForQuitCommand();
        
        cancellationTokenSource.Cancel();
        
        try
        {
            await subscriptionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        Console.WriteLine();
        Console.WriteLine("📊 Vote monitoring stopped.");
        subscriber.DisplayCurrentTally();
    }

    private static async Task WaitForQuitCommand()
    {
        await Task.Run(() =>
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
            }
            while (keyInfo.KeyChar != 'q' && keyInfo.KeyChar != 'Q');
        });
    }
}

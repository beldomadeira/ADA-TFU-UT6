namespace AdaTfuUt6;

public record VoteMessage
{
    public required string VoterId { get; set; }
    public required string CandidateName { get; set; }
    public required DateTime VoteTimestamp { get; set; }
}

public record VoteTally
{
    public required string CandidateName { get; set; }
    public required int VoteCount { get; set; }
}

public record RabbitMQConfig
{
    public string HostName { get; set; } = "localhost";
    public string QueueName { get; set; } = "voting_queue";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

namespace AdaTfuUt6;

public class Cli(string[] args)
{
    public IDemo? ProcessArg()
    {
        return args switch {
            ["produce", ..] => new ProducerDemo(),
            ["subscribe", ..] => new SubscriberDemo(),
            ["consume", ..] => new ConsumerDemo(),
            _ => null,
        };
    }
}

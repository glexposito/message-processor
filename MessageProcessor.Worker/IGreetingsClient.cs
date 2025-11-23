namespace MessageProcessor.Worker;

public interface IGreetingsClient
{
    Task<string> SayHelloAsync();
}
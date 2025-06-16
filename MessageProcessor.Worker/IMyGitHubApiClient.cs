namespace MessageProcessor.Worker;

public interface IMyGitHubApiClient
{
    Task<string> GetRootAsync();
}
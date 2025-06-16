namespace MessageProcessor.Worker;

public class MyGitHubApiClient : IMyGitHubApiClient
{
    private readonly HttpClient _httpClient;

    public MyGitHubApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("poc-app");
    }

    public async Task<string> GetRootAsync()
    {
        var response = await _httpClient.GetAsync("/");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
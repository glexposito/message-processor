namespace MessageProcessor.Worker;

public class GreetingsClient(HttpClient httpClient) : IGreetingsClient
{
    public async Task<string> SayHelloAsync()
    {
        var response = await httpClient.GetAsync("/hello");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
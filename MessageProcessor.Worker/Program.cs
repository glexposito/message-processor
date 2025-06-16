using MessageProcessor.Worker;
    
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<IMyGitHubApiClient, MyGitHubApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
});

builder.Services.AddSingleton<ServiceBusMessageProcessor>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
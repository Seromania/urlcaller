namespace urlcaller;

public class Worker : BackgroundService
{
    private const string WorkerSection = "Worker";
    private const string DelayInMsKey = "delayInMs";
    private const string UrlKey = "url";

    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;

    private static readonly HttpClient HttpClient = new();

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        var configBuilder = new ConfigurationBuilder();
        configBuilder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        _config = configBuilder.Build();
        HttpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = _config.GetSection(WorkerSection)
            .GetValue<int>(DelayInMsKey);
        var url = _config.GetSection(WorkerSection)
            .GetValue<string>(UrlKey);

        if (url == default)
        {
            throw new InvalidOperationException("Url is missing from configuration");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker will call: {url}", url);
            }

            await CallUrl(url, stoppingToken);

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task CallUrl(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("Error calling url {statusCode}", response.StatusCode);
                }
            }
        }
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(e, "Error calling url {url}", url);
            }
        }
    }
}
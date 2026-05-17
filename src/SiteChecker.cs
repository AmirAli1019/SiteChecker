using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Text;
using Spectre.Console;

record SiteCheckerConfig(string fileName, int maxConcurrentTests, int pingTimeout, int httpTimeout);

partial class SiteChecker(string[] commandLineArgs)
{
    private SiteCheckerConfig _config = null!;
    private SemaphoreSlim _semaphore = null!;
    HttpClient _httpClient = null!;

    private readonly string[] _commandLineArgs = commandLineArgs;
    private readonly Table _table = new();
    private readonly ConcurrentBag<string> _availableSites = new();

    private readonly object _lock = new();

    public async Task Start()
    {
        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs args) =>
        {
            AnsiConsole.MarkupLine(
                "[yellow]\nReceived interrupt. Showing partial results ...\n[/]"
            );

            ShowResults();
        };

        ParseResult parseResult = CommandLineSetup(_commandLineArgs);

        _config = new(
            parseResult.GetValue(CliOptions.fileNameArgument)!,
            parseResult.GetValue(CliOptions.maxConcurrentTestsOption),
            parseResult.GetValue(CliOptions.pingTimeoutOption),
            parseResult.GetValue(CliOptions.httpTimeoutOption)
        );

        _semaphore = new(_config.maxConcurrentTests);
        _httpClient = new() { Timeout = TimeSpan.FromSeconds(_config.httpTimeout) };

        string[] sites = null!;
        try
        {
            sites = (
                await File.ReadAllTextAsync(_config.fileName, new UTF8Encoding(false, true))
            ).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }
        catch (DecoderFallbackException)
        {
            ShowErrorAndExit("The input file cannot be a binary file.");
        }
        catch (Exception exp)
        {
            ShowErrorAndExit(exp.Message);
        }

        _table.AddColumns("Website", "ICMP (ms)", "HTTP Status");

        var tasks = sites.Select(TestWebsite);
        await Task.WhenAll(tasks);

        ShowResults();
    }

    async Task TestWebsite(string site)
    {
        await _semaphore.WaitAsync();
        try
        {
            string pingResult = "[red]FAILED[/]";
            try
            {
                using var pinger = new Ping();
                var reply = await pinger.SendPingAsync(site, _config.pingTimeout);
                if (reply.Status == IPStatus.Success)
                    pingResult = $"[green]{reply.RoundtripTime}[/]";
            }
            catch { }

            string httpResult = "[red]ERR[/]";
            bool isSuccess = true;
            try
            {
                var response = await _httpClient.GetAsync($"https://{site}");
                httpResult = $"[green]{((int)response.StatusCode)}[/]";
            }
            catch
            {
                isSuccess = false;
            }

            lock (_lock)
            {
                _table.AddRow($"[blue]{site}[/]", pingResult, httpResult);
                if (isSuccess)
                    _availableSites.Add(site);

                AnsiConsole.MarkupLine($"[grey]Finished:[/] {site}");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    void ShowResults()
    {
        AnsiConsole.Write(_table);
        Console.WriteLine("\nAvailable: " + string.Join(", ", _availableSites));
    }

    static void ShowErrorAndExit(string message)
    {
        Console.WriteLine(message);
        Environment.Exit(1);
    }
}

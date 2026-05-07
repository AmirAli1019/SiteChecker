using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Spectre.Console;

class Program
{
    static readonly Table table = new();
    static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(15) };
    static readonly SemaphoreSlim semaphore = new(5);
    static readonly ConcurrentBag<string> availableSites = new();

    static readonly object _lock = new();

    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs args) =>
        {
            AnsiConsole.MarkupLine(
                "[yellow]\nReceived interrupt. Showing partial results ...\n[/]"
            );

            ShowResults();
        };

        if (args.Length == 0)
            return;

        string[] sites = (await File.ReadAllTextAsync(args[0])).Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        table.AddColumns("Website", "ICMP (ms)", "HTTP Status");

        var tasks = sites.Select(TestWebsite);
        await Task.WhenAll(tasks);

        ShowResults();
    }

    static async Task TestWebsite(string site)
    {
        await semaphore.WaitAsync();
        try
        {
            // 1. ICMP Ping (Native .NET)
            string pingResult = "[red]FAILED[/]";
            try
            {
                using var pinger = new Ping();
                var reply = await pinger.SendPingAsync(site, 2000);
                if (reply.Status == IPStatus.Success)
                    pingResult = $"[green]{reply.RoundtripTime}[/]";
            }
            catch { }

            string httpResult = "[red]ERR[/]";
            bool isSuccess = false;
            try
            {
                var response = await client.GetAsync($"https://{site}");
                isSuccess = response.IsSuccessStatusCode;
                httpResult = isSuccess
                    ? $"[green]{(int)response.StatusCode}[/]"
                    : $"[red]{(int)response.StatusCode}[/]";
            }
            catch { }

            lock (_lock)
            {
                table.AddRow($"[blue]{site}[/]", pingResult, httpResult);
                if (isSuccess)
                    availableSites.Add(site);

                AnsiConsole.MarkupLine($"[grey]Finished:[/] {site}");
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    static void ShowResults()
    {
        AnsiConsole.Write(table);
        Console.WriteLine("\nAvailable: " + string.Join(", ", availableSites));
    }
}

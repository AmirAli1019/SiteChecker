using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

class Program
{
    static Table table = new();

    const string FAILED = "[red]FAILED[/]";

    static HttpClient client = new();

    const int MAX_CONCURRENT_THREADS = 5;
    static SemaphoreSlim semaphore = new SemaphoreSlim(MAX_CONCURRENT_THREADS);

    static List<string> availableSites = new();

    static void ShowResults()
    {
        AnsiConsole.Write(table);

        Console.WriteLine("\n" + string.Join(',', availableSites));
    }

    static string GetHTTPSuccessOutput(int statusCode) => $"[green]{statusCode}[/]";

    static string GetHTTPFailOutput(int statusCode) => $"[red]{statusCode}[/]";

    static string ExtractPingAverage(StreamReader pingStdout)
    {
        string output = pingStdout.ReadToEnd();

        string average = Regex
            .Match(output, @"rtt min/avg/max/mdev = [\d\.]+/([\d\.]+)/[\d\.]+/[\d\.]+ ms")
            .Groups[1]
            .Value;

        return $"[green]{average}[/]";
    }

    static async Task TestWebsite(string site)
    {
        try
        {
            Console.WriteLine($"Pinging {site} ...");
            Process pingProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "ping",
                    Arguments = $"-c 3 -W 1 {site}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            )!;
            pingProcess.WaitForExit();

            HttpResponseMessage response = new();

            try
            {
                Console.WriteLine($"Getting {site} ...");
                response = await client.GetAsync($"https://{site}");
            }
            catch { }

            lock (table)
            {
                table.AddRow(
                    $"[blue]{site}[/]",
                    pingProcess.ExitCode == 0
                        ? ExtractPingAverage(pingProcess.StandardOutput)
                        : FAILED,
                    response.IsSuccessStatusCode
                        ? GetHTTPSuccessOutput(((int)response.StatusCode))
                        : GetHTTPFailOutput(((int)response.StatusCode))
                );

                if (response.IsSuccessStatusCode)
                    availableSites.Add(site);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs args) =>
        {
            AnsiConsole.MarkupLine(
                "[yellow]\nReceived interrupt. Showing partial results ...\n[/]"
            );

            ShowResults();
        };

        using StreamReader f = new(args[0]);

        client.Timeout = new TimeSpan(0, 0, 15);

        string[] sites = f.ReadToEnd().Split("\n", StringSplitOptions.RemoveEmptyEntries);

        table.AddColumns("Website", "ICMP", "HTTP");

        var tasks = new List<Task>();

        foreach (string site in sites)
        {
            if (string.IsNullOrWhiteSpace(site))
                continue;

            await semaphore.WaitAsync();

            var task = Task.Run(() => TestWebsite(site));

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        ShowResults();
    }
}

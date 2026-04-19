using System.Diagnostics;
using System.Threading.Tasks;
using Spectre.Console;

class Program
{
    static async Task Main(string[] args)
    {
        using StreamReader f = new(args[0]);

        HttpClient client = new();
        client.Timeout = new TimeSpan(0, 0, 15);

        string[] sites = f.ReadToEnd().Split("\n", StringSplitOptions.RemoveEmptyEntries);

        Table table = new();
        table.AddColumns("Website", "ICMP", "HTTP");

        const string OK = "[green]OK[/]";
        const string FAILED = "[red]FAILED[/]";

        const int MAX_CONCURRENT_THREADS = 5;

        var semaphore = new SemaphoreSlim(MAX_CONCURRENT_THREADS);
        var tasks = new List<Task>();

        foreach (string site in sites)
        {
            if (string.IsNullOrWhiteSpace(site))
                continue;

            await semaphore.WaitAsync();

            var task = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"Pinging {site} ...");
                    Process p = Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "ping",
                            Arguments = $"-c 3 -W 1 {site}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                        }
                    )!;
                    p.WaitForExit();

                    HttpResponseMessage response = new(System.Net.HttpStatusCode.Unauthorized);

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
                            p.ExitCode == 0 ? OK : FAILED,
                            response.IsSuccessStatusCode ? OK : FAILED
                        );
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        AnsiConsole.Write(table);
    }
}

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

        string[] sites = f.ReadToEnd().Split("\n");

        Table table = new();
        table.AddColumns("Website", "ICMP", "HTTP");

        const string OK = "[green]OK[/]";
        const string FAILED = "[red]FAILED[/]";

        foreach (string site in sites)
        {
            if (string.IsNullOrWhiteSpace(site))
                continue;

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

            table.AddRow(
                $"[blue]{site}[/]",
                p.ExitCode == 0 ? OK : FAILED,
                response.IsSuccessStatusCode ? OK : FAILED
            );
        }

        AnsiConsole.Write(table);
    }
}

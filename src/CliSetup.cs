global using System.CommandLine;

static class CliOptions
{
    public static Argument<string> fileNameArgument = new("file-path");

    public static Option<int> maxConcurrentTestsOption = new("--max-concurrent", "-m")
    {
        DefaultValueFactory = parseResult => 5,
        Description = "The number of maximum concurrent tests",
    };

    public static Option<int> pingTimeoutOption = new("--ping-timeout", "-p")
    {
        DefaultValueFactory = parseResult => 2000,
        Description = "Timeout for every ping request in milliseconds",
    };

    public static Option<int> httpTimeoutOption = new("--http-timeout", "-t")
    {
        DefaultValueFactory = parseResult => 15,
        Description = "Timeout for every HTTP request in seconds",
    };

    public static Option<bool> versionOption = new("--version")
    {
        Description = "Show version information",
    };
}

partial class SiteChecker
{
    static ParseResult CommandLineSetup(string[] args)
    {
        RootCommand rootCommand = new(
            "Test domains reachability using both ICMP and HTTP protocols"
        )
        {
            CliOptions.fileNameArgument,
            CliOptions.httpTimeoutOption,
            CliOptions.maxConcurrentTestsOption,
            CliOptions.pingTimeoutOption,
            CliOptions.versionOption,
        };

        var parseResult = rootCommand.Parse(args);
        parseResult.Invoke();

        if (parseResult.GetValue(CliOptions.versionOption))
        {
            string version = typeof(Program).Assembly.GetName().Version!.ToString(3);
            Console.WriteLine($"SiteChecker v{version}");
        }

        if (
            parseResult.Errors.Count > 0
            || args.Contains("--help")
            || args.Contains("-help")
            || args.Contains("-h")
            || args.Contains("-?")
        )
        {
            Environment.Exit(1);
        }

        return parseResult;
    }
}

partial class Program
{
    static async Task Main(string[] args)
    {
        SiteChecker checker = new(args);
        await checker.Start();
    }
}

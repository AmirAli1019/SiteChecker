# SiteChecker

A fast, concurrent command-line utility to test domain reachability using both ICMP (ping) and HTTP protocols. SiteChecker helps you quickly verify which websites are accessible and responsive from your network.

## Requirements

- **.NET 10.0** or later

## Installation

### Option 1: Build from Source

```bash
git clone https://github.com/AmirAli1019/SiteChecker.git
cd SiteChecker
dotnet build -c Release
```

### Option 2: Publish as Single File

```bash
dotnet publish -c Release -o ./publish
```

This creates a standalone executable in the `./publish` directory that requires no .NET runtime.

## Dependencies

- **Spectre.Console** (v0.55.0) - For beautiful console output and tables
- **System.CommandLine** (v2.0.8) - For command-line argument parsing

## Usage

### Basic Command

```bash
SiteChecker <file-path> [options]
```

### Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `file-path` | Yes | Path to a text file containing one domain per line |

### Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--max-concurrent` | `-m` | 5 | Maximum number of concurrent domain tests |
| `--ping-timeout` | `-p` | 2000 | Timeout for ICMP (ping) requests in milliseconds |
| `--http-timeout` | `-t` | 15 | Timeout for HTTP requests in seconds |

### Examples

#### Basic usage with default settings (5 concurrent tests)

```bash
SiteChecker sites.txt
```

#### Test with 10 concurrent requests

```bash
SiteChecker sites.txt --max-concurrent 10
```

#### Short form with custom timeouts

```bash
SiteChecker sites.txt -m 20 -p 5000 -t 30
```

#### Test with aggressive settings (good for fast networks)

```bash
SiteChecker sites.txt --max-concurrent 50 --ping-timeout 1000 --http-timeout 10
```

## Input File Format

Create a text file with one domain per line (without `https://` or `http://`):

```
example.com
google.com
github.com
stackoverflow.com
```

Example files are included in the repository:

- `sites.txt` - General websites
- `chinese-websites.txt` - Chinese websites

## Output Format

The program displays results in a formatted table showing:

- **Website**: The domain being tested
- **ICMP (ms)**: Response time in milliseconds (green = success, red = failed)
- **HTTP Status**: HTTP status code (green = success, red = error/timeout)

After all tests complete, available websites (those with successful HTTP responses) are listed separately.

Example output:

```
┌─────────────────┬────────────┬──────────────┐
│ Website         │ ICMP (ms)  │ HTTP Status  │
├─────────────────┼────────────┼──────────────┤
│ example.com     │ 45         │ 200          │
│ google.com      │ 23         │ 200          │
│ failed-site.com │ FAILED     │ ERR          │
└─────────────────┴────────────┴──────────────┘

Available: example.com, google.com
```

## Performance Tips

- **Increase max-concurrent**: For networks with good latency and bandwidth, increase `-m` to 50-100 for faster bulk testing
- **Reduce timeouts**: If testing from a fast network, reduce `--ping-timeout` and `--http-timeout` for quicker detection of failures
- **Monitor system resources**: Very high concurrent values may impact system performance; start with 10-20 and adjust

## Keyboard Shortcuts

- **Ctrl+C**: Stop testing and display partial results immediately

## License

This project is open source and available under the MIT License.

## Contributing

Feel free to open issues and submit pull requests to improve SiteChecker!

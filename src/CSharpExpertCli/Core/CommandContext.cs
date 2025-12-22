using Microsoft.CodeAnalysis;

namespace CSharpExpertCli.Core;

/// <summary>
/// Shared context for all commands, providing access to the Roslyn API client
/// and global options.
/// </summary>
public class CommandContext : IAsyncDisposable
{
    private RoslynApiClient? _client;
    private Solution? _loadedSolution;

    public string? SolutionPath { get; set; }
    public string? ProjectPath { get; set; }
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Json;
    public bool Verbose { get; set; }
    public OutputFormatter Formatter { get; } = new();

    /// <summary>
    /// Gets the Roslyn API client, creating it if necessary.
    /// </summary>
    public RoslynApiClient Client
    {
        get
        {
            _client ??= new RoslynApiClient();
            return _client;
        }
    }

    /// <summary>
    /// Gets the loaded solution, loading it if necessary.
    /// </summary>
    public async Task<Solution> GetSolutionAsync()
    {
        if (_loadedSolution != null)
        {
            return _loadedSolution;
        }

        if (string.IsNullOrEmpty(SolutionPath) && string.IsNullOrEmpty(ProjectPath))
        {
            throw new InvalidOperationException("Either --solution or --project must be specified");
        }

        var path = SolutionPath ?? ProjectPath!;

        if (Verbose)
        {
            Console.Error.WriteLine($"Loading: {path}");
        }

        _loadedSolution = await Client.OpenSolutionAsync(path);
        return _loadedSolution;
    }

    /// <summary>
    /// Logs a message to stderr if verbose mode is enabled.
    /// </summary>
    public void LogVerbose(string message)
    {
        if (Verbose)
        {
            Console.Error.WriteLine($"[Verbose] {message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}

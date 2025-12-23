namespace CSharpExpertCli.Core;

/// <summary>
/// Utility class for discovering .sln and .csproj files in the current directory.
/// </summary>
public static class SolutionDiscovery
{
    /// <summary>
    /// Attempts to find a solution or project file in the current directory.
    /// Priority: .sln files first, then .csproj files if no solution is found.
    /// </summary>
    /// <param name="searchDirectory">Directory to search (defaults to current directory)</param>
    /// <returns>Path to the found file, or null if none found</returns>
    public static string? FindSolutionOrProject(string? searchDirectory = null)
    {
        var directory = searchDirectory ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(directory))
        {
            return null;
        }

        // First, try to find .sln files (preferred)
        var solutionFiles = Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly);

        if (solutionFiles.Length == 1)
        {
            return solutionFiles[0];
        }

        if (solutionFiles.Length > 1)
        {
            // Multiple solution files found - ambiguous
            var fileNames = string.Join(", ", solutionFiles.Select(Path.GetFileName));
            throw new InvalidOperationException(
                $"Multiple solution files found in '{directory}': {fileNames}. " +
                "Please specify which one to use with --solution or -s.");
        }

        // No .sln found, try .csproj files
        var projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);

        if (projectFiles.Length == 1)
        {
            return projectFiles[0];
        }

        if (projectFiles.Length > 1)
        {
            // Multiple project files found - ambiguous
            var fileNames = string.Join(", ", projectFiles.Select(Path.GetFileName));
            throw new InvalidOperationException(
                $"Multiple project files found in '{directory}': {fileNames}. " +
                "Please specify which one to use with --project or -p.");
        }

        // Nothing found
        return null;
    }

    /// <summary>
    /// Gets a descriptive message about what was found or not found during discovery.
    /// </summary>
    public static string GetDiscoveryMessage(string directory)
    {
        var solutionFiles = Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly);
        var projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);

        if (solutionFiles.Length == 0 && projectFiles.Length == 0)
        {
            return $"No .sln or .csproj files found in '{directory}'. " +
                   "Please specify a solution or project file with --solution or --project.";
        }

        if (solutionFiles.Length > 1)
        {
            return $"Multiple solution files found: {string.Join(", ", solutionFiles.Select(Path.GetFileName))}";
        }

        if (projectFiles.Length > 1)
        {
            return $"Multiple project files found: {string.Join(", ", projectFiles.Select(Path.GetFileName))}";
        }

        return ""; // Should not reach here if used correctly
    }
}

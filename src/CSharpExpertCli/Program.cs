using System.CommandLine;
using CSharpExpertCli.Core;
using CSharpExpertCli.Commands;
using CSharpExpertCli.Commands.Symbol;
using CSharpExpertCli.Commands.Compilation;
using CSharpExpertCli.Commands.TypeHierarchy;
using CSharpExpertCli.Commands.CallAnalysis;
using CSharpExpertCli.Commands.Dependency;
using CSharpExpertCli.Commands.CodeGeneration;
using CSharpExpertCli.Commands.Organization;

namespace CSharpExpertCli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Build root command
        var rootCommand = new RootCommand("C# skill tool for code analysis and refactoring using Roslyn APIs");

        // Add global options
        rootCommand.AddGlobalOption(GlobalOptions.SolutionOption);
        rootCommand.AddGlobalOption(GlobalOptions.ProjectOption);
        rootCommand.AddGlobalOption(GlobalOptions.OutputOption);
        rootCommand.AddGlobalOption(GlobalOptions.VerboseOption);

        // Create command context
        await using var context = new CommandContext();

        // Register all 18 commands
        var commandHandlers = new ICommandHandler[]
        {
            // Symbol commands (5)
            new FindDefinitionCommand(),
            new FindReferencesCommand(),
            new SignatureCommand(),
            new ListMembersCommand(),
            new RenameCommand(),

            // Compilation commands (2)
            new DiagnosticsCommand(),
            new CheckSymbolExistsCommand(),

            // Type hierarchy commands (2)
            new FindImplementationsCommand(),
            new InheritanceTreeCommand(),

            // Call analysis commands (2)
            new FindCallersCommand(),
            new FindCalleesCommand(),

            // Dependency commands (2)
            new DependenciesCommand(),
            new UnusedCodeCommand(),

            // Code generation commands (2)
            new GenerateInterfaceCommand(),
            new ImplementInterfaceCommand(),

            // Organization commands (3)
            new ListTypesCommand(),
            new NamespaceTreeCommand(),
            new AnalyzeFileCommand(),
        };

        foreach (var handler in commandHandlers)
        {
            rootCommand.AddCommand(handler.BuildCommand(context));
        }

        // Parse and invoke
        return await rootCommand.InvokeAsync(args);
    }
}

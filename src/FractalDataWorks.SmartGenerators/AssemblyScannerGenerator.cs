using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace FractalDataWorks.SmartGenerators;

/// <summary>
/// Incremental generator that scans assemblies for all named types and registers the scanner service.
/// </summary>
[Generator]
public sealed class AssemblyScannerGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the generator by registering the assembly scanner when the enable attribute is present.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            // Gate: only proceed if attribute is present on compilation
            var enableAttr = compilation.Assembly.GetAttributes()
                .FirstOrDefault(a => string.Equals(a.AttributeClass?.ToDisplayString(), $"FractalDataWorks.SmartGenerators.EnableAssemblyScannerAttribute", System.StringComparison.Ordinal));
            if (enableAttr is null)
            {
                return;
            }

            var scanner = new AssemblyScanner(compilation);
            AssemblyScannerService.Register(compilation, scanner);
        });
    }
}

/// <summary>
/// Service to register and retrieve the assembly scanner for a compilation.
/// </summary>
public static class AssemblyScannerService
{
    private static readonly ConditionalWeakTable<Compilation, IAssemblyScanner> _cache = new();

    /// <summary>
    /// Registers the assembly scanner for the given compilation.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="scanner">The scanner instance.</param>
    public static void Register(Compilation compilation, IAssemblyScanner scanner)
    {
        _ = _cache.Remove(compilation);
        _cache.Add(compilation, scanner);
    }

    /// <summary>
    /// Retrieves the registered assembly scanner for the given compilation.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The registered scanner or null.</returns>
    public static IAssemblyScanner? Get(Compilation compilation)
        => _cache.TryGetValue(compilation, out var scanner) ? scanner : null;
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FractalDataWorks.SmartGenerators;

/// <inheritdoc/>
public sealed class AssemblyScanner : IAssemblyScanner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyScanner"/> class.
    /// </summary>
    /// <param name="compilation">The compilation to scan for types.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="compilation"/> is null.</exception>
    public AssemblyScanner(Compilation compilation)
    {
        // Validate parameter 'compilation' before using it
        if (compilation == null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        AllNamedTypes = [.. WalkAssemblies(compilation.Assembly)];
    }

    /// <inheritdoc/>
    /// <summary>
    /// Gets all named types in the compilation.
    /// </summary>
    public IEnumerable<INamedTypeSymbol> AllNamedTypes { get; }

    private static IEnumerable<INamedTypeSymbol> WalkAssemblies(IAssemblySymbol root)
    {
        var stack = new Stack<IAssemblySymbol>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        stack.Push(root);

        while (stack.Count > 0)
        {
            var asm = stack.Pop();
            if (!visited.Add(asm.Identity.ToString()))
            {
                continue;
            }

            foreach (var module in asm.Modules)
            {
                foreach (var type in EnumerateTypes(module.GlobalNamespace))
                {
                    yield return type;
                }
            }

            foreach (var reference in asm.Modules.First().ReferencedAssemblySymbols)
            {
                // only scan assemblies that opt-in via [EnableAssemblyScanner]
                if (reference.GetAttributes().Any(ad => string.Equals(ad.AttributeClass?.ToDisplayString(), $"FractalDataWorks.SmartGenerators.EnableAssemblyScannerAttribute", StringComparison.Ordinal)))
                {
                    stack.Push(reference);
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol ns)
    {
        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            foreach (var nt in EnumerateTypes(nestedNs))
            {
                yield return nt;
            }
        }

        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
        }
    }
}

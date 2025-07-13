using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace FractalDataWorks.SmartGenerators;

/// <summary>
/// Provides a cached enumeration of all named type symbols available in the current compilation
/// and its referenced assemblies.
/// </summary>
public interface IAssemblyScanner
{
    /// <summary>
    /// Gets all named type symbols in the compilation and its references.
    /// </summary>
    IEnumerable<INamedTypeSymbol> AllNamedTypes { get; }
}

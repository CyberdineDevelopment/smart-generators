using System;
using Microsoft.CodeAnalysis;

namespace FractalDataWorks.SmartGenerators;

/// <summary>
/// Extension methods for <see cref="SourceProductionContext"/> to simplify service retrieval.
/// </summary>
internal static class GeneratorExecutionContextExtensions
{
    /// <summary>
    /// Retrieves the required assembly scanner service for the given compilation.
    /// </summary>
    /// <typeparam name="T">Type of service to retrieve. Only <see cref="IAssemblyScanner"/> is supported.</typeparam>
    /// <param name="context">The source production context.</param>
    /// <param name="compilation">The compilation context.</param>
    /// <returns>The registered <see cref="IAssemblyScanner"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the requested service is not registered or not supported.
    /// </exception>
    public static T GetRequiredService<T>(this SourceProductionContext context, Compilation compilation)
        where T : class
    {
        if (typeof(T) != typeof(IAssemblyScanner))
        {
            throw new InvalidOperationException($"No service for type '{typeof(T).Name}' is available.");
        }

        var scanner = AssemblyScannerService.Get(compilation)
                      ?? throw new InvalidOperationException(
                          $"Assembly scanner not registered. Ensure {nameof(EnableAssemblyScannerAttribute)} is applied or MSBuild flag is set.");

        return (T)(object)scanner;
    }
}

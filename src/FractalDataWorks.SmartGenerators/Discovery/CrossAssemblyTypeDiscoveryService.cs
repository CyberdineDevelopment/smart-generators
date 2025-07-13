using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#pragma warning disable SA1202

namespace FractalDataWorks.SmartGenerators.Discovery;

/// <summary>
/// Service for discovering types across assembly boundaries.
/// </summary>
public class CrossAssemblyTypeDiscoveryService : ICrossAssemblyTypeDiscoveryService
{
    private const string _enableCrossAssemblyDiscoveryProperty = $"EnableCrossAssemblyDiscovery";
    private readonly ConcurrentDictionary<(INamedTypeSymbol, Compilation), INamedTypeSymbol[]> _derivedTypesCache =
        new(new TypeSymbolEqualityComparer());
    private readonly ConcurrentDictionary<(INamedTypeSymbol, Compilation), INamedTypeSymbol[]> _attributedTypesCache =
        new(new TypeSymbolEqualityComparer());
    private readonly ConcurrentDictionary<(string, Compilation), INamedTypeSymbol[]> _attributedTypesByNameCache = new();

    /// <summary>
    /// Determines if cross-assembly discovery is enabled for the current compilation.
    /// </summary>
    /// <param name="compilation">The compilation context.</param>
    /// <returns>True if cross-assembly discovery is enabled.</returns>
    public bool IsCrossAssemblyDiscoveryEnabled(Compilation compilation)
    {
        if (compilation == null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        // Check for the MSBuild property
        var enableCrossAssemblyDiscovery = GetMsBuildProperty(compilation, _enableCrossAssemblyDiscoveryProperty);

        // Default to enabled if the property isn't explicitly set to false
        if (string.IsNullOrEmpty(enableCrossAssemblyDiscovery))
        {
            return true;
        }

        // Only return false if it's explicitly set to 'false' or '0'
        if (string.Equals(enableCrossAssemblyDiscovery, $"false", StringComparison.OrdinalIgnoreCase) ||
string.Equals(enableCrossAssemblyDiscovery, $"0", StringComparison.Ordinal))
        {
            return false;
        }

        // Otherwise return true (for 'true', '1' or any other value)
        return true;
    }

    /// <summary>
    /// Finds all types in the compilation and referenced assemblies that derive from the specified base type.
    /// </summary>
    /// <param name="baseType">The base type to search for derived types.</param>
    /// <param name="compilation">The compilation context.</param>
    /// <returns>A collection of named type symbols that derive from the base type.</returns>
    public IEnumerable<INamedTypeSymbol> FindDerivedTypes(INamedTypeSymbol baseType, Compilation compilation)
    {
        return baseType == null
            ? throw new ArgumentNullException(nameof(baseType))
            : compilation == null
            ? throw new ArgumentNullException(nameof(compilation))
            : (IEnumerable<INamedTypeSymbol>)_derivedTypesCache.GetOrAdd((baseType, compilation), tuple =>
        {
            var (type, comp) = tuple;
            var results = new List<INamedTypeSymbol>();

            // Always find derived types in the current assembly
            var currentAssemblyTypes = FindDerivedTypesInCurrentAssembly(type, comp);
            results.AddRange(currentAssemblyTypes);

            // Only include referenced assemblies if cross-assembly discovery is enabled
            if (IsCrossAssemblyDiscoveryEnabled(comp))
            {
                var referencedAssemblyTypes = FindDerivedTypesInReferencedAssemblies(type, comp);
                results.AddRange(referencedAssemblyTypes);
            }

            return [.. results];
        });
    }

    /// <summary>
    /// Finds derived types in the current assembly.
    /// </summary>
    /// <param name="baseType">Base type to derive from.</param>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Derived types.</returns>
    private static List<INamedTypeSymbol> FindDerivedTypesInCurrentAssembly(INamedTypeSymbol baseType, Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();

        // Get all named types in the compilation
        var types = GetAllNamedTypesInCompilation(compilation);

        foreach (var type in types)
        {
            if (IsDerivedFrom(type, baseType) && !IsAbstract(type))
            {
                results.Add(type);
            }
        }

        return results;
    }

    /// <summary>
    /// Finds derived types in referenced assemblies.
    /// </summary>
    /// <param name="baseType">Base type to derive from.</param>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Derived types.</returns>
    private static List<INamedTypeSymbol> FindDerivedTypesInReferencedAssemblies(INamedTypeSymbol baseType, Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();
        _ = baseType.GetFullMetadataName();

        // Process each referenced assembly
        foreach (var reference in compilation.References)
        {
            // Get the assembly symbol for the reference
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            // Get the global namespace of the referenced assembly
            var globalNamespace = assemblySymbol.GlobalNamespace;

            // Find all named types in the referenced assembly
            var typesInAssembly = GetAllTypesInNamespace(globalNamespace);

            // Check each type to see if it derives from the base type
            foreach (var type in typesInAssembly)
            {
                if (IsDerivedFrom(type, baseType) && !IsAbstract(type))
                {
                    results.Add(type);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Gets all named types in the compilation.
    /// </summary>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Named types.</returns>
    private static IEnumerable<INamedTypeSymbol> GetAllNamedTypesInCompilation(Compilation compilation) => GetAllTypesInNamespace(compilation.GlobalNamespace);

    /// <summary>
    /// Gets all types in the namespace.
    /// </summary>
    /// <param name="namespaceSymbol">Namespace symbol.</param>
    /// <returns>Types.</returns>
    private static IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;

            // Recursively process nested types
            foreach (var nestedType in GetNestedTypes(type))
            {
                yield return nestedType;
            }
        }

        // Recursively process nested namespaces
        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypesInNamespace(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    /// <summary>
    /// Gets nested types.
    /// </summary>
    /// <param name="typeSymbol">Type symbol.</param>
    /// <returns>Nested types.</returns>
    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol typeSymbol)
    {
        foreach (var nestedType in typeSymbol.GetTypeMembers())
        {
            yield return nestedType;

            // Recursively process nested types within this nested type
            foreach (var nestedNestedType in GetNestedTypes(nestedType))
            {
                yield return nestedNestedType;
            }
        }
    }

    /// <summary>
    /// Determines if a type is derived from another type.
    /// </summary>
    /// <param name="typeSymbol">Type symbol.</param>
    /// <param name="baseType">Base type.</param>
    /// <returns>True if derived.</returns>
    private static bool IsDerivedFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseType)
    {
        var current = typeSymbol.BaseType;

        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }

            current = current.BaseType;
        }

        // Check for interfaces
        foreach (var @interface in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(@interface, baseType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a type is abstract.
    /// </summary>
    /// <param name="typeSymbol">Type symbol.</param>
    /// <returns>True if abstract.</returns>
    private static bool IsAbstract(INamedTypeSymbol typeSymbol) => typeSymbol.IsAbstract;

    /// <summary>
    /// Finds all types in the compilation and referenced assemblies that have the specified attribute.
    /// </summary>
    /// <param name="attributeType">The attribute type to search for.</param>
    /// <param name="compilation">The compilation context.</param>
    /// <returns>A collection of named type symbols that have the specified attribute.</returns>
    public IEnumerable<INamedTypeSymbol> FindTypesWithAttribute(INamedTypeSymbol attributeType, Compilation compilation)
    {
        return attributeType == null
            ? throw new ArgumentNullException(nameof(attributeType))
            : compilation == null
            ? throw new ArgumentNullException(nameof(compilation))
            : (IEnumerable<INamedTypeSymbol>)_attributedTypesCache.GetOrAdd((attributeType, compilation), tuple =>
        {
            var (type, comp) = tuple;
            var results = new List<INamedTypeSymbol>();

            // Always find types in the current assembly
            var currentAssemblyTypes = FindTypesWithAttributeInCurrentAssembly(type, comp);
            results.AddRange(currentAssemblyTypes);

            // Only include referenced assemblies if cross-assembly discovery is enabled
            if (IsCrossAssemblyDiscoveryEnabled(comp))
            {
                var referencedAssemblyTypes = FindTypesWithAttributeInReferencedAssemblies(type, comp);
                results.AddRange(referencedAssemblyTypes);
            }

            return [.. results];
        });
    }

    /// <summary>
    /// Finds types with attribute in the current assembly.
    /// </summary>
    /// <param name="attributeType">Attribute type.</param>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Types with attribute.</returns>
    private static List<INamedTypeSymbol> FindTypesWithAttributeInCurrentAssembly(INamedTypeSymbol attributeType, Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();
        var attributeFullName = attributeType.GetFullMetadataName();

        // Get all named types in the compilation
        var types = GetAllNamedTypesInCompilation(compilation);

        foreach (var type in types)
        {
            if (HasAttribute(type, attributeFullName))
            {
                results.Add(type);
            }
        }

        return results;
    }

    /// <summary>
    /// Finds types with attribute in referenced assemblies.
    /// </summary>
    /// <param name="attributeType">Attribute type.</param>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Types with attribute.</returns>
    private static List<INamedTypeSymbol> FindTypesWithAttributeInReferencedAssemblies(INamedTypeSymbol attributeType, Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();
        var attributeFullName = attributeType.GetFullMetadataName();

        // Process each referenced assembly
        foreach (var reference in compilation.References)
        {
            // Get the assembly symbol for the reference
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            // Get the global namespace of the referenced assembly
            var globalNamespace = assemblySymbol.GlobalNamespace;

            // Find all named types in the referenced assembly
            var types = GetAllTypesInNamespace(globalNamespace);

            foreach (var type in types)
            {
                if (HasAttribute(type, attributeFullName))
                {
                    results.Add(type);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Finds all types in the compilation and referenced assemblies that have an attribute with the specified name.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to search for (e.g., "EnumCollection" for [EnumCollection]).</param>
    /// <param name="compilation">The compilation context.</param>
    /// <returns>A collection of named type symbols that have the specified attribute.</returns>
    public IEnumerable<INamedTypeSymbol> FindTypesWithAttributeName(string attributeName, Compilation compilation)
    {
        if (string.IsNullOrEmpty(attributeName))
        {
            throw new ArgumentNullException(nameof(attributeName));
        }

        if (compilation == null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        // Add "Attribute" suffix if not present
        var fullAttributeName = attributeName.EndsWith($"Attribute", StringComparison.OrdinalIgnoreCase)
            ? attributeName
            : attributeName + $"Attribute";

        return _attributedTypesByNameCache.GetOrAdd((fullAttributeName, compilation), tuple =>
        {
            var (attrName, comp) = tuple;
            var results = new List<INamedTypeSymbol>();

            // Always find types in the current assembly
            var currentAssemblyTypes = FindTypesWithAttributeNameInCurrentAssembly(attrName, comp);
            results.AddRange(currentAssemblyTypes);

            // Only include referenced assemblies if cross-assembly discovery is enabled
            if (IsCrossAssemblyDiscoveryEnabled(comp))
            {
                var referencedAssemblyTypes = FindTypesWithAttributeNameInReferencedAssemblies(attrName, comp);
                results.AddRange(referencedAssemblyTypes);
            }

            return [.. results];
        });
    }

    /// <summary>
    /// Finds types with attribute name in the current assembly.
    /// </summary>
    /// <param name="attributeName">Attribute name.</param>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Types with attribute name.</returns>
    private static List<INamedTypeSymbol> FindTypesWithAttributeNameInCurrentAssembly(string attributeName, Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();

        // Get all named types in the compilation
        var types = GetAllNamedTypesInCompilation(compilation);

        foreach (var type in types)
        {
            if (HasAttributeWithName(type, attributeName))
            {
                results.Add(type);
            }
        }

        return results;
    }

    /// <summary>
    /// Finds types with attribute name in referenced assemblies.
    /// </summary>
    /// <param name="attributeName">Attribute name.</param>
    /// <param name="compilation">Compilation context.</param>
    /// <returns>Types with attribute name.</returns>
    private static List<INamedTypeSymbol> FindTypesWithAttributeNameInReferencedAssemblies(string attributeName, Compilation compilation)
    {
        var results = new List<INamedTypeSymbol>();

        // Process each referenced assembly
        foreach (var reference in compilation.References)
        {
            // Get the assembly symbol for the reference
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            // Get the global namespace of the referenced assembly
            var globalNamespace = assemblySymbol.GlobalNamespace;

            // Find all named types in the referenced assembly
            var types = GetAllTypesInNamespace(globalNamespace);

            foreach (var type in types)
            {
                if (HasAttributeWithName(type, attributeName))
                {
                    results.Add(type);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Determines if a type has an attribute.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="attributeFullName">Attribute full name.</param>
    /// <returns>True if attribute is present.</returns>
    private static bool HasAttribute(ITypeSymbol type, string attributeFullName)
    {
        if (string.IsNullOrEmpty(attributeFullName))
        {
            return false;
        }

        var attributes = type?.GetAttributes() ?? [];
        return attributes.Any(a =>
        {
            var attrClass = a.AttributeClass;
            return attrClass != null && string.Equals(attrClass.GetFullMetadataName(), attributeFullName, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// Determines if a type has an attribute with the specified name.
    /// </summary>
    /// <param name="typeSymbol">Type symbol.</param>
    /// <param name="attributeName">Attribute name.</param>
    /// <returns>True if attribute is present.</returns>
    private static bool HasAttributeWithName(INamedTypeSymbol typeSymbol, string attributeName)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass != null)
            {
                var name = attributeClass.Name;
                if (string.Equals(name, attributeName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, attributeName + $"Attribute", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the MSBuild property.
    /// </summary>
    /// <param name="compilation">Compilation context.</param>
    /// <param name="propertyName">Property name.</param>
    /// <returns>Property value.</returns>
    private static string? GetMsBuildProperty(Compilation compilation, string propertyName)
    {
        try
        {
            // Try to get the property from the compilation options
            if (compilation.Options is CSharpCompilationOptions csharpOptions)
            {
                // Access MSBuild properties through PreprocessorSymbols or SyntaxTrees metadata
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var options = tree.Options as CSharpParseOptions;
                    if (options is CSharpParseOptions opts)
                    {
                        foreach (var symbol in opts.PreprocessorSymbolNames)
                        {
                            if (symbol.StartsWith(propertyName + $"=", StringComparison.OrdinalIgnoreCase))
                            {
                                return symbol.Substring(propertyName.Length + 1);
                            }
                        }
                    }
                }
            }

            // Try alternate method through assembly attribute
            var attributes = compilation.Assembly.GetAttributes();
            foreach (var attribute in attributes)
            {
                if (string.Equals(attribute.AttributeClass?.Name, $"AssemblyMetadataAttribute", StringComparison.Ordinal))
                {
                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        var key = attribute.ConstructorArguments[0].Value as string;
                        if (string.Equals(key, propertyName, StringComparison.Ordinal))
                        {
                            return attribute.ConstructorArguments[1].Value as string;
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            // If any exception occurs during property retrieval, assume the property is not set
            return null;
        }
    }
}

#pragma warning disable SA1202
/// <summary>
/// Symbol extension methods for metadata and helper operations.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Gets the full metadata name of the symbol.
    /// </summary>
    /// <param name="symbol">Symbol.</param>
    /// <returns>Full metadata name.</returns>
    public static string GetFullMetadataName(this ISymbol symbol)
    {
        if (symbol == null)
        {
            return string.Empty;
        }

        var containingNamespace = symbol.ContainingNamespace;
        var namespaceName = containingNamespace == null || containingNamespace.IsGlobalNamespace
            ? string.Empty
            : containingNamespace.GetFullMetadataName();

        return string.IsNullOrEmpty(namespaceName)
            ? symbol.MetadataName
            : $"{namespaceName}.{symbol.MetadataName}";
    }
}
#pragma warning restore SA1202

/// <summary>
/// Equality comparer for INamedTypeSymbol to be used in dictionary keys.
/// </summary>
internal sealed class TypeSymbolEqualityComparer : IEqualityComparer<(INamedTypeSymbol, Compilation)>
{
    /// <summary>
    /// Determines if two values are equal.
    /// </summary>
    /// <param name="x">First value.</param>
    /// <param name="y">Second value.</param>
    /// <returns>True if equal.</returns>
    public bool Equals((INamedTypeSymbol, Compilation) x, (INamedTypeSymbol, Compilation) y)
    {
        return SymbolEqualityComparer.Default.Equals(x.Item1, y.Item1) &&
               x.Item2.Equals(y.Item2);
    }

    /// <summary>
    /// Gets the hash code of the value.
    /// </summary>
    /// <param name="obj">Value.</param>
    /// <returns>Hash code.</returns>
    public int GetHashCode((INamedTypeSymbol, Compilation) obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj.Item1) ^
               obj.Item2.GetHashCode();
    }
}
#pragma warning restore SA1202

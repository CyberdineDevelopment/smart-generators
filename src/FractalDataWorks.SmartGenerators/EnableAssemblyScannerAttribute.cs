using System;

namespace FractalDataWorks.SmartGenerators;

/// <summary>
/// Assembly‑level attribute that enables the generic assembly‑scanner at build time.
/// Apply this attribute to an assembly to allow <see cref="IAssemblyScanner"/> to
/// traverse all referenced assemblies and expose their <c>INamedTypeSymbol</c>s
/// to other generators in the same DLL.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class EnableAssemblyScannerAttribute : Attribute
{
}

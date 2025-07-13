# FractalDataWorks Smart Generators Developer's Guide

## Table of Contents
- [Getting Started](#getting-started)
- [Creating Your First Generator](#creating-your-first-generator)
- [Using the Code Builder API](#using-the-code-builder-api)
- [Best Practices](#best-practices)
- [Advanced Features](#advanced-features)

## Getting Started

FractalDataWorks Smart Generators provides a comprehensive toolkit for building Roslyn source generators with three core packages:

- **FractalDataWorks.SmartGenerators** - Base classes and utilities for incremental generators
- **FractalDataWorks.SmartGenerators.CodeBuilders** - Fluent API for code generation
- **FractalDataWorks.SmartGenerators.TestUtilities** - Testing framework with expectations API

### Installation

```xml
<PackageReference Include="FractalDataWorks.SmartGenerators" Version="*" />
<PackageReference Include="FractalDataWorks.SmartGenerators.CodeBuilders" Version="*" />
```

## Creating Your First Generator

### Basic Incremental Generator

```csharp
using FractalDataWorks.SmartGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class MyGenerator : IncrementalGeneratorBase<MyInputInfo>
{
    protected override bool IsRelevantSyntax(SyntaxNode syntaxNode)
    {
        return syntaxNode is ClassDeclarationSyntax classDecl &&
               classDecl.AttributeLists.Count > 0;
    }

    protected override MyInputInfo? TransformSyntax(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        
        if (symbol?.GetAttributes().Any(a => a.AttributeClass?.Name == "GenerateAttribute") == true)
        {
            return new MyInputInfo
            {
                ClassName = symbol.Name,
                Namespace = symbol.ContainingNamespace.ToDisplayString()
            };
        }
        
        return null;
    }

    protected override void RegisterSourceOutput(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<MyInputInfo?> syntaxProvider)
    {
        context.RegisterSourceOutput(syntaxProvider.Where(x => x != null), GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context, MyInputInfo? input)
    {
        if (input == null) return;
        
        var code = GenerateClassCode(input);
        context.AddSource($"{input.ClassName}.g.cs", code);
    }
}

public class MyInputInfo : IInputInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    
    public string InputHash => InputTracker.CalculateInputHash(this);
}
```

## Using the Code Builder API

### Building a Class

```csharp
using FractalDataWorks.SmartGenerators.CodeBuilders;

var classBuilder = new ClassBuilder("PersonDto")
    .MakePublic()
    .MakePartial()
    .WithSummary("Data transfer object for Person entity")
    .AddProperty("Id", "int", property => property
        .WithSummary("Gets or sets the unique identifier")
        .WithGetter()
        .WithSetter())
    .AddProperty("Name", "string", property => property
        .WithSummary("Gets or sets the person's name")
        .WithGetter()
        .WithInitSetter())
    .AddMethod("ToString", "string", method => method
        .MakePublic()
        .WithOverride()
        .WithBody("return $\"Person {{ Id = {Id}, Name = {Name} }}\";"));

var namespaceBuilder = new NamespaceBuilder("MyApp.Models")
    .AddUsing("System")
    .AddMember(classBuilder);

var code = namespaceBuilder.Build();
```

### Building an Interface

```csharp
var interfaceBuilder = new InterfaceBuilder("IRepository")
    .MakePublic()
    .AddGenericParameter("T", constraints => constraints
        .AddConstraint("class")
        .AddConstraint("new()"))
    .AddMethod("GetById", "T", method => method
        .AddParameter("id", "int"))
    .AddMethod("Save", "void", method => method
        .AddParameter("entity", "T"))
    .AddProperty("Count", "int", property => property
        .WithGetter());
```

### Building an Enum

```csharp
var enumBuilder = new EnumBuilder("Status")
    .MakePublic()
    .WithSummary("Represents the status of an operation")
    .AddValue("None", 0, "No status")
    .AddValue("Pending", 1, "Operation is pending")
    .AddValue("Completed", 2, "Operation completed successfully")
    .AddValue("Failed", 3, "Operation failed");
```

## Best Practices

### 1. Use Incremental Generators

Always inherit from `IncrementalGeneratorBase<T>` for better performance:

```csharp
public class MyGenerator : IncrementalGeneratorBase<MyInputInfo>
{
    // Implementation
}
```

### 2. Implement IInputInfo for Change Detection

```csharp
public class MyInputInfo : IInputInfo
{
    public string TypeName { get; set; }
    public List<PropertyInfo> Properties { get; set; }
    
    public string InputHash => InputTracker.CalculateInputHash(this);
}
```

### 3. Use Code Builders for Clean Generation

Instead of string concatenation, use the fluent API:

```csharp
// Good
var code = new ClassBuilder("MyClass")
    .MakePublic()
    .AddProperty("Name", "string")
    .Build();

// Avoid
var code = "public class MyClass { public string Name { get; set; } }";
```

### 4. Generate Attributes First

When your generator needs attributes, register them early:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // Register attribute source first
    context.RegisterPostInitializationOutput(ctx =>
    {
        ctx.AddSource("Attributes.g.cs", AttributeSource);
    });
    
    // Then set up the main generator
    var provider = context.SyntaxProvider...
}
```

## Advanced Features

### Cross-Assembly Type Discovery

Enable assembly scanning in projects that need it:

```csharp
[assembly: FractalDataWorks.SmartGenerators.EnableAssemblyScanner]
```

Use in your generator:

```csharp
protected override MyInputInfo? TransformSyntax(GeneratorSyntaxContext context)
{
    var compilation = context.SemanticModel.Compilation;
    var scanner = AssemblyScannerService.Get(compilation);
    
    if (scanner != null)
    {
        var allTypes = scanner.GetAllNamedTypes();
        // Process types across assemblies
    }
    
    return null;
}
```

### XML Documentation Generation

```csharp
var method = new MethodBuilder("Calculate", "double")
    .WithSummary("Calculates the result based on input parameters")
    .WithParameter("value", "double", param => param
        .WithDescription("The input value"))
    .WithParameter("factor", "double", param => param
        .WithDescription("The multiplication factor"))
    .WithReturns("The calculated result")
    .WithException("ArgumentException", "Thrown when factor is zero");
```

### Custom Code Blocks

```csharp
var customCode = new CodeBlockBuilder()
    .AddLine("#if DEBUG")
    .AddLine("Console.WriteLine(\"Debug mode\");")
    .AddLine("#endif")
    .Build();

classBuilder.AddMember(customCode);
```

## Naming Conventions

- Generators: `[Feature]Generator` (e.g., `DtoGenerator`, `BuilderGenerator`)
- Input models: `[Feature]Info` implementing `IInputInfo`
- Generated files: `[OriginalName].[Feature].g.cs`

## Debugging Generators

1. Add `Debugger.Launch()` in your generator
2. Use diagnostic logging:

```csharp
protected override void RegisterSourceOutput(
    IncrementalGeneratorInitializationContext context,
    IncrementalValuesProvider<MyInputInfo?> syntaxProvider)
{
    context.RegisterSourceOutput(syntaxProvider, (spc, input) =>
    {
        // Add diagnostics
        spc.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "GEN001",
                "Generator Info",
                $"Processing {input?.ClassName}",
                "Generator",
                DiagnosticSeverity.Info,
                true),
            Location.None));
            
        // Generate code
        GenerateCode(spc, input);
    });
}
```

## License

Licensed under the Apache License 2.0.
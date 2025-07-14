# FractalDataWorks Smart Generators

Part of the FractalDataWorks toolkit.

## Build Status

[![Master Build](https://github.com/CyberdineDevelopment/smart-generators/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/CyberdineDevelopment/smart-generators/actions/workflows/ci.yml)
[![Develop Build](https://github.com/CyberdineDevelopment/smart-generators/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/CyberdineDevelopment/smart-generators/actions/workflows/ci.yml)

## Release Status

![GitHub release (latest by date)](https://img.shields.io/github/v/release/CyberdineDevelopment/smart-generators)
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/CyberdineDevelopment/smart-generators?include_prereleases&label=pre-release)

## Package Status

![Nuget](https://img.shields.io/nuget/v/FractalDataWorks.SmartGenerators)
![GitHub Packages](https://img.shields.io/badge/github%20packages-available-blue)

## License

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

A comprehensive toolkit for building, testing, and deploying Roslyn source generators with a fluent code generation API.

## Overview

FractalDataWorks Smart Generators provides three core packages:

- **FractalDataWorks.SmartGenerators**: Base classes and utilities for building incremental source generators
- **FractalDataWorks.SmartGenerators.CodeBuilders**: Fluent API for generating C# code programmatically
- **FractalDataWorks.SmartGenerators.TestUtilities**: Testing framework for source generators with expectations API

## Installation

```bash
dotnet add package FractalDataWorks.SmartGenerators
dotnet add package FractalDataWorks.SmartGenerators.CodeBuilders
dotnet add package FractalDataWorks.SmartGenerators.TestUtilities
```

## Key Features

### Source Generator Base Classes
- `IncrementalGeneratorBase<T>` - Simplified base class for incremental generators
- Built-in assembly scanning for cross-project type discovery
- Attribute source registration helpers
- Diagnostic reporting utilities

### Code Builders
- Fluent API for building C# code structures
- Builders for classes, interfaces, records, enums, methods, properties, and more
- Automatic formatting and proper indentation
- XML documentation support

### Testing Framework
- `SourceGeneratorTestHelper` for running generators in tests
- Expectations API for asserting generated code structure
- Compilation verification
- Multi-generator test support

## Quick Start

### Creating an Incremental Generator

```csharp
using FractalDataWorks.SmartGenerators;
using Microsoft.CodeAnalysis;

[Generator]
public class MyGenerator : IncrementalGeneratorBase<MyInputInfo>
{
    protected override bool IsRelevantSyntax(SyntaxNode syntaxNode)
    {
        // Filter to classes with specific attributes
        return syntaxNode is ClassDeclarationSyntax;
    }

    protected override MyInputInfo? TransformSyntax(GeneratorSyntaxContext context)
    {
        // Transform syntax into your input model
        return new MyInputInfo { /* ... */ };
    }

    protected override void RegisterSourceOutput(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<MyInputInfo?> syntaxProvider)
    {
        context.RegisterSourceOutput(syntaxProvider, (spc, input) =>
        {
            if (input is null) return;
            
            // Generate code using input
            var code = GenerateCode(input);
            spc.AddSource($"{input.Name}.g.cs", code);
        });
    }
}
```

### Using Code Builders

```csharp
using FractalDataWorks.SmartGenerators.CodeBuilders;

var classBuilder = new ClassBuilder("PersonDto")
    .MakePublic()
    .MakePartial()
    .WithSummary("Data transfer object for Person entity")
    .AddProperty("Id", "int", property => property
        .WithXmlDocSummary("Gets or sets the unique identifier"))
    .AddProperty("Name", "string", property => property
        .WithXmlDocSummary("Gets or sets the person's name")
        .WithInitSetter())
    .AddMethod("ToString", "string", method => method
        .MakePublic()
        .MakeOverride()
        .WithBody("return $\"Person {{ Id = {Id}, Name = {Name} }}\";"));

var code = new NamespaceBuilder("MyApp.Models")
    .AddMember(classBuilder)
    .Build();
```

### Testing Your Generator

```csharp
using FractalDataWorks.SmartGenerators.TestUtilities;
using Xunit;
using Shouldly;

public class MyGeneratorTests
{
    [Fact]
    public void GeneratesExpectedCode()
    {
        // Arrange
        var source = @"
            [GenerateDto]
            public class Person
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }";

        // Act
        var generator = new MyGenerator();
        var output = SourceGeneratorTestHelper.RunGenerator(
            generator,
            new[] { source },
            out var diagnostics);

        // Assert
        diagnostics.ShouldBeEmpty();
        output.Count.ShouldBe(1);
        
        // Use expectations API for structural assertions
        ExpectationsFactory.ExpectCode(output.Values.First())
            .HasNamespace("MyApp.Models")
            .HasClass("PersonDto", c => c
                .HasModifier("public")
                .HasModifier("partial")
                .HasProperty("Id", p => p.HasType("int"))
                .HasProperty("Name", p => p.HasType("string"))
                .HasMethod("ToString", m => m.HasReturnType("string")))
            .Assert();
    }
}
```

### Assembly Scanner Usage

Enable assembly scanning in your project:

```csharp
[assembly: FractalDataWorks.SmartGenerators.EnableAssemblyScanner]
```

Then use it in your generator:

```csharp
var scanner = AssemblyScannerService.Get(compilation);
if (scanner != null)
{
    var allTypes = scanner.AllNamedTypes;
    // Process types across the compilation
}
```

## Documentation

- [Developer's Guide](docs/DevelopersGuide.md)
- [Code Builder Guide](docs/CodeBuilderGuide.md)
- [Testing Guide](docs/TestingGuide.md)
- [Assembly Scanner Guide](docs/AssemblyScannerGuide.md)

## Requirements

- .NET Standard 2.0 (for generators)
- .NET SDK 8.0 or later (for development)
- C# 12.0 or later

## License

This project is licensed under the Apache License 2.0.
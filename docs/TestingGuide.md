# FractalDataWorks Smart Generators Testing Guide

## Table of Contents
- [Overview](#overview)
- [Test Project Setup](#test-project-setup)
- [Testing Patterns](#testing-patterns)
- [Expectations API](#expectations-api)
- [Advanced Testing](#advanced-testing)
- [Best Practices](#best-practices)

## Overview

The FractalDataWorks.SmartGenerators.TestUtilities package provides comprehensive testing utilities for source generators, including:

- `SourceGeneratorTestHelper` - Run generators and analyze output
- `ExpectationsFactory` - Fluent API for asserting code structure
- `CompilationVerifier` - Verify compilation success and diagnostics
- `MultiGeneratorTestHelper` - Test multiple generators together

## Test Project Setup

### Project Structure

```
MyGenerator.Tests/
├── MyGenerator.Tests.csproj
├── Generators/
│   └── MyGeneratorTests.cs
├── Snapshots/
│   └── Expected/
│       └── MyClass.g.cs
└── TestHelpers/
    └── TestSources.cs
```

### Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="FractalDataWorks.SmartGenerators.TestUtilities" />
    
    <!-- Reference your generator project -->
    <ProjectReference Include="..\MyGenerator\MyGenerator.csproj" />
  </ItemGroup>
</Project>
```

## Testing Patterns

### Basic Generator Test

```csharp
using FractalDataWorks.SmartGenerators.TestUtilities;
using Shouldly;
using Xunit;

public class MyGeneratorTests
{
    [Fact]
    public void GeneratesExpectedCodeForSimpleClass()
    {
        // Arrange
        var source = @"
            using System;
            
            namespace TestNamespace
            {
                [GenerateBuilder]
                public class Person
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
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
        
        var generatedCode = output.First();
        generatedCode.Key.ShouldBe("Person.Builder.g.cs");
        generatedCode.Value.ShouldContain("public class PersonBuilder");
    }
}
```

### Using Expectations API

```csharp
[Fact]
public void GeneratedBuilderHasCorrectStructure()
{
    // Arrange
    var source = TestSources.PersonClass;
    
    // Act
    var generator = new BuilderGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source },
        out var diagnostics);
    
    // Assert
    ExpectationsFactory.ExpectCode(output.Values.First())
        .HasNamespace("TestNamespace", ns => ns
            .HasClass("PersonBuilder", cls => cls
                .HasModifier("public")
                .HasModifier("sealed")
                .HasField("_name", f => f
                    .HasType("string")
                    .HasModifier("private"))
                .HasField("_age", f => f
                    .HasType("int")
                    .HasModifier("private"))
                .HasMethod("WithName", m => m
                    .HasModifier("public")
                    .HasReturnType("PersonBuilder")
                    .HasParameter("value", p => p.HasType("string")))
                .HasMethod("WithAge", m => m
                    .HasModifier("public")
                    .HasReturnType("PersonBuilder")
                    .HasParameter("value", p => p.HasType("int")))
                .HasMethod("Build", m => m
                    .HasModifier("public")
                    .HasReturnType("Person"))))
        .Assert();
}
```

### Testing with Multiple Sources

```csharp
[Fact]
public void HandlesMultipleClassesInSameCompilation()
{
    // Arrange
    var sources = new[]
    {
        @"[GenerateBuilder] public class Customer { public string Name { get; set; } }",
        @"[GenerateBuilder] public class Order { public int Id { get; set; } }",
        @"[GenerateBuilder] public class Product { public decimal Price { get; set; } }"
    };
    
    // Act
    var generator = new BuilderGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        sources,
        out var diagnostics);
    
    // Assert
    output.Count.ShouldBe(3);
    output.Keys.ShouldContain("Customer.Builder.g.cs");
    output.Keys.ShouldContain("Order.Builder.g.cs");
    output.Keys.ShouldContain("Product.Builder.g.cs");
}
```

### Testing Diagnostics

```csharp
[Fact]
public void ReportsDiagnosticForInvalidClass()
{
    // Arrange
    var source = @"
        [GenerateBuilder]
        public static class InvalidClass
        {
            public string Property { get; set; }
        }";
    
    // Act
    var generator = new BuilderGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source },
        out var diagnostics);
    
    // Assert
    diagnostics.Length.ShouldBe(1);
    var diagnostic = diagnostics[0];
    diagnostic.Id.ShouldBe("BG001");
    diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
    diagnostic.GetMessage().ShouldContain("cannot be static");
}
```

## Expectations API

### Class Expectations

```csharp
ExpectationsFactory.ExpectCode(generatedSource)
    .HasClass("MyClass", cls => cls
        .HasModifier("public")
        .HasModifier("partial")
        .HasBaseClass("BaseEntity")
        .ImplementsInterface("IEntity")
        .ImplementsInterface("IValidatable")
        .HasAttribute("Serializable")
        .HasAttribute("Table", attr => attr
            .HasArgument("\"MyTable\"")
            .HasArgument("Schema", "\"dbo\""))
        .HasConstructor(ctor => ctor
            .HasModifier("public")
            .HasParameter("id", p => p.HasType("int"))
            .HasParameter("name", p => p.HasType("string")))
        .HasProperty("Id", prop => prop
            .HasType("int")
            .HasGetter()
            .HasSetter())
        .HasMethod("Validate", method => method
            .HasModifier("public")
            .HasReturnType("bool")
            .HasBody(body => body.Contains("return true;"))))
    .Assert();
```

### Interface Expectations

```csharp
ExpectationsFactory.ExpectCode(generatedSource)
    .HasInterface("IRepository", iface => iface
        .HasModifier("public")
        .HasGenericParameter("T")
        .HasGenericConstraint("T", "class")
        .HasMethod("GetById", m => m
            .HasReturnType("T")
            .HasParameter("id", p => p.HasType("int")))
        .HasProperty("Count", p => p
            .HasType("int")
            .HasGetter()))
    .Assert();
```

### Enum Expectations

```csharp
ExpectationsFactory.ExpectCode(generatedSource)
    .HasEnum("Status", e => e
        .HasModifier("public")
        .HasBaseType("int")
        .HasAttribute("Flags")
        .HasValue("None", v => v.HasValue("0"))
        .HasValue("Active", v => v.HasValue("1"))
        .HasValue("Inactive", v => v.HasValue("2")))
    .Assert();
```

### Record Expectations

```csharp
ExpectationsFactory.ExpectCode(generatedSource)
    .HasRecord("PersonRecord", rec => rec
        .HasModifier("public")
        .HasPrimaryConstructor(pc => pc
            .HasParameter("FirstName", p => p.HasType("string"))
            .HasParameter("LastName", p => p.HasType("string")))
        .HasMethod("ToString", m => m
            .HasModifier("public")
            .HasModifier("override")))
    .Assert();
```

## Advanced Testing

### Testing Incremental Generators

```csharp
[Fact]
public void OnlyRegeneratesWhenInputChanges()
{
    // Arrange
    var source1 = "[Generate] public class Class1 { }";
    var source2 = "[Generate] public class Class2 { }";
    
    // First run
    var generator = new MyIncrementalGenerator();
    var output1 = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source1 },
        out var diagnostics1);
    output1.Count.ShouldBe(1);
    
    // Add new source
    var output2 = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source1, source2 },
        out var diagnostics2);
    output2.Count.ShouldBe(2);
    
    // Note: Cannot verify incremental behavior with current test helper API
}
```

### Testing Multiple Generators

```csharp
[Fact]
public void MultipleGeneratorsWorkTogether()
{
    // Arrange
    var source = @"
        [GenerateBuilder]
        [GenerateDto]
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }";
    
    var generators = new IIncrementalGenerator[]
    {
        new BuilderGenerator(),
        new DtoGenerator()
    };
    
    // Act
    var result = MultiGeneratorTestHelper.RunGenerators(generators, source);
    
    // Assert
    result.GeneratedSources.Length.ShouldBe(2);
    result.GeneratedSources.ShouldContain(s => s.HintName.EndsWith("Builder.g.cs"));
    result.GeneratedSources.ShouldContain(s => s.HintName.EndsWith("Dto.g.cs"));
}
```

### Compilation Verification

```csharp
[Fact]
public void GeneratedCodeCompiles()
{
    // Arrange
    var source = TestSources.PersonClass;
    
    // Act
    var generator = new BuilderGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source },
        out var diagnostics);
    
    // Assert
    var verifier = new CompilationVerifier();
    var assembly = verifier.CompileAndVerify(output.Values.ToArray());
    
    // Verify we can instantiate the generated type
    var builderType = assembly.GetType("TestNamespace.PersonBuilder");
    builderType.ShouldNotBeNull();
    
    var instance = Activator.CreateInstance(builderType);
    instance.ShouldNotBeNull();
}
```

### Snapshot Testing

```csharp
[Fact]
public void GeneratedCodeMatchesSnapshot()
{
    // Arrange
    var source = TestSources.CustomerClass;
    var expectedPath = Path.Combine("Snapshots", "Expected", "Customer.Builder.g.cs");
    
    // Act
    var generator = new BuilderGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source },
        out var diagnostics);
    
    // Assert
    var generated = output.Values.First();
    var expected = File.ReadAllText(expectedPath);
    
    generated.ShouldBe(expected);
}
```

## Best Practices

### 1. Use Descriptive Test Names

```csharp
[Fact]
public void BuilderGeneratorCreatesWithMethodsForAllPublicProperties() { }

[Fact]
public void BuilderGeneratorSkipsStaticProperties() { }

[Fact]
public void BuilderGeneratorHandlesGenericTypes() { }
```

### 2. Test Edge Cases

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void HandlesInvalidClassNames(string className)
{
    var source = $"[GenerateBuilder] public class {className} {{ }}";
    var generator = new BuilderGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source },
        out var diagnostics);
    diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error);
}
```

### 3. Organize Test Sources

```csharp
public static class TestSources
{
    public const string SimpleClass = @"
        [GenerateBuilder]
        public class Simple
        {
            public string Name { get; set; }
        }";
    
    public const string ComplexClass = @"
        [GenerateBuilder]
        public class Complex<T> where T : class
        {
            public T Value { get; set; }
            public List<T> Items { get; set; }
        }";
    
    public const string InheritedClass = @"
        public class Base
        {
            public int Id { get; set; }
        }
        
        [GenerateBuilder]
        public class Derived : Base
        {
            public string Name { get; set; }
        }";
}
```

### 4. Test Compilation Results

Always verify that generated code compiles:

```csharp
[Fact]
public void AllGeneratedCodeCompiles()
{
    var sources = GetAllTestSources();
    
    foreach (var source in sources)
    {
        var generator = new MyGenerator();
        var output = SourceGeneratorTestHelper.RunGenerator(
            generator,
            new[] { source },
            out var diagnostics);
        
        var verifier = new CompilationVerifier();
        var assembly = verifier.CompileAndVerify(output.Values.ToArray());
    }
}
```

### 5. Use Theory for Parameterized Tests

```csharp
[Theory]
[MemberData(nameof(GetTestCases))]
public void GeneratesCorrectCodeForAllTestCases(string source, string expectedHintName)
{
    var generator = new MyGenerator();
    var output = SourceGeneratorTestHelper.RunGenerator(
        generator,
        new[] { source },
        out var diagnostics);
    
    output.Count.ShouldBe(1);
    output.Keys.First().ShouldBe(expectedHintName);
}

public static IEnumerable<object[]> GetTestCases()
{
    yield return new object[] { TestSources.SimpleClass, "Simple.g.cs" };
    yield return new object[] { TestSources.ComplexClass, "Complex.g.cs" };
}
```

## Common Assertions

### Using Shouldly

```csharp
// Basic assertions
output.ShouldNotBeEmpty();
diagnostics.ShouldBeEmpty();
generatedCode.ShouldContain("public class");
generatedCode.ShouldNotContain("error");

// Collection assertions
output.Values.ShouldAllBe(s => s.Length > 0);
diagnostics.ShouldContain(d => d.Id == "GEN001");
```

### Custom Assertions

```csharp
public static class GeneratorAssertions
{
    public static void ShouldGenerateValidBuilder(this Dictionary<string, string> output, string className)
    {
        output.Count.ShouldBe(1);
        
        var source = output.First();
        source.Key.ShouldBe($"{className}.Builder.g.cs");
        
        ExpectationsFactory.ExpectCode(source.Value)
            .HasClass($"{className}Builder", cls => cls
                .HasMethod("Build", m => m.HasReturnType(className)))
            .Assert();
    }
}
```

## License

Licensed under the Apache License 2.0.
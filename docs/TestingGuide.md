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
        var result = SourceGeneratorTestHelper.RunGenerator<MyGenerator>(source);
        
        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedSources.Length.ShouldBe(1);
        
        var generatedCode = result.GeneratedSources[0];
        generatedCode.HintName.ShouldBe("Person.Builder.g.cs");
        generatedCode.SourceText.ShouldContain("public class PersonBuilder");
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
    var result = SourceGeneratorTestHelper.RunGenerator<BuilderGenerator>(source);
    
    // Assert
    ExpectationsFactory.ExpectCode(result.GeneratedSources[0])
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
    var result = SourceGeneratorTestHelper.RunGenerator<BuilderGenerator>(sources);
    
    // Assert
    result.GeneratedSources.Length.ShouldBe(3);
    result.GeneratedSources.ShouldContain(s => s.HintName == "Customer.Builder.g.cs");
    result.GeneratedSources.ShouldContain(s => s.HintName == "Order.Builder.g.cs");
    result.GeneratedSources.ShouldContain(s => s.HintName == "Product.Builder.g.cs");
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
    var result = SourceGeneratorTestHelper.RunGenerator<BuilderGenerator>(source);
    
    // Assert
    result.Diagnostics.Length.ShouldBe(1);
    var diagnostic = result.Diagnostics[0];
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
    var result1 = SourceGeneratorTestHelper.RunGenerator<MyIncrementalGenerator>(source1);
    result1.GeneratedSources.Length.ShouldBe(1);
    
    // Add new source
    var result2 = SourceGeneratorTestHelper.RunGenerator<MyIncrementalGenerator>(
        new[] { source1, source2 });
    result2.GeneratedSources.Length.ShouldBe(2);
    
    // Verify first source wasn't regenerated (same content)
    result2.GeneratedSources[0].SourceText.ShouldBe(result1.GeneratedSources[0].SourceText);
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
    
    var generators = new ISourceGenerator[]
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
    var result = SourceGeneratorTestHelper.RunGenerator<BuilderGenerator>(source);
    var compilation = CompilationVerifier.CreateCompilation(
        result.GeneratedSources.Select(s => s.SourceText.ToString()).ToArray());
    
    // Assert
    var verifier = new CompilationVerifier(compilation);
    verifier.ShouldCompileWithoutErrors();
    verifier.ShouldNotHaveWarnings();
    
    // Verify we can instantiate the generated type
    var assembly = verifier.CompileToAssembly();
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
    var result = SourceGeneratorTestHelper.RunGenerator<BuilderGenerator>(source);
    
    // Assert
    var generated = result.GeneratedSources[0].SourceText.ToString();
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
    var result = SourceGeneratorTestHelper.RunGenerator<BuilderGenerator>(source);
    result.Diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error);
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
        var result = SourceGeneratorTestHelper.RunGenerator<MyGenerator>(source);
        var verifier = new CompilationVerifier(CreateCompilation(result));
        
        verifier.ShouldCompileWithoutErrors();
    }
}
```

### 5. Use Theory for Parameterized Tests

```csharp
[Theory]
[MemberData(nameof(GetTestCases))]
public void GeneratesCorrectCodeForAllTestCases(string source, string expectedHintName)
{
    var result = SourceGeneratorTestHelper.RunGenerator<MyGenerator>(source);
    
    result.GeneratedSources.Length.ShouldBe(1);
    result.GeneratedSources[0].HintName.ShouldBe(expectedHintName);
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
result.GeneratedSources.ShouldNotBeEmpty();
result.Diagnostics.ShouldBeEmpty();
generatedCode.ShouldContain("public class");
generatedCode.ShouldNotContain("error");

// Collection assertions
result.GeneratedSources.ShouldAllBe(s => s.SourceText.Length > 0);
result.Diagnostics.ShouldContain(d => d.Id == "GEN001");
```

### Custom Assertions

```csharp
public static class GeneratorAssertions
{
    public static void ShouldGenerateValidBuilder(this GeneratorRunResult result, string className)
    {
        result.GeneratedSources.Length.ShouldBe(1);
        
        var source = result.GeneratedSources[0];
        source.HintName.ShouldBe($"{className}.Builder.g.cs");
        
        ExpectationsFactory.ExpectCode(source)
            .HasClass($"{className}Builder", cls => cls
                .HasMethod("Build", m => m.HasReturnType(className)))
            .Assert();
    }
}
```

## License

Licensed under the Apache License 2.0.
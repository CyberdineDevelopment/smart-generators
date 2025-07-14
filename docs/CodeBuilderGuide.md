# FractalDataWorks Code Builder API Guide

## Table of Contents
- [Overview](#overview)
- [Core Builders](#core-builders)
- [Common Patterns](#common-patterns)
- [API Reference](#api-reference)
- [Advanced Usage](#advanced-usage)

## Overview

The FractalDataWorks.SmartGenerators.CodeBuilders package provides a fluent API for generating C# code programmatically. It handles formatting, indentation, and syntax correctness automatically.

## Core Builders

### NamespaceBuilder

The root container for generated code:

```csharp
var ns = new NamespaceBuilder("MyApp.Models")
    .AddUsing("System")
    .AddUsing("System.Collections.Generic")
    .AddMember(classBuilder)
    .Build();
```

### ClassBuilder

```csharp
var classBuilder = new ClassBuilder("Customer")
    .MakePublic()
    .MakePartial()
    .MakeSealed()
    .AddBase("BaseEntity")
    .AddInterface("ICustomer")
    .AddGenericParameter("T", constraints => constraints
        .AddConstraint("class")
        .AddConstraint("new()"))
    .WithSummary("Represents a customer in the system")
    .AddAttribute("Serializable")
    .AddAttribute("Table", attr => attr
        .AddArgument("\"Customers\"")
        .AddArgument("Schema", "\"dbo\""));
```

### PropertyBuilder

```csharp
classBuilder.AddProperty("Id", "int", property => property
    .MakePublic()
    .WithXmlDocSummary("Gets or sets the customer ID")
    .AddAttribute("Key")
    .AddAttribute("DatabaseGenerated", attr => attr
        .AddArgument("DatabaseGeneratedOption.Identity")));

// Auto-implemented property with init setter
classBuilder.AddProperty("CreatedDate", "DateTime", property => property
    .WithInitSetter()
    .WithInitializer("DateTime.UtcNow"));

// Property with custom getter/setter
classBuilder.AddField("string", "_name")
    .MakePrivate();
classBuilder.AddProperty("Name", "string", property => property
    .WithGetter("return _name;")
    .WithSetter("_name = value?.Trim() ?? string.Empty;"));
```

### MethodBuilder

```csharp
classBuilder.AddMethod("CalculateDiscount", "decimal", method => method
    .MakePublic()
    .MakeVirtual()
    .WithXmlDocSummary("Calculates the discount for the customer")
    .AddParameter("decimal", "orderTotal")
    .AddParameter("string?", "promoCode", "null")
    .WithXmlDocParam("orderTotal", "The total order amount")
    .WithXmlDocParam("promoCode", "Optional promotional code")
    .WithXmlDocReturns("The calculated discount amount")
    .WithBody(@"
        if (promoCode == ""SAVE10"")
            return orderTotal * 0.1m;
        
        return orderTotal > 100 ? orderTotal * 0.05m : 0;
    "));

// Expression-bodied method
classBuilder.AddMethod("GetFullName", "string", method => method
    .MakePublic()
    .WithExpressionBody("$\"{FirstName} {LastName}\""));
```

### InterfaceBuilder

```csharp
var interfaceBuilder = new InterfaceBuilder("IRepository")
    .MakePublic()
    .AddGenericParameter("TEntity", constraints => constraints
        .AddConstraint("class"))
    .AddGenericParameter("TKey")
    .WithSummary("Defines repository operations")
    .AddMethod("GetById", "Task<TEntity?>", method => method
        .AddParameter("id", "TKey"))
    .AddMethod("Add", "Task", method => method
        .AddParameter("entity", "TEntity"))
    .AddProperty("Count", "int");
```

### RecordBuilder

```csharp
var recordBuilder = new RecordBuilder("PersonRecord")
    .MakePublic()
    .AddPrimaryConstructorParameter("FirstName", "string")
    .AddPrimaryConstructorParameter("LastName", "string")
    .AddPrimaryConstructorParameter("Age", "int", param => param
        .WithDefaultValue("0"))
    .WithSummary("Immutable person record")
    .AddMethod("GetFullName", "string", method => method
        .WithExpressionBody("$\"{FirstName} {LastName}\""));
```

### EnumBuilder

```csharp
var enumBuilder = new EnumBuilder("OrderStatus")
    .MakePublic()
    .WithBaseType("byte")
    .WithSummary("Represents the status of an order")
    .AddAttribute("Flags")
    .AddValue("None", "0", "No status")
    .AddValue("Pending", "1", "Order is pending")
    .AddValue("Processing", "2", "Order is being processed")
    .AddValue("Shipped", "4", "Order has been shipped")
    .AddValue("Delivered", "8", "Order has been delivered")
    .AddValue("Cancelled", "16", "Order was cancelled");
```

## Common Patterns

### Builder Pattern Generation

```csharp
public string GenerateBuilder(string className, List<PropertyInfo> properties)
{
    var builderClass = new ClassBuilder($"{className}Builder")
        .MakePublic()
        .MakeSealed();
    
    // Add private fields
    foreach (var prop in properties)
    {
        builderClass.AddField(prop.Type, $"_{prop.Name.ToLower()}")
            .MakePrivate();
    }
    
    // Add builder methods
    foreach (var prop in properties)
    {
        builderClass.AddMethod($"With{prop.Name}", $"{className}Builder", method => method
            .MakePublic()
            .AddParameter("value", prop.Type)
            .WithBody($"_{prop.Name.ToLower()} = value;\nreturn this;"));
    }
    
    // Add Build method
    builderClass.AddMethod("Build", className, method => method
        .MakePublic()
        .WithBody($"return new {className}\n{{\n" +
                  string.Join(",\n", properties.Select(p => 
                      $"    {p.Name} = _{p.Name.ToLower()}")) +
                  "\n};"));
    
    return new NamespaceBuilder("Generated")
        .AddMember(builderClass)
        .Build();
}
```

### DTO Generation

```csharp
public string GenerateDto(INamedTypeSymbol entityType)
{
    var dtoClass = new ClassBuilder($"{entityType.Name}Dto")
        .MakePublic()
        .MakePartial()
        .WithSummary($"Data transfer object for {entityType.Name}");
    
    // Copy properties
    foreach (var property in entityType.GetMembers().OfType<IPropertySymbol>())
    {
        if (property.DeclaredAccessibility == Accessibility.Public)
        {
            dtoClass.AddProperty(property.Name, property.Type.ToDisplayString(), 
                prop => prop
                    .WithXmlDocSummary($"Gets or sets {property.Name}"));
        }
    }
    
    // Add mapping methods
    dtoClass.AddMethod("FromEntity", $"{entityType.Name}Dto", method => method
        .MakePublic()
        .MakeStatic()
        .AddParameter("entity", entityType.Name)
        .WithBody($"return new {entityType.Name}Dto\n{{\n" +
                  string.Join(",\n", entityType.GetMembers()
                      .OfType<IPropertySymbol>()
                      .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                      .Select(p => $"    {p.Name} = entity.{p.Name}")) +
                  "\n};"));
    
    return new NamespaceBuilder(entityType.ContainingNamespace.ToDisplayString())
        .AddMember(dtoClass)
        .Build();
}
```

## API Reference

### Common Enums

```csharp
public enum AccessModifier
{
    Private,
    Protected,
    Internal,
    Public,
    ProtectedInternal,
    PrivateProtected
}

public static class Modifiers
{
    public const string Public = "public";
    public const string Private = "private";
    public const string Protected = "protected";
    public const string Internal = "internal";
    public const string Static = "static";
    public const string Abstract = "abstract";
    public const string Virtual = "virtual";
    public const string Override = "override";
    public const string Sealed = "sealed";
    public const string Partial = "partial";
    public const string Readonly = "readonly";
    public const string Const = "const";
    public const string Async = "async";
    public const string Unsafe = "unsafe";
    public const string Extern = "extern";
    public const string New = "new";
}
```

### Extension Methods

```csharp
// MethodBuilderExtensions
methodBuilder
    .MakePublic()
    .MakeStatic()
    .MakeAsync()
    .WithGenericParameter("T")
    .WithConstraint("T", "class", "new()");

// ClassBuilderExtensions  
classBuilder
    .MakePublicPartial()
    .MakePublicSealed()
    .MakeInternalStatic()
    .AddConstructor(ctor => ctor
        .MakePublic()
        .AddParameter("name", "string")
        .WithBody("Name = name;"));
```

### AttributeBuilder

```csharp
var attribute = new AttributeBuilder("JsonProperty")
    .AddArgument("\"customer_name\"")
    .AddArgument("Required", "Required.Always")
    .AddArgument("NullValueHandling", "NullValueHandling.Ignore");

propertyBuilder.AddAttribute(attribute);
```

### XML Documentation

```csharp
methodBuilder
    .WithXmlDocSummary("Processes the order and returns the result")
    .AddParameter("Order", "order")
    .AddParameter("ProcessOptions", "options")
    .WithXmlDocParam("order", "The order to process")
    .WithXmlDocParam("options", "Processing options")
    .WithXmlDocReturns("The processing result")
    .WithXmlDocException("ArgumentNullException", "Thrown when order is null")
    .WithXmlDocException("InvalidOperationException", "Thrown when order is invalid");
```

## Advanced Usage

### Custom Code Blocks

```csharp
// Add raw code blocks to a class
classBuilder.AddCodeBlock(@"
    #region Properties
    
    // Auto-generated properties
    // Properties would be added separately using AddProperty
    
    #endregion");
```

### Conditional Compilation

```csharp
classBuilder.AddCodeBlock(@"
    #if DEBUG
    private readonly ILogger _logger;
    #endif");
```

### Nested Types

```csharp
var outerClass = new ClassBuilder("Container")
    .MakePublic()
    .AddNestedClass(nested => nested
        .WithName("Nested")
        .MakePrivate()
        .AddProperty("Value", "string"))
    .AddNestedEnum(nested => nested
        .WithName("NestedEnum")
        .MakePublic()
        .AddValue("Option1", "1")
        .AddValue("Option2", "2"));
```

### Generic Constraints

```csharp
var genericClass = new ClassBuilder("Repository")
    .MakePublic()
    .AddGenericParameter("TEntity", constraints => constraints
        .AddConstraint("class")
        .AddConstraint("IEntity")
        .AddConstraint("new()"))
    .AddGenericParameter("TKey", constraints => constraints
        .AddConstraint("struct")
        .AddConstraint("IComparable<TKey>"));
```

### File-Scoped Namespaces

```csharp
// File-scoped namespaces need to be built manually
var classCode = classBuilder.Build();
var code = $"namespace MyApp.Models;\n\n{classCode}";
```

## Best Practices

1. **Use Fluent API**: Chain method calls for cleaner code
2. **Add Documentation**: Always include XML documentation for public APIs
3. **Validate Input**: Check for null/empty strings in your generators
4. **Use Constants**: Leverage the `Modifiers` class for consistency
5. **Format Properly**: Let the builders handle indentation

## Performance Tips

1. **Reuse Builders**: Create builder instances once and reuse when possible
2. **Batch Operations**: Add multiple members at once using collections
3. **Avoid String Concatenation**: Use the builders instead of manual string building
4. **Cache Common Patterns**: Store frequently used code patterns

## License

Licensed under the Apache License 2.0.
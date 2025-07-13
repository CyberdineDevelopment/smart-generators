using FractalDataWorks.SmartGenerators.CodeBuilders;
using FractalDataWorks.SmartGenerators.TestUtilities;
using System;
using Xunit;

namespace FractalDataWorks.SmartGenerators.CodeBuilder.Tests.UnitTests;

public class FieldBuilderTests
{
    [Fact]
    public void GeneratesPrivateReadonlyField()
    {
        var builder = new FieldBuilder("_value").WithType("int").MakePrivate().MakeReadOnly();
        var complete = $"namespace Test {{ {builder.Build()} }}";

        ExpectationsFactory.ExpectCode(complete)
            .HasField(f => f
                .HasName("_value")
                .HasType("int")
                .HasModifier("private")
                .HasModifier("readonly"))
            .Assert();
    }

    [Fact]
    public void Constructor_WithNameAndType_CreatesField()
    {
        // Arrange & Act
        var builder = new FieldBuilder("_count", "int");
        var result = builder.Build();

        // Assert
        Assert.Contains("int _count;", result);
    }

    [Fact]
    public void Constructor_WithNameOnly_CreatesFieldWithEmptyType()
    {
        // Arrange & Act
        var builder = new FieldBuilder("_field");

        // Field with empty type should still build but with empty type
        var result = builder.Build();

        // Assert - will have empty type
        Assert.Contains(" _field;", result);
    }

    [Fact]
    public void Constructor_WithNullTypeName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FieldBuilder("_field", null!));
    }

    [Fact]
    public void WithType_SetsFieldType()
    {
        // Arrange
        var builder = new FieldBuilder("_field");

        // Act
        var result = builder
            .WithType("string")
            .Build();

        // Assert
        Assert.Contains("string _field;", result);
    }

    [Fact]
    public void WithType_NullType_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FieldBuilder("_field");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithType(null!));
    }

    [Fact]
    public void WithType_EmptyType_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FieldBuilder("_field");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithType(""));
    }

    [Fact]
    public void WithType_WhitespaceType_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FieldBuilder("_field");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithType("   "));
    }

    [Fact]
    public void MakePublic_SetsPublicAccessModifier()
    {
        // Arrange
        var builder = new FieldBuilder("Field", "int");

        // Act
        var result = builder
            .MakePublic()
            .Build();

        // Assert
        Assert.Contains("public int Field;", result);
    }

    [Fact]
    public void MakePrivate_SetsPrivateAccessModifier()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "int");

        // Act
        var result = builder
            .MakePrivate()
            .Build();

        // Assert
        Assert.Contains("private int _field;", result);
    }

    [Fact]
    public void MakeProtected_SetsProtectedAccessModifier()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "int");

        // Act
        var result = builder
            .MakeProtected()
            .Build();

        // Assert
        Assert.Contains("protected int _field;", result);
    }

    [Fact]
    public void MakeInternal_SetsInternalAccessModifier()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "int");

        // Act
        var result = builder
            .MakeInternal()
            .Build();

        // Assert
        Assert.Contains("internal int _field;", result);
    }

    [Fact]
    public void MakeStatic_SetsStaticModifier()
    {
        // Arrange
        var builder = new FieldBuilder("Count", "int");

        // Act
        var result = builder
            .MakeStatic()
            .Build();

        // Assert
        Assert.Contains("static int Count;", result);
    }

    [Fact]
    public void MakeReadOnly_SetsReadOnlyModifier()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "string");

        // Act
        var result = builder
            .MakeReadOnly()
            .Build();

        // Assert
        Assert.Contains("readonly string _field;", result);
    }

    [Fact]
    public void MakeConst_WithValue_CreatesConstantField()
    {
        // Arrange
        var builder = new FieldBuilder("MaxValue", "int");

        // Act
        var result = builder
            .MakeConst("100")
            .Build();

        // Assert
        Assert.Contains("const int MaxValue = 100;", result);
    }

    [Fact]
    public void MakeConst_NullValue_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FieldBuilder("Constant", "int");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.MakeConst(null!));
    }

    [Fact]
    public void MakeConst_EmptyValue_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FieldBuilder("Constant", "int");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.MakeConst(""));
    }

    [Fact]
    public void MakeConst_PublicConstant_GeneratesCorrectly()
    {
        // Arrange
        var builder = new FieldBuilder("Pi", "double");

        // Act
        var result = builder
            .MakePublic()
            .MakeConst("3.14159")
            .Build();

        // Assert
        Assert.Contains("public const double Pi = 3.14159;", result);
    }

    [Fact]
    public void WithInitializer_String_AddsInitializer()
    {
        // Arrange
        var builder = new FieldBuilder("_list", "List<string>");

        // Act
        var result = builder
            .WithInitializer("new List<string>()")
            .Build();

        // Assert
        Assert.Contains("List<string> _list = new List<string>();", result);
    }

    [Fact]
    public void WithInitializer_AfterMakeConst_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new FieldBuilder("Constant", "int");
        builder.MakeConst("10");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.WithInitializer("20"));
    }


    [Fact]
    public void AddAttribute_AddsAttributeToField()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "string");
        var attribute = new AttributeBuilder("Obsolete");

        // Act
        var result = builder
            .AddAttribute(attribute)
            .Build();

        // Assert
        Assert.Contains("[Obsolete]", result);
        Assert.Contains("string _field;", result);
    }

    [Fact]
    public void AddAttribute_NullAttribute_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "string");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddAttribute(null!));
    }

    [Fact]
    public void WithXmlDocSummary_AddsDocumentation()
    {
        // Arrange
        var builder = new FieldBuilder("_count", "int");

        // Act
        var result = builder
            .WithXmlDocSummary("The total count of items.")
            .Build();

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// The total count of items.", result);
        Assert.Contains("/// </summary>", result);
    }

    [Fact]
    public void Build_ComplexField_GeneratesCorrectCode()
    {
        // Arrange
        var builder = new FieldBuilder("_logger", "ILogger");

        // Act
        var result = builder
            .MakePrivate()
            .MakeReadOnly()
            .WithXmlDocSummary("The logger instance.")
            .Build();

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// The logger instance.", result);
        Assert.Contains("private readonly ILogger _logger;", result);
    }

    [Fact]
    public void Build_StaticReadOnlyField_GeneratesCorrectCode()
    {
        // Arrange
        var builder = new FieldBuilder("Empty", "string");

        // Act
        var result = builder
            .MakePublic()
            .MakeStatic()
            .MakeReadOnly()
            .WithInitializer("string.Empty")
            .Build();

        // Assert
        Assert.Contains("public static readonly string Empty = string.Empty;", result);
    }

    [Fact]
    public void Build_ConstantWithModifiers_IgnoresOtherModifiers()
    {
        // Arrange
        var builder = new FieldBuilder("MaxSize", "int");

        // Act
        var result = builder
            .MakePublic()
            .MakeStatic() // Should be ignored for const
            .MakeConst("1024")
            .Build();

        // Assert
        Assert.Contains("public const int MaxSize = 1024;", result);
        Assert.DoesNotContain("static", result);
    }

    [Fact]
    public void Build_MultipleAttributes_GeneratesCorrectly()
    {
        // Arrange
        var builder = new FieldBuilder("_field", "object");
        var attr1 = new AttributeBuilder("Obsolete");
        var attr2 = new AttributeBuilder("DebuggerBrowsable")
            .WithArgument("DebuggerBrowsableState.Never");

        // Act
        var result = builder
            .AddAttribute(attr1)
            .AddAttribute(attr2)
            .Build();

        // Assert
        Assert.Contains("[Obsolete]", result);
        Assert.Contains("[DebuggerBrowsable(DebuggerBrowsableState.Never)]", result);
    }

    [Fact]
    public void Build_FieldWithComplexType_GeneratesCorrectly()
    {
        // Arrange
        var builder = new FieldBuilder("_dictionary", "Dictionary<string, List<int>>");

        // Act
        var result = builder
            .MakePrivate()
            .WithInitializer("new Dictionary<string, List<int>>()")
            .Build();

        // Assert
        Assert.Contains("private Dictionary<string, List<int>> _dictionary = new Dictionary<string, List<int>>();", result);
    }

    [Fact]
    public void FluentInterface_ChainsCorrectly()
    {
        // Arrange & Act
        var result = new FieldBuilder("_value")
            .WithType("decimal")
            .MakePrivate()
            .MakeReadOnly()
            .WithXmlDocSummary("The cached value.")
            .WithInitializer("0m")
            .Build();

        // Assert
        Assert.Contains("/// The cached value.", result);
        Assert.Contains("private readonly decimal _value = 0m;", result);
    }
}

using NetMX.Ddd.Application.ObjectMapping;

namespace NetMX.Ddd.Application.Tests.ObjectMapping;

public class ObjectMapperTests
{
    private readonly IObjectMapper _mapper = new ObjectMapper();

    [Fact]
    public void Map_SimpleProperties_CopiesCorrectly()
    {
        // Arrange
        var source = new SourceClass
        {
            Id = 1,
            Name = "Test",
            Age = 25
        };

        // Act
        var destination = _mapper.Map<DestinationClass>(source);

        // Assert
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.Name, destination.Name);
        Assert.Equal(source.Age, destination.Age);
    }

    [Fact]
    public void Map_ToExistingInstance_UpdatesProperties()
    {
        // Arrange
        var source = new SourceClass { Id = 1, Name = "Updated", Age = 30 };
        var destination = new DestinationClass { Id = 999, Name = "Old", Age = 20 };

        // Act
        _mapper.Map(source, destination);

        // Assert
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.Name, destination.Name);
        Assert.Equal(source.Age, destination.Age);
    }

    [Fact]
    public void Map_PartialMatching_OnlyCopiesMatchingProperties()
    {
        // Arrange
        var source = new SourceClass { Id = 1, Name = "Test", Age = 25 };

        // Act
        var destination = _mapper.Map<PartialDestination>(source);

        // Assert
        Assert.Equal(source.Name, destination.Name);
    }

    [Fact]
    public void Map_NullableProperties_HandlesCorrectly()
    {
        // Arrange
        var source = new NullableSource { Id = 1, OptionalAge = 25 };

        // Act
        var destination = _mapper.Map<NullableDestination>(source);

        // Assert
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.OptionalAge, destination.OptionalAge);
    }

    [Fact]
    public void Map_WithTypedMethod_WorksCorrectly()
    {
        // Arrange
        var source = new SourceClass { Id = 1, Name = "Typed", Age = 40 };

        // Act
        var destination = _mapper.Map<SourceClass, DestinationClass>(source);

        // Assert
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.Name, destination.Name);
    }

    [Fact]
    public void Map_NullSource_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _mapper.Map<DestinationClass>(null!));
    }

    // Test classes
    private class SourceClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private class DestinationClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private class PartialDestination
    {
        public string Name { get; set; } = string.Empty;
    }

    private class NullableSource
    {
        public int Id { get; set; }
        public int? OptionalAge { get; set; }
    }

    private class NullableDestination
    {
        public int Id { get; set; }
        public int? OptionalAge { get; set; }
    }
}

using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public class Tests
{
    [Fact]
    public void RecordsAreCreatedEqually()
    {
        var customer1 = new Customer("666", "Cheranga");
        var customer2 = new Customer("666", "Cheranga");

        (customer1 == customer2).Should().BeTrue();
    }

    [Fact]
    public void ClassesAreNotCreatedEqually()
    {
        var employee1 = new Employee{Id = "666", Name = "Cheranga"};
        var employee2 = new Employee{Id = "666", Name = "Cheranga"};

        (employee1 == employee2).Should().BeFalse();
    }

    [Fact]
    public void LanguageExtRecordsAreCreatedEqually()
    {
        var employee1 = new RecordEmployee { Id = "666", Name = "Cheranga" };
        var employee2 = new RecordEmployee { Id = "666", Name = "Cheranga" };

        (employee1 == employee2).Should().BeTrue();
    }

    [Fact]
    public void NoMoreOutParameters()
    {
        var invalidNumericData = "blah";
        var outputForInvalidNumericData = parseInt(invalidNumericData).Match(i => i, () => 0);
        outputForInvalidNumericData.Should().Be(0);
        
        var validNumericData = "666";
        var outputForValidNumericData = parseInt(validNumericData).Match(i => i, () => 0);
        outputForValidNumericData.Should().Be(666);
        
        // even can perform operations on them
        var transformedValidData = parseInt(validNumericData).Map(i => i * 2).Match(i => i, () => 0);
        transformedValidData.Should().Be(666 * 2);
        
        var transformedInvalidData = parseInt(invalidNumericData).Map(i => i * 2).Match(i => i, () => 0);
        transformedInvalidData.Should().Be(0);
    }

    [Fact]
    public void ImmutableCollections()
    {
        var numbers1 = Seq(Range(1, 5));
        var numbers2 = numbers1.Map(x => ++x);

        numbers1.Map(x => ++x).Should().BeEquivalentTo(Range(2, 5));
        (numbers1 == numbers2).Should().BeFalse();

        
        // can use LanguageExt's `List` or `toList` to create immutable lists
        toList(Range(1, 5)).Map(x => ++x).Should<Lst<int>>().BeEquivalentTo(Range(2, 5));
    }
}
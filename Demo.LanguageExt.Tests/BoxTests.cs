using FluentAssertions;
using LanguageExt;

namespace Demo.LanguageExt.Tests;

public record Box<TData>
{
    public TData Data { get; }
    public bool IsEmpty { get; init; }

    public Box()
    {
        IsEmpty = true;
    }

    public Box(TData data)
    {
        Data = data;

        IsEmpty = false;
    }
}

public static class BoxExtensions
{
    public static Box<TReturn> Select<TData, TReturn>(this Box<TData> box, Func<TData, TReturn> mapper)
    {
        if (box.IsEmpty)
        {
            return new Box<TReturn>();
        }

        return new Box<TReturn>(mapper(box.Data));
    }
}

public class BoxTests
{
    [Fact]
    public void SelectTests()
    {
        var emptyEmployeeBox = new Box<Employee>();
        var cheranga = new Box<Employee>(new Employee { Id = "1", Name = "Cheranga" });

        (from emp in emptyEmployeeBox select emp.Name).IsEmpty.Should().BeTrue();

        (from che in cheranga select che.Name).Data.Should().Be("Cheranga");
    }
}
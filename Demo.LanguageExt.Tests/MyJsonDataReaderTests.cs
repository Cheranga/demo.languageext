using System.Text.Json;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace Demo.LanguageExt.Tests;

public class MyJsonDataReaderTests
{
    [Fact]
    public async Task ReadValidData()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = (await dataReader.DeserializeData("TestData/valid-employee.json")
                .BiMap(employee => employee,
                    error => error)
                .Run())
            .Match(Either<Error, Employee>.Right,
                Either<Error, Employee>.Left);

        operation.IsRight.Should().BeTrue();
        operation.IfRight(employee => employee.Should().NotBeNull());
    }

    [Fact]
    public async Task EmptyContent()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = (await dataReader.DeserializeData("TestData/empty-content.json")
                .BiMap(employee => employee,
                    error => error)
                .Run())
            .Match(Either<Error, Employee>.Right,
                Either<Error, Employee>.Left);
        operation.IsLeft.Should().BeTrue();
        operation.IfLeft(error => error.ToException().Message.Should().Be("empty file content"));
    }

    [Fact]
    public async Task InvalidContent()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = (await dataReader.DeserializeData("TestData/invalid-content.json")
                .BiMap(employee => employee,
                    error => error)
                .Run())
            .Match(Either<Error, Employee>.Right,
                Either<Error, Employee>.Left);
        operation.IsLeft.Should().BeTrue();
        operation.IfLeft(error => error.ToException().Should().BeOfType<JsonException>());
    }

    [Fact]
    public async Task FileDoesNotExist()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = (await dataReader.DeserializeData("TestData/blah.json")
                .BiMap(employee => employee,
                    error => error)
                .Run())
            .Match(Either<Error, Employee>.Right,
                Either<Error, Employee>.Left);
        operation.IsLeft.Should().BeTrue();
        operation.IfLeft(error => error.ToException().Should().BeOfType<FileNotFoundException>());
    }
}
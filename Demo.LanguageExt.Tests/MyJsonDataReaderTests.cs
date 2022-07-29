using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Demo.LanguageExt.Tests;

public class MyJsonDataReaderTests
{
    [Fact]
    public async Task ReadValidData()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = await dataReader.DeserializeData("TestData/valid-employee.json").Run();
        operation.IsSucc.Should().BeTrue();
        operation.IfSucc(employee => employee.Should().NotBeNull());
    }

    [Fact]
    public async Task EmptyContent()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = await dataReader.DeserializeData("TestData/empty-content.json").Run();
        operation.IsFail.Should().BeTrue();
        operation.IfFail(error => error.ToException().Message.Should().Be("empty file content"));
    }

    [Fact]
    public async Task InvalidContent()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = await dataReader.DeserializeData("TestData/invalid-content.json").Run();
        operation.IsFail.Should().BeTrue();
        operation.IfFail(error => error.ToException().Should().BeOfType<JsonException>());
    }

    [Fact]
    public async Task FileDoesNotExist()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());
        var operation = await dataReader.DeserializeData("TestData/blah.json").Run();
        operation.IsFail.Should().BeTrue();
        operation.IfFail(error => error.ToException().Should().BeOfType<FileNotFoundException>());
    }
}
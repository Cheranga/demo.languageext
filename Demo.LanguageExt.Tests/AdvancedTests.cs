using System.Text;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using static LanguageExt.Prelude;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Demo.LanguageExt.Tests;

public class AdvancedTests
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

    [Fact]
    public async Task WhatTheAff3()
    {
        var dataReader = new MyJsonDataReader<Employee>(Mock.Of<ILogger<MyJsonDataReader<Employee>>>());


        (await dataReader.DeserializeData("TestData/blah.json")
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error =>
                {
                    var exception = error.ToException();
                    return error.ToException().Should().BeOfType<FileNotFoundException>();
                });
    }

    [Fact]
    public async Task WhatTheAff()
    {
        // `Aff` is like a `TryAsync`
        Aff<Option<string>> ReadFileContent(string filePath)
        {
            return AffMaybe<Option<string>>(async () =>
            {
                var content = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(content)) throw new Exception("empty file content");
                
                return Optional(content);
            });
        }

        Eff<Stream> GetStream(string content)
        {
            return Eff<Stream>(() => new MemoryStream(Encoding.Default.GetBytes(content)));
        }

        Aff<Option<TData>> GetEmployee<TData>(string content)
        {
            return AffMaybe<Option<TData>>(async () =>
            {
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    var model = await JsonSerializer.DeserializeAsync<TData>(stream);
                    if (model == null) throw new Exception("cannot deserialize into required type");

                    return Optional(model);
                }
            });
        }

        // valid content in file
        (await (from readFileOperation in ReadFileContent("TestData/valid-employee.json")
                    from content in readFileOperation.ToAff(Error.New("error when reading the file"))
                    from getEmployeeOperation in GetEmployee<Employee>(content)
                    from employeeData in getEmployeeOperation.ToAff("cannot convert content to employee")
                    select employeeData)
                .BiMap(
                    employee => employee,
                    error => error)
                .Run())
            .Match(
                employee => employee.Should().NotBeNull(),
                error => error.Should().BeNull()
            );

        // invalid content in file
        (await (from readFileOperation in ReadFileContent("TestData/invalid-content.json")
                    from content in readFileOperation.ToAff(Error.New("error when reading the file"))
                    from getEmployeeOperation in GetEmployee<Employee>(content)
                    from employeeData in getEmployeeOperation.ToAff("cannot convert content to employee")
                    select employeeData)
                .BiMap(
                    employee => employee,
                    error => error)
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error => { error.ToException().Should().BeOfType<JsonException>(); });

        // file does not exist
        (await (from readFileOperation in ReadFileContent("TestData/blah.json")
                    from content in readFileOperation.ToAff(Error.New("error when reading the file"))
                    from getEmployeeOperation in GetEmployee<Employee>(content)
                    from employeeData in getEmployeeOperation.ToAff("cannot convert content to employee")
                    select employeeData)
                .BiMap(
                    employee => employee,
                    error => error)
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error => { error.ToException().Should().BeOfType<FileNotFoundException>(); });

        // empty file
        (await (from readFileOperation in ReadFileContent("TestData/empty-content.json")
                    from content in readFileOperation.ToAff(Error.New("error when reading the file"))
                    from getEmployeeOperation in GetEmployee<Employee>(content)
                    from employeeData in getEmployeeOperation.ToAff("cannot convert content to employee")
                    select employeeData)
                .BiMap(
                    employee => employee,
                    error => error)
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error =>
                {
                    error.ToException().Should().BeOfType<Exception>();
                    error.ToException().Message.Should().Be("empty file content");
                });
    }

    [Fact]
    public async Task WhatTheAff2()
    {
        Aff<string> ReadFileContent(string filePath)
        {
            return Aff(async () =>
            {
                var content = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(content)) throw new Exception("empty file content");

                return content;
            });
        }

        Aff<TData> GetData<TData>(string content)
        {
            return Aff<TData>(async () =>
            {
                using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
                {
                    var model = await JsonSerializer.DeserializeAsync<TData>(stream);
                    if (model == null) throw new Exception("cannot deserialize into required type");

                    return model;
                }
            });
        }

        (await (from readFileOperation in ReadFileContent("TestData/valid-employee.json")
                    from employeeData in GetData<Employee>(readFileOperation)
                    select employeeData)
                .Run())
            .Match(
                employee => employee.Should().NotBeNull(),
                error => error.Should().BeNull());

        (await (from readFileOperation in ReadFileContent("TestData/invalid-content.json")
                    from employeeData in GetData<Employee>(readFileOperation)
                    select employeeData)
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error => error.ToException().Should().BeOfType<JsonException>());

        (await (from readFileOperation in ReadFileContent("TestData/empty-content.json")
                    from employeeData in GetData<Employee>(readFileOperation)
                    select employeeData)
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error => error.ToException().Should().BeOfType<Exception>());

        (await (from readFileOperation in ReadFileContent("TestData/blah.json")
                    from employeeData in GetData<Employee>(readFileOperation)
                    select employeeData)
                .Run())
            .Match(
                employee => employee.Should().BeNull(),
                error => error.ToException().Should().BeOfType<FileNotFoundException>());
    }

    [Fact]
    public void TryAsyncTests()
    {
        TryAsync<string> ReadFileAsync(string filePath)
        {
            return TryAsync(async () =>
            {
                if (!File.Exists(filePath)) throw new FileNotFoundException("file not found", filePath);

                return await File.ReadAllTextAsync(filePath);
            });
        }

        TryAsync<Employee> ProcessContentAsync(string content)
        {
            return TryAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(content)) throw new Exception("invalid data");

                var employee = JsonConvert.DeserializeObject<Employee>(content);
                if (string.IsNullOrWhiteSpace(employee?.Id) || string.IsNullOrWhiteSpace(employee?.Name)) throw new Exception("employee data is invalid");

                return employee.AsTask();
            });
        }


        ReadFileAsync("TestData/valid-employee.json")
            .Match(content => { content.Should().NotBeEmpty(); },
                exception => exception.Should().BeNull());

        ReadFileAsync("blah.json")
            .Match(content => { content.Should().BeEmpty(); },
                exception => exception.Should().BeOfType<FileNotFoundException>());
    }
}
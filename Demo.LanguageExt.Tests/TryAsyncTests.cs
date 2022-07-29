using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public class TryAsyncTests
{
    [Fact]
    public async Task Test()
    {
        TryAsync<string> ReadFileAsync(string filePath) =>
            TryAsync(async () =>
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("file not found", filePath);
                }

                return await File.ReadAllTextAsync(filePath);
            });

        TryAsync<Employee> ProcessContentAsync(string content) =>
            TryAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new Exception("invalid data");
                }

                var employee = JsonConvert.DeserializeObject<Employee>(content);
                if (string.IsNullOrWhiteSpace(employee.Id) || string.IsNullOrWhiteSpace(employee.Name))
                {
                    throw new Exception("employee data is invalid");
                }

                return employee.AsTask();
            });


        await ReadFileAsync("TestData/valid-employee.json").Bind(ProcessContentAsync).Match(
            Succ: employee => { employee.Should().NotBeNull(); },
            Fail: exception => { exception.Should().BeNull(); });

        await ReadFileAsync("TestData/file-does-not-exist.json").Bind(ProcessContentAsync).Match(
            Succ: employee => { employee.Should().BeNull(); },
            Fail: exception => { exception.Should().BeOfType<FileNotFoundException>(); });

        await ReadFileAsync("TestData/invalid-employee.json").Bind(ProcessContentAsync).Match(
            Succ: employee => { employee.Should().BeNull(); },
            Fail: exception =>
            {
                exception.Should().BeOfType<Exception>();
                exception.Message.Should().Be("employee data is invalid");
            });

        await ReadFileAsync("TestData/empty-content.json").Bind(ProcessContentAsync).Match(
            Succ: employee => { employee.Should().BeNull(); },
            Fail: exception =>
            {
                exception.Should().BeOfType<Exception>();
                exception.Message.Should().Be("invalid data");
            });

        // if you would like to return from the above operations, you will need to obviously return a single type, for that you can do the below
        var operation = await ReadFileAsync("TestData/valid-employee.json")
            .Bind(ProcessContentAsync)
            .Match(
                Either<Error, Employee>.Right,
                exception =>
                {
                    // probably you could add a logging operation in here
                    return Either<Error, Employee>.Left(Error.New(exception));
                });

        operation.IsRight.Should().BeTrue();
        operation.IfRight(employee => employee.Should().NotBeNull());
    }
}
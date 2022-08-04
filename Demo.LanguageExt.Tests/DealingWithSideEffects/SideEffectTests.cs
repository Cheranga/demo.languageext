using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests.DealingWithSideEffects;

public class SideEffectTests
{
    // Damn, what not to love in here? It abstracts the catching of the exception, and it's intuitive that if
    // the operation fails it will return a `None`.
    // But if you don't use a `Try` (which is a monad - still hazy on this though..) you will be screwed. Also when you think of it
    // if makes sense, because the operation is not wrapped inside something which could handle a failure, and return the fallback value gracefully
    private static Option<string> ReadAllTextWithOption(string filePath) =>
        Try(() => File.ReadAllText(filePath)).ToOption();

    // Same like before, but the `Either` wraps the exception as an error, and as per the name it returns "either" an `Error` or what's required
    private static Either<Error, string> ReadAllTextWithEither(string filePath) =>
        Try(() => File.ReadAllText(filePath)).ToEither(Error.New);

    [Fact]
    public void ReadAllTextTest()
    {
        const string validFile = "TestData/valid-employee.json";
        const string fileDoesNotExist = "TestData/blah blah.json";
        ReadAllTextWithOption(validFile).IfNone(string.Empty).Should().NotBeEmpty();
        ReadAllTextWithOption(fileDoesNotExist).IfNone(string.Empty).Should().BeEmpty();

        ReadAllTextWithEither(validFile).IfLeft(string.Empty).Should().NotBeEmpty();
        ReadAllTextWithEither(fileDoesNotExist).IfLeft(string.Empty).Should().BeEmpty();
    }
}
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

public class SideEffectWithDelegateTests
{
    public delegate Either<Error, A> IO<A>();

    public static IO<string> ReadAllText(string filePath) =>
        () => Try(() => File.ReadAllText(filePath)).ToEither(Error.New);

    public static IO<Unit> WriteText(string filePath, string content) =>
        () => Try(() =>
        {
            File.WriteAllLines(filePath, new[]{content});
            return unit;
        }).ToEither(Error.New);

    const string validFile = "TestData/test-log.txt";
    const string fileDoesNotExist = "TestData/blah blah.json";

    [Fact]
    public void DelegateTests()
    {
        // not a big fan of this test, as the write operation assertions are twisted.
        WriteText(validFile, $"{DateTime.Now:yyyyMMddHHmmss}-testing IO delegate")()
            .IfRight(Error.New("all good")).Should().Be(Error.New("all good"));
        ReadAllText(validFile)()
            .IfLeft(string.Empty).Should().NotBeEmpty();


        // (from a in WriteText(validFile, "blah")
        //     from b in ReadAllText(validFile)
        //     select b).Should().Be("blah");

        var blah = from a in ReadAllText(validFile)
            from b in WriteText(validFile, "blah")
            select unit;

        IList<SomeBaseClass> list = new List<SomeBaseClass>
        {
            new SomeDataA { Id = "1", Name = "Cheranga" },
            new SomeDataB { Id = "2", Address = "Melbourne" }
        };

        var aData = list.OfType<SomeDataA>();
        var bData = list.OfType<SomeDataB>();

    }

    public class SomeDataA : SomeBaseClass
    {
        public string Name { get; set; }
    }

    public class SomeDataB : SomeBaseClass
    {
        public string Address { get; set; }
    }

    public class SomeBaseClass
    {
        public string Id { get; set; }
    }
}

public static class IO
{
    private static SideEffectWithDelegateTests.IO<A> Pure<A>(A value) =>
        () => value;

    // run it and return result
    private static Either<Error, A> Run<A>(this SideEffectWithDelegateTests.IO<A> ma) =>
        Try(ma()).IfFail(exception => Error.New(exception, "IO error"));

    // functor map
    private static SideEffectWithDelegateTests.IO<B> Select<A, B>(this SideEffectWithDelegateTests.IO<A> ma, Func<A, B> func) =>
        () =>
            ma().Match(
                a => func(a),
                Left<Error, B>
            );

    // functor map
    private static SideEffectWithDelegateTests.IO<B> Map<A, B>(this SideEffectWithDelegateTests.IO<A> ma, Func<A, B> func) =>
        () => ma.Select(func)();

    // monadic bind
    public static SideEffectWithDelegateTests.IO<B> SelectMany<A, B>(this SideEffectWithDelegateTests.IO<A> ma, Func<A, SideEffectWithDelegateTests.IO<B>> f) => () =>
        ma().Match(
            Right: x => f(x)(),
            Left: Left<Error, B>);

    // monadic bind
    public static SideEffectWithDelegateTests.IO<B> Bind<A, B>(this SideEffectWithDelegateTests.IO<A> ma, Func<A, SideEffectWithDelegateTests.IO<B>> f) =>
        SelectMany(ma, f);

    // monadic bind + projection
    public static SideEffectWithDelegateTests.IO<C> SelectMany<A, B, C>(
        this SideEffectWithDelegateTests.IO<A> ma,
        Func<A, SideEffectWithDelegateTests.IO<B>> bind,
        Func<A, B, C> project) =>
        ma.SelectMany(a => bind(a).Select(b => project(a, b)));

    [Fact]
    public static void IOTests()
    {
        SideEffectWithDelegateTests.IO<string> success = () => "Hey there!";
        SideEffectWithDelegateTests.IO<string> exception = () => Try<string>(() => throw new Exception("error")).IfFail(ex => ex.Message);

        Pure(10)().IfLeft(-666).Should().Be(10);

        success.Run().IfLeft("error").Should().Be("Hey there!");

        exception.Run().IfLeft(error => error.Message).Should().Be("error");
    }
}
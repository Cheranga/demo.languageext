using FluentAssertions;
using LanguageExt.Common;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests.HowToDealWithSideEffects;

public class SideEffectTests
{
    // A method to return all the content of the file
    static Option<string> ReadAllTextWithOption(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return None;
        }
    }

    // You can write the same using an Either as well
    static Either<Error, string> ReadAllTextWithEither(string path) =>
        File.ReadAllText(path);

    [Fact]
    public void OptionAndEitherTests()
    {
        ReadAllTextWithOption("TestData/valid-employee.json").IfNone("").Should().NotBeEmpty();

        ReadAllTextWithEither("TestData/valid-employee.json").IfLeft("").Should().NotBeEmpty();
    }

    static IO<string> ReadAllTextWithIO(string filePath) =>
        () => File.ReadAllText(filePath);

    static IO<Unit> WriteTextWithIO(string filePath, string content) =>
        () =>
        {
            File.WriteAllText(filePath, content);
            return unit;
        };
    

}

// let's define a delegate
public delegate Either<Error, T> IO<T>();

// now let's make IO<T> to a functor and a monad
public static class IO
{
    // lift to a IO
    public static IO<A> Pure<A>(A value) =>
        () => value;

    // run and wrap up the error handling
    public static Either<Error, A> Run<A>(this IO<A> ma)
    {
        try
        {
            return ma();
        }
        catch (Exception exception)
        {
            return Error.New("IO error", exception);
        }
    }
    
    // functor map
    public static IO<B> Select<A, B>(this IO<A> ma, Func<A, B> f) =>
        ()=> ma().Match(
            a => f(a),
            Left<Error,B>
        );

}
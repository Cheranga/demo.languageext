using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.DataTypes.Serialisation;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public class EitherTests
{
    private Either<Error, TRight> ToRightEither<TRight>(TRight right) => Either<Error, TRight>.Right(right);
    private Either<Error, TRight> ToLeftEither<TRight>(string errorMessage) => Either<Error, TRight>.Left(Error.New(errorMessage));


    [Fact]
    public void ItsEitherThisOrThat()
    {
        // setting the right value
        Either<Error, Employee> operation = new Employee {Id = "1", Name = "Cheranga"};
        operation.IsRight.Should().BeTrue();
        operation.IfRight(emp => emp.Should().NotBeNull());

        // setting the left value
        operation = Error.New("employee not found");
        operation.IsLeft.Should().BeTrue();
        operation.IfLeft(error => error.Message.Should().Be("employee not found"));

        // transforming operations like "Bind" only works on the right value
        operation = new Employee {Id = "2", Name = "Cheranga Hatangala"};
        var employeeOperation = operation.Bind(emp => Either<Error, Employee>.Right(new Employee {Id = $"updated-{emp.Id}", Name = $"updated-{emp.Name}"}));

        employeeOperation.IsRight.Should().BeTrue();
        employeeOperation.IfRight(emp =>
        {
            emp.Id.Should().StartWith("updated");
            emp.Name.Should().StartWith("updated");
        });
    }

    [Fact]
    public void BiBind()
    {
        Either<Error, Employee> operation = new Employee {Id = "1", Name = "Cheranga"};
        var biBindOperation = operation.BiBind(
            emp => Either<Error, Employee>.Right(new Employee {Id = $"bibind-{emp.Id}", Name = emp.Name}),
            error => Error.New(error.Code, "failed operation")
        );

        biBindOperation.IsRight.Should().BeTrue();
        biBindOperation.IfRight(emp => emp.Id.Should().StartWith("bibind"));
    }

    [Fact]
    public void BiExists()
    {
        Either<Error, Employee> operation = new Employee {Id = "1", Name = "Cheranga", Age = 10};
        var isOver18 = operation.BiExists(emp => emp.Age > 18, error => error.Code == 500);
        isOver18.Should().BeFalse();

        operation = Error.New(500, "system error");
        isOver18 = operation.BiExists(emp => emp.Age > 18, error => error.Code == 500);
        isOver18.Should().BeTrue();
    }

    [Fact(Skip = "TODO: check more of `fold` operation")]
    public void Fold()
    {
        Either<int, string> intOrString = "start";
        var result = intOrString.Fold("InitialState", (previousResult, extract) => ChangeState(extract, previousResult));

        string ChangeState(EitherData<int, string> extracted, string previousResult)
        {
            var content = extracted.State == EitherStatus.IsLeft ? $"{extracted.Left}" : $"{extracted.Right}";
            var newResult = $"{previousResult} and {content}";
            return newResult;
        }
    }

    [Fact]
    public void IterAndBiIter()
    {
        Either<Error, Employee> employeeOperation = new Employee {Id = "1", Name = "Cheranga"};
        DoSomeAction(employeeOperation);
        employeeOperation.IsRight.Should().BeTrue();
        employeeOperation.IfRight(emp => emp.Id.Should().StartWith("updated"));

        employeeOperation = Error.New("error occurred");
        DoSomeAction(employeeOperation);
        employeeOperation.IsLeft.Should().BeTrue();
        employeeOperation.IfLeft(error => error.Message.Should().Be("error occurred"));

        void DoSomeAction(Either<Error, Employee> operation)
        {
            operation.BiIter(employee => employee.Id = $"updated-{employee.Id}", error => Console.WriteLine(error.Message));
        }
    }

    [Fact]
    public void BiMap()
    {
        Either<Error, Customer> TransformCustomer(Either<Error, Employee> operation) =>
            operation.BiMap(
                emp => new Customer(emp.Id, emp.Name),
                error => error
            );

        Either<Error, Employee> operation = new Employee {Id = "1", Name = "Cheranga"};
        var customerOperation = TransformCustomer(operation);

        customerOperation.IsRight.Should().BeTrue();
        customerOperation.IfRight(customer => customer.Id.Should().Be("1"));

        operation = Error.New("employee not found");
        customerOperation = TransformCustomer(operation);

        customerOperation.IsLeft.Should().BeTrue();
        customerOperation.IfLeft(error => error.Message.Should().Be("employee not found"));
    }

    [Fact]
    public void BindLeft()
    {
        var operation = ToLeftEither<Customer>("customer not found");
        var transformedOperation = operation.BindLeft(error => Either<Employee, Customer>.Left(new Employee()));

        transformedOperation.IsLeft.Should().BeTrue();
        transformedOperation.IfLeft(employee => employee.Should().NotBeNull());
    }

    [Fact]
    public void MapLeft()
    {
        Either<Error, Employee> operation = new Employee {Id = "1", Name = "Cheranga"};
        var transformedOperation1 = operation.MapLeft(error => Error.New("error1"));

        transformedOperation1.IsLeft.Should().BeFalse();

        operation = Error.New("employee not found");
        var transformedOperation2 = operation.MapLeft(error => error);
        transformedOperation2.IsLeft.Should().BeTrue();
        transformedOperation2.IfLeft(error => error.Message.Should().Be("employee not found"));
    }

    [Fact]
    public void MatchTests()
    {
        var operation = ToRightEither(new Customer("1", "Cheranga"));
        var transformedOperation = operation.Match(
            customer => ToRightEither(new Employee {Id = customer.Id, Name = customer.Name}),
            _ => ToLeftEither<Employee>("employee not found")
        );

        transformedOperation.IsRight.Should().BeTrue();
        transformedOperation.IfRight(employee => employee.Should().NotBeNull());
    }

    [Fact]
    public void BiMapT()
    {
        var customerOperations = Range(1, 10).Select(x => ToRightEither(new Customer(x.ToString(), $"name-{x}"))).ToSeq();
        var customerOperationsWithError = customerOperations.Add(ToLeftEither<Customer>("customer not found"));

        var employeeOperations = customerOperationsWithError.BiMapT(
            customer => new Employee {Id = customer.Id, Name = customer.Name},
            error => error
        ).ToSeq();

        employeeOperations.Rights()
            .Map(x => x.Id).Should<Seq<string>>().BeEquivalentTo(Range(1, 10).Select(x => x.ToString()));

        employeeOperations.Lefts()
            .Map(x => x).Should<Seq<Error>>().BeEquivalentTo(toList(new[] {Error.New("customer not found")}));

        employeeOperations.Iter(employeeOperation =>
        {
            employeeOperation.IfLeft(error => Console.WriteLine($"error occurred : {error.Message}"));
            employeeOperation.IfRight(employee => Console.WriteLine($"employee id {employee.Id}, customer name: {employee.Name}"));
        });
    }
}
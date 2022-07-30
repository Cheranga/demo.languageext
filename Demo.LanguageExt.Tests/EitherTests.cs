using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.DataTypes.Serialisation;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public class EitherTests
{
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
        var employeeOperation = operation.Bind(emp => TransformEmployee(emp, x =>
        {
            x.Id = $"updated-{x.Id}";
            x.Name = $"updated-{x.Name}";
        }));

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
            emp => TransformEmployee(emp, x => x.Id = $"bibind-{x.Id}"),
            error => Error.New(error.Code, "failed operation")
        );

        biBindOperation.IsRight.Should().BeTrue();
        biBindOperation.IfRight(emp => emp.Id.Should().StartWith("bibind"));
    }

    [Fact]
    public void BiExists()
    {
        Either<Error, Employee> operation = new Employee {Id = "1", Name = "Cheranga", Age = 10};
        var isOver18 = operation.BiExists(emp => emp.Age > 18 ? true : false, error => error.Code == 500);
        isOver18.Should().BeFalse();

        operation = Error.New(500, "system error");
        isOver18 = operation.BiExists(emp => emp.Age > 18 ? true : false, error => error.Code == 500);
        isOver18.Should().BeTrue();
    }

    [Fact]
    public void Fold()
    {
        Either<int, string> intOrString = "start";
        var result = intOrString.Fold("InitialState", (previousResult, extract) => changeState(extract, previousResult));
        
        string changeState(EitherData<int, string> extracted, string previousResult)
        {
            var content = extracted.State == EitherStatus.IsLeft ?  $"{extracted.Left}" : $"{extracted.Right}" ;
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

    private Either<Error, Employee> TransformEmployee(Employee employee, Action<Employee> updateFunc)
    {
        var updatedEmployee = new Employee {Id = employee.Id, Name = employee.Name};
        updateFunc(updatedEmployee);
        Either<Error, Employee> operation = updatedEmployee;
        return operation;
    }
}
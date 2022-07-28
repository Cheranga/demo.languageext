using AutoFixture;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public class Tests
{
    private readonly Fixture _fixture;

    public Tests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void RecordsAreCreatedEqually()
    {
        var customer1 = new Customer("666", "Cheranga");
        var customer2 = new Customer("666", "Cheranga");

        

        (customer1 == customer2).Should().BeTrue();
    }

    [Fact]
    public void ClassesAreNotCreatedEqually()
    {
        var employee1 = new Employee { Id = "666", Name = "Cheranga" };
        var employee2 = new Employee { Id = "666", Name = "Cheranga" };

        (employee1 == employee2).Should().BeFalse();
    }

    [Fact]
    public void LanguageExtRecordsAreCreatedEqually()
    {
        var employee1 = new RecordEmployee { Id = "666", Name = "Cheranga" };
        var employee2 = new RecordEmployee { Id = "666", Name = "Cheranga" };

        (employee1 == employee2).Should().BeTrue();
    }

    [Fact]
    public void NoMoreOutParameters()
    {
        var invalidNumericData = "blah";
        var outputForInvalidNumericData = parseInt(invalidNumericData).Match(i => i, () => 0);
        outputForInvalidNumericData.Should().Be(0);

        var validNumericData = "666";
        var outputForValidNumericData = parseInt(validNumericData).Match(i => i, () => 0);
        outputForValidNumericData.Should().Be(666);

        // even can perform operations on them
        var transformedValidData = parseInt(validNumericData).Map(i => i * 2).Match(i => i, () => 0);
        transformedValidData.Should().Be(666 * 2);

        var transformedInvalidData = parseInt(invalidNumericData).Map(i => i * 2).Match(i => i, () => 0);
        transformedInvalidData.Should().Be(0);
    }

    [Fact]
    public void ImmutableCollections()
    {
        var numbers1 = Seq(Range(1, 5));
        var numbers2 = numbers1.Map(x => ++x);

        numbers1.Map(x => ++x).Should().BeEquivalentTo(Range(2, 5));
        (numbers1 == numbers2).Should().BeFalse();


        // can use LanguageExt's `List` or `toList` to create immutable lists
        toList(Range(1, 5)).Map(x => ++x).Should<Lst<int>>().BeEquivalentTo(Range(2, 5));
    }

    [Fact]
    public void FunctionComposition()
    {
        Func<int, int> add2 = i => i + 2;
        Func<int, int> multiplyBy10 = i => i * 10;

        Func<Option<int>, Option<int>> add1Safely = x => x.Match(Some: i => Option<int>.Some(i + 1), None: () => Option<int>.None);
        Func<Option<int>, Option<int>> add2Safely = x => x.Match(Some: i => Option<int>.Some(i + 2), None: () => Option<int>.None);

        Func<Option<Lst<string>>, Option<Lst<string>>> boilWater = x => x.Match(Some: lst => Option<Lst<string>>.Some(lst.Add("boil water")), None: Option<Lst<string>>.None);
        Func<Option<Lst<string>>, Option<Lst<string>>> addCoffee = x => x.Match(Some: lst => Option<Lst<string>>.Some(lst.Add("add coffee")), None: Option<Lst<string>>.None);
        Func<Option<Lst<string>>, Option<Lst<string>>> addMilk = x => x.Match(Some: lst => Option<Lst<string>>.Some(lst.Add("add milk")), None: Option<Lst<string>>.None);

        compose(add2, multiplyBy10)(10).Should().Be(120);

        compose(add1Safely, add2Safely)(Option<int>.None).Should<Option<int>>().Be(Option<int>.None);
        compose(add1Safely, add2Safely)(Optional(10)).Should<Option<int>>().Be(Optional(13));

        var coffeeMakingProcess = compose(boilWater, addCoffee, addMilk)(Optional(Lst<string>.Empty));
        coffeeMakingProcess.IsSome.Should().BeTrue();
        coffeeMakingProcess.IfSome(lst => lst.Should<Lst<string>>().BeEquivalentTo(toList(Seq("boil water", "add coffee", "add milk"))));

       
    }

    [Fact]
    public void BindTest()
    {
        //TODO:
    }

    [Fact]
    public void TryCatchMadeEasier()
    {
        Try<string> ReadFile(string filePath) =>
            Try(() =>
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("file not found", filePath);
                }

                return File.ReadAllText(filePath);
            });

        Try<Employee> ProcessContent(string content) =>
            Try(() =>
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new Exception("invalid data");
                }

                var employee = JsonConvert.DeserializeObject<Employee>(content);
                if (string.IsNullOrWhiteSpace(employee?.Id) || string.IsNullOrWhiteSpace(employee?.Name))
                {
                    throw new Exception("employee data is invalid");
                }

                return employee;
            });

        ReadFile("TestData/valid-employee.json").Bind(ProcessContent).Match(
            Succ: employee =>
            {
                employee.Should().NotBeNull();
            },
            Fail: exception =>
            {
                exception.Should().BeNull();
            });
        
        ReadFile("TestData/file-does-not-exist.json").Bind(ProcessContent).Match(
            Succ: employee =>
            {
                employee.Should().BeNull();
            },
            Fail: exception =>
            {
                exception.Should().BeOfType<FileNotFoundException>();
            });
        
        ReadFile("TestData/invalid-employee.json").Bind(ProcessContent).Match(
            Succ: employee =>
            {
                employee.Should().BeNull();
            },
            Fail: exception =>
            {
                exception.Should().BeOfType<Exception>();
                exception.Message.Should().Be("employee data is invalid");
            });
    }
    
    [Fact]
    public void TryCatchAsyncMadeEasier()
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
                if (string.IsNullOrWhiteSpace(employee?.Id) || string.IsNullOrWhiteSpace(employee?.Name))
                {
                    throw new Exception("employee data is invalid");
                }

                return employee.AsTask();
            });

        ReadFileAsync("TestData/valid-employee.json").Bind(ProcessContentAsync).Match(
            Succ: employee =>
            {
                employee.Should().NotBeNull();
            },
            Fail: exception =>
            {
                exception.Should().BeNull();
            });
        
        ReadFileAsync("TestData/file-does-not-exist.json").Bind(ProcessContentAsync).Match(
            Succ: employee =>
            {
                employee.Should().BeNull();
            },
            Fail: exception =>
            {
                exception.Should().BeOfType<FileNotFoundException>();
            });
        
        ReadFileAsync("TestData/invalid-employee.json").Bind(ProcessContentAsync).Match(
            Succ: employee =>
            {
                employee.Should().BeNull();
            },
            Fail: exception =>
            {
                exception.Should().BeOfType<Exception>();
                exception.Message.Should().Be("employee data is invalid");
            });
        
        
    }

    [Fact]
    public void FunWithMemoization()
    {
        Func<Employee, string> generateHash = employee => $"{employee.GetHashCode()}-{Guid.NewGuid().ToString("N")}";
        var cachedEmployeeId = memo(generateHash);

        var employee = _fixture.Create<Employee>();
        var generatedId = cachedEmployeeId(employee);

        generatedId.Should().BeEquivalentTo(cachedEmployeeId(employee));
    }

    [Fact]
    public void PartialFunctions()
    {
        Func<int, int, int> multiply = (a, b) => a * b;
        Func<int, int> twoTimes = par(multiply, 2);

        multiply(3, 4).Should().Be(12);
        twoTimes(9).Should().Be(18);
    }

    [Fact]
    public void ItsEitherThisOrThat()
    {
        //
        // todo: is there a better way to return an `Either`?
        // earlier versions have the `ToEither`, couldn't find a similar helper with the latest package
        //
        var customers = List(new Customer("1", "A"), new Customer("2", "B"));
        Func<string, Either<Error, Customer>> getCustomerById = id =>
        {
            var customer = customers.FirstOrDefault(x => x.Id == id);
            return customer == null ? Either<Error, Customer>.Left(Error.New("customer not found")) : Either<Error, Customer>.Right(customer);
        };

        getCustomerById("1").Match(
            Left: error => error.Should().BeNull(),
            Right: customer => customer.Should().NotBeNull()
        );
        
        getCustomerById("3").Match(
            Left: error => error.Message.Should().Be("customer not found"),
            Right: customer => customer.Should().BeNull()
        );
    }

    [Fact]
    public void FoldAndReduce()
    {
        var employees = toList(_fixture.CreateMany<Employee>());

        var oldestFromFold = employees.Fold(employees.First(), (s, x) => s.Age > x.Age ? s : x);
        var oldestFromReduce = employees.Reduce((s, x) => s.Age > x.Age ? s : x);

        oldestFromFold.Should().BeEquivalentTo(oldestFromReduce);
    }
}
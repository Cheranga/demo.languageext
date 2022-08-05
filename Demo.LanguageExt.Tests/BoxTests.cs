using FluentAssertions;

namespace Demo.LanguageExt.Tests;

public record Box<TData>
{
    public TData Data { get; }
    public bool IsEmpty { get; init; }

    public Box()
    {
        IsEmpty = true;
    }

    public Box(TData data)
    {
        Data = data;

        IsEmpty = false;
    }
}

public static class BoxExtensions
{
    public static Box<TReturn> Select<TData, TReturn>(this Box<TData> box, Func<TData, TReturn> mapper)
    {
        if (box.IsEmpty)
        {
            return new Box<TReturn>();
        }

        return new Box<TReturn>(mapper(box.Data));
    }

    public static Box<TReturn> Map<TData, TReturn>(this Box<TData> box, Func<TData, TReturn> mapper) =>
        box.Select(mapper);

    public static Box<TReturn> Bind<TData, TReturn>(this Box<TData> box, Func<TData, Box<TReturn>> binder)
    {
        if (box.IsEmpty)
        {
            return new Box<TReturn>();
        }

        return binder(box.Data);
    }

    public static Box<TC> SelectMany<TA, TB, TC>(this Box<TA> box, Func<TA, Box<TB>> mapper, Func<TA, TB, TC> project)
    {
        if (box.IsEmpty)
        {
            return new Box<TC>();
        }

        var mappedItem = mapper(box.Data);
        if (mappedItem.IsEmpty)
        {
            return new Box<TC>();
        }

        var projectedItem = project(box.Data, mappedItem.Data);
        return new Box<TC>(projectedItem);
    }
}

public class BoxTests
{
    [Fact]
    public void SelectTests()
    {
        var emptyEmployeeBox = new Box<Employee>();
        var cheranga = new Box<Employee>(new Employee { Id = "1", Name = "Cheranga" });

        (from emp in emptyEmployeeBox select emp.Name).IsEmpty.Should().BeTrue();

        (from che in cheranga select che.Name).Data.Should().Be("Cheranga");
    }

    [Fact]
    public void BindTests()
    {
        Func<Employee, Box<Customer>> ToCustomerBox = emp => new Box<Employee>(emp).Map(x =>
        {
            var customer = new Customer(x.Id, x.Name);
            return customer;
        });

        Func<Box<Employee>, Box<Customer>> MapToCustomer2 = x => x.Map(e => new Customer(e.Id, e.Name));

        var boxedEmployee = new Box<Employee>(new Employee { Id = "2", Name = "Cheranga" });
        var customer1 = (from emp in boxedEmployee
            from cust in ToCustomerBox(emp)
            select cust).Data;

        var customer2 = from customer in MapToCustomer2(boxedEmployee) //boxedEmployee.Bind(emp => new Box<Customer>(new Customer(emp.Id, emp.Name)))
            select customer;

        Func<Employee, Box<Customer>> toVipCustomer = emp => emp.Id == "1" ? new Box<Customer>() : new Box<Customer>(new Customer(emp.Id, emp.Name));
        Func<Customer, Box<Customer>> toLocalVipCustomer = x => new Box<Customer>(x);

        var customer3 = from emp in boxedEmployee
            from vipCustomer in toVipCustomer(emp)
            from localCustomer in toLocalVipCustomer(vipCustomer)
            select localCustomer;

        var customer4 = boxedEmployee.Bind(toVipCustomer).Bind(toLocalVipCustomer);

        var employee = boxedEmployee.Map(x => new Customer(x.Id, x.Name))
            .Map(x => new Employee { Id = x.Id, Name = x.Name })
            .Select(x => x);
    }
}
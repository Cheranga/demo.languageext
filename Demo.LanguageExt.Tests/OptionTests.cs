using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public class OptionTests
{
    [Fact]
    public void NoPeskyNulls()
    {
        var customers = Seq(Range(1, 10).Select(x => new Customer(x.ToString(), $"customer-{x}")));

        Option<Customer> GetCustomerById(string id) =>
            Optional(customers.FirstOrDefault(x => x.Id == id));

        GetCustomerById("1").IsSome.Should().BeTrue();
        GetCustomerById("1").IfSome(x => x.Should().NotBeNull());

        GetCustomerById("666").IsNone.Should().BeTrue();
    }

    [Fact]
    public void OptionToEither()
    {
        var customers = Seq(Range(1, 10).Select(x => new Customer(x.ToString(), $"customer-{x}")));

        Either<Error, Customer> GetCustomerById(string customerId) =>
            Optional(customers.FirstOrDefault(x => x.Id == customerId)).ToEither(Error.New("customer not found"));

        GetCustomerById("1").IsRight.Should().BeTrue();
        GetCustomerById("1").BiIter(
            customer => customer.Should().NotBeNull(),
            error => error.Should().BeNull()
        );

        GetCustomerById("666").IsLeft.Should().BeTrue();
        GetCustomerById("666").BiIter(
            customer => customer.Should().BeNull(),
            error => error.Message.Should().Be("customer not found")
        );
    }

    [Fact]
    public void PipeliningWithOptions()
    {
        //
        // get employee
        // add the employee to customer collection
        //

        var employees = Seq(Range(1, 11).Select(x => new Employee {Id = x.ToString(), Name = $"employee-{x}"}));
        var customers = Seq(Range(1, 10).Select(x => new Customer(x.ToString(), $"customer-{x}")));

        Option<TData> Search<TData>(Seq<TData> items, Predicate<TData> filter) =>
            Optional(items.FirstOrDefault(x => filter(x)));

        Either<Error, Seq<TData>> AddToCollection<TData>(Seq<TData> items, Option<TData> item) =>
            // in here we can add to a data store
            item.Match(
                data => Optional(items.Add(data)),
                () => None
            ).ToEither(Error.New("invalid data"));

        var updatedCustomers = (from employee in Search(employees, x => x.Id == "11")
            from operation in AddToCollection(customers, Optional(new Customer(employee.Id, employee.Name)))
            select operation).Bind(data => data.Right).ToSeq();

        Search(updatedCustomers, x => x.Id == "11").IsSome.Should().BeTrue();
        Search(updatedCustomers, x => x.Id == "11").IfSome(customer => customer.Should().NotBeNull());
    }
}
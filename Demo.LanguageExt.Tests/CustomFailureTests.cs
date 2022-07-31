using LanguageExt;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public sealed class CustomFailureTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CustomFailureTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public sealed class CustomFailure
    {
        public string ErrorCode { get; }
        public Seq<string> ErrorMessages { get; }

        public CustomFailure(string errorCode, Seq<string> errorMessages)
        {
            ErrorCode = errorCode;
            ErrorMessages = errorMessages;
        }
    }

    [Fact]
    public void FindAndSendNotificationToCustomer()
    {
        //
        // find customer
        // send a notification to the customer
        // update the system that a notification was sent to the said customer
        //
        Either<CustomFailure, TData> FindItem<TData>(Seq<TData> items, Predicate<TData> predicate) =>
            Optional(items.FirstOrDefault(x => predicate(x))).ToEither(new CustomFailure("404", new[] {"not found"}.ToSeq()));

        bool IsVip(string id) =>
            parseInt(id).Match(x => x % 2 == 0, false);

        Either<CustomFailure, TData> SendGreeting<TData>(TData data)
        {
            _testOutputHelper.WriteLine("sending greeting");
            return data;
        }

        Either<CustomFailure, TData> UpdateDataStore<TData>(TData data)
        {
            _testOutputHelper.WriteLine("updating data store");
            return data;
        }

        Either<CustomFailure, TData> ToFailure<TData>(string errorCode, string errorMessage) =>
            new CustomFailure(errorCode, new[] {errorMessage}.ToSeq());

        Either<CustomFailure, Customer> SendNotification(Customer customer) =>
            Optional(customer).Match(
                x => IsVip(x.Id) ? SendGreeting(x) : ToFailure<Customer>("NotVip", "customer is not VIP"),
                () => ToFailure<Customer>("invalid-customer", "invalid customer")
            );

        Either<CustomFailure, Customer> UpdateSystem(Customer customer) =>
            Optional(customer).Match(
                UpdateDataStore,
                () => ToFailure<Customer>("data-access-error", "invalid data, cannot update data store")
            );

        var customers = Range(1, 10).Select(x => new Customer(x.ToString(), $"customer-{x}")).ToSeq();

        (from customer in FindItem(customers, customer => customer.Id == "11")
                from sendNotificationOperation in SendNotification(customer)
                from updateDataStoreOperation in UpdateSystem(sendNotificationOperation)
                select updateDataStoreOperation)
            .BiIter(
                customer => Console.WriteLine("successful operation"),
                failure => Console.WriteLine($"error occurred: {failure.ErrorMessages}")
            );
    }
}
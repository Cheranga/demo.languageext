using LanguageExt;

namespace Demo.LanguageExt.Tests;

public sealed class Employee
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public sealed class RecordEmployee : Record<RecordEmployee>
{
    public string Id { get; set; }
    public string Name { get; set; }
}
using LanguageExt;

namespace Demo.LanguageExt.Tests;

public interface IJsonDataReader<TData> where TData : class
{
    Aff<TData> DeserializeData(string filePath);
}
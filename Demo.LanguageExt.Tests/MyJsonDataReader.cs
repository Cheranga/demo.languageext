using System.Text;
using System.Text.Json;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public sealed class MyJsonDataReader<TData> : IJsonDataReader<TData> where TData : class
{
    private readonly ILogger<MyJsonDataReader<TData>> _logger;

    public MyJsonDataReader(ILogger<MyJsonDataReader<TData>> logger)
    {
        _logger = logger;
    }

    private static Aff<Stream> OpenStream(string filePath, Func<FileMode> fileMode, Func<FileAccess> fileAccess, Func<FileShare> fileShare) =>
        Aff(async () => await Task.FromResult<Stream>(new FileStream(filePath, fileMode(), fileAccess(), fileShare())));

    private static Aff<Stream> OpenStreamToReadWithSharedAccess(string filePath) =>
        OpenStream(filePath, () => FileMode.Open, () => FileAccess.Read, () => FileShare.ReadWrite);

    private static Aff<StreamReader> OpenReader(Stream stream) =>
        Aff(async () => await Task.FromResult(new StreamReader(stream)));

    private static Aff<Option<string>> ReadContent(StreamReader reader) =>
        AffMaybe<Option<string>>(async () =>
        {
            var content = await reader.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(content) ? Option<string>.None : Optional(content);
        });

    private static Aff<Stream> GetStreamForContent(string content) =>
        AffMaybe<Stream>(async () => await Task.FromResult(new MemoryStream(Encoding.Default.GetBytes(content))));

    private static Aff<Option<TData>> DeserializeTo(Stream stream) =>
        AffMaybe<Option<TData>>(async () => Optional(await JsonSerializer.DeserializeAsync<TData>(stream)));


    private static Aff<TData> ToModel(Stream stream) =>
        (from operation in DeserializeTo(stream)
            from model in operation.ToAff(Error.New("cannot convert to target type"))
            select model)
        .BiMap(
            data => data,
            error => error);

    public Aff<TData> DeserializeData(string filePath) =>
        (from contentOperation in use(OpenStreamToReadWithSharedAccess(filePath), stream => use(OpenReader(stream), ReadContent))
            from content in contentOperation.ToAff(Error.New("empty file content"))
            from data in use(GetStreamForContent(content), ToModel)
            select data)
        .BiMap(
            data => data,
            error =>
            {
                _logger.LogError(error.ToException(), "error occurred when getting the {DataType} data from {File}", typeof(TData).Name, filePath);
                return error;
            }
        );
}
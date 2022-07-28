using System.Text;
using System.Text.Json;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Demo.LanguageExt.Tests;

public sealed class DataReader<TData> where TData : class
{
    private Aff<Stream> OpenStream(string filePath) =>
        Aff(async () => await Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)));

    private Aff<StreamReader> OpenReader(Stream stream) =>
        Aff(async () => await Task.FromResult(new StreamReader(stream)));

    private Aff<Option<string>> ReadContent(StreamReader reader) =>
        AffMaybe<Option<string>>(async () =>
        {
            var content = await reader.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(content) ? Option<string>.None : Optional(content);
        });

    private Aff<Stream> GetStreamForContent(string content) =>
        AffMaybe<Stream>(async () => await Task.FromResult(new MemoryStream(Encoding.Default.GetBytes(content))));

    private Aff<Option<TData>> DeserializeTo(Stream stream) =>
        AffMaybe<Option<TData>>(async () => Optional(await JsonSerializer.DeserializeAsync<TData>(stream)));


    private Aff<TData> ToModel(Stream stream) =>
        (from operation in DeserializeTo(stream)
            from model in operation.ToAff(Error.New("cannot convert to target type"))
            select model)
        .BiMap(
            data => data,
            error => error);

    public Aff<TData> DeserializeData(string filePath) =>
        (from contentOperation in use(OpenStream(filePath), stream => use(OpenReader(stream), ReadContent))
            from content in contentOperation.ToAff(Error.New("empty file content"))
            from data in use(GetStreamForContent(content), ToModel)
            select data)
        .BiMap(
            data => data,
            error => error
        );
}
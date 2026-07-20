using EasyCore.Audit;
using FluentAssertions;

namespace EasyCore.Audit.Tests;

public sealed class AuditPayloadSerializerTests
{
    [Fact]
    public void Truncates_Long_Strings()
    {
        var serializer = new AuditPayloadSerializer(10);
        var result = serializer.Serialize(new string('a', 50));
        result.Should().Contain("[truncated]");
        result!.Length.Should().BeLessThan(30);
    }

    [Fact]
    public void Serializes_FormFile_Metadata_Only()
    {
        var file = new FakeFormFile("a.png", "image/png", 1234);
        var serializer = new AuditPayloadSerializer(4096);
        var json = serializer.Serialize(file)!;
        json.Should().Contain("a.png");
        json.Should().Contain("image/png");
        json.Should().Contain("1234");
        json.Should().NotContain("BINARY");
    }

    private sealed class FakeFormFile : Microsoft.AspNetCore.Http.IFormFile
    {
        public FakeFormFile(string fileName, string contentType, long length)
        {
            FileName = fileName;
            ContentType = contentType;
            Length = length;
        }

        public string ContentType { get; }
        public string ContentDisposition => string.Empty;
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get; } = new Microsoft.AspNetCore.Http.HeaderDictionary();
        public long Length { get; }
        public string Name => "file";
        public string FileName { get; }
        public void CopyTo(Stream target) => throw new NotSupportedException();
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Stream OpenReadStream() => throw new NotSupportedException();
    }
}

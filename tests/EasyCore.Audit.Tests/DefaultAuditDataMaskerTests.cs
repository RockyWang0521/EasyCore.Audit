using EasyCore.Audit;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Tests;

public sealed class DefaultAuditDataMaskerTests
{
    [Fact]
    public void Masks_Json_Fields_Case_Insensitive()
    {
        var masker = CreateMasker();
        var json = """{"Password":"secret","userName":"alice","token":"abc"}""";
        var masked = masker.Mask(json)!;

        masked.Should().Contain("***MASKED***");
        masked.Should().NotContain("secret");
        masked.Should().NotContain("\"abc\"");
        masked.Should().Contain("alice");
    }

    [Fact]
    public void Masks_Dictionary_Fields()
    {
        var masker = CreateMasker();
        var dict = new Dictionary<string, object?>
        {
            ["Pwd"] = "123456",
            ["Name"] = "bob",
            ["Phone"] = "13800000000"
        };

        var masked = masker.MaskDictionary(dict)!;
        masked["Pwd"].Should().Be("***MASKED***");
        masked["Phone"].Should().Be("***MASKED***");
        masked["Name"].Should().Be("bob");
    }

    [Fact]
    public void Mask_Failure_Does_Not_Return_Original()
    {
        var masker = CreateMasker();
        var result = masker.Mask("{not-json password=supersecret");
        result.Should().Be("***MASKED***");
        result.Should().NotContain("supersecret");
    }

    private static DefaultAuditDataMasker CreateMasker()
    {
        return new DefaultAuditDataMasker(Options.Create(new AuditOptions()));
    }
}

using EasyCore.Audit;
using FluentAssertions;

namespace EasyCore.Audit.Tests;

public sealed class OperationTypeInferrerTests
{
    [Theory]
    [InlineData("GET", null, "Query")]
    [InlineData("HEAD", null, "Query")]
    [InlineData("POST", null, "Create")]
    [InlineData("PUT", null, "Update")]
    [InlineData("PATCH", null, "Update")]
    [InlineData("DELETE", null, "Delete")]
    [InlineData("OPTIONS", null, "Execute")]
    [InlineData("POST", "Login", "Login")]
    [InlineData("POST", "ExportOrders", "Export")]
    [InlineData("POST", "UploadFile", "Upload")]
    public void Infers_Operation_Type(string method, string? action, string expected)
    {
        OperationTypeInferrer.InferAsString(method, action).Should().Be(expected);
    }
}

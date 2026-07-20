using EasyCore.Audit;
using Microsoft.AspNetCore.Mvc;

namespace Web.EasyCore.Audit.Controllers;

[ApiController]
[Route("api/orders")]
[Tags("MVC · Orders")]
public sealed class OrderController : ControllerBase
{
    /// <summary>Create order — request body audited; Password masked.</summary>
    [HttpPost]
    public IActionResult Create([FromBody] OrderInput input)
        => Ok(new { id = Guid.NewGuid(), input.Name });

    /// <summary>Get order — HTTP GET inferred as Query.</summary>
    [HttpGet("{id}")]
    public IActionResult Get(string id) => Ok(new { id });

    /// <summary>Delete — [IgnoreAudit], should not appear in audit-logs.</summary>
    [HttpDelete("{id}")]
    [IgnoreAudit]
    public IActionResult Delete(string id) => NoContent();

    /// <summary>Export — [Audit] overrides module / operation type.</summary>
    [HttpPost("export")]
    [Audit(ModuleName = "Orders", OperationName = "Export Orders", OperationType = nameof(AuditOperationType.Export))]
    public IActionResult Export() => Ok(new { exported = true, at = DateTimeOffset.UtcNow });
}

public sealed class OrderInput
{
    public string Name { get; set; } = string.Empty;
    public string? Password { get; set; }
}

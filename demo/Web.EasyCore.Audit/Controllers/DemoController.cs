using Microsoft.AspNetCore.Mvc;

namespace Web.EasyCore.Audit.Controllers;

/// <summary>
/// Index of EasyCore.Audit demo endpoints. Open Swagger for try-it-out.
/// </summary>
[ApiController]
[Route("api/demo")]
[Tags("0. Demo guide")]
public sealed class DemoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        title = "EasyCore.Audit demos",
        tip =
            "After calling audited APIs: (1) audit-logs/audit-yyyyMMdd.jsonl (2) Kibana http://localhost:5601 → Discover, index pattern easycore-audit-*. " +
            "/swagger is excluded from audit.",
        scenarios = new object[]
        {
            new
            {
                id = "A",
                name = "MVC · Create order",
                rule = "POST body audited; Password masked",
                routes = new[] { "POST /api/orders" }
            },
            new
            {
                id = "B",
                name = "MVC · Get order",
                rule = "GET inferred as Query",
                routes = new[] { "GET /api/orders/{id}" }
            },
            new
            {
                id = "C",
                name = "MVC · Export (AuditAttribute)",
                rule = "[Audit] ModuleName / OperationType override",
                routes = new[] { "POST /api/orders/export" }
            },
            new
            {
                id = "D",
                name = "MVC · Delete ignored",
                rule = "[IgnoreAudit] — no file line",
                routes = new[] { "DELETE /api/orders/{id}" }
            },
            new
            {
                id = "E",
                name = "Minimal API",
                rule = "Global endpoint filter via UseEasyCoreAudit()",
                routes = new[] { "GET /api/hello", "POST /api/minimal/orders" }
            }
        },
        auditLogDirectory = "audit-logs/",
        elasticsearch = new
        {
            nodes = new[] { "http://localhost:9200" },
            indexPrefix = "easycore-audit",
            kibana = "http://localhost:5601"
        }
    });
}

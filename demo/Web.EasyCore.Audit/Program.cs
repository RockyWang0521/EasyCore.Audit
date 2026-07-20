using EasyCore.Audit;
using EasyCore.Audit.Stores.File;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "EasyCore.Audit Demo",
        Version = "v1",
        Description =
            "HTTP audit logging demo. Writes to audit-logs/*.jsonl and Elasticsearch (easycore-audit-*).\n" +
            "Guide: GET /api/demo · Kibana: http://localhost:5601 · Swagger excludes itself via ExcludedPaths."
    });
});

builder.Services.AddEasyCoreAudit(options =>
{
    options.Enabled = true;
    options.ApplicationName = "Web.EasyCore.Audit";
    options.EnableBatch = false;

    // File store — always available for offline inspection.
    options.UseFile(file =>
    {
        file.Directory = Path.Combine(builder.Environment.ContentRootPath, "audit-logs");
        file.FileNamePrefix = "audit";
        file.UseDailyFile = true;
    });

    // Elasticsearch — requires docker ES on :9200 (see demo/docker-compose.yml).
    options.UseElasticsearch(es =>
    {
        es.Nodes = ["http://localhost:9200"];
        es.IndexPrefix = "easycore-audit";
        es.UseDailyIndex = true;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseEasyCoreAudit();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/hello", () => Results.Ok(new { message = "hello", tip = "Minimal API — audited via endpoint filter" }))
    .WithTags("Minimal API")
    .WithSummary("Minimal GET — writes an audit record");

app.MapPost("/api/minimal/orders", (CreateOrderInput input) =>
        Results.Ok(new { id = Guid.NewGuid(), input.Name, tip = "Password field is masked in audit payload" }))
    .WithTags("Minimal API")
    .WithSummary("Minimal POST — body audited (Password masked)");

app.Run();

public sealed record CreateOrderInput(string Name, string? Password);

public partial class Program;

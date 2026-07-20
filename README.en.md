# 📋 EasyCore.Audit

> **EasyCore.Audit** is an HTTP audit-logging wrapper for .NET 8. One `EasyCoreAudit` + `UseEasyCoreAudit` captures MVC / API Controllers / Minimal APIs automatically, with pluggable **File / Elasticsearch / Database / Custom** stores — no controller base class and no `IAuditService` injection.

<p align="center">
  <img src="https://raw.githubusercontent.com/RockyWang0521/EasyCore.Audit/master/png/EasyCoreLogo.png" alt="EasyCore Logo" width="120" />
</p>

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![ASP.NET](https://img.shields.io/badge/ASP.NET%20Core-Audit-512BD4)
![Stores](https://img.shields.io/badge/Stores-File%20%7C%20ES%20%7C%20DB-0ea5e9)
![License](https://img.shields.io/badge/License-MIT%20OR%20Apache--2.0-yellow)
![Version](https://img.shields.io/badge/Version-8.0.0-blue)

---

## 🌍 Language

- Chinese: [README.md](https://github.com/RockyWang0521/EasyCore.Audit/blob/master/README.md)
- **English (this document)**

Source: [github.com/RockyWang0521/EasyCore.Audit](https://github.com/RockyWang0521/EasyCore.Audit)

---

## 📚 Table of Contents

### Part I — Overview & Architecture
- [1. Positioning](#1--positioning)
- [2. Architecture & Data Flow](#2--architecture--data-flow)
- [3. Repository Layout](#3--repository-layout)

### Part II — Getting Started
- [4. Installation](#4--installation)
- [5. Quick Start](#5--quick-start)
- [6. Stores & Options](#6--stores--options)

### Part III — Demo & Production
- [7. Demo (Swagger)](#7--demo-swagger)
- [8. Production Checklist](#8--production-checklist)
- [9. FAQ](#9--faq)
- [10. License](#10--license)

---

## 1. 🎯 Positioning

EasyCore.Audit solves “audit HTTP traffic automatically instead of hand-writing logs in every action”:

| Pain point | EasyCore.Audit approach |
|---|---|
| Manual audit in every endpoint | Middleware + MVC filter / endpoint filter |
| Forced base class / service injection | Plain Controllers / Minimal APIs work |
| Single sink only | File / ES / Database / Custom — multi-store |
| Secrets in clear text | Built-in field masking (Password, Token, …) |
| Swagger noise in audit | `/swagger` excluded by default |

### 1.1 Design Principles

| Principle | Meaning |
|---|---|
| **Low friction** | `EasyCoreAudit` + `UseEasyCoreAudit` |
| **Zero intrusion** | No business base class |
| **Pluggable stores** | Register multiple `IAuditStore`s |
| **Trimable fields** | Bodies / exceptions / batching are optional |
| **Demo self-check** | Swagger traffic → local `audit-logs` files |

---

## 2. 🏗️ Architecture & Data Flow

### 2.1 Components (text diagram)

```text
┌─────────────────────────────────────────────────────────┐
│  ASP.NET Core Host                                      │
│                                                         │
│   EasyCoreAudit(options)  ──► IAuditStore[]             │
│         │                      ├─ File                  │
│         │                      ├─ Elasticsearch         │
│         │                      ├─ Database              │
│         │                      └─ Custom                │
│         ▼                                               │
│   UseEasyCoreAudit()                                    │
│     ├─ AuditMiddleware                                  │
│     ├─ AuditActionFilter (MVC)                          │
│     └─ AuditEndpointFilter (Minimal API)                │
│              │                                          │
│              ▼                                          │
│     AuditLogDispatcher (± Batch Queue)                  │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Local loop

```text
[dotnet run Demo] ──► hit APIs via Swagger
         │
         ▼
   audit-logs/audit-yyyyMMdd.jsonl
```

---

## 3. 📁 Repository Layout

```text
EasyCore.Audit/
├── src/EasyCore.Audit/
│   ├── Attributes/           # [Audit] / [IgnoreAudit]
│   ├── Middleware/           # HTTP middleware
│   ├── Filters/              # MVC + Minimal API
│   ├── Stores/               # File / ES / Database / Custom
│   └── Options/              # AuditOptions
├── demo/Web.EasyCore.Audit/  # Swagger + MVC + Minimal API
├── tests/EasyCore.Audit.Tests/
├── png/EasyCoreLogo.png
├── README.md
└── README.en.md
```

---

## 4. 📦 Installation

```bash
dotnet add package EasyCore.Audit
```

Requires **.NET 8** / ASP.NET Core. Elasticsearch store depends on `EasyCore.Elasticsearch` (referenced by this package).

---

## 5. ⚡ Quick Start

### 5.1 Register

```csharp
builder.Services.EasyCoreAudit(options =>
{
    options.Enabled = true;
    options.ApplicationName = "MyApp";
    options.UseFile(file =>
    {
        file.Directory = "audit-logs";
        file.FileNamePrefix = "audit";
        file.UseDailyFile = true;
    });
});

var app = builder.Build();
app.UseEasyCoreAudit(); // also attaches Minimal API endpoint filter
app.MapControllers();
```

### 5.2 Attribute overrides (optional)

```csharp
[Audit(ModuleName = "Orders", OperationName = "Export", OperationType = "Export")]
public IActionResult Export() => Ok();

[IgnoreAudit]
public IActionResult Delete(string id) => NoContent();
```

---

## 6. ⚙️ Stores & Options

### 6.1 📁 File store

```csharp
options.UseFile(file =>
{
    file.Directory = "audit-logs";
    file.FileNamePrefix = "audit";
    file.UseDailyFile = true;
});
```

### 6.2 🔎 Elasticsearch

```csharp
options.UseElasticsearch(es =>
{
    es.Nodes = ["http://localhost:9200"];
    es.IndexPrefix = "easycore-audit";
    es.UseDailyIndex = true;
});
```

### 6.3 🗄️ Database

SqlServer / MySql / PostgreSql / Sqlite. Table auto-create is on by default.

```csharp
options.UseDatabase(db =>
{
    db.Provider = AuditDatabaseProvider.SqlServer;
    db.ConnectionString = "...";
    db.TableName = "SysAuditLog";
    db.AutoCreateTable = true;
});
```

### 6.4 🧩 Multi-store / custom

```csharp
options.UseElasticsearch(es => { /* ... */ });
options.UseDatabase(db => { /* ... */ });
options.UseCustomStore<MyAuditStore>();
```

```csharp
public sealed class MyAuditStore : IAuditStore
{
    public string Name => "MyStore";
    public Task WriteAsync(AuditLogRecord record, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken ct = default)
        => Task.CompletedTask;
}
```

### 6.5 Common options

| Key | Meaning | Default | Icon |
|---|---|---|---|
| `Enabled` | Master switch | `true` | 🎚️ |
| `ApplicationName` | App name | `null` | 🏷️ |
| `EnableBatch` | Background batching | `true` | 📦 |
| `BatchSize` | Batch size | `50` | 🔢 |
| `RecordRequestBody` | Capture request body | `true` | 📥 |
| `RecordResponseBody` | Capture response body | `true` | 📤 |
| `SensitiveFieldNames` | Masked fields | Password / Token… | 🔏 |
| `ExcludedPaths` | Skip paths | `/swagger` `/health`… | 🚫 |

---

## 7. 🧪 Demo (Swagger)

### 7.1 🚀 Run

```bash
dotnet run --project demo/Web.EasyCore.Audit
```

| Endpoint | Description |
|---|---|
| http://localhost:5272/swagger | Swagger UI (default launch page) |
| `GET /api/demo` | Scenario guide |
| `POST /api/orders` | MVC create (Password masked) |
| `GET /api/orders/{id}` | MVC query |
| `POST /api/orders/export` | `[Audit]` module override |
| `DELETE /api/orders/{id}` | `[IgnoreAudit]` — no file line |
| `GET /api/hello` | Minimal API |
| `POST /api/minimal/orders` | Minimal POST |

**Order**: start Demo → open Swagger → call APIs → inspect `demo/Web.EasyCore.Audit/audit-logs/audit-yyyyMMdd.jsonl`.

Demo defaults to the **file store** — no ES / database required.

---

## 8. ✅ Production Checklist

- [ ] Prefer ES or database over local files in production
- [ ] Set `ApplicationName` / `ServiceName` per environment
- [ ] Extend `SensitiveFieldNames` for business secrets
- [ ] Enable `EnableBatch` and size `QueueCapacity` for high traffic
- [ ] Keep `/swagger` and `/health` in `ExcludedPaths`
- [ ] Choose `StoreFailureMode` deliberately for multi-store setups

---

## 9. ❓ FAQ

**Q: Why is `UseEasyCoreAudit` required?**  
A: It registers middleware and the global Minimal API endpoint filter. DI alone does not capture HTTP.

**Q: Do Controllers need a base class?**  
A: No. Plain `[ApiController]` is enough.

**Q: Are Swagger requests audited?**  
A: Not by default — `/swagger` is in `ExcludedPaths`.

**Q: Why is Password `***`?**  
A: It matches `SensitiveFieldNames`. Add more names as needed.

**Q: `[IgnoreAudit]` vs excluded paths?**  
A: Paths match URL prefixes; `[IgnoreAudit]` is endpoint metadata.

---

## 10. 📄 License

MIT OR Apache-2.0

Repository: [https://github.com/RockyWang0521/EasyCore.Audit](https://github.com/RockyWang0521/EasyCore.Audit)

### 🤝 Contributing

1. Fork [EasyCore.Audit](https://github.com/RockyWang0521/EasyCore.Audit) and create a feature branch  
2. Add tests under `tests/EasyCore.Audit.Tests`  
3. Run `dotnet test` and `dotnet build`  
4. Open a Pull Request  

Issues / PRs welcome 🚀

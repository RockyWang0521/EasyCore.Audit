# 📋 EasyCore.Audit

> **EasyCore.Audit** 是面向 .NET 8 的 HTTP 审计日志封装。一次 `AddEasyCoreAudit` + `UseEasyCoreAudit`，自动记录 MVC / API Controller / Minimal API 请求，支持 **文件 / Elasticsearch / 数据库 / 自定义** 多存储，无需控制器基类或注入 `IAuditService`。

<p align="center">
  <img src="https://raw.githubusercontent.com/RockyWang0521/EasyCore.Audit/master/png/EasyCoreLogo.png" alt="EasyCore Logo" width="120" />
</p>

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![ASP.NET](https://img.shields.io/badge/ASP.NET%20Core-Audit-512BD4)
![Stores](https://img.shields.io/badge/Stores-File%20%7C%20ES%20%7C%20DB-0ea5e9)
![License](https://img.shields.io/badge/License-MIT-yellow)
![Version](https://img.shields.io/badge/Version-8.3.0-blue)

---

## 🌍 Language

- **中文（当前文档）**
- English: [README.en.md](https://github.com/RockyWang0521/EasyCore.Audit/blob/master/README.en.md)

源码：[github.com/RockyWang0521/EasyCore.Audit](https://github.com/RockyWang0521/EasyCore.Audit)

---

## 📚 目录

### 第一部分：总览与架构
- [1. 项目定位](#1--项目定位)
- [2. 架构与数据流](#2--架构与数据流)
- [3. 仓库结构](#3--仓库结构)

### 第二部分：快速上手
- [4. 安装](#4--安装)
- [5. 三分钟快速开始](#5--三分钟快速开始)
- [6. 存储与配置](#6--存储与配置)

### 第三部分：Demo 与生产
- [7. Demo（Swagger）](#7--demoSwagger)
- [8. 生产清单](#8--生产清单)
- [9. FAQ](#9--faq)
- [10. License](#10--license)

---

## 1. 🎯 项目定位

EasyCore.Audit 解决「在 ASP.NET Core 里自动记审计，而不是每个 Action 手写日志」的问题：

| 痛点 | EasyCore.Audit 做法 |
|---|---|
| 每个接口手写审计 | 中间件 + Filter / EndpointFilter 自动采集 |
| 必须继承基类 / 注入服务 | 普通 Controller / Minimal API 即可 |
| 只支持一种落库 | File / ES / Database / Custom 可多存储并行 |
| 敏感字段明文 | 内置 Password / Token 等字段脱敏 |
| Swagger 也写进审计 | `/swagger` 等路径默认排除 |

### 1.1 设计原则

| 原则 | 说明 |
|---|---|
| **低摩擦接入** | `AddEasyCoreAudit` + `UseEasyCoreAudit` 即可 |
| **零侵入** | 不要求业务基类 |
| **可插拔存储** | 扩展方法注册多个 `IAuditStore` |
| **可裁剪字段** | 请求/响应体、异常、批处理均可配 |
| **Demo 可自检** | Swagger 打流量 → 本地 `audit-logs` 文件 |

---

## 2. 🏗️ 架构与数据流

### 2.1 组件关系（文本示意）

```text
┌─────────────────────────────────────────────────────────┐
│  ASP.NET Core Host                                      │
│                                                         │
│   AddEasyCoreAudit(options)  ──► IAuditStore[]             │
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

### 2.2 本地联调顺序

```text
[dotnet run Demo] ──► Swagger 调 API
         │
         ▼
   audit-logs/audit-yyyyMMdd.jsonl
```

---

## 3. 📁 仓库结构

```text
EasyCore.Audit/
├── src/EasyCore.Audit/
│   ├── Attributes/           # [Audit] / [IgnoreAudit]
│   ├── Middleware/           # HTTP 中间件
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

## 4. 📦 安装

```bash
dotnet add package EasyCore.Audit
```

需要 **.NET 8** / ASP.NET Core。Elasticsearch 存储依赖 `EasyCore.Elasticsearch`（已由本包引用）。

---

## 5. ⚡ 三分钟快速开始

### 5.1 代码注册

```csharp
builder.Services.AddEasyCoreAudit(options =>
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
app.UseEasyCoreAudit(); // 同时挂上 Minimal API EndpointFilter
app.MapControllers();
```

### 5.2 特性覆盖（可选）

```csharp
[Audit(ModuleName = "Orders", OperationName = "Export", OperationType = "Export")]
public IActionResult Export() => Ok();

[IgnoreAudit]
public IActionResult Delete(string id) => NoContent();
```

---

## 6. ⚙️ 存储与配置

### 6.1 📁 文件存储

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

### 6.3 🗄️ 数据库

支持 SqlServer / MySql / PostgreSql / Sqlite。默认自动建表。

```csharp
options.UseDatabase(db =>
{
    db.Provider = AuditDatabaseProvider.SqlServer;
    db.ConnectionString = "...";
    db.TableName = "SysAuditLog";
    db.AutoCreateTable = true;
});
```

### 6.4 🧩 多存储 / 自定义

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

### 6.5 常用选项

| 键 | 说明 | 默认 | 图标语义 |
|---|---|---|---|
| `Enabled` | 总开关 | `true` | 🎚️ |
| `ApplicationName` | 应用名 | `null` | 🏷️ |
| `EnableBatch` | 后台批写 | `true` | 📦 |
| `BatchSize` | 批大小 | `50` | 🔢 |
| `RecordRequestBody` | 记请求体 | `true` | 📥 |
| `RecordResponseBody` | 记响应体 | `true` | 📤 |
| `SensitiveFieldNames` | 脱敏字段 | Password / Token… | 🔏 |
| `ExcludedPaths` | 排除路径 | `/swagger` `/health`… | 🚫 |

---

## 7. 🧪 Demo（Swagger）

### 7.1 🚀 运行

```bash
dotnet run --project demo/Web.EasyCore.Audit
```

| 端点 | 说明 |
|---|---|
| http://localhost:5272/swagger | Swagger UI（默认启动页） |
| `GET /api/demo` | 场景指南 |
| `POST /api/orders` | MVC 创建（Password 脱敏） |
| `GET /api/orders/{id}` | MVC 查询 |
| `POST /api/orders/export` | `[Audit]` 覆盖模块名 |
| `DELETE /api/orders/{id}` | `[IgnoreAudit]` 不落盘 |
| `GET /api/hello` | Minimal API |
| `POST /api/minimal/orders` | Minimal POST |

**顺序**：启动 Demo → 打开 Swagger → 调接口 → 查看 `demo/Web.EasyCore.Audit/audit-logs/audit-yyyyMMdd.jsonl`。

Demo 默认 **文件存储**，无需 ES / 数据库。

---

## 8. ✅ 生产清单

- [ ] 生产使用 ES 或数据库，而非仅本地文件
- [ ] 按环境设置 `ApplicationName` / `ServiceName`
- [ ] 确认 `SensitiveFieldNames` 覆盖业务敏感字段
- [ ] 高流量开启 `EnableBatch` 并调大 `QueueCapacity`
- [ ] `/swagger` `/health` 保持在 `ExcludedPaths`
- [ ] 多存储时明确 `StoreFailureMode`（Ignore / Throw）

---

## 9. ❓ FAQ

**Q: 为什么必须调 `UseEasyCoreAudit`？**  
A: 它注册中间件，并为 Minimal API 挂全局 EndpointFilter；只调 `AddEasyCoreAudit` 不会采集 HTTP。

**Q: Controller 要不要继承基类？**  
A: 不要。普通 `[ApiController]` 即可。

**Q: Swagger 请求会写审计吗？**  
A: 默认不会。`ExcludedPaths` 含 `/swagger`。

**Q: Password 为什么变成 `***`？**  
A: 命中 `SensitiveFieldNames` 脱敏。可自行追加字段名。

**Q: `[IgnoreAudit]` 和排除路径的区别？**  
A: 排除路径按 URL；`[IgnoreAudit]` 按 Action / Endpoint 元数据。

---

## 10. 📄 License

MIT
仓库：[https://github.com/RockyWang0521/EasyCore.Audit](https://github.com/RockyWang0521/EasyCore.Audit)

### 🤝 贡献

1. Fork [EasyCore.Audit](https://github.com/RockyWang0521/EasyCore.Audit) 并创建特性分支  
2. 在 `tests/EasyCore.Audit.Tests` 补充测试  
3. 执行 `dotnet test` 与 `dotnet build`  
4. 提交 Pull Request  

欢迎 Issue / PR 🚀

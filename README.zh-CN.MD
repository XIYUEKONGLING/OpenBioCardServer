# OpenBioCard Server

✨ OpenBioCard 的独立高性能后端实现 ✨

![.NET](https://img.shields.io/badge/.NET-10.0%2B-512BD4?logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Supported-2496ED?logo=docker)
![License](https://img.shields.io/badge/license-MIT-green)

[English](./README.MD) | [📚 API 文档](./DOCUMENTS.zh-CN.MD)

## 📋 目录

- [项目简介](#-项目简介)
- [功能特性](#-功能特性)
- [功能差异说明](#-功能差异说明)
- [快速开始](#-快速开始)
- [环境配置](#-环境配置)
- [部署指南](#-部署指南)
- [项目结构](#-项目结构)
- [技术栈](#-技术栈)
- [许可证](#-许可证)

## 🌟 项目简介

**OpenBioCard Server** 是 [OpenBioCard](https://github.com/OpenBioCard/OpenBioCard/) 生态系统的独立后端解决方案。

与原版基于 Cloudflare Workers 的实现不同，本项目基于 **C# ASP.NET Core** 构建，专为传统服务器、VPS 或容器化部署（Docker/Kubernetes）设计。

它提供了与 OpenBioCard 前端兼容的 RESTful API，允许您使用 SQLite 或 PostgreSQL 在自己的基础设施上完全掌控数据。

## ✨ 功能特性

- 🚀 **高性能** - 基于极速的 ASP.NET Core 框架构建，集成 **响应压缩** (Brotli/Gzip) 与 **多级缓存** (内存/Redis) 机制。
- 💾 **灵活存储** - 支持 **SQLite**、**PostgreSQL** 和 **MySQL**，可根据需求灵活切换。
- 🛡️ **安全防护** - 内置登录接口限流保护（10次/分钟），防止暴力破解。
- 🐳 **容器化** - 提供完善的 Docker 支持，部署轻松便捷。
- 📄 **OpenAPI 支持** - 原生集成 Swagger/OpenAPI，便于接口调试。
- 🛠️ **跨平台** - 支持 Linux, Windows, macOS 等所有 .NET 支持的平台。

## ⚠️ 功能差异说明

> **关于架构差异与兼容配置：**
>
> 本项目基于关系型数据库重构，底层数据模型与上游项目存在本质差异。若配合上游项目的前端使用，请务必将 API 端点配置为 `/classic` 以启用兼容层。

> 兼容层将持续兼容上游项目前端部分。

## 🚀 快速开始

### 前置要求

- **.NET SDK**: 10.0 或更高版本
- **数据库**: PostgreSQL 或 MySQL（可选，开发环境默认使用 SQLite）

### 本地开发

#### 后端部分

1. **克隆仓库**

2. **检查配置**
   项目默认使用 `appsettings.Development.json`。

   ```bash
   $ cd OpenBioCardServer
   $ vim appsettings.Development.json
   ```

   ```json
   // appsettings.Development.json
   "DatabaseSettings": {
     "Type": "SQLite",
     "ConnectionString": "Data Source=development.db"
   }
   ```

3. **运行应用**
   ```bash
   dotnet run
   ```
   服务器将启动在 `http://localhost:5046`（或配置的端口）。

   > 需要配置 OpenBioCard 后端地址为 `http://localhost:5046/classic` （或你的其他端口）。

4. **访问 Swagger UI**
   访问 `http://localhost:5046/swagger` 查看 API 文档并进行调试。

#### 前端部分

1. **克隆仓库** `https://github.com/OpenBioCard/OpenBioCard.git`

2. **安装依赖**

   ```bash
   $ pnpm install
   ```

3. 更改前端 API 端点 （或者**直接更改源代码文件中硬编码的部分**）

   ```bash
   $ vim apps/frontend/.env.local
   ```

   ```env
   VITE_API_BASE_URL=http://localhost:5046/classic
   ```

4. 启动应用

   ```bash
   $ cd apps/frontend
   $ pnpm dev
   ```

## ⚙️ 环境配置

配置通过 `appsettings.json` 或环境变量进行管理。
使用环境变量时，请使用双下划线 `__` 作为层级分隔符（例如：`CacheSettings__Enabled`）。

| 分类 | 配置项 | 环境变量 Key | 说明 | 默认值 |
| :--- | :--- | :--- | :--- | :--- |
| **数据库** | `Type` | `DatabaseSettings__Type` | 数据库类型：`SQLite`, `PgSQL`, `MySQL`。 | - |
| | `ConnectionString` | `DatabaseSettings__ConnectionString` | 数据库连接字符串。 | - |
| **认证** | `RootUsername` | `AuthSettings__RootUsername` | 系统 Root 管理员用户名。 | - |
| | `RootPassword` | `AuthSettings__RootPassword` | **必填**。系统 Root 管理员密码（至少6位）。 | - |
| **缓存** | `Enabled` | `CacheSettings__Enabled` | 是否启用缓存层的总开关。 | `true` |
| | `UseRedis` | `CacheSettings__UseRedis` | 是否使用 Redis。`false` 为使用本机内存缓存。 | `false` |
| | `RedisConnectionString` | `CacheSettings__RedisConnectionString` | Redis 连接字符串 (启用 Redis 时必填)。 | - |
| | `InstanceName` | `CacheSettings__InstanceName` | Redis 键前缀，用于数据隔离。 | `OpenBioCard:` |
| | `CacheSizeLimit` | `CacheSettings__CacheSizeLimit` | 内存缓存的最大条目数量限制 (非字节大小)。 | `null` |
| | `ExpirationMinutes` | `CacheSettings__ExpirationMinutes` | 绝对过期时间（分钟）。 | `30` |
| | `SlidingExpirationMinutes` | `CacheSettings__SlidingExpirationMinutes` | 滑动过期时间（分钟），访问时自动续期。 | `5` |
| | `CompactionPercentage` | `CacheSettings__CompactionPercentage` | 内存缓存满时清理的比例 (0.0 - 1.0)。 | `0.2` |
| | `FactorySoftTimeoutMilliseconds` | `CacheSettings__FactorySoftTimeoutMilliseconds` | 缓存工厂软超时时间（毫秒）。 | `500` |
| | `EnableFailSafe` | `CacheSettings__EnableFailSafe` | 是否启用故障兜底 (Fail-Safe) 机制。 | `true` |
| | `FailSafeMaxDurationMinutes` | `CacheSettings__FailSafeMaxDurationMinutes` | 故障兜底时陈旧数据的最大保留时间（分钟）。 | `120` |
| | `FailSafeThrottleDurationSeconds` | `CacheSettings__FailSafeThrottleDurationSeconds` | 瞬时故障的节流时间（秒）。 | `30` |
| | `DistributedCacheCircuitBreakerDurationSeconds` | `CacheSettings__DistributedCacheCircuitBreakerDurationSeconds` | 分布式缓存熔断时间（秒）。 | `2` |
| **压缩** | `Enabled` | `CompressionSettings__Enabled` | 是否启用 Gzip/Brotli 响应压缩。 | `true` |
| | `EnableForHttps` | `CompressionSettings__EnableForHttps` | 是否在 HTTPS 请求中启用压缩。 | `true` |
| | `Level` | `CompressionSettings__Level` | 压缩级别：`Fastest` (最快), `Optimal` (最佳), `SmallestSize` (最小体积)。 | `Fastest` |
| **限流** | `Policies` | `RateLimitSettings__Policies__0__...` | 限流策略列表配置。 | `[]` |
| | `[n].PolicyName` | `...__PolicyName` | **必填**。策略名称（如 `login`, `general`）。 | - |
| | `[n].Type` | `...__Type` | 算法类型：`FixedWindow`, `SlidingWindow`, `TokenBucket`, `Concurrency`。 | `FixedWindow` |
| | `[n].PermitLimit` | `...__PermitLimit` | 每个时间窗口允许的最大请求数。 | - |
| | `[n].WindowSeconds` | `...__WindowSeconds` | 时间窗口长度（单位：**秒**）。 | - |
| | `[n].QueueLimit` | `...__QueueLimit` | 超过限制时允许排队的请求数。 | `0` |
| **资源** | `MaxFileSizeMB` | `AssetSettings__MaxFileSizeMB` | 上传文件的最大大小（MB）。 | `5` |
| | `AllowedImageTypes` | `AssetSettings__AllowedImageTypes` | 允许上传的图片 MIME 类型列表。 | `["image/jpeg", ...]` |
| **跨域** | `AllowedOrigins` | `CorsSettings__AllowedOrigins` | 允许的前端域名（如 `https://example.com` 或 `*`）。 | `[]` |


> **注意：** 由于 `Policies` 是一个数组，环境变量需使用索引格式（例如：`RateLimitSettings__Policies__0__PolicyName` 代表第一个策略，`__1__` 代表第二个）。

## 🏗️ 部署指南

### 方式一：Docker Compose（推荐）

这是部署服务器和 PostgreSQL 数据库最简单的方法。

### 方式二：手动部署 (Linux)

1. **发布应用：**
   ```bash
   dotnet publish -c Release -o ./publish
   ```
2. **上传文件** 到您的服务器目录。
3. **设置环境变量** 配置生产环境数据库连接。
4. **运行：** `dotnet OpenBioCardServer.dll`

## 📁 项目结构

```
OpenBioCardServer/
├── Controllers/      # API 控制器
├── Data/             # EF Core 上下文与迁移
├── Middlewares/      # 中间件 (如限流配置)
├── Models/           # 数据库实体模型
├── Services/         # 业务逻辑 (Auth, User, Admin)
├── Utilities/        # 工具类
├── Program.cs        # 程序入口与依赖注入配置
├── appsettings.json  # 配置文件
└── Dockerfile        # 容器构建文件
```

## 🛠️ 技术栈

- **框架:** .NET 10 / ASP.NET Core
- **ORM:** Entity Framework Core
- **数据库:** PostgreSQL / MySQL / SQLite
- **文档:** Microsoft.AspNetCore.OpenApi

## 📄 许可证

本项目采用 MIT 许可证。

---

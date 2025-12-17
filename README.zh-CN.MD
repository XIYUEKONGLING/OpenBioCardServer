# OpenBioCard Server

✨ OpenBioCard 的独立高性能后端实现 ✨

> 注意：本项目不属于 OpenBioCard 官方。

![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4?logo=dotnet)
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

- 🚀 **高性能** - 基于极速的 ASP.NET Core 框架构建。
- 💾 **灵活存储** - 开发环境默认支持 **SQLite**，生产环境支持 **PostgreSQL**。
- 🐳 **容器化** - 提供完善的 Docker 支持，部署轻松便捷。
- 📄 **OpenAPI 支持** - 原生集成 Swagger/OpenAPI，便于接口调试。
- 🛠️ **跨平台** - 支持 Linux, Windows, macOS 等所有 .NET 支持的平台。

## ⚠️ 功能差异说明

> **关于功能完整性的说明：**
>
> 由于上游项目的 API 文档尚不完整，部分特定字段（如 **教育经历**、**在校经历** 等）目前暂不支持保存。
>

> **关于独立 API 的说明：**
>
> 此外，本项目还包含上游项目中不存在的独立 API，用于实现功能扩展及安全性增强。

## 🚀 快速开始

### 前置要求

- **.NET SDK**: 8.0 或更高版本
- **数据库**: PostgreSQL（可选，开发环境默认使用 SQLite）

### 本地开发

1. **克隆仓库**

2. **检查配置**
   项目默认使用 `appsettings.Development.json`。
   ```json
   // appsettings.Development.json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=development.db"
   }
   ```

3. **运行应用**
   ```bash
   dotnet run
   ```
   服务器将启动在 `http://localhost:5046`（或配置的端口）。

4. **访问 Swagger UI**
   访问 `http://localhost:5046/swagger` 查看 API 文档并进行调试。

## ⚙️ 环境配置

配置通过 `appsettings.json` 或环境变量进行管理。

| 配置项 | 环境变量 Key | 说明 |
| :--- | :--- | :--- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | 数据库连接字符串（Postgres）或文件路径（SQLite）。 |
| `AuthSettings:RootUsername` | `AuthSettings__RootUsername` | 系统管理员用户名。 |
| `AuthSettings:RootPassword` | `AuthSettings__RootPassword` | **必填**。系统管理员密码。 |
| `CorsSettings:AllowedOrigins` | `CorsSettings__AllowedOrigins` | 允许的前端域名列表（逗号分隔）。 |
| `AppSettings::Version` | `AppSettings__Version` | `/check-backend` 返回给前端的服务器版本名称。 |

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
├── Models/           # 数据库实体模型
├── Services/         # 业务逻辑 (Auth, User, Admin)
├── Program.cs        # 程序入口与依赖注入配置
├── appsettings.json  # 配置文件
└── Dockerfile        # 容器构建文件
```

## 🛠️ 技术栈

- **框架:** .NET 8 / ASP.NET Core
- **ORM:** Entity Framework Core
- **数据库:** PostgreSQL (Npgsql) / SQLite
- **文档:** Microsoft.AspNetCore.OpenApi

## 📄 许可证

本项目采用 MIT 许可证。

---


# OpenBioCard Server

✨ Independent high-performance backend implementation of OpenBioCard ✨

![.NET](https://img.shields.io/badge/.NET-10.0%2B-512BD4?logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Supported-2496ED?logo=docker)
![License](https://img.shields.io/badge/license-MIT-green)

[简体中文](./README.zh-CN.MD) | [📚 API Documentation](./DOCUMENTS.MD)

## 📋 Table of Contents

- [Project Overview](#-project-overview)
- [Features](#-features)
- [Functional Differences](#-functional-differences)
- [Quick Start](#-quick-start)
- [Environment Configuration](#-environment-configuration)
- [Deployment Guide](#-deployment-guide)
- [Project Structure](#-project-structure)
- [Tech Stack](#-tech-stack)
- [License](#-license)

## 🌟 Project Overview

**OpenBioCard Server** is an independent backend solution for the [OpenBioCard](https://github.com/OpenBioCard/OpenBioCard/) ecosystem.

Unlike the original implementation based on Cloudflare Workers, this project is built on **C# ASP.NET Core**, designed specifically for traditional servers, VPS, or containerized deployments (Docker/Kubernetes).

It provides RESTful APIs compatible with the OpenBioCard frontend, allowing you to fully control your data on your own infrastructure using SQLite or PostgreSQL.

## ✨ Features

- 🚀 **High Performance** - Built on ASP.NET Core with **Response Compression** (Brotli/Gzip) and **Multi-level Caching** (Memory/Redis) support.
- 💾 **Flexible Storage** - Supports **SQLite**, **PostgreSQL**, and **MySQL**, easily switchable based on requirements.
- 🛡️ **Security Protection** - Built-in login rate limiting (10 requests per minute) to prevent brute-force attacks.
- 🐳 **Containerization** - Comprehensive Docker support for easy deployment.
- 📄 **OpenAPI Support** - Native integration with Swagger/OpenAPI for convenient API debugging.
- 🛠️ **Cross-platform** - Supports Linux, Windows, macOS, and all platforms supported by .NET.

## ⚠️ Functional Differences

> **Regarding architectural differences and compatibility configuration:**
>
> This project is rebuilt on relational databases, with a fundamentally different underlying data model compared to the upstream project. If used with the upstream project's frontend, please ensure the API endpoint is configured to `/classic` to enable the compatibility layer.

> The compatibility layer will continue to support the frontend of the upstream project.

## 🚀 Quick Start

### Prerequisites

- **.NET SDK**: 10.0 or higher
- **Database**: PostgreSQL or MySQL (optional, SQLite is used by default in development)

### Local Development

#### Backend

1. **Clone the repository**

2. **Check configuration**
   The project uses `appsettings.Development.json` by default.
   ```json
   // appsettings.Development.json
   "DatabaseSettings": {
     "Type": "SQLite",
     "ConnectionString": "Data Source=development.db"
   }
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```
   The server will start at `http://localhost:5046` (or configured port).

   > You need to set the OpenBioCard backend URL to `http://localhost:5046/classic` (or your custom port).

4. **Access Swagger UI**
   Visit `http://localhost:5046/swagger` to view API documentation and perform debugging.

#### Frontend

1. **Clone the repository** `https://github.com/OpenBioCard/OpenBioCard.git`

2. **Install dependencies**

   ```bash
   $ pnpm install
   ```

3. Change the frontend API endpoint (or **directly modify the hardcoded parts in the source code**)

   ```bash
   $ vim apps/frontend/.env.local
   ```

   ```env
   VITE_API_BASE_URL=http://localhost:5046/classic
   ```

4. Start the application

   ```bash
   $ cd apps/frontend
   $ pnpm dev
   ```

## ⚙️ Environment Configuration

Configuration is managed via `appsettings.json` or environment variables.
For environment variables, use double underscores `__` to separate sections (e.g., `CacheSettings__Enabled`).

| Section | Setting | Env Variable Key | Description | Default |
| :--- | :--- | :--- | :--- | :--- |
| **Database** | `Type` | `DatabaseSettings__Type` | Database provider: `SQLite`, `PgSQL`, `MySQL`. | - |
| | `ConnectionString` | `DatabaseSettings__ConnectionString` | Database connection string. | - |
| **Auth** | `RootUsername` | `AuthSettings__RootUsername` | System root administrator username. | - |
| | `RootPassword` | `AuthSettings__RootPassword` | **Required**. System root password (min 6 chars). | - |
| **Cache** | `Enabled` | `CacheSettings__Enabled` | Master switch to enable/disable caching. | `true` |
| | `UseRedis` | `CacheSettings__UseRedis` | Use Redis distributed cache (`true`) or Memory cache (`false`). | `false` |
| | `RedisConnectionString` | `CacheSettings__RedisConnectionString` | Redis connection string (Required if UseRedis is true). | - |
| | `InstanceName` | `CacheSettings__InstanceName` | Redis key prefix to isolate data. | `OpenBioCard:` |
| | `CacheSizeLimit` | `CacheSettings__CacheSizeLimit` | Max entry count for Memory Cache (not bytes). | `null` |
| | `ExpirationMinutes` | `CacheSettings__ExpirationMinutes` | Absolute expiration time in minutes. | `30` |
| | `SlidingExpirationMinutes` | `CacheSettings__SlidingExpirationMinutes` | Reset expiration if accessed within this time. | `5` |
| | `CompactionPercentage` | `CacheSettings__CompactionPercentage` | Percentage (0.0-1.0) of cache to compact when full. | `0.2` |
| **Compression**| `Enabled` | `CompressionSettings__Enabled` | Enable Gzip/Brotli response compression. | `true` |
| | `EnableForHttps` | `CompressionSettings__EnableForHttps` | Secure CRIME/BREACH protection flag. | `true` |
| | `Level` | `CompressionSettings__Level` | Level: `Fastest`, `Optimal`, `NoCompression`, `SmallestSize`. | `Fastest` |
| **Rate Limit**| `Policies` | `RateLimitSettings__Policies__0__...` | List of rate limit configurations. | `[]` |
| | `[n].PolicyName` | `...__PolicyName` | **Required**. Name of the policy (e.g., `login`, `general`). | - |
| | `[n].Type` | `...__Type` | Algorithm: `FixedWindow`, `SlidingWindow`, `TokenBucket`, `Concurrency`. | `FixedWindow` |
| | `[n].PermitLimit` | `...__PermitLimit` | Max requests allowed per window. | - |
| | `[n].WindowSeconds` | `...__WindowSeconds` | Time window duration in **seconds**. | - |
| | `[n].QueueLimit` | `...__QueueLimit` | Max queued requests when limit is exceeded. | `0` |
| **Assets** | `MaxFileSizeMB` | `AssetSettings__MaxFileSizeMB` | Maximum upload file size in MB. | `5` |
| | `AllowedImageTypes` | `AssetSettings__AllowedImageTypes` | List of allowed MIME types (JSON array in `appsettings`). | `["image/jpeg", ...]` |
| **CORS** | `AllowedOrigins` | `CorsSettings__AllowedOrigins` | Allowed origins (e.g., `https://example.com` or `*`). | `[]` |

> **Note:** Since `Policies` is an array, environment variables use index notation (e.g., `RateLimitSettings__Policies__0__PolicyName` for the first policy, `__1__` for the second).

## 🏗️ Deployment Guide

### Method 1: Docker Compose (Recommended)

This is the easiest way to deploy the server along with a PostgreSQL database.

### Method 2: Manual Deployment (Linux)

1. **Publish the app:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```
2. **Upload files** to your server directory.
3. **Set environment variables** to configure the production database connection.
4. **Run:** `dotnet OpenBioCardServer.dll`

## 📁 Project Structure

```
OpenBioCardServer/
├── Controllers/      # API controllers
├── Data/             # EF Core context and migrations
├── Middlewares/      # Middleware (e.g., rate limiting)
├── Models/           # Database entity models
├── Services/         # Business logic (Auth, User, Admin)
├── Utilities/        # Utility classes
├── Program.cs        # Application entry point and DI configuration
├── appsettings.json  # Configuration file
└── Dockerfile        # Container build file
```

## 🛠️ Tech Stack

- **Framework:** .NET 10 / ASP.NET Core
- **ORM:** Entity Framework Core
- **Database:** PostgreSQL / MySQL / SQLite
- **Documentation:** Microsoft.AspNetCore.OpenApi

## 📄 License

This project is licensed under the MIT License.

---
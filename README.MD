# OpenBioCard Server

✨ Independent high-performance backend implementation of OpenBioCard ✨

> Note: This project is not part of the official OpenBioCard.

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

- 🚀 **High Performance** - Built on the fast ASP.NET Core framework.
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

## ⚙️ Environment Configuration

Configuration is managed via `appsettings.json` or environment variables.

| Setting | Environment Variable Key | Description |
| :--- | :--- | :--- |
| `DatabaseSettings:Type` | `DatabaseSettings__Type` | Database type, supports `SQLite`, `PgSQL`, `MySQL`. |
| `DatabaseSettings:ConnectionString` | `DatabaseSettings__ConnectionString` | Database connection string. |
| `AuthSettings:RootUsername` | `AuthSettings__RootUsername` | System admin username. |
| `AuthSettings:RootPassword` | `AuthSettings__RootPassword` | **Required**. System admin password. |
| `CorsSettings:AllowedOrigins` | `CorsSettings__AllowedOrigins` | List of allowed frontend domains (comma-separated). |
| `AssetSettings:MaxFileSizeMB` | `AssetSettings__MaxFileSizeMB` | Maximum upload file size (MB), default is 5MB. |
| `AssetSettings:AllowedImageTypes` | `AssetSettings__AllowedImageTypes` | List of allowed image MIME types for upload. |

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
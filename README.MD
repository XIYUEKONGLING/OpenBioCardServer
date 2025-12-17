# OpenBioCard Server

✨ Standalone high-performance backend implementation for OpenBioCard ✨

> Note: This project is not affiliated with OpenBioCard official.

![.NET](https://img.shields.io/badge/.NET-8.0%2B-512BD4?logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Supported-2496ED?logo=docker)
![License](https://img.shields.io/badge/license-MIT-green)

[简体中文](./README.zh-CN.MD) | [📚 API Documentation](./DOCUMENTS.MD)

## 📋 Table of Contents

- [Project Introduction](#-project-introduction)
- [Features](#-features)
- [Feature Differences](#-feature-differences)
- [Quick Start](#-quick-start)
- [Environment Configuration](#-environment-configuration)
- [Deployment Guide](#-deployment-guide)
- [Project Structure](#-project-structure)
- [Tech Stack](#-tech-stack)
- [License](#-license)

## 🌟 Project Introduction

**OpenBioCard Server** is a standalone backend solution for the [OpenBioCard](https://github.com/OpenBioCard/OpenBioCard/) ecosystem.

Unlike the original implementation based on Cloudflare Workers, this project is built on **C# ASP.NET Core**, designed specifically for traditional servers, VPS, or containerized deployments (Docker/Kubernetes).

It provides a RESTful API compatible with the OpenBioCard frontend, allowing you to fully control your data on your own infrastructure using SQLite or PostgreSQL.

## ✨ Features

- 🚀 **High Performance** - Built on the ultra-fast ASP.NET Core framework.
- 💾 **Flexible Storage** - Supports **SQLite** by default in development, and **PostgreSQL** in production.
- 🐳 **Containerized** - Full Docker support for easy and seamless deployment.
- 📄 **OpenAPI Support** - Native Swagger/OpenAPI integration for convenient API debugging.
- 🛠️ **Cross-Platform** - Runs on all .NET-supported platforms including Linux, Windows, and macOS.

## ⚠️ Feature Differences

> **Note on Feature Completeness:**
>
> Due to incomplete upstream API documentation, certain specific fields (such as **Education Background**, **School Experience**, etc.) currently do not support saving.

> **Note on Independent APIs:**
>
> Additionally, this project includes independent APIs not present in the upstream project, designed to extend functionality and enhance security.

## 🚀 Quick Start

### Prerequisites

- **.NET SDK**: 8.0 or higher
- **Database**: PostgreSQL (optional, SQLite is used by default in development)

### Local Development

1. **Clone the repository**

2. **Check Configuration**
   The project uses `appsettings.Development.json` by default.
   ```json
   // appsettings.Development.json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=development.db"
   }
   ```

3. **Run the Application**
   ```bash
   dotnet run
   ```
   The server will start at `http://localhost:5046` (or the configured port).

4. **Access Swagger UI**
   Visit `http://localhost:5046/swagger` to view API documentation and debug.

## ⚙️ Environment Configuration

Configuration is managed via `appsettings.json` or environment variables.

| Setting | Environment Variable Key | Description |
| :--- | :--- | :--- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Database connection string (Postgres) or file path (SQLite). |
| `AuthSettings:RootUsername` | `AuthSettings__RootUsername` | System admin username. |
| `AuthSettings:RootPassword` | `AuthSettings__RootPassword` | **Required**. System admin password. |
| `CorsSettings:AllowedOrigins` | `CorsSettings__AllowedOrigins` | List of allowed frontend domains (comma-separated). |
| `AppSettings::Version` | `AppSettings__Version` | Server version name returned to the frontend by `/check-backend`. |

## 🏗️ Deployment Guide

### Option 1: Docker Compose (Recommended)

This is the easiest way to deploy both the server and a PostgreSQL database.

### Option 2: Manual Deployment (Linux)

1. **Publish the application:**
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
├── Models/           # Database entity models
├── Services/         # Business logic (Auth, User, Admin)
├── Program.cs        # Application entry and dependency injection setup
├── appsettings.json  # Configuration file
└── Dockerfile        # Container build file
```

## 🛠️ Tech Stack

- **Framework:** .NET 8 / ASP.NET Core
- **ORM:** Entity Framework Core
- **Database:** PostgreSQL (Npgsql) / SQLite
- **Documentation:** Microsoft.AspNetCore.OpenApi

## 📄 License

This project is licensed under the MIT License.

---

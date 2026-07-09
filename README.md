<p align="center">
  <h1 align="center">🛒 Shoppy</h1>
  <p align="center">
    A production-grade e-commerce platform built with ASP.NET Core 10 &amp; React 19
    <br />
    <em>Clean architecture • JWT auth with refresh token rotation • Permission-based RBAC • HybridCache (L1+Redis L2) • OpenTelemetry observability</em>
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=black" alt="React 19" />
  <img src="https://img.shields.io/badge/TypeScript-6.0-3178C6?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
  <img src="https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
</p>

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Features](#-features)
- [Getting Started](#-getting-started)
- [API Reference](#-api-reference)
- [Security](#-security)
- [Testing](#-testing)
- [Observability](#-observability)
- [Frontend](#-frontend)
- [Project Structure](#-project-structure)
- [CI/CD](#-cicd)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🎯 Overview

**Shoppy** is a full-stack e-commerce platform designed as a production-ready reference architecture. It demonstrates modern .NET practices including minimal APIs with Carter, layered architecture, JWT authentication with refresh token rotation and theft detection, fine-grained permission-based authorization, hybrid caching, and full observability — all backed by a React 19 frontend with admin panel and customer storefront.

> This project is built as a portfolio/reference implementation showcasing enterprise-level patterns and security practices in a real-world e-commerce context.

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend (React 19)                      │
│         Vite • TanStack Query • Zustand • shadcn/ui             │
└──────────────────────────┬──────────────────────────────────────┘
                           │ REST / JWT
┌──────────────────────────▼──────────────────────────────────────┐
│                   Shoppy.WebAPI (Presentation)                   │
│      Carter Modules • Rate Limiting • Validation Filters         │
│      Correlation Middleware • Exception Handler • Scalar UI      │
├──────────────────────────┬──────────────────────────────────────┤
│                   Shoppy.Business (Application)                  │
│      Services • DTOs • Validators • Result<T> Pattern            │
│      Auth/JWT • Permissions • Caching • Mapster Mappings         │
├──────────────────────────┬──────────────────────────────────────┤
│                   Shoppy.DataAccess (Infrastructure)             │
│      EF Core DbContext • Configurations • Migrations            │
│      Soft Delete Interceptor • Global Query Filters              │
├──────────────────────────┬──────────────────────────────────────┤
│                   Shoppy.Entity (Domain)                         │
│      BaseEntity (Guid v7 • Audit Fields • Soft Delete)           │
│      Product • Category • Order • OrderItem • User • Role        │
└──────────────────────────┴──────────────────────────────────────┘
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │SQL Server│    │  Redis   │    │  Jaeger  │
    │  (Data)  │    │ (Cache)  │    │ (Traces) │
    └──────────┘    └──────────┘    └──────────┘
```

**Dependency direction** is strictly unidirectional: `WebAPI → Business → DataAccess → Entity`. The Business layer accesses `ApplicationDbContext` directly — no repository/unit-of-work abstraction is used, intentionally keeping the architecture lean for the project's scope.

### Key Design Patterns

| Pattern | Implementation |
|---------|---------------|
| **Result Pattern** | Services return `Result<T>` instead of throwing exceptions for expected failures (404, 409, etc.). `ToHttpResult()` extension maps them to HTTP responses consistently. |
| **Soft Delete** | All entities inherit `BaseEntity` with `IsDeleted` flag. A `SaveChangesAsync` interceptor converts deletes to soft-deletes and cascades to related collections. Global EF Core query filters exclude deleted records automatically. |
| **Hybrid Caching** | `ICacheService` wraps `HybridCache` (in-process L1 + Redis L2). Paginated list queries are cached with tag-based invalidation on writes. |
| **Dynamic Sorting** | `sortBy`/`sortDirection` query params are safely mapped via allow-listed `switch` expressions — no expression injection risk. |
| **Optimistic Concurrency** | `[Timestamp] RowVersion` column on all entities enables conflict detection with automatic 409 responses. |

---

## 🛠 Tech Stack

### Backend

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 10 / ASP.NET Core 10 |
| **API Framework** | Carter (Minimal API modules) |
| **API Versioning** | Asp.Versioning (URL segment + header) |
| **Authentication** | ASP.NET Identity (`IdentityCore<User>`), JWT (access + refresh tokens) |
| **Authorization** | Custom `PermissionAuthorizationHandler` with JWT permission claims |
| **ORM** | EF Core 10, SQL Server, Code-First Migrations |
| **Caching** | `HybridCache` (L1 in-process + L2 Redis), tag-based invalidation |
| **Validation** | FluentValidation (endpoint filters) |
| **Mapping** | Mapster |
| **Logging** | Serilog (Console + File sinks, correlation IDs, structured logging) |
| **Observability** | OpenTelemetry (tracing + metrics) → Jaeger (OTLP/gRPC) |
| **Rate Limiting** | ASP.NET Core `RateLimiter` (global fixed-window + auth IP-partitioned) |
| **API Docs** | OpenAPI + Scalar UI (development only) |
| **Health Checks** | SQL Server health check endpoint (`/health`) |
| **Containerization** | Docker (multi-stage build), Docker Compose |

### Frontend

| Layer | Technology |
|-------|-----------|
| **Framework** | React 19 + TypeScript 6.0 |
| **Build Tool** | Vite 8 |
| **Styling** | Tailwind CSS 4 + shadcn/ui (Radix primitives) |
| **State Management** | Zustand |
| **Server State** | TanStack Query (React Query) |
| **Routing** | React Router DOM 7 |
| **Forms** | React Hook Form + Zod validation |
| **Animations** | Framer Motion |
| **HTTP Client** | Axios (with interceptors for JWT refresh) |
| **Icons** | Lucide React |
| **Notifications** | Sonner (toast) |

### Testing

| Type | Technologies |
|------|-------------|
| **Unit Tests** | xUnit, NSubstitute, FluentAssertions, EF Core InMemory |
| **Integration Tests** | xUnit, Testcontainers (SQL Server + Redis), `WebApplicationFactory` |

---

## ✨ Features

### 🔐 Authentication & Security
- JWT access tokens (1 hour) + cryptographic refresh tokens (7 days)
- Refresh token rotation with **family-based reuse/theft detection** — reusing a revoked token invalidates the entire token family
- Refresh tokens stored as **SHA-256 hashes** in the database
- Account lockout: 5 failed attempts → 15-minute lock
- Time-limited, single-use password reset OTP codes
- Secrets managed via `dotnet user-secrets` (not committed to repo)

### 🛡 Authorization
- Fine-grained **permission-based** access control (`Group.Action` format)
- 27 granular permissions across 7 resource groups
- Permissions embedded in JWT claims, enforced by `PermissionAuthorizationHandler`
- Built-in roles: **Admin** (all permissions) and **Customer** (browse + self-manage)
- Frontend `<RequirePermission>` route guards mirror backend enforcement

### 📦 E-Commerce
- **Products** — Full CRUD with category association, pagination, sorting, and image URLs
- **Categories** — Hierarchical product categorization
- **Orders** — Order lifecycle management with status tracking
- **Order Items** — Line-item management with product references and quantities
- **Users** — Admin user management + self-service profile/password updates
- **Roles** — Dynamic role management with permission assignments

### 🚀 Performance
- **HybridCache** with L1 (in-process) + L2 (Redis) and tag-based invalidation
- **Response compression** enabled for HTTPS
- Rate limiting: 50 req/5s (general) + 5 req/s per IP (auth endpoints)
- **Pagination** with configurable page size (max 100)
- Docker multi-stage build for minimal image size

### 📊 Observability
- Structured logging with Serilog (console + rolling file)
- Correlation IDs across all requests
- OpenTelemetry distributed tracing (ASP.NET Core + HTTP Client + EF Core)
- OpenTelemetry metrics exported to Jaeger via OTLP/gRPC
- Health check endpoint with detailed JSON response

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version | Required For |
|------|---------|-------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Backend |
| [Node.js](https://nodejs.org/) | 20+ | Frontend |
| [SQL Server](https://www.microsoft.com/sql-server) | 2019+ | Database |
| [Docker](https://www.docker.com/) | 24+ | Redis, Jaeger, Integration Tests |

### 1. Clone the Repository

```bash
git clone https://github.com/omeruren/Shoppy.git
cd Shoppy
```

### 2. Start Infrastructure Services

```bash
docker compose up -d
```

This starts:
- **Redis** on `localhost:6379` (L2 cache)
- **Jaeger** on `localhost:16686` (tracing UI) and `localhost:4317` (OTLP)

### 3. Configure Secrets

```bash
cd src/Shoppy/Shoppy.WebAPI

# Set your JWT secret key
dotnet user-secrets set "Jwt:SecretKey" "your-256-bit-secret-key-here"

# Set your email provider password (optional — for password reset emails)
dotnet user-secrets set "EmailSettings:Password" "your-smtp-password"

# Set your SQL Server connection string
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=ShoppyDb;Trusted_Connection=true;TrustServerCertificate=true"
```

### 4. Apply Database Migrations

```bash
cd src/Shoppy
dotnet ef database update --project Shoppy.DataAccess --startup-project Shoppy.WebAPI
```

### 5. Run the API

```bash
cd src/Shoppy
dotnet run --project Shoppy.WebAPI
```

The API will be available at `https://localhost:5001` (or the port configured in your launch profile).

**Development-only endpoints:**
- 📖 OpenAPI Schema: `/openapi`
- 🧪 Scalar Interactive UI: `/scalar`
- ❤️ Health Check: `/health`

### 6. Run the Frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend will be available at `http://localhost:5173`.

### 7. Seed Data

On startup, the API automatically seeds:
- **Roles**: `Admin` and `Customer` with their permission mappings
- **Sample data**: Categories, Products, Users, and Orders for local development

---

## 📡 API Reference

All endpoints are versioned under `/api/v{version}/...` and require authentication unless noted otherwise.

### Auth (`/api/v1/auth`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| `POST` | `/login` | Authenticate and receive access + refresh tokens | ❌ |
| `POST` | `/refresh` | Rotate refresh token and get new token pair | ❌ |
| `POST` | `/forgot-password` | Request password reset OTP (always returns 200) | ❌ |
| `POST` | `/reset-password` | Reset password with OTP code | ❌ |

### Users (`/api/v1/users`)

| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET` | `/` | List users (paginated, sortable) | `Users.Read` |
| `GET` | `/{id}` | Get user by ID | `Users.Read` |
| `GET` | `/me` | Get current user's profile | `Users.UpdateSelf` |
| `POST` | `/` | Create new user | `Users.Create` |
| `PUT` | `/{id}` | Update user | `Users.Update` |
| `PUT` | `/me` | Update own profile | `Users.UpdateSelf` |
| `PUT` | `/me/password` | Change own password | `Users.ChangePassword` |
| `DELETE` | `/{id}` | Soft-delete user | `Users.Delete` |

### Products (`/api/v1/products`)

| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET` | `/` | List products (paginated, sortable) | `Products.Read` |
| `GET` | `/{id}` | Get product by ID | `Products.Read` |
| `POST` | `/` | Create product | `Products.Create` |
| `PUT` | `/{id}` | Update product | `Products.Update` |
| `DELETE` | `/{id}` | Soft-delete product | `Products.Delete` |

### Categories (`/api/v1/categories`)

| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET` | `/` | List categories (paginated, sortable) | `Categories.Read` |
| `GET` | `/{id}` | Get category by ID | `Categories.Read` |
| `POST` | `/` | Create category | `Categories.Create` |
| `PUT` | `/{id}` | Update category | `Categories.Update` |
| `DELETE` | `/{id}` | Soft-delete category | `Categories.Delete` |

### Orders (`/api/v1/orders`)

| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET` | `/` | List orders (paginated, sortable) | `Orders.Read` |
| `GET` | `/{id}` | Get order by ID | `Orders.Read` |
| `POST` | `/` | Create order | `Orders.Create` |
| `PUT` | `/{id}` | Update order | `Orders.Update` |
| `DELETE` | `/{id}` | Soft-delete order | `Orders.Delete` |

### Order Items (`/api/v1/orderitems`)

| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET` | `/` | List order items (paginated) | `OrderItems.Read` |
| `GET` | `/{id}` | Get order item by ID | `OrderItems.Read` |
| `POST` | `/` | Add item to order | `OrderItems.Create` |
| `PUT` | `/{id}` | Update order item | `OrderItems.Update` |
| `DELETE` | `/{id}` | Delete order item | `OrderItems.Delete` |

### Roles (`/api/v1/roles`)

| Method | Endpoint | Description | Permission |
|--------|----------|-------------|------------|
| `GET` | `/` | List roles | `Roles.Read` |
| `GET` | `/{id}` | Get role by ID with permissions | `Roles.Read` |
| `POST` | `/` | Create role | `Roles.Create` |
| `PUT` | `/{id}` | Update role | `Roles.Update` |
| `DELETE` | `/{id}` | Delete role | `Roles.Delete` |

### Pagination & Sorting

All list endpoints accept these query parameters:

```
?pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc
```

---

## 🔒 Security

| Feature | Details |
|---------|---------|
| **JWT Access Token** | 1-hour expiry, contains permission claims |
| **Refresh Token** | 7-day expiry, cryptographic random, stored as SHA-256 hash |
| **Token Rotation** | Every refresh issues a new pair; old token is revoked |
| **Theft Detection** | Reusing a rotated token revokes the entire token family |
| **Account Lockout** | 5 failed logins → 15-minute lockout (`AccessFailedAsync`/`IsLockedOutAsync`) |
| **Rate Limiting** | General: 50 req/5s • Auth: 5 req/s per IP (429 rejection) |
| **CORS** | Configurable allowed origins via `appsettings` |
| **Soft Delete** | Data is never physically deleted; `IsDeleted` flag + global query filters |
| **Optimistic Concurrency** | `RowVersion` timestamp prevents lost updates (HTTP 409) |
| **Password Reset** | Time-limited, single-use OTP codes via email |
| **API Docs Restriction** | OpenAPI/Scalar UI only available in `Development` environment |
| **Non-Root Container** | Docker image runs as `app` user (UID 64198) |

---

## 🧪 Testing

### Unit Tests (97 tests)

Covers all Business layer services and validators:
- `ProductService`, `CategoryService`, `OrderService`, `OrderItemService`
- `UserService`, `RoleService`, `AuthService`
- `PermissionAuthorizationHandler`
- FluentValidation validators

```bash
cd src/Shoppy
dotnet test ../../test/Shoppy.UnitTests
```

**Run a specific test class:**
```bash
dotnet test ../../test/Shoppy.UnitTests --filter "FullyQualifiedName~ProductServiceTests"
```

**Run a single test:**
```bash
dotnet test ../../test/Shoppy.UnitTests --filter "DisplayName~CreateAsync_Should_Return_Conflict"
```

### Integration Tests (9 tests)

End-to-end tests against real SQL Server + Redis containers via Testcontainers:

- CRUD workflows (create → read → update → delete)
- Optimistic concurrency conflict detection (409)
- Full auth flow: login → refresh → token reuse detection
- Permission enforcement (403 for unauthorized access)
- Rate limiting verification (429)

```bash
cd src/Shoppy
dotnet test ../../test/Shoppy.IntegrationTests
```

> ⚠️ **Docker must be running** — integration tests use Testcontainers to spin up SQL Server and Redis containers automatically.

---

## 📊 Observability

### Structured Logging (Serilog)

- **Console sink** — colored, structured output for development
- **File sink** — daily rolling files in `logs/` with 30-day retention
- **Enrichers** — `FromLogContext`, `MachineName`, `ThreadId`
- **Correlation IDs** — every request gets a unique ID via `CorrelationMiddleware`, included in all log entries

### Distributed Tracing (OpenTelemetry → Jaeger)

```bash
# View traces at:
http://localhost:16686
```

Instrumented components:
- ASP.NET Core HTTP pipeline
- Outgoing HTTP client calls
- Entity Framework Core database queries

### Health Checks

```bash
curl http://localhost:5001/health
```

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "sqlserver",
      "status": "Healthy",
      "duration": "42ms"
    }
  ],
  "totalDuration": "45ms"
}
```

---

## 🎨 Frontend

The frontend is a **React 19 + TypeScript** SPA with two distinct interfaces:

### Customer Storefront
- 🏠 Landing page with marketing content
- 🛍 Product catalog with search, filtering, and pagination
- 📄 Product detail pages with add-to-cart
- 🛒 Cart drawer with checkout flow
- 📋 Order history
- 👤 Profile management (update info, change password)

### Admin Panel
- 📊 Dashboard overview
- 📦 Product management (CRUD + data tables)
- 🏷 Category management
- 📋 Order management
- 👥 User management
- 🔑 Role & permission management

### Key Frontend Patterns
- **Auth bootstrap** — on app load, attempts to refresh existing session before rendering
- **Permission-based route guards** — `<ProtectedRoute>` and `<RequirePermission>` components mirror backend authorization
- **Zustand stores** — `auth.store.ts` (JWT/session state) and `cart.store.ts` (shopping cart)
- **Axios interceptors** — automatic token refresh on 401, transparent to components
- **TanStack Query** — server state caching, optimistic updates, background refetching
- **Dark/Light theme** — via `ThemeProvider`

---

## 📁 Project Structure

```
Shoppy/
├── src/Shoppy/
│   ├── Shoppy.slnx                  # Solution file
│   ├── Shoppy.Entity/               # Domain entities & BaseEntity
│   │   ├── Abstraction/             #   BaseEntity (Guid v7, audit, soft-delete)
│   │   └── Models/                  #   Product, Category, Order, OrderItem, User, Role, RefreshToken
│   ├── Shoppy.DataAccess/           # Data access layer
│   │   ├── Context/                 #   ApplicationDbContext
│   │   ├── Configurations/          #   EF Core entity configurations
│   │   └── Migrations/              #   Code-first migrations
│   ├── Shoppy.Business/             # Business logic layer
│   │   ├── Auth/                    #   AuthService, JwtProvider, DTOs
│   │   ├── Products/                #   ProductService, DTOs, Validators
│   │   ├── Categories/              #   CategoryService, DTOs, Validators
│   │   ├── Orders/                  #   OrderService, DTOs, Validators
│   │   ├── OrderItems/              #   OrderItemService, DTOs, Validators
│   │   ├── Users/                   #   UserService, DTOs, Validators
│   │   ├── Roles/                   #   RoleService, DTOs, Validators
│   │   ├── Permissions/             #   Permission constants & authorization handler
│   │   ├── Caching/                 #   ICacheService + HybridCache implementation
│   │   ├── BaseResult/              #   Result<T> pattern
│   │   └── Extensions/              #   Sorting, HTTP result mapping
│   └── Shoppy.WebAPI/               # API presentation layer
│       ├── Modules/                 #   Carter endpoint modules (Auth, Product, etc.)
│       ├── Filters/                 #   FluentValidation endpoint filter
│       ├── Handlers/                #   Global exception handler
│       ├── MiddleWares/             #   Correlation ID middleware
│       ├── Seed/                    #   Role/permission + sample data seeders
│       └── Program.cs              #   Composition root
├── test/
│   ├── Shoppy.UnitTests/            # 97 unit tests
│   └── Shoppy.IntegrationTests/     # 9 integration tests (Testcontainers)
├── frontend/                        # React 19 + TypeScript SPA
│   └── src/
│       ├── api/                     #   API client & endpoint modules
│       ├── components/              #   shadcn/ui + custom components
│       ├── features/                #   Feature-based pages
│       │   ├── admin/               #     Admin panel (dashboard, CRUD pages)
│       │   ├── auth/                #     Login, forgot/reset password
│       │   ├── customer/            #     Storefront (catalog, cart, orders, profile)
│       │   └── marketing/           #     Landing page
│       ├── hooks/                   #   Custom React hooks
│       ├── providers/               #   QueryProvider, ThemeProvider
│       ├── routes/                  #   Route definitions with permission guards
│       ├── stores/                  #   Zustand stores (auth, cart)
│       └── types/                   #   TypeScript type definitions
├── postman/                         # Postman collection & environments
├── .github/workflows/ci.yml        # GitHub Actions CI pipeline
├── Dockerfile                       # Multi-stage Docker build
└── docker-compose.yml               # Redis + Jaeger for local dev
```

---

## 🔄 CI/CD

The project uses **GitHub Actions** for continuous integration:

```yaml
# Triggered on push/PR to master
Build → Unit Tests → Integration Tests → Docker Image Build
```

Pipeline steps:
1. **Checkout** — Clone the repository
2. **Setup .NET 10** — Install the SDK
3. **Restore & Build** — Compile the entire solution
4. **Unit Tests** — Run 97 unit tests
5. **Integration Tests** — Run 9 integration tests (Docker-in-Docker with Testcontainers)
6. **Docker Build** — Validate the production Docker image builds successfully

---

## 🤝 Contributing

1. **Fork** the repository
2. **Create** your feature branch: `git checkout -b feature/amazing-feature`
3. **Commit** your changes: `git commit -m 'feat: add amazing feature'`
4. **Push** to the branch: `git push origin feature/amazing-feature`
5. **Open** a Pull Request

### Coding Guidelines

- Follow the existing layered architecture pattern
- Add FluentValidation validators for new DTOs
- Use the `Result<T>` pattern for service return types
- Add permission constants to `PermissionConstants.cs` for new protected endpoints
- Write unit tests for new services/validators
- Use `dotnet user-secrets` for sensitive configuration — never commit secrets

---

## 📄 License

This project is open-source and available for educational and portfolio purposes.

---

<p align="center">
  Built with ❤️ by <a href="https://github.com/omeruren">Ömer Üren</a>
</p>

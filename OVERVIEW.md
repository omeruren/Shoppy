# Shoppy — Proje Özeti

Shoppy, ASP.NET Core 10 üzerinde yazılmış bir e-ticaret API'si. Minimal API endpoint'leri, JWT tabanlı kimlik doğrulama (refresh token rotation + reuse detection dahil), permission-bazlı yetkilendirme, EF Core/SQL Server veri katmanı ve Redis destekli caching ile production-a yakın bir referans/portföy projesi olarak geliştirildi.

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Runtime | .NET 10 / ASP.NET Core 10 |
| API | Carter (minimal API modülleri), Asp.Versioning (URL + header bazlı versiyonlama) |
| Kimlik & Yetki | ASP.NET Identity (`IdentityCore<User>`), JWT (access + refresh token), custom permission-claim tabanlı `PermissionAuthorizationHandler` |
| Veri Erişimi | EF Core 10, SQL Server, code-first migrations |
| Cache | `HybridCache` (in-process L1 + Redis L2), tag-bazlı invalidation |
| Validasyon | FluentValidation (endpoint filter olarak) |
| Mapping | Mapster |
| Logging | Serilog (console + file sink, correlation id, structured logging) |
| Observability | OpenTelemetry (tracing + metrics) → Jaeger (OTLP/gRPC) |
| Rate Limiting | ASP.NET Core `RateLimiter` (genel + auth-özel, IP-partitioned) |
| Dokümantasyon | OpenAPI + Scalar UI (yalnızca Development ortamında açık) |
| Konteynerleştirme | Docker (multi-stage build), Docker Compose (Redis + Jaeger) |
| CI/CD | GitHub Actions (build + unit + integration test + Docker image doğrulama) |
| Test | xUnit, NSubstitute, FluentAssertions, EF Core InMemory (unit), Testcontainers (MsSql + Redis, integration) |

## Mimari

```
Shoppy.Entity       → Domain entity'leri, BaseEntity (audit + soft-delete alanları)
Shoppy.DataAccess   → EF Core DbContext, entity configuration'ları, migrations
Shoppy.Business     → Servisler, DTO'lar, validator'lar, auth/JWT, permission sistemi
Shoppy.WebAPI       → Carter modülleri (endpoint'ler), Program.cs composition root
test/UnitTests       → Servis/validator unit testleri (EF InMemory + NSubstitute)
test/IntegrationTests → Gerçek HTTP + Testcontainers ile uçtan uca testler
```

Bağımlılık yönü tek yönlü: `WebAPI → Business → DataAccess → Entity`. Business katmanı `ApplicationDbContext`'e doğrudan erişir; repository/unit-of-work soyutlaması bilinçli olarak eklenmedi (proje ölçeğinde gerekçesi yok — ayrıntı için `shoppy_analysis.md` §2'ye bakılabilir).

### Katmanlar arası öne çıkan desenler

- **Result pattern**: Servisler exception fırlatmak yerine `Result<T>` (`IsSuccessful`, `StatusCode`, `ErrorMessages`, `Data`) döndürür; `ToHttpResult()` extension'ı bunu tutarlı HTTP response'lara çevirir.
- **Soft delete**: Tüm entity'ler `BaseEntity`'den türer (`Guid` v7 id, audit alanları, `IsDeleted` + global query filter). Silme işlemleri `SaveChangesAsync` interceptor'ı ile otomatik soft-delete'e çevrilir; `Order → OrderItem` gibi ilişkili koleksiyonlara da cascade uygulanır.
- **Cache**: `ICacheService` soyutlaması `HybridCache` üzerine kurulu; her `GetAllAsync` çağrısı prefix + sayfalama/sıralama parametrelerinden oluşan bir cache key ile önbelleğe alınır, yazma işlemlerinde tag-bazlı olarak invalidate edilir.
- **Dinamik sıralama**: `sortBy`/`sortDirection` query parametreleri, allow-list edilmiş `switch` ifadeleriyle (`SortingExtension`) `OrderBy`/`OrderByDescending`'e çevrilir — `System.Linq.Dynamic.Core` gibi expression-injection riski taşıyan bir kütüphane kullanılmaz.

## Kaynaklar (Endpoint Grupları)

`/api/v{version}/...` altında versiyonlanmış, hepsi rate-limited ve (self-servis auth endpoint'leri hariç) permission-korumalı:

- **Auth** — login, refresh, forgot/reset password
- **Users** — admin CRUD + self-servis (`/users/me`, profil güncelleme, şifre değiştirme)
- **Roles** — CRUD, `RolePermission` eşlemesi ile
- **Products**, **Categories**, **Orders**, **OrderItems** — arama + sayfalama + sıralama destekli CRUD

## Güvenlik

- JWT access token (1 saat) + kriptografik rastgele refresh token (7 gün); refresh token'lar DB'de **SHA-256 hash'i olarak** saklanır, ham değeri yalnızca client görür.
- **Refresh token rotation + family-bazlı reuse (theft) detection**: bir token tekrar kullanılırsa (rotate edilmiş/revoke edilmiş bir token yeniden gelirse) o "family"deki tüm token'lar anında iptal edilir.
- **Permission-bazlı yetkilendirme**: `Permissions` sabitleri → JWT claim'leri → `PermissionAuthorizationHandler`; roller (`Admin`/`Customer`) uygulama açılışında seed edilir.
- **Account lockout**: 5 başarısız girişte 15 dakikalık kilit (gerçekten uygulanıyor — `AccessFailedAsync`/`IsLockedOutAsync` ile).
- **Rate limiting**: genel endpoint'ler için 50 istek/5sn, auth endpoint'leri için IP-partitioned 5 istek/sn (429 ile reddediyor).
- CORS origin'leri ve pagination limitleri (`PageSize` max 100) konfigürasyon/kod ile sınırlı; API dokümantasyonu (`/openapi`, `/scalar`) yalnızca Development'ta açık.
- Şifre reset kodları (OTP) süre sınırlı ve tek kullanımlık.
- JWT/SMTP secret'ları `dotnet user-secrets` üzerinden yönetiliyor (repo'daki `appsettings.json` yalnızca placeholder içeriyor).

## Ortam & Deployment

- `appsettings.json` (paylaşılan varsayılanlar) → `appsettings.Development.json` (yerel SQL Server + dev CORS origin'leri) → `appsettings.Production.json` (daha sessiz log seviyesi) katmanlı yapı.
- `Dockerfile`: multi-stage (SDK build → `aspnet:10.0` runtime), non-root `app` kullanıcısı, port 8080.
- `docker-compose.yml`: Redis (`:6379`) + Jaeger (`:16686` UI, `:4317` OTLP) — yerel geliştirme için.
- `.github/workflows/ci.yml`: her push/PR'da build + unit + integration testler (Testcontainers ile) + Docker image derleme doğrulaması.

## Test Kapsamı

- **Unit testler** (97): tüm Business servisleri (Product/Category/Order/OrderItem/User/Role/Auth) + `PermissionAuthorizationHandler`, EF Core InMemory + NSubstitute ile.
- **Integration testler** (9): gerçek SQL Server + Redis container'larına karşı — CRUD akışları, concurrency (409), auth flow (login/refresh/reuse), permission enforcement (403), rate limiting (429).



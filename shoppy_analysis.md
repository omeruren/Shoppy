# Shoppy .NET Proje Analizi — Principal Engineer Gözüyle

> **Analizi yapan:** 15+ yıl deneyimli Principal Engineer / .NET Solution Architect perspektifi
> **Proje:** Shoppy — ASP.NET Core 10 E-Ticaret API

---

## ⚠️ GÜNCELLEME NOTU (2026-07-02)

Bu doküman ilk yazıldığından beri projeye bir **permission (izin) sistemi** eklendi (`Permissions` sabitleri, `RolePermission` entity/tablosu, `PermissionRequirement`/`PermissionAuthorizationHandler`) ve kullanıcı self-servis DTO'ları (`ChangePasswordDto`, `UserProfileDto`, `UserUpdateSelfDto`) eklendi — ancak bu değişiklikler commit edilmeden yarım bırakılmıştı. Dokümanı güncel koda karşı tekrar doğrularken şunlar ortaya çıktı:

1. **Doküman bazı noktalarda artık güncel değil** — aşağıda ilgili bölümlerde **DURUM** etiketiyle işaretlendi:
   - `RefreshToken.Token` üzerinde artık **unique index var** (bkz. §3).
   - `reset-password` endpoint'i zaten `auth-fixed` rate limiter'ı route grubundan **miras alıyor** (bkz. §5) — orijinal bulgu yanlıştı.
2. **Proje şu anda derlenmiyordu** (bu oturumda düzeltildi — bkz. §0). `JwtProvider.CreateToken` imzası `(User, roles, permissions)` olacak şekilde değiştirilmiş ama `AuthService` hâlâ 2 parametreyle çağırıyordu (`dotnet build` ile doğrulanan CS7036 hatası). Yeni permission sistemi tamamen bağlı değildi: handler DI'a kayıtlı değildi, hiçbir yerde kullanıcının rollerinden permission listesi hesaplanmıyordu, hiçbir endpoint `PermissionRequirement` kullanmıyordu, `RolePermissions` tablosu hiç seed edilmiyordu. Ayrıca `UserModule` tüm admin endpoint'lerinde `.RequireRateLimiting("Admin")` çağırıyordu (yetkilendirme değil, rate-limiter — ve "Admin" diye kayıtlı bir rate-limiter policy'si de yok) → bu endpoint'ler hem **kimlik doğrulamasız**, hem de **çağrıldığında exception fırlatıyordu**.
3. Bu oturumda kapsam: **Faz 0** (build fix + permission sisteminin gerçekten çalışır hale getirilmesi) + **Faz 1** (aşağıdaki kritik bug listesi). Faz 2-4 (Repository pattern, ICacheService, Redis, CQRS, Docker/CI) bu oturuma dahil değil, olduğu gibi bırakıldı.
4. Secrets (JWT/SMTP) için: sadece **ileriye dönük düzeltme** yapıldı (yeni değerler üretilip `dotnet user-secrets`'a taşındı). Git geçmişi temizlenmedi — bu, ayrı ve açık onay gerektiren yıkıcı bir işlem (history rewrite + force-push).

---

## 0. P0 — BUILD BREAK & PERMISSION SİSTEMİ (Bu oturumda bulundu)

### 🔴 P0 — Proje Derlenmiyordu

```
IAuthService.cs(52,42): error CS7036: There is no argument given that corresponds
to the required parameter 'permissions' of 'JwtProvider.CreateToken(User, List<string?>, List<string>)'
IAuthService.cs(105,42): error CS7036: ... aynı hata
```

`JwtProvider.CreateToken` 3 parametre istiyor (permission claim'lerini token'a gömmek için), ama `AuthService.LoginAsync`/`RefreshTokenAsync` hâlâ 2 parametreyle çağırıyordu.

**DURUM:** ✅ Düzeltildi — `AuthService` artık kullanıcının rollerine ait `RolePermissions` kayıtlarından permission listesini hesaplayıp `CreateToken`'a 3. parametre olarak geçiyor. `dotnet build src/Shoppy/Shoppy.slnx` 0 hata / 0 uyarı ile başarılı.

---

### 🔴 P0 — Permission Sistemi Hiç Bağlı Değildi

- `PermissionAuthorizationHandler`, `IAuthorizationHandler` olarak hiçbir yerde (`BusinessRegistrar`, `Program.cs`) DI'a kayıtlı değildi.
- `Permissions.GetAll()` / `GetAdminPermissions()` / `GetCustomerPermissions()` hiçbir yerden çağrılmıyordu — `RolePermissions` tablosu asla seed edilmiyordu.
- Hiçbir endpoint `PermissionRequirement` veya permission-bazlı bir policy kullanmıyordu.
- Yeni self-servis metotları (`GetProfileAsync`/`UpdateSelfAsync`/`ChangePasswordAsync`) `UserModule`'de hiçbir endpoint'e bağlı değildi — API'den erişilemezdi.

**DURUM:** ✅ Düzeltildi — handler singleton olarak kayıtlı, `Permissions.GetAll()`'daki her permission için bir authorization policy kayıtlı, uygulama açılışında Admin/Customer rolleri + `RolePermissions` yeni bir `RolePermissionSeeder` ile seed ediliyor, tüm modüllerde ilgili permission policy'leri uygulanıyor, `GET/PUT /api/v1/users/me` ve `POST /api/v1/users/me/change-password` endpoint'leri eklendi. Ayrıca bu çalışma sırasında `ApplicationDbContext.SaveChangesAsync`'te gizli bir NRE riski bulundu ve düzeltildi (`_httpContextAccessor?.HttpContext.User` → `?.HttpContext?.User`) — HTTP isteği dışında (ör. seeder) `SaveChangesAsync` çağrıldığında çökme riski vardı, testler NSubstitute'ün `HttpContext`'i otomatik taklit etmesi nedeniyle bunu yakalamıyordu.

---

### 🔴 P0 — `UserModule`: Yetkilendirme Yerine Bozuk Rate-Limiter Çağrısı

```csharp
app.MapGet(string.Empty, async (...) => { ... }).RequireRateLimiting("Admin");
```

`"Admin"` `Program.cs`'de kayıtlı bir rate-limiter policy'si değil (`Program.cs`'de sadece `"fixed"` ve `"auth-fixed"` var). Sonuç: (a) tüm admin User CRUD endpoint'leri **kimlik doğrulamasından tamamen muaf**; (b) istek geldiğinde `InvalidOperationException` fırlatıyor (policy bulunamadı).

**DURUM:** ✅ Düzeltildi — `RequireRateLimiting("Admin")` çağrıları kaldırıldı, yerine ilgili `Permissions.Users.*` policy'leriyle `.RequireAuthorization(...)` kondu.

---

### 🟠 P0 — `RoleModule`: Sadece Kimlik Doğrulama, Yetki Kontrolü Yok

```csharp
.RequireAuthorization(); // policy adı yok — herhangi bir authenticated kullanıcı rol oluşturabilir/silebilir
```

**DURUM:** ✅ Düzeltildi — her endpoint artık kendi `Permissions.Roles.*` policy'sini talep ediyor.

---

### 🟠 P0 — `ApplicationDbContext.SaveChangesAsync`: Gizli NRE Riski (bu oturumda bulundu)

```csharp
if (_httpContextAccessor?.HttpContext.User?.Identity?.IsAuthenticated == true)
```

`?.` yalnızca `_httpContextAccessor`'ı null'a karşı koruyor; `.HttpContext` sonrasında düz `.User` erişimi var. HTTP isteği dışında (startup seeding, background job, hosted service) `HttpContext` gerçekten null olduğunda bu satır `NullReferenceException` fırlatır. Mevcut testler bunu yakalamıyordu çünkü `NSubstitute`'ün `Substitute.For<IHttpContextAccessor>()`'ı, `HttpContext`'i null yerine otomatik bir substitute ile dolduruyor (recursive/auto substitution) — yani test double'lar gerçek runtime davranışını maskeliyordu.

**DURUM:** ✅ Düzeltildi — `?.HttpContext?.User` olarak güncellendi. Bu düzeltme, permission seed mekanizmasının (aşağıda) startup'ta `SaveChangesAsync` çağırabilmesi için ön koşuldu.

---

## 1. MİMARİ İNCELEME

### Katman Yapısı

```
Shoppy.Entity       → Domain modeller, BaseEntity
Shoppy.DataAccess   → EF Core, DbContext, Configurations
Shoppy.Business     → Services, DTOs, Validators, Permissions
Shoppy.WebAPI       → Minimal API, Carter Modules
test/               → UnitTests, IntegrationTests
```

### ✅ İyi Yapılanlar

- Carter ile Minimal API modüler endpoint organizasyonu
- Global exception handler + ProblemDetails (RFC 7807)
- API Versioning (Asp.Versioning)
- OpenTelemetry entegrasyonu (tracing + metrics)
- Serilog + CorrelationId middleware
- Health checks + rate limiting
- Soft delete pattern (query filters + audit fields)
- Fluent Validation + endpoint filter entegrasyonu
- JWT + Refresh token mekanizması
- Password reset OTP ile kullanıcı enumeration koruması
- **(Yeni)** Permission-bazlı authorization (RolePermissions → JWT claim → `PermissionAuthorizationHandler`)

---

### 🔴 CRITICAL — Interface/Implementation Separation İhlali

**`IAuthService.cs` dosyası hem interface hem implementation içeriyor.**

**DURUM:** 🔲 Açık — bu oturumda düzeltilecek (implementation `AuthService.cs`'e taşınacak).

---

### 🔴 CRITICAL — Static Mutable State: Thread Safety Riski

Her service'de şu pattern mevcut:

```csharp
// ProductService, OrderService, OrderItemService, CategoryService...
private static CancellationTokenSource _cacheResetToken = new();
```

**Neden problem:** `static` field tüm DI scope'ları arasında paylaşılır. Unit testlerde test izolasyonunu bozar.

**DURUM:** 🔲 Açık — bu oturumun kapsamı dışında (Faz 2, `ICacheService` abstraction gerektiriyor).

---

### 🟠 HIGH — Repository Pattern Kullanılmıyor

Service sınıfları doğrudan `ApplicationDbContext` ve `DbSet<T>` kullanıyor. **DURUM:** 🔲 Açık — Faz 2 kapsamında (mimari refactor, bu oturuma dahil değil).

---

### 🟠 HIGH — Business Katmanı DataAccess'e Doğrudan Referans Veriyor

`Business → DataAccess` bağımlılığı, Clean Architecture'da ters olmalı. **DURUM:** 🔲 Açık — Faz 2/3 kapsamında.

---

### 🟠 HIGH — Unit of Work Eksik

**DURUM:** 🔲 Kısmen geçersiz — `AuthService.RefreshTokenAsync`'deki revoke+add işlemi tek bir `SaveChangesAsync` çağrısında yapılıyor; EF Core tek bir `SaveChanges` çağrısını zaten örtük bir transaction'a sarar, yani bu spesifik akış atomik. Genel olarak Unit of Work / explicit transaction yönetimi katmanı yine de yok (birden fazla `SaveChangesAsync` çağrısı gerektiren senaryolar için) — Faz 2/3 kapsamında kalıyor.

---

### 🟡 MEDIUM — CQRS Uygulanmalı

**DURUM:** 🔲 Açık — Faz 2/3 kapsamında.

---

### 🟡 MEDIUM — Domain Logic Entity'lerde Eksik / Tutarsız

**DURUM:** 🔲 Açık — Faz 3/4 kapsamında.

---

## 2. KOD KALİTESİ ANALİZİ

### 🔴 CRITICAL — Açık Bug: `ProductService.DeleteAsync` Yanlış Mesaj

```csharp
public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
{
    // ... başarılı silme işlemi ...
    return "Product not found."; // ← BUG! Başarılı silmede hata mesajı dönüyor
}
```

**DURUM:** ✅ Düzeltildi — `return "Product deleted.";`

---

### 🔴 CRITICAL — Magic Strings Her Yerde

**DURUM:** 🔲 Açık — Faz 2 kapsamında (`ErrorMessages` static class, bu oturuma dahil değil).

---

### 🔴 CRITICAL — Credentials Açık `appsettings.json`'da

```json
{
  "Jwt": { "SecretKey": "eafb04d9df8bc22183c874af3104247dd7a12070b0b0f44eb5d25190bb8be13b" },
  "EmailSettings": { "Password": "0b675d506ae636" },
  "ConnectionStrings": { "SqlServer": "Data Source=OMER;..." }
}
```

**DURUM:** 🔲 Açık — bu oturumda ileriye dönük düzeltilecek (yeni bir JWT secret'ı üretilip mevcut SMTP şifresiyle birlikte `dotnet user-secrets`'a taşınacak, `appsettings.json`'daki değerler placeholder ile değiştirilecek, SQL `Data Source` generic hale getirilecek). **Not:** eski değerler git geçmişinde kalacak — geçmişi temizlemek (BFG/`git filter-repo` + force-push) ayrı, yıkıcı ve açık onay gerektiren bir işlem; bu oturumda yapılmayacak.

---

### 🟠 HIGH — Duplicate Code: Cache Pattern 4 Serviste Tekrarlanıyor

**DURUM:** 🔲 Açık — Faz 2 kapsamında.

---

### 🟠 HIGH — Tutarsız Error Handling (Endpoint Seviyesinde)

**DURUM:** 🔲 Açık — Faz 2 kapsamında (`Result<T>.ToHttpResult()` extension, bu oturuma dahil değil).

---

### 🟡 MEDIUM — `OrderUpdateDto` DTO ile Servis Tutarsızlığı — GENİŞLETİLMİŞ BULGU

Orijinal bulgu bunun "DTO var ama Items hiç güncellenmiyor" olduğunu söylüyordu. Bu oturumdaki denetimde daha ciddi bir şey ortaya çıktı:

`OrderService.UpdateAsync`, `order`'ı `_orders.FindAsync([request.Id], ...)` ile **`.Include(o => o.Items)` olmadan** yüklüyor, sonra `request.Adapt(order)` çağırıyor. Mapster, `order.Items` boş/yüklenmemiş olduğu için DTO'lardan **yepyeni bir `List<OrderItem>`** oluşturup navigation property'ye atıyor — EF bunları mevcut satırlarla eşleştirmek yerine yeni/detached entity olarak görüyor. Sonuç: `Items` içeren bir update çağrısı mevcut kalemleri güncellemek yerine **yinelenen/öksüz satırlar** oluşturma riski taşıyor.

**DURUM:** 🔲 Açık — bu oturumun onaylanan kapsamı dışında bırakıldı (davranış değişikliği + veri düzeltme stratejisi gerektiriyor), Faz 2'ye ertelendi.

---

### 🟡 MEDIUM — Typo: `cancelllationToken` (3 l)

**DURUM:** 🔲 Açık — bu oturumda düzeltilecek (`Shoppy.WebAPI/Modules/*.cs` içindeki 8 kullanım).

---

### 🟡 MEDIUM — `OrderItemConfiguration` Yanlış Dosyada + Typo

**DURUM:** 🔲 Açık — bu oturumda düzeltilecek (`OrdertemConfiguration` → `OrderItemConfiguration` yeniden adlandırılıp kendi dosyasına taşınacak).

---

## 3. ENTITY FRAMEWORK CORE ANALİZİ

### ✅ İyi

- `IEntityTypeConfiguration<T>` ile ayrılmış konfigürasyonlar
- `HasQueryFilter` ile global soft-delete
- `RowVersion` ile optimistic concurrency
- `SaveChangesAsync` override ile audit fields (CreatedAt, UpdatedBy vb.)
- `AsNoTracking()` read-only sorgularda

---

### ✅ ÇÖZÜLMÜŞ (Doküman Güncel Değildi) — `RefreshToken.Token` Index

Orijinal bulgu `RefreshToken.Token` üzerinde index olmadığını söylüyordu. **Bu artık doğru değil** — `RefreshTokenConfiguration.cs` içinde hem `HasIndex(rt => rt.Token).IsUnique()` hem `HasIndex(rt => rt.UserId)` mevcut. Bu bulgu kapatılmış olarak işaretleniyor, herhangi bir kod değişikliği gerekmedi.

---

### 🟠 HIGH — SearchTerm Gerçekte Çalışmıyor

`PaginationRequestDto` içinde `SearchTerm` var ve cache key'e dahil, ancak **hiçbir servis** bu field'a göre filtreleme yapmıyor.

**DURUM:** 🔲 Açık — Faz 2 kapsamında, bu oturuma dahil değil.

---

### 🟡 MEDIUM — Soft Delete Cascade Tutarsızlığı — GENİŞLETİLMİŞ BULGU

Orijinal bulgu genel bir gözlemdi. Bu oturumda kök neden tam olarak izlendi:

- `OrderConfiguration`/`OrderItemConfiguration` her ikisi de `HasQueryFilter(x => !x.IsDeleted)` uyguluyor.
- `OrderConfiguration`: `HasMany(o => o.Items)...OnDelete(DeleteBehavior.Cascade)` — bu bir **DB-seviyesi FK cascade**, sadece gerçek bir SQL `DELETE` çalıştığında tetiklenir.
- `ApplicationDbContext.SaveChangesAsync` her `EntityState.Deleted` durumunu yakalayıp fiziksel `DELETE` yerine soft-delete `UPDATE`'e çeviriyor.
- **Sonuç:** `OrderService.DeleteAsync`, `_orders.Remove(order)` çağırıyor → interceptor bunu sadece Order'ın soft-delete'ine çeviriyor → SQL `ON DELETE CASCADE` hiç tetiklenmiyor → `OrderItem` satırları `IsDeleted = false` olarak kalıyor → silinmiş bir Order'a ait "aktif" görünen öksüz `OrderItem`'lar oluşuyor (`OrderItemService` bunları Order'ın durumuna bakmadan, sadece kendi `IsDeleted` filtresiyle sorguluyor).

**DURUM:** 🔲 Açık — bu oturumun onaylanan kapsamı dışında bırakıldı (soft-delete interceptor'ında cascade mantığı eklenmesi gerekiyor), Faz 2'ye ertelendi.

---

### 🟡 MEDIUM — Transaction Yönetimi Eksik

**DURUM:** 🔲 Kısmen geçersiz — bkz. §1 "Unit of Work Eksik" güncellemesi. `AuthService.RefreshTokenAsync` tek `SaveChangesAsync` çağrısı olduğu için zaten atomik.

---

## 4. API TASARIMI ANALİZİ

### ✅ İyi

- API versioning (URL segment + header)
- Rate limiting (genel + auth için ayrı)
- Carter ile modüler endpoint organizasyonu
- ProblemDetails (RFC 7807)
- FluentValidation + endpoint filter
- Correlation ID
- **(Yeni)** Permission-bazlı authorization tüm modüllerde tutarlı şekilde uygulanıyor

---

### 🟠 HIGH — PUT Endpoint REST Anti-Pattern

```csharp
app.MapPut(string.Empty, ...) // URL: /api/v1/orders — ID URL'de yok!
```

**DURUM:** 🔲 Açık — bu oturumun onaylanan kapsamı dışında (tüm modüllerde route + client sözleşmesi değişikliği gerektiriyor), Faz 2'ye bırakıldı.

---

### 🟠 HIGH — Inconsistent HTTP Status Codes

**DURUM:** 🔲 Açık — Faz 2 kapsamında (`Result<T>.ToHttpResult()`).

---

### 🟡 MEDIUM — CORS Origin'leri Hardcoded

**DURUM:** 🔲 Açık — bu oturuma dahil değil.

---

### 🟡 MEDIUM — Sorting Desteği Yok

**DURUM:** 🔲 Açık — Faz 3 kapsamında.

---

## 5. GÜVENLİK ANALİZİ

### 🔴 CRITICAL — JWT Secret Key Git'te (OWASP A02)

**DURUM:** 🔲 Açık — bu oturumda ileriye dönük düzeltilecek, bkz. §2 "Credentials Açık appsettings.json'da".

### 🔴 CRITICAL — `AllowedHosts: "*"` Production Tehlikesi

**DURUM:** 🔲 Açık — ortam bazlı konfigürasyon stratejisi gerektiriyor, bu oturuma dahil değil.

---

### 🟠 HIGH — ResetPassword: Expiry Kontrolü Yok

```csharp
if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCode != request.Code)
    return Failure(400, "Invalid reset code.");
// ← PasswordResetCodeExpires < DateTimeOffset.UtcNow kontrolü YOK!
```

**DURUM:** ✅ Düzeltildi — `PasswordResetCodeExpires` kontrolü eklendi; ayrıca başarılı reset sonrası kod artık `ClearPasswordResetCode()` ile temizleniyor (önceden başarılı bir reset'ten sonra bile kod süresi dolana kadar tekrar kullanılabilir durumdaydı).

---

### 🟠 HIGH — ResetPasswordAsync: Yanlış Error Variable

```csharp
var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
if (!addResult.Succeeded)
    return Failure(400, string.Join(", ", removeResult.Errors...));
    //                               ^^^^^^^^^^^ removeResult olmamalı, addResult olmalı!
```

**DURUM:** ✅ Düzeltildi — `addResult.Errors` kullanılıyor.

---

### ✅ ÇÖZÜLMÜŞ (Doküman Güncel Değildi) — `/reset-password` Rate Limiting

Orijinal bulgu `/reset-password`'un `auth-fixed` rate limiter'dan muaf olduğunu söylüyordu. **Bu doğru değil** — `AuthModule`'de route grubu seviyesinde `.RequireRateLimiting("auth-fixed")` tanımlı ve `reset-password` da dahil tüm auth endpoint'leri bunu miras alıyor. Herhangi bir kod değişikliği gerekmedi.

---

## 6. PERFORMANS ANALİZİ

### 🟠 HIGH — InMemory Cache Distributed Ortamda Ölçeklenmiyor

**DURUM:** 🔲 Açık — Faz 3 kapsamında (Redis/HybridCache).

### 🟠 HIGH — `WithPagination`: 2 Ayrı DB Sorgusu

**DURUM:** 🔲 Açık — bu oturuma dahil değil.

### 🟡 MEDIUM — `AnyAsync` + `SaveChangesAsync` Race Condition

`ProductService.CreateAsync` hâlâ `AnyAsync` existence-check + `SaveChangesAsync` yapıyor, `DbUpdateException` yakalanmıyor (unique index var, yani race durumunda unhandled 500 dönebilir).

**DURUM:** 🔲 Açık — bu oturumun onaylanan kapsamı dışında, Faz 2/3'e bırakıldı.

---

## 7. LOGGING VE MONITORING

### ✅ İyi

- Serilog structured logging
- CorrelationId her log satırına
- OpenTelemetry tracing + metrics
- Health checks (SqlServer)
- 5xx → LogError, 4xx → LogWarning ayrımı

---

### 🟡 MEDIUM — Serilog Enricher Typo

```json
"Enrich": ["FromLogContext", "WithMachineName", "WithTreadId"]
//                                                ^^^^^^ "WithThreadId" olmalı
```

**DURUM:** 🔲 Açık — bu oturumda düzeltilecek (typo giderilip `Serilog.Enrichers.Thread` paket referansı eklenecek — önceden zaten yok, eklenmezse enricher sessizce no-op kalır).

### 🟡 MEDIUM — OpenTelemetry Sadece Console'a Yazıyor

**DURUM:** 🔲 Açık — Faz 3 kapsamında.

### 🟡 MEDIUM — Business Logic Katmanında Sıfır Logging

**DURUM:** 🔲 Açık — Faz 3 kapsamında.

---

## 8. TEST ALTYAPISI

| Alan | Kapsam |
|------|--------|
| OrderService CRUD | ✅ Test var |
| OrderItemService CRUD | ✅ Test var |
| CategoryService CRUD | ✅ Test var |
| ProductService | ✅ Test var *(doküman güncel değildi — artık `test/Shoppy.UnitTests/Services/ProductServiceTests.cs` mevcut)* |
| AuthService | ❌ Hâlâ hiç yok |
| UserService | ❌ Hâlâ hiç yok |
| RoleService / PermissionAuthorizationHandler | ❌ Hâlâ hiç yok |
| Validators | ✅ Kısmi |
| Integration Tests | ✅ Başlangıç düzeyi |

### 🟠 HIGH — AuthService, UserService, RoleService Hiç Test Edilmemiş

En karmaşık iş mantığı (JWT, token rotation, OTP, password reset, permission hesaplama) hâlâ test yok. **DURUM:** 🔲 Açık — bu oturuma dahil değil, Faz 4 kapsamında.

### 🟡 MEDIUM — `static _cacheResetToken` Test İzolasyonunu Bozuyor

**DURUM:** 🔲 Açık.

---

## 9. PRODUCTION READINESS PUANI: 4 / 10 → 5 / 10

Şu anda proje derlenmiyor ve permission sistemi bağlı değil (bkz. §0) — bu oturumdaki Faz 0/1 çalışması tamamlandığında puan yükselecek, ancak secrets git geçmişinde kalacağı ve Docker/CI hâlâ olmayacağı için üretime hazırlık düşük kalmaya devam edecek.

| Risk | Durum |
|------|-------|
| Secrets git'te (geçmişte) | 🔴 Açık — bu oturumda ileriye dönük düzeltilecek, geçmiş temiz kalacak |
| Docker desteği | ❌ Yok |
| CI/CD pipeline | ❌ Yok |
| Migration stratejisi | ❓ Belirsiz |
| Environment ayrımı | 🟡 Eksik |
| Build durumu | ✅ Derleniyor (önceden derlenmiyordu) |
| Permission enforcement | ✅ Çalışıyor (önceden tamamen bağlı değildi) |

---

## 10. TEKNİK BORÇ TABLOSU

| Öncelik | Sorun | Etki | Durum |
|---------|-------|------|-------|
| 🔴 P0 | Proje derlenmiyordu (JwtProvider imza uyuşmazlığı) | Hiçbir şey build/run edilemiyor | ✅ Düzeltildi |
| 🔴 P0 | Permission sistemi hiç bağlı değil (handler kayıtsız, seed yok, endpoint yok) | Yeni özellik tamamen işlevsiz | ✅ Düzeltildi |
| 🔴 P0 | UserModule: `RequireRateLimiting("Admin")` yetkilendirme yerine kullanılmış | Admin endpoint'leri açık + runtime exception | ✅ Düzeltildi |
| 🟠 P0 | RoleModule: policy'siz `RequireAuthorization()` | Herhangi bir kullanıcı rol yönetebilir | ✅ Düzeltildi |
| 🔴 P0 | Secrets git'te | Güvenlik açığı | 🔲 Bu oturumda ileriye dönük düzeltilecek (geçmiş temiz kalacak) |
| 🔴 P0 | `ProductService.DeleteAsync` bug | Yanlış mesaj | ✅ Düzeltildi |
| 🔴 P0 | Interface+Implementation tek dosya | SOC ihlali | 🔲 Bu oturumda düzeltilecek |
| 🟠 P1 | ResetPassword expiry kontrolü yok + kod tekrar kullanılabiliyor | Güvenlik açığı | ✅ Düzeltildi |
| 🟠 P1 | removeResult → addResult bug | Yanlış hata mesajı | ✅ Düzeltildi |
| 🟡 P2 | WithTreadId typo | Thread ID loglanmıyor | 🔲 Bu oturumda düzeltilecek |
| 🟡 P2 | `cancelllationToken` typo (8 yer) | Kod kalitesi | 🔲 Bu oturumda düzeltilecek |
| 🟡 P2 | `OrdertemConfiguration` typo + yanlış dosya | Kod kalitesi | 🔲 Bu oturumda düzeltilecek |
| 🟠 P1 | Static cache token | Thread safety / test izolasyon | 🔲 Açık (Faz 2) |
| 🟠 P1 | SearchTerm çalışmıyor | Feature eksik | 🔲 Açık (Faz 2) |
| 🟠 P1 | RefreshToken.Token index yok | ~~Table scan~~ | ✅ Zaten çözülmüş (doküman güncel değildi) |
| 🟠 P1 | Duplicate cache pattern x4 | Bakım zorluğu | 🔲 Açık (Faz 2) |
| 🟡 P1 | `OrderService.UpdateAsync` Items reconciliation bozuk | Veri bütünlüğü riski (yinelenen/öksüz satır) | 🔲 Açık (Faz 2) — bu oturumda tespit edildi |
| 🟡 P1 | Order soft-delete, OrderItem'lara cascade olmuyor | Öksüz "aktif" OrderItem'lar | 🔲 Açık (Faz 2) — bu oturumda kök nedeni netleşti |
| 🟡 P2 | PUT endpoint URL'de ID yok | REST ihlali | 🔲 Açık (Faz 2) |
| 🟡 P2 | Magic strings | Bakım / test kırılganlığı | 🔲 Açık (Faz 2) |
| 🟡 P2 | CORS hardcoded | Configuration | 🔲 Açık |
| 🟡 P2 | InMemory cache | Ölçeklenemiyor | 🔲 Açık (Faz 3) |
| 🟡 P2 | Business'ta logging yok | Observability | 🔲 Açık (Faz 3) |
| 🟡 P2 | Docker desteği yok | Deployment | 🔲 Açık (Faz 4) |
| 🔵 P3 | Sorting desteği yok | UX | 🔲 Açık (Faz 3) |
| 🔵 P3 | OpenTelemetry console-only | Observability | 🔲 Açık (Faz 3) |
| 🔵 P3 | ProductService `AnyAsync`+`SaveChanges` race condition | Unhandled 500 riski | 🔲 Açık (Faz 2/3) |

---

## 11. ROADMAP

### 🔲 Faz 0 — Build Fix + Permission Sistemi (bu oturumda planlı)

- [x] `JwtProvider.CreateToken` çağrılarını 3 parametreye güncelle (permission hesaplama)
- [x] `PermissionAuthorizationHandler`'ı DI'a kaydet + her permission için policy tanımla
- [x] Admin/Customer rolleri + `RolePermissions` için seed mekanizması ekle
- [x] Tüm modüllerde (Product/Category/Order/OrderItem/Role/User) gerçek permission policy'lerini uygula
- [x] `UserModule`'e self-servis endpoint'leri ekle (`GET/PUT /me`, `POST /me/change-password`)

### 🔲 Faz 1 — Kritik Düzeltmeler (bu oturumda planlı)

- [x] `ProductService.DeleteAsync` bug düzelt → `"Product deleted."`
- [ ] `IAuthService.cs` → dosya ayırma
- [x] `ResetPassword` OTP expiry kontrolü ekle + kullanılan kodu temizle
- [x] `removeResult.Errors` → `addResult.Errors` bug düzelt
- [ ] `WithTreadId` → `WithThreadId` Serilog düzelt (+ paket referansı ekle)
- [ ] `OrdertemConfiguration` → `OrderItemConfiguration` rename + kendi dosyasına taşı
- [ ] `cancelllationToken` typo'larını düzelt
- [ ] Secrets'ı `appsettings.json`'dan çıkar (ileriye dönük — git geçmişi temizlenmeyecek)

**Beklenen Kazanım:** Proje derlenir hale gelecek, permission sistemi gerçekten çalışacak, güvenlik açıkları ve critical buglar kapanacak.

---

### 🟠 Faz 2 — Mimari İyileştirmeler (tahmini ~2 hafta, bu oturuma dahil değil)

- [ ] `ICacheService` abstraction (static field kaldır, duplicate code temizle)
- [ ] `ErrorMessages` static class (magic strings)
- [ ] `Result<T>.ToHttpResult()` extension method (tutarlı response)
- [ ] SearchTerm filter implementasyonu (tüm GetAll)
- [ ] PUT endpointlerde `{id}` URL parametresi
- [ ] `OrderService.UpdateAsync` Items reconciliation'ı düzelt (bu oturumda tespit edildi)
- [ ] Order soft-delete → OrderItem cascade mantığını `SaveChangesAsync` interceptor'ına ekle (bu oturumda tespit edildi)
- [ ] `ProductService.CreateAsync` TOCTOU: `DbUpdateException` yakalanıp 409'a çevrilmeli
- [ ] Repository pattern veya CQRS + MediatR değerlendirmesi

**Beklenen Kazanım:** API tutarlılığı, test edilebilirlik artışı, veri bütünlüğü.

---

### 🟡 Faz 3 — Performans ve Güvenlik (tahmini ~2 hafta)

- [ ] Redis / HybridCache geçişi
- [ ] Dinamik sorting desteği
- [ ] Refresh token family tracking
- [ ] Business logic katmanına `ILogger` inject
- [ ] OpenTelemetry OTLP exporter (Grafana/Jaeger)

**Beklenen Kazanım:** Production performansı, güvenlik olgunluğu.

---

### 🔵 Faz 4 — Kurumsal Seviye Hazırlık (tahmini ~4 hafta)

- [ ] `Dockerfile` + `docker-compose.yml`
- [ ] GitHub Actions CI/CD pipeline
- [ ] `AuthService`, `UserService`, `RoleService` unit testleri
- [ ] Integration test genişletme (auth flow, permission enforcement, rate limiting)
- [ ] Environment-specific appsettings stratejisi
- [ ] Git geçmişinden secret temizliği (BFG/`git filter-repo` + force-push — ayrı onay gerektirir)
- [ ] OWASP security review

**Beklenen Kazanım:** Production deployment hazırlığı, enterprise kalite.

---

## SONUÇ PUANLAMASI

| Kategori | Puan (10 üzerinden) — Şu an | Hedef (Faz 0+1 tamamlanınca) |
|----------|----------------------------|-------------------------------|
| **Mimari** | 6.5 | 6.5 *(değişmeyecek, Faz 2 bekliyor)* |
| **Kod Kalitesi** | 6.0 | 7.0 *(build fix + critical buglar kapanacak)* |
| **Güvenlik** | 5.0 | 6.5 *(permission enforcement + secrets ileriye dönük düzeltilecek)* |
| **Performans** | 6.5 | 6.5 *(değişmeyecek)* |
| **Test Edilebilirlik** | 6.0 | 6.0 *(değişmeyecek — AuthService/UserService hâlâ test edilmiyor)* |
| **Bakım Kolaylığı** | 6.0 | 6.0 *(değişmeyecek)* |
| **Production Readiness** | 4.0 | 5.0 *(build çalışacak + auth gerçek olacak, ama Docker/CI/history cleanup hâlâ yok)* |

---

## SEVİYE DEĞERLENDİRMESİ

Orijinal değerlendirme (Mid-Level → Senior arası) büyük ölçüde geçerliliğini koruyor. Bu oturumda ortaya çıkan ek gözlem: yarım bırakılmış, commit edilmemiş ve **derlenmeyen** bir özellik dalı (permission sistemi) — bu, "bitirilmemiş iş"in gerçek bir production ortamında ne kadar tehlikeli olabileceğinin iyi bir örneği. Kodun kendisi (bir kere tamamlandığında) sağlam bir tasarım gösteriyor: `RolePermission` şeması, claim-bazlı `PermissionAuthorizationHandler`, self-servis DTO ayrımı (email admin-only, diğer alanlar self-editable) hepsi makul kararlar. Asıl eksik olan, iş bitmeden commit/deploy edilmemesi disiplini.

---

## "GITHUB PORTFÖYÜ OLARAK PAYLAŞSAYDIM?" SORUSUNA YANIT

Orijinal değerlendirme geçerliliğini koruyor (bkz. altındaki dört kırmızı bayrak), ek olarak:

5. **Derlenmeyen kod commit edilmemiş olsa da working tree'de bırakılmış.** Bir code review'da veya CI'da yakalanacak en temel şey budur — "çalışmayan dal" disiplini production'a çıkmadan önce mutlaka kapatılmalı.

**Önerim (güncellendi):** Faz 0 + Faz 1 bu oturumda planlanıp uygulanıyor. Faz 2 (mimari) + Faz 4'teki `AuthService`/`UserService` testleri + Docker/CI eklenip git geçmişindeki secret'lar temizlendiğinde bu proje güçlü bir Senior .NET Developer portföyü haline gelir.

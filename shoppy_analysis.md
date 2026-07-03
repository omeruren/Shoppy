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

**DURUM:** ✅ İleriye dönük düzeltildi — `dotnet user-secrets init` çalıştırıldı, yeni bir JWT secret'ı üretilip mevcut SMTP şifresiyle birlikte `dotnet user-secrets`'a taşındı, `appsettings.json`'daki değerler placeholder ile değiştirildi, SQL `Data Source` generic (`.`) hale getirildi. Uygulama başlatılıp `/health` ile SQL Server bağlantısının hâlâ çalıştığı doğrulandı. **Not:** eski değerler git geçmişinde kaldı — geçmişi temizlemek (BFG/`git filter-repo` + force-push) ayrı, yıkıcı ve açık onay gerektiren bir işlem; bu oturumda yapılmadı.

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

**DURUM:** ✅ Düzeltildi — `OrderModule.cs` ve `ProductModule.cs` içindeki 8 kullanım da düzeltildi.

---

### 🟡 MEDIUM — `OrderItemConfiguration` Yanlış Dosyada + Typo

**DURUM:** ✅ Düzeltildi — `OrdertemConfiguration` → `OrderItemConfiguration` olarak yeniden adlandırıldı ve kendi dosyasına taşındı. Saf rename olduğu için (şema değişikliği yok, EF konfigürasyonları interface üzerinden keşfediyor) yeni bir migration gerekmedi.

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

**DURUM:** ✅ İleriye dönük düzeltildi, bkz. §2 "Credentials Açık appsettings.json'da".

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

**DURUM:** ✅ Düzeltildi — typo giderildi, `Serilog.Enrichers.Thread` paket referansı eklendi. Denetim sırasında `WithMachineName`'in de aynı şekilde bozuk olduğu ortaya çıktı — `Serilog.Enrichers.Environment` paketi de hiç referans edilmiyordu, yani bu enricher da sessizce no-op kalıyordu; o paket referansı da eklendi.

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
| AuthService | ✅ Test var *(Faz 3'te eklendi — `AuthServiceTests.cs`: login/family başlatma, rotation, reuse-detection)* |
| UserService | ❌ Hâlâ hiç yok |
| RoleService / PermissionAuthorizationHandler | ❌ Hâlâ hiç yok |
| Validators | ✅ Kısmi |
| Integration Tests | ✅ Başlangıç düzeyi |

### 🟠 HIGH — UserService, RoleService Hiç Test Edilmemiş

`AuthService` Faz 3'te (refresh token family tracking ile birlikte) test edildi. `UserService`/`RoleService` hâlâ test yok. **DURUM:** 🔲 Açık — bu oturuma dahil değil, Faz 4 kapsamında.

### 🟡 MEDIUM — `static _cacheResetToken` Test İzolasyonunu Bozuyor

**DURUM:** 🔲 Açık.

---

## 9. PRODUCTION READINESS PUANI: 4 / 10 → 5 / 10

Bu oturumdaki Faz 0/1 çalışması tamamlandı: build çalışıyor, permission sistemi gerçekten çalışıyor. Ancak secrets git geçmişinde kaldığı ve Docker/CI hâlâ olmadığı için üretime hazırlık düşük kalmaya devam ediyor.

| Risk | Durum |
|------|-------|
| Secrets git'te (geçmişte) | 🟠 İleriye dönük düzeltildi, geçmiş temiz değil |
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
| 🔴 P0 | Secrets git'te | Güvenlik açığı | ✅ İleriye dönük düzeltildi (geçmiş temiz değil) |
| 🔴 P0 | `ProductService.DeleteAsync` bug | Yanlış mesaj | ✅ Düzeltildi |
| 🔴 P0 | Interface+Implementation tek dosya | SOC ihlali | 🔲 Bu oturumda düzeltilecek |
| 🟠 P1 | ResetPassword expiry kontrolü yok + kod tekrar kullanılabiliyor | Güvenlik açığı | ✅ Düzeltildi |
| 🟠 P1 | removeResult → addResult bug | Yanlış hata mesajı | ✅ Düzeltildi |
| 🟡 P2 | WithTreadId typo (+ WithMachineName da aynı sebeple bozuktu) | Thread ID / MachineName loglanmıyor | ✅ Düzeltildi |
| 🟡 P2 | `cancelllationToken` typo (8 yer) | Kod kalitesi | ✅ Düzeltildi |
| 🟡 P2 | `OrdertemConfiguration` typo + yanlış dosya | Kod kalitesi | ✅ Düzeltildi |
| 🟠 P1 | Static cache token | Thread safety / test izolasyon | ✅ Düzeltildi (`ICacheService` singleton) |
| 🟠 P1 | SearchTerm çalışmıyor | Feature eksik | ✅ Düzeltildi (Product/Category/Order/OrderItem) |
| 🟠 P1 | RefreshToken.Token index yok | ~~Table scan~~ | ✅ Zaten çözülmüş (doküman güncel değildi) |
| 🟠 P1 | Duplicate cache pattern x4 | Bakım zorluğu | ✅ Düzeltildi (`ICacheService`) |
| 🟡 P1 | `OrderService.UpdateAsync` Items reconciliation bozuk | Veri bütünlüğü riski (yinelenen/öksüz satır) | ✅ Düzeltildi |
| 🟡 P1 | Order soft-delete, OrderItem'lara cascade olmuyor | Öksüz "aktif" OrderItem'lar | ✅ Düzeltildi (`SaveChangesAsync` interceptor) |
| 🟡 P2 | PUT endpoint URL'de ID yok | REST ihlali | ✅ Düzeltildi (`{id}` route param) |
| 🟡 P2 | Magic strings | Bakım / test kırılganlığı | ✅ Düzeltildi (`ErrorMessages`) |
| 🟡 P2 | CORS hardcoded | Configuration | 🔲 Açık |
| 🟡 P2 | InMemory cache | Ölçeklenemiyor | 🔲 Açık (Faz 3) |
| 🟡 P2 | Business'ta logging yok | Observability | 🔲 Açık (Faz 3) |
| 🟡 P2 | Docker desteği yok | Deployment | 🔲 Açık (Faz 4) |
| 🔵 P3 | Sorting desteği yok | UX | 🔲 Açık (Faz 3) |
| 🔵 P3 | OpenTelemetry console-only | Observability | 🔲 Açık (Faz 3) |
| 🔵 P3 | ProductService `AnyAsync`+`SaveChanges` race condition | Unhandled 500 riski | ✅ Düzeltildi (`DbUpdateException` → 409) |

---

## 11. ROADMAP

### ✅ Faz 0 — Build Fix + Permission Sistemi (bu oturumda tamamlandı)

- [x] `JwtProvider.CreateToken` çağrılarını 3 parametreye güncelle (permission hesaplama)
- [x] `PermissionAuthorizationHandler`'ı DI'a kaydet + her permission için policy tanımla
- [x] Admin/Customer rolleri + `RolePermissions` için seed mekanizması ekle
- [x] Tüm modüllerde (Product/Category/Order/OrderItem/Role/User) gerçek permission policy'lerini uygula
- [x] `UserModule`'e self-servis endpoint'leri ekle (`GET/PUT /me`, `POST /me/change-password`)

### ✅ Faz 1 — Kritik Düzeltmeler (bu oturumda tamamlandı)

- [x] `ProductService.DeleteAsync` bug düzelt → `"Product deleted."`
- [ ] `IAuthService.cs` → dosya ayırma
- [x] `ResetPassword` OTP expiry kontrolü ekle + kullanılan kodu temizle
- [x] `removeResult.Errors` → `addResult.Errors` bug düzelt
- [x] `WithTreadId` → `WithThreadId` Serilog düzelt (+ paket referansı ekle)
- [x] `OrdertemConfiguration` → `OrderItemConfiguration` rename + kendi dosyasına taşı
- [x] `cancelllationToken` typo'larını düzelt
- [x] Secrets'ı `appsettings.json`'dan çıkar (ileriye dönük — git geçmişi temizlenmedi)

**Beklenen Kazanım:** Proje derleniyor, permission sistemi gerçekten çalışıyor, güvenlik açıkları ve critical buglar kapandı.

---

### ✅ Faz 2 — Mimari İyileştirmeler (bu oturumda tamamlandı)

- [x] `ICacheService` abstraction (static field kaldır, duplicate code temizle)
- [x] `ErrorMessages` static class (magic strings)
- [x] `Result<T>.ToHttpResult()` extension method (tutarlı response)
- [x] SearchTerm filter implementasyonu (tüm GetAll)
- [x] PUT endpointlerde `{id}` URL parametresi
- [x] `OrderService.UpdateAsync` Items reconciliation'ı düzelt (bu oturumda tespit edildi)
- [x] Order soft-delete → OrderItem cascade mantığını `SaveChangesAsync` interceptor'ına ekle (bu oturumda tespit edildi)
- [x] `ProductService.CreateAsync` TOCTOU: `DbUpdateException` yakalanıp 409'a çevrilmeli
- [x] Repository pattern veya CQRS + MediatR değerlendirmesi (yazılı değerlendirme — bkz. aşağıdaki alt bölüm, kod değişikliği yapılmadı)

**Kazanım:** `ICacheService` singleton'ı 4 serviste tekrarlanan `static CancellationTokenSource` deseni yerine geldi; `ErrorMessages` tüm hardcoded hata string'lerini (ve "is already exists" gramer tutarsızlığını) merkezileştirdi; `ToHttpResult()` modüller arası tutarsız `Results.Conflict/StatusCode/NotFound/Problem` karışımını `result.StatusCode`'a güvenen tek bir mapping'e indirdi; SearchTerm artık Product/Category/Order/OrderItem'da gerçekten filtreliyor; PUT endpoint'leri `{id}` route parametresi alıyor (route id source of truth); `OrderService.UpdateAsync` artık `.Include(o => o.Items)` ile yükleyip Items'ı reconcile ediyor (whole-collection Mapster Adapt kaldırıldı — yinelenen/öksüz satır riski kapandı); `ApplicationDbContext.SaveChangesAsync` artık yüklü child collection'ları soft-delete cascade ediyor; `ProductService.CreateAsync`/`UpdateAsync` artık `DbUpdateException`'ı yakalayıp 409'a çeviriyor.

#### Faz 2 — Değerlendirme: Repository/CQRS

Bu oturumda kod değişikliği yapılmadı; sadece değerlendirme.

Bu boyuttaki bir projede (~6-8 kaynak: Product, Category, Order, OrderItem, Role, User, UserRole — her biri tek-aggregate CRUD, en fazla tek seviye join) ne Repository/Unit-of-Work ne de MediatR/CQRS için somut bir acı noktası gözlemlenmedi. Klasik Repository gerekçesi (persistence engine'in değiştirilebilir olması) burada geçerli değil — proje SQL Server + EF Core'a sıkı bağlı ve bunun değişmesi planlanmıyor. Klasik CQRS gerekçesi (read/write model ayrışması, karmaşık query fan-out) da yok — her `GetAllAsync` zaten basit bir sayfalanmış projeksiyon. Test edilebilirlik açısından da bir kazanç yok: `test/Shoppy.UnitTests/Services/*Tests.cs` zaten `ApplicationDbContext`'i EF InMemory provider'a karşı doğrudan kurup gerçek LINQ sorgu davranışını test ediyor; bir repository abstraction'ı bunu ölçülebilir şekilde iyileştirmez, sadece her aggregate için bir interface + implementasyon ekler. Bu faz zaten cross-cutting concern'lerin (cache) repository/mediator olmadan da temiz bir şekilde çözülebildiğinin kanıtı (`ICacheService`).

Business→DataAccess bağımlılık yönü (§1'de "Clean Architecture'da ters olmalı" olarak işaretlenmiş) bir Repository/UoW katmanıyla formalize edilebilirdi, ama bu proje ölçeğinde bu inversiyon büyük ölçüde törensel kalır — ikinci bir persistence teknolojisi veya gerçekten bağımsız bir domain katmanı ihtiyacı doğana kadar ertelenebilir.

**Öneri:** Faz 2 kapsamında Repository/CQRS'e geçilmedi, mevcut direkt-`DbContext` servis deseni korundu. Bu kararı şu durumlar oluşursa tekrar gözden geçirin: (a) tek bir işlemin birden fazla aggregate root'u tek servis metodu içine sığmayacak şekilde orkestre etmesi gerekiyorsa, (b) EF Core bağımlılığının gerçekten değiştirilebilir olması gerekiyorsa, (c) ekip büyüyüp per-service duplication (auth, logging, validation, caching) bir pipeline'ın ölçülebilir şekilde azaltacağı bir bakım yüküne dönüşüyorsa.

---

### ✅ Faz 3 — Performans ve Güvenlik (bu oturumda tamamlandı)

- [x] Redis / HybridCache geçişi (`ICacheService` artık `HybridCache` üzerinde; L1 in-process + Redis L2, `docker-compose.yml`'daki `redis` servisiyle)
- [x] Dinamik sorting desteği (allow-list edilmiş `SortBy`/`SortDirection`, `SortingExtension` üzerinden Product/Category/Order/OrderItem'da; Order/OrderItem ayrıca eksik olan default `OrderBy`'a kavuştu)
- [x] Refresh token family tracking (`FamilyId`/`ReplacedByToken`; reuse tespitinde tüm family revoke ediliyor — canlı ortamda uçtan uca doğrulandı)
- [x] Business logic katmanına `ILogger` inject (7 servisin tamamı — Auth/User/Role/Product/Order/OrderItem/Category)
- [x] OpenTelemetry OTLP exporter (Jaeger — `docker-compose.yml`'daki `jaeger` servisine gRPC 4317 üzerinden, Console exporter da korunuyor)

**Kazanım:** Cache artık gerçek bir dağıtık backend'e (Redis) yaslanıyor ve tag-bazlı invalidation kullanıyor (eski `CancellationTokenSource`-per-prefix hilesi kaldırıldı); `RoleService` da `IMemoryCache`'den `ICacheService`'e taşınarak son doğrudan `IMemoryCache` bağımlılığı kapandı. Sorting artık `sortBy`/`sortDirection` query param'larıyla çalışıyor ve cache key'e dahil. Refresh token rotation artık bir "family" zinciri tutuyor; çalınmış/tekrar kullanılmış bir token tespit edildiğinde zincirdeki TÜM token'lar (henüz süresi dolmamış olanlar dahil) anında iptal ediliyor — canlı sunucu üzerinde login→rotate→reuse akışı çalıştırılarak doğrulandı. Business katmanında daha önce sıfır olan logging artık auth başarısı/başarısızlığı, permission-relevant olaylar (özellikle token reuse) ve CRUD işlemleri için mevcut. OTLP exporter Jaeger'e gerçek trace gönderiyor (canlı ortamda `Shoppy.WebAPI` servisi Jaeger UI'da görüldü). `docker-compose.yml` bu oturumda ilk kez eklendi (Faz 4'ün bir parçası öne çekildi).

**Yeni testler:** `AuthServiceTests.cs` (daha önce hiç yoktu) — login yeni family başlatıyor, rotation aynı family'de kalıp `ReplacedByToken` set ediyor, reuse tüm family'yi revoke ediyor.

**Not:** `test/Shoppy.IntegrationTests/ApiIntegrationTests.cs`'de bu oturuma dahil olmayan, önceden var olan bir kurulum sorunu tespit edildi — test sınıfı `IClassFixture<CustomWebApplicationFactory>` implement etmiyor, bu yüzden xUnit `factory` constructor parametresini çözemiyor (4 test "did not have matching fixture data" ile başarısız oluyor, `master` üzerinde de aynı hata mevcut). Faz 3 kapsamı dışında bırakıldı.

---

### ✅ Faz 4 — Kurumsal Seviye Hazırlık (bu oturumda büyük ölçüde tamamlandı)

- [x] `docker-compose.yml` (Faz 3'te redis + jaeger servisleriyle eklendi)
- [x] `Dockerfile` (multi-stage: SDK build → `aspnet:10.0` runtime, non-root `app` user, `docker build` ile doğrulandı — bkz. aşağıda)
- [x] GitHub Actions CI/CD pipeline (`.github/workflows/ci.yml`: build + unit + integration testler + Docker image doğrulaması)
- [x] `UserService`, `RoleService` unit testleri (+ `PermissionAuthorizationHandler` testleri de eklendi — `AuthService` Faz 3'te eklenmişti)
- [x] Integration test genişletme (auth flow, permission enforcement, rate limiting) — ayrıca bu çalışma sırasında `ApiIntegrationTests`'in daha önce hiç çalışmadığı (bkz. aşağıda) ortaya çıktı ve düzeltildi
- [x] Environment-specific appsettings stratejisi (CORS + dev-only SQL connection string artık `appsettings.Development.json`'da, `appsettings.Production.json` eklendi)
- [ ] Git geçmişinden secret temizliği (BFG/`git filter-repo` + force-push — ayrı ve açık onay gerektiren yıkıcı bir işlem, bu oturuma dahil değil)
- [x] OWASP security review (bkz. §12 aşağıda)

**Kazanım:** `Dockerfile` gerçek bir `docker build` ile doğrulandı (image başarıyla derleniyor ve non-root kullanıcıyla çalışıyor). CI pipeline artık her push/PR'da build + tüm test suite'lerini (Testcontainers dahil) + Docker image derlemesini otomatik çalıştırıyor. `UserService`/`RoleService`/`PermissionAuthorizationHandler` için toplam 30 yeni unit test eklendi (62 → 92). CORS artık `Program.cs`'de hardcoded değil, `Cors:AllowedOrigins` konfigürasyonundan okunuyor (base'de güvenli varsayılan: boş liste; dev origin'leri `appsettings.Development.json`'da) — canlı preflight istekleriyle doğrulandı.

**Bu oturumda bulunan ek buglar (test genişletmesi sırasında ortaya çıktı):**
- `ApiIntegrationTests` sınıfı `IClassFixture<CustomWebApplicationFactory>` implement etmiyordu (Faz 3'te "önceden var olan, kapsam dışı" olarak not edilmişti) — düzeltildi, ama düzeltince ARKASINDA İKİ GERÇEK BUG daha ortaya çıktı:
  1. `CustomWebApplicationFactory.InitializeAsync`, EF migration'ını `Services` property'sine eriştikten SONRA çalıştırıyordu; ama `Services`'e erişmek `Program.cs`'in tamamını (içindeki `RolePermissionSeeder` dahil) başlatıyor — seeder henüz migrate edilmemiş bir şemaya karşı sorgu atıp patlıyordu. Migration artık host başlamadan önce, bağımsız bir `DbContext` üzerinden çalışıyor.
  2. Test helper'ı, `POST /api/v1/users`'ın anonim self-registration için çalıştığını varsayıyordu — ama bu endpoint `Users.Create` permission'ı gerektiriyor (uygulamada public bir kayıt endpoint'i hiç yok). Test kullanıcıları artık doğrudan `UserManager` üzerinden seed ediliyor.
- `auth-fixed` rate limiter policy'sinin global, partition'sız tek bir bucket olduğu (bkz. §12, madde A04/A05) test yazarken ortaya çıktı — fonksiyonel auth testleri için ayrı, gevşetilmiş limitli bir factory (`RelaxedAuthRateLimitWebApplicationFactory`) eklendi; gerçek rate-limit davranışı `RateLimitingIntegrationTests`'te production limitleriyle test ediliyor.

**Beklenen Kazanım:** Production deployment hazırlığı büyük ölçüde tamamlandı; kalan tek kapsam dışı madde git geçmişi secret temizliği (yıkıcı, ayrı onay gerektiriyor).

---

## 12. OWASP GÜVENLİK DEĞERLENDİRMESİ (Faz 4)

OWASP Top 10:2021 + API Security Top 10:2023 kategorilerine göre kod okunarak yapılan bir değerlendirme (otomatik tarama aracı kullanılmadı). Daha önceki fazlarda kapatılmış maddeler kısaca özetlenip yeni bulgulara odaklanıldı.

### ✅ İyi Durumda Olanlar (önceki fazlarda kapatıldı)

- **A01 Broken Access Control** — Permission-bazlı authorization tüm admin endpoint'lerinde tutarlı şekilde uygulanıyor (Faz 0); self-servis endpoint'ler (`/users/me*`) claim'den gelen kullanıcı id'sine göre scope'lanıyor, email değişikliği admin-only.
- **A03 Injection** — Tüm sorgular EF Core LINQ üzerinden parametrize ediliyor, hiçbir yerde raw SQL/string concatenation yok.
- **A04 Insecure Design (auth akışı)** — Refresh token rotation + family-bazlı reuse/theft detection (Faz 3), password reset OTP expiry + tek kullanımlık kod (Faz 1).
- **A07 Identification & Authentication (JWT)** — `JwtOptionsSetup` içinde issuer/audience/signing-key/lifetime validasyonlarının hepsi açık (`JwtOptions.cs:24-31`); refresh token'lar `RandomNumberGenerator` ile 64 byte kriptografik rastgelelik kullanıyor (`JwtProvider.cs:60-66`); access token TTL 1 saat, refresh token TTL 7 gün — makul.
- **A09 Logging & Monitoring** — Business katmanında `ILogger` (Faz 3), Serilog request logging + correlation id, OpenTelemetry OTLP → Jaeger tracing.

### 🔴 YENİ BULGU — Account Lockout Yapılandırılmış Ama Hiç Devrede Değil

`DataAccessRegistrar.cs:29-31` şunu yapılandırıyor:
```csharp
opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
opt.Lockout.MaxFailedAccessAttempts = 5;
opt.Lockout.AllowedForNewUsers = true;
```
Ama `AuthService.LoginAsync` (`AuthService.cs:35`) şifre kontrolünü doğrudan `_userManager.CheckPasswordAsync(user, request.Password)` ile yapıyor — bu metod lockout mekanizmasına hiç dokunmuyor. Identity'nin lockout'u normalde `SignInManager.PasswordSignInAsync` (veya elle `AccessFailedAsync`/`IsLockedOutAsync`/`ResetAccessFailedCountAsync` çağrıları) üzerinden çalışır; burada hiçbiri çağrılmıyor. **Sonuç: yapılandırılmış "5 başarısız denemede 15 dakika kilitle" korumasi tamamen ölü kod — sınırsız şifre denemesi mümkün** (auth-fixed rate limiter'ın genel hız sınırı dışında, hesap-bazlı hiçbir koruma yok).

**DURUM:** 🔲 Açık — bu oturumun kapsamı dışında (davranış değişikliği gerektiriyor), gelecek bir faza bırakıldı.

### 🟠 YENİ BULGU — `auth-fixed` Rate Limiter Global ve Partition'sız

`Program.cs`'deki `"auth-fixed"` policy (5 istek / 1 saniye) tüm `/api/v1/auth/*` endpoint'leri için **tek, paylaşılan, IP/kullanıcı bazında ayrılmamış bir sayaç** kullanıyor. Bu, entegrasyon testleri yazılırken (`RateLimitingIntegrationTests`) doğrulandı. Pratik sonucu: kötü niyetli veya hatalı davranan **tek bir client**, `/auth/login`'e saniyede 5'ten fazla istek atarak o saniyelik pencerede **uygulamadaki TÜM kullanıcıların** login/refresh/forgot-password/reset-password isteklerini 503 ile reddettirebilir — kendi kendine DoS. Doğru çözüm, `PartitionedRateLimiter` ile IP adresine (veya kimlik doğrulanmışsa kullanıcıya) göre partition'lamak.

**DURUM:** 🔲 Açık — davranış değişikliği + muhtemelen ek test gerektiriyor, gelecek faza bırakıldı.

### 🟡 YENİ BULGU — Rate Limit Reddi 503 Dönüyor, Semantik Olarak 429 Olmalı

`AddRateLimiter` çağrılarının hiçbirinde `RejectionStatusCode` ayarlanmamış, bu yüzden ASP.NET Core varsayılanı olan `503 Service Unavailable` kullanılıyor. HTTP semantiğine göre rate-limit reddi için doğru kod `429 Too Many Requests`'tir (503 "sunucu aşırı yüklü/bakımda" anlamına gelir ve istemcilere farklı bir retry stratejisi sinyali verir). Küçük, düşük riskli bir düzeltme: `RateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests`.

**DURUM:** 🔲 Açık.

### 🟡 YENİ BULGU — API Dokümantasyonu Ortam Ayrımı Olmadan Açık

`Program.cs`'de `app.MapOpenApi()` ve `app.MapScalarApiReference()` herhangi bir `IsDevelopment()` kontrolü olmadan çağrılıyor — yani `/openapi` (tam şema) ve `/scalar` (interaktif dokümantasyon UI) **Production'da da erişilebilir**. Bu doğrudan bir açık değil (endpoint'lerin kendisi hâlâ authorization gerektiriyor) ama saldırı yüzeyini büyütüyor: route/DTO şemasının tamamı kimlik doğrulaması olmadan keşfedilebilir hale geliyor.

**DURUM:** 🔲 Açık — üretimde bu endpoint'leri `IsDevelopment()` arkasına almak veya ayrı bir authorization policy ile korumak önerilir.

### 🟡 YENİ BULGU (API4:2023 Unrestricted Resource Consumption) — Pagination Sınırsız

`PaginationRequestDto.PageSize`/`PageNumber` üzerinde hiçbir üst sınır veya pozitiflik kontrolü yok (`PaginationExtension.cs`). Bir client `pageSize=1000000` göndererek tek istekte büyük bir DB round-trip'i + bellek baskısı yaratabilir.

**DURUM:** 🔲 Açık — `PageSize`'a makul bir üst sınır (ör. 100) eklenmesi önerilir.

### 🟡 DÜŞÜK ÖNCELİK — Refresh Token'lar DB'de Düz Metin Saklanıyor

`RefreshTokens.Token` kolonu, üretilen rastgele string'i doğrudan (hash'lenmeden) saklıyor. Token zaten 64 byte kriptografik rastgelelik taşıdığı için brute-force riski yok, ama bir DB dump'ı çalınırsa saldırgan ek bir işlem yapmadan doğrudan kullanılabilir refresh token'lara sahip olur (şifrelerin hash'lenmesiyle aynı savunma-derinliği mantığı). Düşük öncelik, ama üretim-sınıfı bir sistemde tokenlar da (SHA-256 gibi) hash'lenip DB'de hash'i tutulur, karşılaştırma hash üzerinden yapılır.

**DURUM:** 🔲 Açık, düşük öncelik.

### Önceden Bilinen, Hâlâ Açık Maddeler (tekrar aynı ayrıntıyla ele alınmadı, bkz. ilgili bölüm)

- **A02 Cryptographic Failures** — JWT/SMTP secret'ları git geçmişinde kalmaya devam ediyor (bkz. §2, §10) — temizlik ayrı onay gerektiren yıkıcı bir işlem.
- **A05 Security Misconfiguration** — `AllowedHosts: "*"` hâlâ açık (bkz. §5) — gerçek bir production hostname'i olmadan anlamlı bir değerle değiştirilemez, bu yüzden bu oturumda dokunulmadı.
- **A06 Vulnerable Components** — Bağımlılık güvenlik açığı taraması (ör. GitHub Dependabot alerts, `dotnet list package --vulnerable`) henüz repoya bağlı değil.

---

## SONUÇ PUANLAMASI

| Kategori | Puan (10 üzerinden) — Önce | Faz 0+1 sonrası | Faz 4 sonrası |
|----------|---------------------------|--------------------------|-----------------|
| **Mimari** | 6.5 | 6.5 | 7.0 *(Faz 2 mimari iyileştirmeleri — `ICacheService`, tutarlı hata yönetimi)* |
| **Kod Kalitesi** | 6.0 | 7.0 | 7.0 *(değişmedi)* |
| **Güvenlik** | 5.0 | 6.5 | 7.0 *(refresh token family/reuse detection + OWASP review ile somut, izlenebilir bir bulgu listesi çıkarıldı — bkz. §12; ama account lockout'un aslında hiç çalışmadığı gibi yeni, gerçek bir P1 bulgu da ortaya çıktı)* |
| **Performans** | 6.5 | 6.5 | 7.5 *(Redis/HybridCache, dinamik sorting — Faz 3)* |
| **Test Edilebilirlik** | 6.0 | 6.0 | 8.0 *(AuthService/UserService/RoleService/PermissionAuthorizationHandler artık test ediliyor — 92 unit + 9 integration test; entegrasyon test suite'i ayrıca gerçekten ÇALIŞIR hale getirildi, önceden hiç koşmuyordu)* |
| **Bakım Kolaylığı** | 6.0 | 6.0 | 6.5 *(env-specific appsettings, config-driven CORS/rate-limit)* |
| **Production Readiness** | 4.0 | 5.0 | 7.0 *(Dockerfile + CI pipeline + environment ayrımı tamamlandı; kalan gerçek eksik: git geçmişi secret temizliği + §12'deki açık güvenlik bulguları)* |

---

## SEVİYE DEĞERLENDİRMESİ

Orijinal değerlendirme (Mid-Level → Senior arası) büyük ölçüde geçerliliğini koruyor. Bu oturumda ortaya çıkan ek gözlem: yarım bırakılmış, commit edilmemiş ve **derlenmeyen** bir özellik dalı (permission sistemi) — bu, "bitirilmemiş iş"in gerçek bir production ortamında ne kadar tehlikeli olabileceğinin iyi bir örneği. Kodun kendisi (bir kere tamamlandığında) sağlam bir tasarım gösteriyor: `RolePermission` şeması, claim-bazlı `PermissionAuthorizationHandler`, self-servis DTO ayrımı (email admin-only, diğer alanlar self-editable) hepsi makul kararlar. Asıl eksik olan, iş bitmeden commit/deploy edilmemesi disiplini.

---

## "GITHUB PORTFÖYÜ OLARAK PAYLAŞSAYDIM?" SORUSUNA YANIT

Orijinal değerlendirme geçerliliğini koruyor (bkz. altındaki dört kırmızı bayrak), ek olarak:

5. **Derlenmeyen kod commit edilmemiş olsa da working tree'de bırakılmış.** Bir code review'da veya CI'da yakalanacak en temel şey budur — "çalışmayan dal" disiplini production'a çıkmadan önce mutlaka kapatılmalı.

**Önerim (güncellendi):** Faz 0 + Faz 1 bu oturumda tamamlandı. Faz 2 (mimari) + Faz 4'teki `AuthService`/`UserService` testleri + Docker/CI eklenip git geçmişindeki secret'lar temizlendiğinde bu proje güçlü bir Senior .NET Developer portföyü haline gelir.

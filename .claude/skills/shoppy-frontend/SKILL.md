---
name: shoppy-frontend
description: |
  Shoppy e-ticaret backend API'si (ASP.NET Core 10, JWT + Refresh Token Rotation, Permission-bazlı yetkilendirme) ile tam entegre çalışacak
  React + TypeScript frontend uygulaması geliştirme kılavuzu.
  API kontratları, kimlik doğrulama akışları, hata yönetimi, DTO yapıları ve tasarım standartlarını kapsar.
---

# Shoppy Frontend — Geliştirme Kılavuzu (Skill)

> Bu doküman backend kaynak kodundan bire bir çıkarılmıştır.
> Herhangi bir API detayı şüphe duyulduğunda buradaki bilgiler referans alınmalıdır.

---

## 1. Teknoloji Yığını

| Katman | Teknoloji | Gerekçe |
|---|---|---|
| Build & Dev Server | **Vite 6+** | Hızlı HMR, optimize edilmiş production build |
| Framework | **React 18+** (TypeScript strict mode) | Bileşen bazlı mimari |
| Routing | **React Router v7** (data API'leri) | Nested routing, loader/action desteği |
| Server State | **TanStack Query v5** | Otomatik cache, retry, tag-bazlı invalidation |
| Client State | **Zustand** | Auth state, sepet, tema — minimal boilerplate |
| Stil | **Tailwind CSS v4** | Utility-first, dark mode, responsive |
| UI Primitives | **Radix UI** veya **Shadcn/UI** | Erişilebilirlik, headless bileşenler |
| HTTP | **Axios** | Interceptor desteği, request/response transform |
| Form | **React Hook Form + Zod** | Şema tabanlı client-side validasyon |
| Animasyon | **Framer Motion** | Sayfa geçişleri, mikro-etkileşimler |
| İkon | **Lucide React** | Tutarlı, hafif SVG ikon seti |

---

## 2. Backend API Kontratı — Tam Referans

### 2.1. Genel Bilgiler

| Özellik | Değer |
|---|---|
| Base URL (Development) | `http://localhost:5176` veya `http://localhost:5226` |
| API Prefix | `/api/v1/` |
| Versiyonlama | URL segment (`/api/v{version}/...`) + `api-version` header |
| Content-Type | `application/json` |
| CORS | `http://localhost:3000`, `http://localhost:5176`, `http://localhost:5226` kayıtlı |
| Response Compression | Aktif (HTTPS dahil) |

### 2.2. Standart Response Yapısı — `Result<T>`

Backend'deki **tüm** endpoint'ler (validation hataları hariç) bu zarfı kullanır:

```typescript
// Her API yanıtı bu yapıda gelir
interface ApiResult<T> {
  data: T | null;
  errorMessages: string[];
  isSuccessful: boolean;
  statusCode: number;
}
```

**Kurallar:**
- `isSuccessful === true` → `data` dolu, `errorMessages` boş dizi
- `isSuccessful === false` → `data` null, `errorMessages` içinde hata mesajları
- `statusCode` HTTP yanıt koduyla eşleşir (200, 201, 400, 404, 409, 500…)

### 2.3. Sayfalama — `PaginationResultDto<T>`

Listeleme endpoint'leri (`GetAll*`) bu yapıyı döner:

```typescript
interface PaginatedResult<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPageCount: number;
}
```

**Sayfalama Query Parametreleri:**

| Parametre | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `pageNumber` | int | 1 | Sayfa numarası (1-based) |
| `pageSize` | int | 5 veya 10 | Sayfa başına öğe (max 100, backend clamp eder) |
| `searchTerm` | string | `""` | Arama terimi |
| `sortBy` | string? | null | Sıralama alanı (backend allow-list kontrolü yapar) |
| `sortDirection` | string? | null | `"asc"` veya `"desc"` |

### 2.4. Hata Yanıtları

Backend üç farklı hata formatı kullanır:

**a) `Result<T>` hataları (business logic):**
```json
{ "data": null, "errorMessages": ["Kullanıcı bulunamadı"], "isSuccessful": false, "statusCode": 404 }
```

**b) `ValidationProblemDetails` (FluentValidation — 422):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation failed",
  "status": 422,
  "errors": {
    "Name": ["'Name' must not be empty."],
    "Price": ["'Price' must be greater than 0."]
  },
  "instance": "/api/v1/products"
}
```

**c) `ProblemDetails` (exception handler — 400/403/404/409/500):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "instance": "/api/v1/auth/login",
  "traceId": "00-abc123..."
}
```

**d) Rate Limiting (429):**
Gövde yok, sadece HTTP 429 status code döner.

### 2.5. Rate Limiting

| Policy | Limit | Pencere | Uygulanan Endpoint'ler |
|---|---|---|---|
| `fixed` | 50 istek | 5 saniye | Products, Categories, Orders, OrderItems, Users, Roles, UserRoles |
| `auth-fixed` | 5 istek/IP | 1 saniye | Auth (login, refresh, forgot-password, reset-password) |

---

## 3. Endpoint Detayları ve TypeScript DTO Tipleri

### 3.1. Auth — `/api/v1/auth/`

Rate limiter: `auth-fixed` (IP bazlı, 5 istek/saniye)

```typescript
// ─── Request DTOs ───────────────────────────────────────
interface LoginRequest {
  userName: string;     // ⚠️ email DEĞİL, userName
  password: string;
}

interface RefreshTokenRequest {
  refreshToken: string;
}

interface ForgotPasswordRequest {
  email: string;
}

interface ResetPasswordRequest {
  email: string;
  code: string;         // 6 haneli OTP
  newPassword: string;
}

// ─── Response DTO ───────────────────────────────────────
interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;    // ISO 8601 DateTimeOffset
}
```

| Method | Path | Request | Response | Yetki | Notlar |
|---|---|---|---|---|---|
| POST | `/login` | `LoginRequest` | `ApiResult<LoginResponse>` | Anonim | 5 hatalı girişte 15 dk hesap kilidi. Lockout mesajı `errorMessages` içinde gelir. |
| POST | `/refresh` | `RefreshTokenRequest` | `ApiResult<LoginResponse>` | Anonim | Token rotation: eski token revoke edilir, yenisi verilir. Reuse tespit edilirse family'deki tüm token'lar iptal edilir → 400/401. |
| POST | `/forgot-password` | `ForgotPasswordRequest` | `ApiResult<string>` | Anonim | Her zaman 200 döner (user enumeration koruması). Geçerli e-posta ise 6 haneli OTP kodu gönderir (15 dk geçerli). |
| POST | `/reset-password` | `ResetPasswordRequest` | `ApiResult<string>` | Anonim | OTP doğrulaması + yeni şifre belirleme. |

### 3.2. Users — `/api/v1/users/`

```typescript
// ─── Request DTOs ───────────────────────────────────────
interface UserCreateRequest {
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  password: string;
}

interface UserUpdateRequest {
  id: string;           // Guid, URL'den alınır
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  password: string;
}

interface UserUpdateSelfRequest {
  firstName: string;
  lastName: string;
  userName: string;
  // ⚠️ email değiştirilemez (admin-only)
}

interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// ─── Response DTO ───────────────────────────────────────
interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  userName: string;
  email: string;
}
```

| Method | Path | Request | Response | Permission |
|---|---|---|---|---|
| GET | `/` | Query params (sayfalama) | `ApiResult<PaginatedResult<UserProfile>>` | `Users.Read` |
| GET | `/{id}` | — | `ApiResult<UserProfile>` | `Users.Read` |
| POST | `/` | `UserCreateRequest` | `ApiResult<string>` (201) | `Users.Create` |
| PUT | `/{id}` | `UserUpdateRequest` | `ApiResult<string>` | `Users.Update` |
| DELETE | `/{id}` | — | `ApiResult<string>` | `Users.Delete` |
| GET | `/me` | — | `ApiResult<UserProfile>` | Authenticated (herhangi) |
| PUT | `/me` | `UserUpdateSelfRequest` | `ApiResult<string>` | Authenticated (herhangi) |
| POST | `/me/change-password` | `ChangePasswordRequest` | `ApiResult<string>` | Authenticated (herhangi) |

### 3.3. Roles — `/api/v1/roles/`

```typescript
interface RoleCreateRequest { name: string; }
interface RoleUpdateRequest { id: string; name: string; rowVersion?: string; } // base64
```

| Method | Path | Permission |
|---|---|---|
| GET | `/` | `Roles.Read` |
| GET | `/{id}` | `Roles.Read` |
| POST | `/` | `Roles.Create` |
| PUT | `/{id}` | `Roles.Update` |
| DELETE | `/{id}` | `Roles.Delete` |

### 3.4. UserRoles — `/api/v1/user-roles/`

```typescript
interface UserRoleCreateRequest {
  userId: string;   // Guid
  roleId: string;   // Guid
}
```

| Method | Path | Permission |
|---|---|---|
| GET | `/` | `Admin` role policy |
| POST | `/` | `Admin` role policy |
| DELETE | `/{id}` | `Admin` role policy |

### 3.5. Products — `/api/v1/products/`

```typescript
interface ProductCreateRequest {
  name: string;
  description?: string;
  price: number;        // decimal → number
  categoryId: string;   // Guid
}

interface ProductUpdateRequest {
  id: string;
  name: string;
  description?: string;
  price: number;
  categoryId: string;
  rowVersion?: string;  // base64 encoded byte[]
}

interface ProductResult extends BaseEntityResult {
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  // ⚠️ Price, ProductResultDto'da yok! Backend'den gelmiyor.
}
```

| Method | Path | Permission |
|---|---|---|
| GET | `/` (sayfalama + sıralama) | `Products.Read` |
| GET | `/{id}` | `Products.Read` |
| POST | `/` | `Products.Create` |
| PUT | `/{id}` | `Products.Update` |
| DELETE | `/{id}` | `Products.Delete` |

### 3.6. Categories — `/api/v1/categories/`

```typescript
interface CategoryCreateRequest { name: string; }
interface CategoryUpdateRequest { id: string; name: string; rowVersion?: string; }

interface CategoryResult extends BaseEntityResult {
  name: string;
}
```

| Method | Path | Permission |
|---|---|---|
| GET | `/` (sayfalama + sıralama) | `Categories.Read` |
| GET | `/{id}` | `Categories.Read` |
| POST | `/` | `Categories.Create` |
| PUT | `/{id}` | `Categories.Update` |
| DELETE | `/{id}` | `Categories.Delete` |

### 3.7. Orders — `/api/v1/orders/`

```typescript
interface OrderItemCreateRequest {
  productId: string;
  quantity: number;
}

interface OrderCreateRequest {
  items: OrderItemCreateRequest[];
}

interface OrderItemUpdateRequest {
  id: string;
  productId: string;
  quantity: number;
  rowVersion?: string;
}

interface OrderUpdateRequest {
  id: string;
  orderDate: string;    // ISO 8601
  items: OrderItemUpdateRequest[];
  rowVersion?: string;
}

interface OrderItemResult extends BaseEntityResult {
  productId: string;
  quantity: number;
}

interface OrderResult extends BaseEntityResult {
  orderDate: string;    // ISO 8601
  items: OrderItemResult[];
}
```

| Method | Path | Permission |
|---|---|---|
| GET | `/` (sayfalama + sıralama) | `Orders.Read` |
| GET | `/{id}` | `Orders.Read` |
| POST | `/` | `Orders.Create` |
| PUT | `/{id}` | `Orders.Update` |
| DELETE | `/{id}` | `Orders.Delete` |

### 3.8. OrderItems — `/api/v1/order-items/`

| Method | Path | Permission |
|---|---|---|
| GET | `/` (sayfalama + sıralama) | `OrderItems.Read` |
| GET | `/{id}` | `OrderItems.Read` |
| POST | `/` | `OrderItems.Create` |
| PUT | `/{id}` | `OrderItems.Update` |
| DELETE | `/{id}` | `OrderItems.Delete` |

### 3.9. Ortak Base DTO

Tüm `*Result` DTO'ları `BaseEntityDto`'dan türer:

```typescript
interface BaseEntityResult {
  id: string;                     // Guid v7
  createdAt: string;              // ISO 8601 DateTimeOffset
  createdBy: string;              // Guid
  updatedAt: string | null;
  updatedBy: string | null;
  isDeleted: boolean;
  deletedAt: string | null;
  deletedBy: string | null;
}
```

---

## 4. Kimlik Doğrulama Mimarisi

### 4.1. JWT Access Token Yapısı

Backend'in ürettiği JWT token şu claim'leri içerir:

| Claim | Örnek Değer | Açıklama |
|---|---|---|
| `nameid` (NameIdentifier) | `"a1b2c3d4-..."` | Kullanıcı ID (Guid) |
| `userName` | `"admin"` | Kullanıcı adı |
| `fullName` | `"Admin User"` | Tam ad |
| `email` | `"admin@shoppy.com"` | E-posta |
| `role` | `"Admin"` (tekil veya çoklu) | Kullanıcının rol(ler)i |
| `permission` | `"Products.Read"` (çoklu) | Her permission ayrı claim |
| `exp` | unix timestamp | Son kullanma (1 saat) |
| `iss` | `"Shoppy"` | Issuer |
| `aud` | `"www.shoppy.com"` | Audience |

### 4.2. Refresh Token Akışı

```
┌─────────┐          ┌─────────┐          ┌──────────────┐
│ Browser │          │ Axios   │          │ Backend API  │
│         │          │Intercept│          │              │
└────┬────┘          └────┬────┘          └──────┬───────┘
     │  API isteği        │                      │
     │───────────────────►│  Authorization:      │
     │                    │  Bearer <token>      │
     │                    │─────────────────────►│
     │                    │                      │
     │                    │  401 Unauthorized     │
     │                    │◄─────────────────────│
     │                    │                      │
     │                    │  POST /auth/refresh  │
     │                    │  { refreshToken }    │
     │                    │─────────────────────►│
     │                    │                      │
     │                    │  Yeni accessToken +  │
     │                    │  yeni refreshToken   │
     │                    │◄─────────────────────│
     │                    │                      │
     │                    │  Orijinal isteği     │
     │                    │  yeni token ile      │
     │                    │  tekrar gönder       │
     │                    │─────────────────────►│
     │  Sonuç             │                      │
     │◄───────────────────│◄─────────────────────│
```

**Kritik Kurallar:**
1. Access token **bellekte** tutulur (Zustand store), **asla** localStorage/sessionStorage'a yazılmaz.
2. Refresh token güvenli bir şekilde saklanır (localStorage kabul edilebilir çünkü backend SHA-256 hash'leyerek DB'de tutar ve rotation yapar).
3. Refresh başarısız olursa (`400`/`401` → reuse detection veya token ailesi iptal edilmiş), tüm auth state temizlenmeli ve login'e yönlendirme yapılmalıdır.
4. Birden fazla eşzamanlı 401 durumunda, sadece **tek bir** refresh isteği gönderilmeli; diğer istekler queue'da bekletilip yeni token ile retry edilmelidir.

### 4.3. Varsayılan Roller ve İzinleri

**Admin** — tüm izinler:

```
Users.Read, Users.Create, Users.Update, Users.Delete, Users.UpdateSelf, Users.ChangePassword
Roles.Read, Roles.Create, Roles.Update, Roles.Delete
Orders.Read, Orders.Create, Orders.Update, Orders.Delete
OrderItems.Read, OrderItems.Create, OrderItems.Update, OrderItems.Delete
Products.Read, Products.Create, Products.Update, Products.Delete
Categories.Read, Categories.Create, Categories.Update, Categories.Delete
```

**Customer** — sınırlı izinler:

```
Products.Read
Categories.Read
Orders.Read, Orders.Create
OrderItems.Read, OrderItems.Create
Users.UpdateSelf, Users.ChangePassword
```

### 4.4. Hesap Kilidi (Account Lockout)

- 5 başarısız giriş denemesi → 15 dakika kilit
- Kilit süresi boyunca login endpoint'i başarısız yanıt döner
- Frontend'de kalan deneme hakkı veya kilit durumu `errorMessages` üzerinden gösterilmelidir

---

## 5. Frontend Mimari Standartları

### 5.1. Dizin Yapısı

```
src/
├── api/                    # Axios client, interceptors, api tanımları
│   ├── client.ts           # Axios instance + interceptor kurulumu
│   ├── auth.api.ts         # Auth endpoint fonksiyonları
│   ├── products.api.ts     # Product endpoint fonksiyonları
│   ├── categories.api.ts
│   ├── orders.api.ts
│   ├── users.api.ts
│   └── roles.api.ts
├── components/             # Paylaşılan, tekrar kullanılabilir bileşenler
│   ├── ui/                 # Temel UI (Button, Input, Modal, Card, Skeleton…)
│   ├── layout/             # Header, Sidebar, Footer, PageContainer
│   ├── data-table/         # Genel tablo bileşeni (sayfalama + sıralama dahili)
│   └── guards/             # PermissionGuard, ProtectedRoute
├── features/               # Dikey özellik modülleri
│   ├── auth/               # Login, ForgotPassword, ResetPassword sayfaları
│   ├── dashboard/          # Admin dashboard
│   ├── products/           # Ürün listeleme, detay, oluşturma, düzenleme
│   ├── categories/         # Kategori yönetimi
│   ├── orders/             # Sipariş yönetimi, müşteri sipariş geçmişi
│   ├── users/              # Kullanıcı yönetimi (admin), profil (self-service)
│   └── roles/              # Rol ve yetki yönetimi
├── hooks/                  # Global custom hook'lar
│   ├── useAuth.ts          # Token decode, permission kontrolü
│   ├── useDebounce.ts      # Arama terimi için debounce
│   └── usePagination.ts    # Sayfalama state yönetimi
├── layouts/                # Sayfa şablonları
│   ├── PublicLayout.tsx    # Auth sayfaları (login, reset password)
│   ├── CustomerLayout.tsx  # Müşteri arayüzü
│   └── AdminLayout.tsx     # Admin paneli
├── lib/                    # Yardımcı kütüphaneler
│   ├── jwt.ts              # JWT decode, claim çıkarma
│   └── utils.ts            # Formatters, cn() helper
├── providers/              # React context/provider sarmalayıcıları
│   ├── QueryProvider.tsx   # TanStack Query client
│   └── ThemeProvider.tsx   # Dark/Light mode
├── stores/                 # Zustand store'ları
│   ├── auth.store.ts       # accessToken, user, permissions, login/logout
│   └── cart.store.ts       # Sepet (localStorage ile sync)
├── types/                  # Global TypeScript tipleri
│   ├── api.types.ts        # ApiResult<T>, PaginatedResult<T>, BaseEntityResult
│   ├── auth.types.ts       # LoginRequest, LoginResponse…
│   ├── product.types.ts
│   ├── order.types.ts
│   └── user.types.ts
└── routes/                 # Route tanımları
    └── index.tsx           # createBrowserRouter yapılandırması
```

### 5.2. Permission Koruması

```tsx
// PermissionGuard — bileşen seviyesi
interface PermissionGuardProps {
  permission: string | string[];
  fallback?: React.ReactNode;  // Gösterilecek alternatif (varsayılan: null)
  children: React.ReactNode;
}

// Kullanım:
<PermissionGuard permission="Products.Create">
  <Button>Yeni Ürün Ekle</Button>
</PermissionGuard>

// ProtectedRoute — rota seviyesi
<Route path="/admin/users" element={
  <ProtectedRoute permission="Users.Read" redirectTo="/unauthorized">
    <AdminUsersPage />
  </ProtectedRoute>
} />
```

### 5.3. Optimistic Concurrency (RowVersion)

Backend `BaseEntity` üzerinde `[Timestamp] byte[] RowVersion` kullanır. Güncelleme sırasında:

1. Veriyi çekerken gelen `rowVersion` değerini saklayın (base64 string).
2. PUT isteğinde `rowVersion` alanını geri gönderin.
3. Backend `409 Conflict` (DbUpdateConcurrencyException) dönerse, kullanıcıya "Bu kayıt başka biri tarafından güncellenmiş. Lütfen sayfayı yenileyip tekrar deneyin" mesajı gösterilmelidir.

---

## 6. Tasarım Sistemi

### 6.1. Renk Paleti

| Token | Light | Dark | Kullanım |
|---|---|---|---|
| `--bg-primary` | `#ffffff` | `#0f172a` (slate-900) | Ana arka plan |
| `--bg-secondary` | `#f8fafc` (slate-50) | `#1e293b` (slate-800) | Kart, sidebar |
| `--bg-tertiary` | `#f1f5f9` (slate-100) | `#334155` (slate-700) | Hover, input bg |
| `--accent` | `#6366f1` (indigo-500) | `#818cf8` (indigo-400) | CTA butonlar, aktif link |
| `--accent-hover` | `#4f46e5` (indigo-600) | `#6366f1` (indigo-500) | Hover durumu |
| `--success` | `#10b981` (emerald-500) | `#34d399` (emerald-400) | Başarı toast |
| `--danger` | `#ef4444` (red-500) | `#f87171` (red-400) | Silme, hata |
| `--warning` | `#f59e0b` (amber-500) | `#fbbf24` (amber-400) | Uyarı |
| `--text-primary` | `#0f172a` (slate-900) | `#f8fafc` (slate-50) | Ana metin |
| `--text-secondary` | `#64748b` (slate-500) | `#94a3b8` (slate-400) | İkincil metin |

### 6.2. Tipografi

- **Font:** Inter (Google Fonts), fallback `system-ui, sans-serif`
- **Başlıklar:** font-weight 700, tracking-tight
- **Gövde:** font-weight 400, leading-relaxed

### 6.3. Glassmorphism

Kart ve modal'larda yumuşak cam efekti:
```css
.glass-card {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
}
```

### 6.4. Animasyonlar

- Sayfa geçişleri: `opacity 0→1`, `translateY 8px→0`, `duration 300ms`
- Modal/Drawer: scale + opacity geçişi
- Buton hover: `scale(1.02)`, `transition 150ms ease`
- Skeleton loader: `animate-pulse` (Tailwind built-in)
- Toast: sağ üstten kayarak giriş, 4 saniye sonra otomatik kapanma

---

## 7. Hata Yönetimi Stratejisi

| Senaryo | HTTP Kodu | Frontend Davranışı |
|---|---|---|
| Başarılı işlem | 200/201 | Veriyi işle, başarı toast'u göster |
| Validasyon hatası | 422 | Form alanlarının altında ilgili hataları göster |
| Business hatası | 400/404 | `errorMessages` içeriğini toast ile göster |
| Yetkisiz erişim | 401 | Token refresh → başarısızsa login'e yönlendir |
| İzin yetersiz | 403 | "Bu işlem için yetkiniz yok" sayfası veya toast |
| Concurrency çakışması | 409 | "Kayıt güncellendi, yeniden yükleyin" uyarısı + veriyi refetch et |
| Rate limit | 429 | "Çok fazla istek. Lütfen bekleyin." toast + retry-after |
| Sunucu hatası | 500 | Generic hata sayfası, `traceId`'yi loglama amacıyla sakla |
| Ağ hatası | — | "İnternet bağlantınızı kontrol edin" uyarısı |

---

## 8. Performans Kuralları

1. Arama inputlarında **minimum 500ms debounce** uygula
2. Liste verilerinde TanStack Query'nin `staleTime: 30_000` ayarını kullan
3. Büyük listelerde **React.memo** ve **virtualization** (TanStack Virtual) düşün
4. Resimleri `loading="lazy"` ile yükle
5. Route-bazlı **code splitting** uygula (`React.lazy + Suspense`)
6. Production build'de Tailwind purge'nin aktif olduğundan emin ol

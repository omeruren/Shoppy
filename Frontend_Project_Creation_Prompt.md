# Shoppy Frontend — Proje Oluşturma ve Geliştirme Promptu

> Bu prompt, ASP.NET Core 10 backend API'si ile tam entegre çalışacak bir React frontend uygulamasını
> sıfırdan oluşturmak için hazırlanmıştır. Tüm API kontratları, DTO yapıları, güvenlik akışları ve
> izin (permission) kuralları doğrudan backend kaynak kodundan çıkarılmıştır.

---

## ROLÜN

Sen deneyimli bir **Senior Frontend Developer** ve **UI/UX Architect** rolündesin.

Görevin: Aşağıda detaylandırılan Shoppy E-Ticaret API'si ile tam uyumlu, **premium görünüme** sahip (dark mode, glassmorphism, mikro-animasyonlar), güvenli ve ölçeklenebilir bir **React + TypeScript** single-page application (SPA) geliştirmektir.

**Dil:** Projedeki tüm kod dosyaları, yorum satırları ve commit mesajları İngilizce olmalıdır. Arayüz metinleri Türkçe olmalıdır.

---

## BÖLÜM A — TEKNOLOJİ YIĞINI VE PROJE BAŞLATMA

Aşağıdaki teknolojileri kullan:

| Katman | Teknoloji |
|---|---|
| Build/Dev | Vite 6+ |
| Framework | React 18+ (TypeScript, strict mode) |
| Routing | React Router v7 |
| Server State | TanStack Query v5 |
| Client State | Zustand |
| HTTP | Axios |
| Form & Validasyon | React Hook Form + Zod |
| Stil | Tailwind CSS v4 |
| UI Primitives | Shadcn/UI (Radix tabanlı) |
| İkonlar | Lucide React |
| Animasyonlar | Framer Motion |
| Font | Inter (Google Fonts) |

**Proje başlatma:** `npx -y create-vite@latest ./ -- --template react-ts` ile başlat, ardından bağımlılıkları kur.

---

## BÖLÜM B — BACKEND API'Sİ İLE ENTEGRASYON DETAYLARI

### B.1. Bağlantı Bilgileri

- **Base URL:** `http://localhost:5176` (development)
- **API Prefix:** `/api/v1/`
- **CORS:** `http://localhost:3000` backend'de izinli origin olarak kayıtlı
- **Content-Type:** `application/json`
- **Compression:** Response compression aktif

### B.2. Standart API Yanıt Yapısı

Backend'deki tüm endpoint'ler şu zarfı kullanır:

```typescript
// types/api.types.ts
interface ApiResult<T> {
  data: T | null;
  errorMessages: string[];
  isSuccessful: boolean;
  statusCode: number;
}

interface PaginatedResult<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPageCount: number;
}

interface BaseEntity {
  id: string;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
  isDeleted: boolean;
  deletedAt: string | null;
  deletedBy: string | null;
}
```

### B.3. Hata Yanıt Formatları

Backend **üç farklı** hata formatı döner, axios interceptor bunları normalize etmeli:

1. **`ApiResult<T>` hatası** (business logic): `{ data: null, errorMessages: [...], isSuccessful: false, statusCode: 404 }`
2. **`ValidationProblemDetails`** (FluentValidation, 422): `{ title, status: 422, errors: { "fieldName": ["hata mesajı"] } }`
3. **`ProblemDetails`** (exception handler, 400/403/404/409/500): `{ title, status, instance, traceId }`
4. **Rate Limiting (429):** Sadece HTTP 429 status, gövde yok.

### B.4. Rate Limiting

| Policy | Limit | Pencere |
|---|---|---|
| `fixed` (tüm CRUD endpoint'leri) | 50 istek | 5 saniye |
| `auth-fixed` (login, refresh, forgot/reset password) | 5 istek/IP | 1 saniye |

429 durumunda kullanıcıya "Çok fazla istek gönderildi, lütfen bekleyin" toast göster.

---

## BÖLÜM C — KİMLİK DOĞRULAMA VE YETKİLENDİRME

### C.1. Login Akışı

1. Kullanıcı `userName` + `password` ile `POST /api/v1/auth/login` endpoint'ine istek gönderir.
   - ⚠️ **Giriş alanı `userName`'dir, `email` değil!**
2. Başarılı yanıt: `{ accessToken, refreshToken, expiresAt }` döner.
3. Access token'ı **Zustand auth store** içinde bellekte tut (XSS koruması).
4. Refresh token'ı `localStorage`'da sakla (backend SHA-256 hash'leyerek DB'de tutar, rotation yapar).
5. Token'ı `jwt-decode` ile çözümle, claim'leri çıkar:
   - `nameid` → kullanıcı ID
   - `userName`, `fullName`, `email` → profil bilgileri
   - `role` → roller (string veya string[])
   - `permission` → izinler (string[])
6. Access token 1 saat geçerli, refresh token 7 gün geçerli.

### C.2. Axios Interceptor Mimarisi

```
REQUEST INTERCEPTOR:
  ─ Her isteğe "Authorization: Bearer {accessToken}" header'ı ekle
  ─ Access token yoksa header ekleme (anonim endpoint'ler)

RESPONSE INTERCEPTOR:
  ─ 401 Unauthorized gelirse:
    1. Eğer zaten refresh yapılıyorsa → isteği kuyruğa al, yeni token gelince retry et
    2. Değilse → refreshing flag'ini true yap
    3. POST /api/v1/auth/refresh → { refreshToken: localStorage'daki değer }
    4. Başarılı ise:
       ─ Yeni accessToken'ı Zustand'a yaz
       ─ Yeni refreshToken'ı localStorage'a yaz
       ─ Kuyruktaki tüm istekleri yeni token ile retry et
       ─ refreshing = false
    5. Başarısız ise (400/401 → refresh token expired veya reuse detection):
       ─ Zustand auth state'i temizle
       ─ localStorage'daki refresh token'ı sil
       ─ Kullanıcıyı /login'e yönlendir
       ─ Toast: "Oturumunuz sonlandırıldı. Lütfen tekrar giriş yapın."
```

### C.3. İzin (Permission) Sistemi

JWT token'ın `permission` claim'leri kullanıcının yetkilerini belirler. İki yerleşik rol vardır:

**Admin** → TÜM izinler:
```
Users.Read, Users.Create, Users.Update, Users.Delete, Users.UpdateSelf, Users.ChangePassword
Roles.Read, Roles.Create, Roles.Update, Roles.Delete
Orders.Read, Orders.Create, Orders.Update, Orders.Delete
OrderItems.Read, OrderItems.Create, OrderItems.Update, OrderItems.Delete
Products.Read, Products.Create, Products.Update, Products.Delete
Categories.Read, Categories.Create, Categories.Update, Categories.Delete
```

**Customer** → Sınırlı izinler:
```
Products.Read, Categories.Read
Orders.Read, Orders.Create
OrderItems.Read, OrderItems.Create
Users.UpdateSelf, Users.ChangePassword
```

**Frontend'de izin kontrolü:**
- `<PermissionGuard permission="Products.Create">` → İzni yoksa bileşeni render etme
- `<ProtectedRoute permission="Users.Read">` → İzni yoksa 403 sayfasına yönlendir
- `useHasPermission("Products.Delete")` → boolean döner, koşullu render için

### C.4. Hesap Kilidi (Account Lockout)

5 ardışık başarısız giriş denemesinde backend hesabı **15 dakika** kilitler.
Login başarısız olduğunda `errorMessages` içinde kilit uyarısı gelir.
Frontend'de bunu uygun bir uyarı mesajıyla göster.

---

## BÖLÜM D — TÜM ENDPOINT'LER VE DTO'LAR

### D.1. Auth — `/api/v1/auth/`

| Method | Path | Body | Response | Auth | Not |
|---|---|---|---|---|---|
| POST | `/login` | `{ userName, password }` | `ApiResult<LoginResponse>` | Yok | `LoginResponse: { accessToken, refreshToken, expiresAt }` |
| POST | `/refresh` | `{ refreshToken }` | `ApiResult<LoginResponse>` | Yok | Token rotation; reuse → family iptal |
| POST | `/forgot-password` | `{ email }` | `ApiResult<string>` | Yok | Her zaman 200 (user enumeration koruması). 6 haneli OTP, 15 dk geçerli |
| POST | `/reset-password` | `{ email, code, newPassword }` | `ApiResult<string>` | Yok | OTP + yeni şifre |

### D.2. Users — `/api/v1/users/`

| Method | Path | Body | Response | Permission |
|---|---|---|---|---|
| GET | `/` | Query: pageNumber, pageSize, searchTerm | `ApiResult<PaginatedResult<UserProfile>>` | `Users.Read` |
| GET | `/{id}` | — | `ApiResult<UserProfile>` | `Users.Read` |
| POST | `/` | `{ firstName, lastName, userName, email, password }` | `ApiResult<string>` (201) | `Users.Create` |
| PUT | `/{id}` | `{ firstName, lastName, userName, email, password }` | `ApiResult<string>` | `Users.Update` |
| DELETE | `/{id}` | — | `ApiResult<string>` | `Users.Delete` |
| GET | `/me` | — | `ApiResult<UserProfile>` | Authenticated |
| PUT | `/me` | `{ firstName, lastName, userName }` | `ApiResult<string>` | Authenticated |
| POST | `/me/change-password` | `{ currentPassword, newPassword, confirmNewPassword }` | `ApiResult<string>` | Authenticated |

`UserProfile: { id, firstName, lastName, fullName, userName, email }`

### D.3. Roles — `/api/v1/roles/`

| Method | Path | Body | Permission |
|---|---|---|---|
| GET | `/` | — | `Roles.Read` |
| GET | `/{id}` | — | `Roles.Read` |
| POST | `/` | `{ name }` | `Roles.Create` |
| PUT | `/{id}` | `{ name, rowVersion? }` | `Roles.Update` |
| DELETE | `/{id}` | — | `Roles.Delete` |

### D.4. UserRoles — `/api/v1/user-roles/`

| Method | Path | Body | Permission |
|---|---|---|---|
| GET | `/` | — | Admin role |
| POST | `/` | `{ userId, roleId }` | Admin role |
| DELETE | `/{id}` | — | Admin role |

### D.5. Products — `/api/v1/products/`

| Method | Path | Body / Query | Permission |
|---|---|---|---|
| GET | `/` | Query: pageNumber, pageSize, searchTerm, sortBy, sortDirection | `Products.Read` |
| GET | `/{id}` | — | `Products.Read` |
| POST | `/` | `{ name, description?, price, categoryId }` | `Products.Create` |
| PUT | `/{id}` | `{ name, description?, price, categoryId, rowVersion? }` | `Products.Update` |
| DELETE | `/{id}` | — | `Products.Delete` |

Response DTO: `{ ...BaseEntity, name, description, categoryId, categoryName }`
> ⚠️ `ProductResultDto`'da `price` alanı **yok**. Backend DTO'sunda eksik.

### D.6. Categories — `/api/v1/categories/`

| Method | Path | Body / Query | Permission |
|---|---|---|---|
| GET | `/` | Query: pageNumber, pageSize, searchTerm, sortBy, sortDirection | `Categories.Read` |
| GET | `/{id}` | — | `Categories.Read` |
| POST | `/` | `{ name }` | `Categories.Create` |
| PUT | `/{id}` | `{ name, rowVersion? }` | `Categories.Update` |
| DELETE | `/{id}` | — | `Categories.Delete` |

### D.7. Orders — `/api/v1/orders/`

| Method | Path | Body / Query | Permission |
|---|---|---|---|
| GET | `/` | Query: pageNumber, pageSize, searchTerm, sortBy, sortDirection | `Orders.Read` |
| GET | `/{id}` | — | `Orders.Read` |
| POST | `/` | `{ items: [{ productId, quantity }] }` | `Orders.Create` |
| PUT | `/{id}` | `{ orderDate, items: [{ id, productId, quantity, rowVersion? }], rowVersion? }` | `Orders.Update` |
| DELETE | `/{id}` | — | `Orders.Delete` |

Response: `{ ...BaseEntity, orderDate, items: [{ ...BaseEntity, productId, quantity }] }`

### D.8. OrderItems — `/api/v1/order-items/`

| Method | Path | Body / Query | Permission |
|---|---|---|---|
| GET | `/` | Query: pageNumber, pageSize, searchTerm, sortBy, sortDirection | `OrderItems.Read` |
| GET | `/{id}` | — | `OrderItems.Read` |
| POST | `/` | `{ productId, quantity }` | `OrderItems.Create` |
| PUT | `/{id}` | `{ productId, quantity, rowVersion? }` | `OrderItems.Update` |
| DELETE | `/{id}` | — | `OrderItems.Delete` |

### D.9. Concurrency (RowVersion)

Tüm entity'lerde `[Timestamp] byte[] RowVersion` var. Güncelleme isteğinde:
1. GET ile çekilen verinin `rowVersion` alanını sakla (base64 string olarak gelir)
2. PUT isteğinde `rowVersion` alanını geri gönder
3. 409 Conflict dönerse: "Bu kayıt başka biri tarafından değiştirilmiş. Sayfayı yenileyip tekrar deneyin." uyarısı göster ve TanStack Query cache'ini invalidate et.

---

## BÖLÜM E — EKRANLAR VE FONKSİYONEL GEREKSİNİMLER

### E.1. Public Sayfalar (Auth olmadan)

#### Login Sayfası
- `userName` ve `password` giriş alanları
- "Beni Hatırla" checkbox (opsiyonel)
- "Şifremi Unuttum" linki
- Hatalı giriş bildirimlerini form üstünde göster
- Hesap kilidi uyarısını belirgin şekilde göster
- Başarılı login sonrası: Admin ise `/admin/dashboard`'a, Customer ise `/` ana sayfaya yönlendir

#### Şifremi Unuttum Sayfası
- E-posta input → `POST /forgot-password`
- Her zaman "E-postanıza bir kod gönderdik" mesajı göster (enumeration koruması)
- OTP giriş adımına geç

#### Şifre Sıfırlama Sayfası
- 6 haneli OTP kodu inputu
- Yeni şifre + şifre tekrar alanları
- `POST /reset-password` → başarılıysa login'e yönlendir

### E.2. Müşteri Arayüzü (Customer Layout)

#### Ana Sayfa / Ürün Kataloğu
- Hero bölümü (banner görseli, kampanya metni)
- Ürün kartları grid'i (sayfalanmış)
- Arama barı (500ms debounce)
- Kategori filtresi (dropdown veya sidebar)
- Sıralama: İsim A-Z / Z-A (sortBy=name, sortDirection=asc/desc)

#### Ürün Detay Sayfası
- Ürün ismi, açıklaması, kategori adı
- "Sepete Ekle" butonu (quantity seçici ile)

#### Sepet (Drawer/Sidebar)
- Zustand `cartStore` ile yönetilir, localStorage ile senkronize
- Ürün adedi artır/azalt
- Ürünü sepetten çıkar
- Toplam tutarı göster
- "Siparişi Tamamla" → `POST /api/v1/orders` ile `items: [{ productId, quantity }]` gönder

#### Profilim
- `GET /me` ile profil bilgilerini göster
- Ad, soyad, kullanıcı adı düzenleme → `PUT /me`
- Şifre değiştirme → `POST /me/change-password`

#### Siparişlerim
- `GET /orders` ile müşterinin siparişlerini listele (sayfalanmış)
- Sipariş detayı: tarih, ürünler ve miktarları

### E.3. Admin Paneli (Admin Layout)

Admin'e özel sidebar navigasyonu: Dashboard, Ürünler, Kategoriler, Siparişler, Kullanıcılar, Roller

#### Dashboard
- Özet istatistik kartları (toplam ürün, kategori, sipariş, kullanıcı sayıları)
- Son siparişler listesi

#### Ürün Yönetimi
- DataTable: sayfalama, arama, sıralama
- "Yeni Ürün" butonu → Modal/Sayfa: isim, açıklama, fiyat, kategori dropdown
- Düzenleme: Mevcut veriyi form'a doldur, `rowVersion`'ı sakla ve PUT'ta gönder
- Silme: Onay dialog'u sonrası DELETE

#### Kategori Yönetimi
- Aynı CRUD pattern'i, daha basit form (sadece isim)

#### Sipariş Yönetimi
- Tüm siparişlerin sayfalanmış listesi
- Sipariş detayı: tarih, ürün kalemleri, miktarlar

#### Kullanıcı Yönetimi
- Kullanıcı listesi (sayfalanmış, aranabilir)
- Yeni kullanıcı oluştur: ad, soyad, userName, email, şifre
- Kullanıcıya rol ata: `POST /user-roles { userId, roleId }`
- Kullanıcının rolünü sil: `DELETE /user-roles/{id}`

#### Rol Yönetimi
- Rol listesi ve CRUD

---

## BÖLÜM F — TASARIM STANDARTLARI

### F.1. Genel Estetik
- **Dark mode** varsayılan, light mode geçişi desteklenmeli
- **Renk paleti:** Slate tonları (arka plan) + Indigo (vurgu/CTA) + Emerald (başarı) + Red (hata)
- **Glassmorphism:** Kartlar ve modal'larda `backdrop-blur-xl`, yarı-saydam border
- **Border radius:** `rounded-xl` (12-16px)
- **Gölgeler:** Subtle shadow-lg, dark mode'da shadow yerine border vurgusu

### F.2. Tipografi
- **Font:** Inter (Google Fonts), fallback: `system-ui, sans-serif`
- **Başlıklar:** Semibold/Bold, tracking-tight
- **Body:** Regular (400), leading-relaxed

### F.3. Yükleme ve Geri Bildirim
- Veri yüklenirken: **Skeleton loader** (pulse animasyonlu gri bloklar)
- Form gönderimi: Butonda spinner + disabled state
- Çift tıklama (double-submit) koruması
- Başarı/hata: Sağ üst köşe toast bildirimi (4 saniye otomatik kapanma)
- Sayfa geçişleri: Framer Motion ile `opacity + translateY` fade-in

### F.4. Responsive Tasarım
- Mobile-first yaklaşım
- Breakpoint'ler: `sm:640px`, `md:768px`, `lg:1024px`, `xl:1280px`
- Admin sidebar: Mobile'da hamburger menü ile açılır

---

## BÖLÜM G — HATA YÖNETİMİ

| HTTP Kodu | Anlam | Frontend Davranışı |
|---|---|---|
| 200/201 | Başarılı | Veriyi işle, toast (opsiyonel) |
| 400 | Bad Request | `errorMessages` toast ile göster |
| 401 | Unauthorized | Token refresh → başarısızsa login'e yönlendir |
| 403 | Forbidden | "Bu işlem için yetkiniz yok" sayfası/toast |
| 404 | Not Found | "Kayıt bulunamadı" toast veya 404 sayfası |
| 409 | Conflict (Concurrency) | "Kayıt güncellendi, yeniden yükleyin" uyarı + refetch |
| 422 | Validation | `errors` objesini React Hook Form `setError` ile form alanlarına bağla |
| 429 | Rate Limited | "Çok fazla istek. Lütfen bekleyin." toast |
| 500 | Server Error | Generic hata sayfası (React Error Boundary) |

Validation hatası (422) örnekleri, form alanına bağlama:
```tsx
// Axios'tan gelen ValidationProblemDetails
if (error.response?.status === 422) {
  const { errors } = error.response.data;
  Object.entries(errors).forEach(([field, messages]) => {
    form.setError(field as any, {
      type: "server",
      message: (messages as string[]).join(". "),
    });
  });
}
```

---

## BÖLÜM H — DİZİN YAPISI

```
src/
├── api/
│   ├── client.ts              # Axios instance + interceptors
│   ├── auth.api.ts
│   ├── products.api.ts
│   ├── categories.api.ts
│   ├── orders.api.ts
│   ├── order-items.api.ts
│   ├── users.api.ts
│   ├── roles.api.ts
│   └── user-roles.api.ts
├── components/
│   ├── ui/                    # Button, Input, Modal, Card, Badge, Skeleton
│   ├── layout/                # Header, Sidebar, Footer, PageContainer
│   ├── data-table/            # Genel DataTable bileşeni (sayfalama + sıralama)
│   └── guards/                # PermissionGuard, ProtectedRoute
├── features/
│   ├── auth/
│   ├── dashboard/
│   ├── products/
│   ├── categories/
│   ├── orders/
│   ├── users/
│   └── roles/
├── hooks/
│   ├── useAuth.ts
│   ├── useDebounce.ts
│   └── usePagination.ts
├── layouts/
│   ├── PublicLayout.tsx
│   ├── CustomerLayout.tsx
│   └── AdminLayout.tsx
├── lib/
│   ├── jwt.ts
│   └── utils.ts
├── providers/
│   ├── QueryProvider.tsx
│   └── ThemeProvider.tsx
├── stores/
│   ├── auth.store.ts
│   └── cart.store.ts
├── types/
│   ├── api.types.ts
│   ├── auth.types.ts
│   ├── product.types.ts
│   ├── category.types.ts
│   ├── order.types.ts
│   ├── user.types.ts
│   └── role.types.ts
└── routes/
    └── index.tsx
```

---

## BÖLÜM I — GELİŞTİRME ADIMLARI (ÖNERİLEN SIRA)

1. **Proje İskeleti:** Vite ile oluştur, tüm bağımlılıkları kur, Tailwind + shadcn/ui yapılandır
2. **Temel Altyapı:** `api/client.ts` (Axios + interceptors), Zustand store'ları, tip tanımları
3. **Auth Akışı:** Login sayfası, token yönetimi, ProtectedRoute, PermissionGuard
4. **Layout'lar:** PublicLayout, CustomerLayout (header + footer), AdminLayout (sidebar)
5. **DataTable Bileşeni:** Sayfalama, arama, sıralama destekli genel tablo
6. **Admin CRUD Sayfaları:** Ürünler → Kategoriler → Siparişler → Kullanıcılar → Roller
7. **Müşteri Sayfaları:** Ürün kataloğu → Ürün detay → Sepet → Checkout → Siparişlerim → Profilim
8. **Şifre Akışları:** Şifremi unuttum, şifre sıfırlama, şifre değiştirme
9. **Cilalanma:** Animasyonlar, skeleton loaderlar, toast sistemi, responsive kontrol
10. **Test & Doğrulama:** Tüm CRUD akışlarını backend ile test et

---

## ÖNEMLİ NOTLAR VE BİLİNEN KISITLAMALAR

1. **Login `userName` ile yapılır**, email ile değil.
2. **ProductResultDto'da `price` alanı eksik.** Backend DTO'sunu kontrol et, eğer hala eksikse backend'e `Price` property'si eklenmeli.
3. **Refresh token backend'de SHA-256 ile hash'lenerek saklanır.** Ham değer sadece client'ta tutulur.
4. **Backend'de register/signup endpoint'i yok.** Kullanıcı oluşturma sadece Admin yetkisiyle `POST /users` üzerinden yapılır.
5. **Soft delete kullanılıyor.** DELETE işlemleri kaydı veritabanından silmez, `isDeleted: true` yapar. Listeleme endpoint'leri silinmiş kayıtları döndürmez (global query filter).
6. **`RowVersion` base64 string olarak gelir/gönderilir.** Concurrency kontrolü için güncelleme isteklerinde mutlaka dahil edilmeli.
7. **UserRoles endpoint'i** `Admin` role policy'si ile korunur (`permission` değil, doğrudan `RequireRole("Admin")`).

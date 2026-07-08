---
name: shoppy-ui-designer
description: |
  Shoppy e-ticaret frontend projesi için UI/UX tasarım kılavuzu.
  Ekran envanteri, bileşen kütüphanesi, renk sistemi, tipografi, spacing, ikon kullanımı,
  durum yönetimi (loading/error/empty), responsive breakpoint'ler, animasyon kuralları ve
  erişilebilirlik standartlarını kapsar. Tasarımcı veya tasarım üreten AI için referans dosyasıdır.
---

# Shoppy UI/UX Tasarım Kılavuzu

> **Proje:** Shoppy — E-Ticaret Web Uygulaması
> **Hedef Kitle:** Son kullanıcılar (müşteriler) + Yönetici paneli (admin)
> **Tasarım Felsefesi:** Premium, minimal, karanlık mod ağırlıklı, cam morfolojisi (glassmorphism) esintili, akıcı mikro-animasyonlarla zenginleştirilmiş modern SPA.

---

## 1. Marka Kimliği ve Tasarım İlkeleri

### 1.1. Marka Değerleri

| Değer | Açıklama |
|---|---|
| **Premium** | İlk bakışta kaliteli ve profesyonel hissettiren arayüz |
| **Güven** | Temiz form alanları, net geri bildirimler, tutarlı durum göstergeleri |
| **Verimlilik** | Admin kullanıcıları için minimum tıklama ile iş yapabilme |
| **Erişilebilirlik** | WCAG 2.1 AA uyumu, keyboard navigation, screen reader desteği |

### 1.2. Tasarım İlkeleri

1. **Contrast over Decoration** — Süsleme yerine kontrast ve hiyerarşi ile yönlendir
2. **Progressive Disclosure** — Bilgiyi katmanlar halinde sun, kullanıcıyı boğma
3. **Feedback Always** — Her etkileşim bir geri bildirim üretmeli (hover, click, loading, success, error)
4. **Consistency is King** — Aynı eleman her yerde aynı görünür ve davranır
5. **Mobile-First, Desktop-Polished** — Mobilde çalışmalı, masaüstünde parlamalı

---

## 2. Renk Sistemi (Design Tokens)

### 2.1. Ana Palet

Tüm renkler **HSL** bazlı tanımlanır, tema geçişi kolaylığı için CSS custom property olarak kullanılır.

#### Dark Mode (Varsayılan)

| Token | HSL Değeri | Hex | Kullanım Alanı |
|---|---|---|---|
| `--bg-base` | `222 47% 11%` | `#0f172a` | Sayfa arka planı |
| `--bg-surface` | `217 33% 17%` | `#1e293b` | Kart, sidebar, dropdown arka planı |
| `--bg-elevated` | `215 25% 27%` | `#334155` | Hover state, input arka planı, aktif öğe |
| `--bg-overlay` | `215 20% 35%` | `#475569` | Tooltip, popover arka planı |
| `--border-subtle` | `217 19% 27%` | `#334155` | Kart sınırları, ayırıcılar |
| `--border-emphasis` | `215 14% 34%` | `#475569` | Aktif input sınırı, focus ring |
| `--text-primary` | `210 40% 98%` | `#f8fafc` | Ana metin, başlıklar |
| `--text-secondary` | `215 16% 63%` | `#94a3b8` | İkincil metin, placeholder, açıklama |
| `--text-muted` | `215 14% 46%` | `#64748b` | Devre dışı metin, zaman damgası |

#### Light Mode

| Token | HSL Değeri | Hex | Kullanım Alanı |
|---|---|---|---|
| `--bg-base` | `0 0% 100%` | `#ffffff` | Sayfa arka planı |
| `--bg-surface` | `210 40% 98%` | `#f8fafc` | Kart, sidebar |
| `--bg-elevated` | `210 40% 96%` | `#f1f5f9` | Hover, input bg |
| `--text-primary` | `222 47% 11%` | `#0f172a` | Ana metin |
| `--text-secondary` | `215 14% 46%` | `#64748b` | İkincil metin |

#### Vurgu (Accent) Renkleri

| Token | Dark | Light | Kullanım |
|---|---|---|---|
| `--accent-primary` | `#818cf8` (indigo-400) | `#6366f1` (indigo-500) | CTA butonlar, aktif linkler, seçili tab |
| `--accent-primary-hover` | `#6366f1` (indigo-500) | `#4f46e5` (indigo-600) | Hover durumu |
| `--accent-primary-subtle` | `rgba(99,102,241,0.15)` | `rgba(99,102,241,0.1)` | Badge arka planı, seçili satır |

#### Semantic (Anlam) Renkleri

| Token | Dark | Light | Kullanım |
|---|---|---|---|
| `--success` | `#34d399` (emerald-400) | `#10b981` (emerald-500) | Başarı toast, onay ikon, aktif badge |
| `--success-subtle` | `rgba(52,211,153,0.15)` | `rgba(16,185,129,0.1)` | Başarı arka planı |
| `--danger` | `#f87171` (red-400) | `#ef4444` (red-500) | Silme butonu, hata mesajı, hata toast |
| `--danger-subtle` | `rgba(248,113,113,0.15)` | `rgba(239,68,68,0.1)` | Hata arka planı |
| `--warning` | `#fbbf24` (amber-400) | `#f59e0b` (amber-500) | Uyarı mesajı, kilitleme bildirimi |
| `--warning-subtle` | `rgba(251,191,36,0.15)` | `rgba(245,158,11,0.1)` | Uyarı arka planı |
| `--info` | `#60a5fa` (blue-400) | `#3b82f6` (blue-500) | Bilgi toast, tooltip vurgu |

### 2.2. Gradyanlar

| İsim | Değer | Kullanım |
|---|---|---|
| `--gradient-hero` | `linear-gradient(135deg, #6366f1 0%, #8b5cf6 50%, #a78bfa 100%)` | Hero section arka planı |
| `--gradient-cta` | `linear-gradient(135deg, #6366f1 0%, #818cf8 100%)` | CTA buton arka planı |
| `--gradient-card-glow` | `radial-gradient(circle at top left, rgba(99,102,241,0.08), transparent 70%)` | Kart hover efekti |

---

## 3. Tipografi

### 3.1. Font Ailesi

| Kullanım | Font | Fallback |
|---|---|---|
| **UI (varsayılan)** | Inter | `system-ui, -apple-system, sans-serif` |
| **Mono (kod, ID)** | JetBrains Mono | `ui-monospace, monospace` |

### 3.2. Tip Ölçeği (Type Scale)

| Token | Boyut | Satır Yüksekliği | Ağırlık | Kullanım |
|---|---|---|---|---|
| `--text-display` | 36px / 2.25rem | 1.2 | 700 (Bold) | Hero başlığı, landing page |
| `--text-h1` | 30px / 1.875rem | 1.25 | 700 (Bold) | Sayfa başlıkları |
| `--text-h2` | 24px / 1.5rem | 1.3 | 600 (Semibold) | Bölüm başlıkları |
| `--text-h3` | 20px / 1.25rem | 1.4 | 600 (Semibold) | Kart başlıkları, modal başlıkları |
| `--text-h4` | 16px / 1rem | 1.5 | 600 (Semibold) | Alt başlıklar, tablo kolon başlıkları |
| `--text-body` | 14px / 0.875rem | 1.6 | 400 (Regular) | Ana gövde metni |
| `--text-body-sm` | 13px / 0.8125rem | 1.5 | 400 (Regular) | Tablo hücreleri, form yardımcı metin |
| `--text-caption` | 12px / 0.75rem | 1.4 | 500 (Medium) | Badge, etiket, zaman damgası |
| `--text-overline` | 11px / 0.6875rem | 1.3 | 600 (Semibold) | Üst etiket (UPPERCASE), kategori etiketi |

### 3.3. Tipografi Kuralları

- Başlıklarda `letter-spacing: -0.025em` (tracking-tight)
- Gövde metinde `letter-spacing: normal`
- Overline metinde `letter-spacing: 0.05em` + `text-transform: uppercase`
- Satır uzunluğu max `65ch` (okunabilirlik için)
- Paragraflarda `--text-body` + `leading-relaxed` (1.6 line-height)

---

## 4. Spacing ve Grid Sistemi

### 4.1. Spacing Ölçeği (8px Grid)

| Token | Değer | Kullanım |
|---|---|---|
| `--space-1` | 4px | Inline ikon ile metin arası |
| `--space-2` | 8px | İlgili öğeler arası (ikon + etiket) |
| `--space-3` | 12px | Form alanları arası |
| `--space-4` | 16px | Kart iç padding (compact) |
| `--space-5` | 20px | Bölüm ayırıcı, kart arası boşluk |
| `--space-6` | 24px | Kart iç padding (normal) |
| `--space-8` | 32px | Bölümler arası boşluk |
| `--space-10` | 40px | Sayfa kenar padding'i |
| `--space-12` | 48px | Büyük bölüm ayırıcısı |
| `--space-16` | 64px | Sayfa üst/alt boşluğu |

### 4.2. Border Radius

| Token | Değer | Kullanım |
|---|---|---|
| `--radius-sm` | 6px | Badge, küçük buton |
| `--radius-md` | 8px | Input, dropdown |
| `--radius-lg` | 12px | Kart, dialog |
| `--radius-xl` | 16px | Modal, büyük kart |
| `--radius-full` | 9999px | Avatar, pill badge |

### 4.3. Sayfa Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ Header (h: 64px, sticky top, z-50)                             │
├──────────┬──────────────────────────────────────────────────────┤
│ Sidebar  │  Main Content                                        │
│ (w:260px)│  (max-w: 1280px, padding: 24-40px)                  │
│          │                                                      │
│          │  ┌──────────────────────────────────────────────┐    │
│          │  │ Page Header (breadcrumb + title + action)    │    │
│          │  ├──────────────────────────────────────────────┤    │
│          │  │ Content Area                                 │    │
│          │  │                                              │    │
│          │  └──────────────────────────────────────────────┘    │
│          │                                                      │
├──────────┴──────────────────────────────────────────────────────┤
│ Footer (sadece Customer layout, h: auto)                        │
└─────────────────────────────────────────────────────────────────┘
```

- **Admin Layout:** Sidebar + Header + Main Content (footer yok)
- **Customer Layout:** Header + Main Content + Footer
- **Public Layout (Auth):** Ortada merkezi kart, arka plan gradyan/pattern

---

## 5. Bileşen Kütüphanesi (Component Library)

### 5.1. Butonlar

| Variant | Arka Plan | Metin | Border | Kullanım |
|---|---|---|---|---|
| **Primary** | `--accent-primary` + gradient | white | yok | CTA, kaydet, onayla |
| **Secondary** | `--bg-elevated` | `--text-primary` | `--border-subtle` | İptal, geri, ikincil aksiyonlar |
| **Ghost** | transparent | `--text-secondary` | yok | Toolbar, dropdown trigger |
| **Danger** | `--danger` | white | yok | Sil, iptal et |
| **Outline** | transparent | `--accent-primary` | `--accent-primary` | Alternatif ikincil aksiyonlar |

**Boyutlar:**

| Size | Yükseklik | Padding (H) | Font | İkon Boyutu |
|---|---|---|---|---|
| `sm` | 32px | 12px | 13px | 14px |
| `md` | 36px | 16px | 14px | 16px |
| `lg` | 44px | 24px | 15px | 18px |

**Durumlar:**
- `default` → Normal görünüm
- `hover` → Arka plan koyulaşır/açılır, `scale(1.02)`, `transition 150ms ease`
- `active` → `scale(0.98)`, opacity düşer
- `focus-visible` → `ring-2 ring-offset-2 ring-accent-primary` (keyboard nav)
- `disabled` → `opacity: 0.5`, `cursor: not-allowed`
- `loading` → İçerik soluklaşır, sol tarafta küçük spinner döner, tıklama engellenir

### 5.2. Input / Form Alanları

```
┌─ Label ──────────────────────────────────────┐
│  ┌────────────────────────────────────────┐   │
│  │ ○ Placeholder text                    │   │  ← height: 40px
│  └────────────────────────────────────────┘   │
│  ⚠ Validation error message                  │
└───────────────────────────────────────────────┘
```

| Durum | Border | Arka Plan | İkon |
|---|---|---|---|
| Default | `--border-subtle` | `--bg-elevated` | — |
| Hover | `--border-emphasis` | değişmez | — |
| Focus | `--accent-primary` (ring-2) | değişmez | — |
| Error | `--danger` | `--danger-subtle` | ⚠️ sağ tarafta |
| Disabled | `--border-subtle` opacity 50% | opacity 50% | — |
| Success | `--success` | — | ✓ sağ tarafta |

**Form Layout Kuralları:**
- Label her zaman input üstünde
- Label font: `--text-body-sm`, `font-weight: 500`, `--text-secondary`
- Hata mesajı: `--text-caption`, `--danger`, input'un hemen altında
- Yardımcı metin: `--text-caption`, `--text-muted`
- Alanlar arası boşluk: `--space-4` (16px)
- Form grupları (ilişkili alanlar) arası: `--space-6` (24px)

### 5.3. Veri Tablosu (DataTable)

```
┌──────────────────────────────────────────────────────────────────┐
│ ┌─ Search ─────────────┐   ┌─ Sort ──────┐   [+ Yeni Ekle]    │  ← Toolbar
├──────────────────────────────────────────────────────────────────┤
│ □  İsim ▼     │ Kategori    │ Fiyat ▼     │ Tarih    │ İşlem   │  ← Header
├──────────────────────────────────────────────────────────────────┤
│ □  Ürün Adı   │ Elektronik  │ ₺1.299,00   │ 3 Tem    │ ✎ 🗑   │  ← Row
│ □  Ürün Adı   │ Giyim       │ ₺449,90     │ 2 Tem    │ ✎ 🗑   │
│    ...        │             │             │          │         │
├──────────────────────────────────────────────────────────────────┤
│ ◀ 1 2 3 ... 12 ▶                          Toplam: 115 kayıt    │  ← Pagination
└──────────────────────────────────────────────────────────────────┘
```

- **Kolon başlıkları:** `--text-h4`, tıklanabilir olanlar hover'da vurgulanır + sıralama ikonu
- **Satır hover:** `--bg-elevated` arka plan, yumuşak geçiş (150ms)
- **Zebra striping:** Kullanma — hover highlight yeterli
- **Boş durum:** Merkezde illüstrasyon/ikon + "Henüz kayıt yok" mesajı
- **Aksiyon butonları:** Ghost butonlar (ikon-only), tooltip ile açıklama

### 5.4. Kartlar (Cards)

```
┌─────────────────────────────────────┐
│                                     │
│  [Görsel / İkon Alanı]              │   ← Ürün kartında görsel
│                                     │
├─────────────────────────────────────┤
│  Kategori Etiketi                   │   ← overline, accent rengi
│  Ürün Başlığı                       │   ← h3, max 2 satır, truncate
│  Kısa açıklama...                   │   ← body-sm, text-secondary
│                                     │
│  ₺1.299,00     [Sepete Ekle]        │   ← fiyat + CTA
└─────────────────────────────────────┘
```

**Kart Stilleri:**
- Arka plan: `--bg-surface`
- Border: `1px solid --border-subtle`
- Border-radius: `--radius-lg` (12px)
- Padding: `--space-4` ile `--space-6` arası
- Hover: `border-color --border-emphasis`, ince `box-shadow`, `translateY(-2px)` geçişi
- Glassmorphism varyantı (opsiyonel): `background: rgba(30,41,59,0.6)`, `backdrop-filter: blur(12px)`

### 5.5. Modal / Dialog

```
┌──── Overlay (bg-black/60, backdrop-blur-sm) ──────────────────────────┐
│                                                                        │
│        ┌──────────────────────────────────────────────┐                │
│        │ Modal Başlığı                          [✕]   │  ← Header     │
│        ├──────────────────────────────────────────────┤                │
│        │                                              │                │
│        │  İçerik alanı                                │  ← Body       │
│        │  (form, bilgi, onay metni)                   │                │
│        │                                              │                │
│        ├──────────────────────────────────────────────┤                │
│        │              [İptal]  [Kaydet / Onayla]      │  ← Footer     │
│        └──────────────────────────────────────────────┘                │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

- Max genişlik: `480px` (form), `640px` (detay), `320px` (onay/silme)
- Açılış: `scale(0.95) → scale(1)` + `opacity(0 → 1)`, `200ms ease-out`
- Kapanış: tersi, `150ms ease-in`
- Overlay tıklamasıyla kapatılabilir (silme onayı hariç)
- `Escape` tuşu ile kapatılabilir

### 5.6. Toast / Notification

```
┌──────────────────────────────────────┐
│ ✓  Ürün başarıyla oluşturuldu    ✕  │
└──────────────────────────────────────┘
```

| Tip | Sol İkon | Sol Accent Border | Arka Plan |
|---|---|---|---|
| Success | ✓ (emerald) | `--success` | `--success-subtle` |
| Error | ✕ (red) | `--danger` | `--danger-subtle` |
| Warning | ⚠ (amber) | `--warning` | `--warning-subtle` |
| Info | ℹ (blue) | `--info` | `--info-subtle` |

- Konum: Sağ üst köşe (veya sağ alt)
- Girişi: sağdan kayarak gelir (`translateX(100%) → translateX(0)`, `300ms`)
- Otomatik kapanma: 4 saniye (hover'da duraklat)
- Üst üste stack olabilir (en yeni üstte, max 3 görünür)

### 5.7. Skeleton Loader

Verinin yerini tutan, kontur şekilli, `pulse` animasyonlu gri bloklar.

```
┌─────────────────────────────────────┐
│  ████████████████████  (h:12px)     │  ← Başlık
│  ███████████  (h:10px)              │  ← Alt başlık
│                                     │
│  ████████████████████████████       │
│  ████████████████████████████       │  ← Paragraf satırları
│  ██████████████████                 │
│                                     │
│  ████████  (h:36px, w:100px)        │  ← Buton
└─────────────────────────────────────┘
```

- Arka plan: `--bg-elevated`
- Animasyon: `animate-pulse` (opacity 0.5 ↔ 1.0, 2s, infinite)
- Tablo skeleton: Header sabit, satırlar skeleton
- Kart skeleton: Görsel alanı kare, altında 2-3 satır text skeleton

### 5.8. Empty State

Tablo veya listenin boş olduğu durumda gösterilir:

```
         ┌────────────────────┐
         │    📦              │  ← İllüstrasyon / Büyük ikon (64px)
         │                    │
         │  Henüz ürün yok    │  ← h3, text-primary
         │  İlk ürününüzü     │  ← body, text-secondary
         │  ekleyerek başlayın│
         │                    │
         │  [+ Yeni Ürün Ekle]│  ← CTA buton
         └────────────────────┘
```

### 5.9. Badge / Etiket

| Variant | Arka Plan | Metin | Kullanım |
|---|---|---|---|
| Default | `--bg-elevated` | `--text-secondary` | Genel etiket |
| Primary | `--accent-primary-subtle` | `--accent-primary` | Aktif durum, seçili filtre |
| Success | `--success-subtle` | `--success` | Tamamlandı, aktif |
| Danger | `--danger-subtle` | `--danger` | Silinmiş, hatalı |
| Warning | `--warning-subtle` | `--warning` | Beklemede |

- Boyut: `height: 22px`, `padding: 0 8px`, `border-radius: --radius-full`
- Font: `--text-caption`, `font-weight: 500`

### 5.10. Avatar

- Boyutlar: `sm: 32px`, `md: 40px`, `lg: 56px`
- İçerik: Kullanıcı fotoğrafı veya ismin baş harfleri (ör. "ÖU")
- Fallback: Gradient arka plan + beyaz harf
- Border-radius: `--radius-full`

### 5.11. Sidebar Navigation (Admin)

```
┌──────────────────────┐
│  🛍 Shoppy           │  ← Logo + Brand (h: 64px)
├──────────────────────┤
│                      │
│  📊 Dashboard        │  ← Nav item
│  📦 Ürünler          │  ← Active (accent bg, accent text)
│  📂 Kategoriler      │
│  🛒 Siparişler       │
│  ──────────────      │  ← Separator
│  👥 Kullanıcılar     │
│  🔐 Roller           │
│                      │
├──────────────────────┤
│  👤 Ömer Üren        │  ← User area
│  Admin               │
│  [Çıkış Yap]         │
└──────────────────────┘
```

- Genişlik: `260px` (desktop), `0px → slide-in 260px` (mobile overlay)
- Nav item yüksekliği: `40px`
- Aktif item: `--accent-primary-subtle` bg, `--accent-primary` text, sol `3px` accent border
- Hover: `--bg-elevated` bg
- İkonlar: Lucide React, `18px`, ikon ile metin arası `--space-3`

---

## 6. Ekran Envanteri ve Wireframe Notları

### 6.1. Public Sayfalar

#### Login
- Merkezi kart (max-w: 420px), arka planda gradyan/subtle pattern
- Logo üstte merkezli
- `userName` input (⚠️ email değil!)
- `password` input (göster/gizle toggle'ı)
- "Giriş Yap" primary butonu (tam genişlik)
- "Şifremi Unuttum" link (altında, merkezi)
- Hata durumu: Kartın üstünde kırmızı alert banner
- Hesap kilidi durumu: Amber uyarı banner ("Hesabınız kilitlendi, 15 dk sonra tekrar deneyin")

#### Şifremi Unuttum → Şifre Sıfırlama (Multi-step)
- **Adım 1:** E-posta inputu → "Kod Gönder" butonu
- **Adım 2:** 6 haneli OTP inputu (her hane ayrı kutu veya tek input)
- **Adım 3:** Yeni şifre + şifre tekrar → "Şifremi Sıfırla"
- Adımlar arası geçiş: sola kayma animasyonu
- Üstte step indicator (3 nokta/çizgi)

### 6.2. Müşteri Sayfaları

#### Ana Sayfa / Ürün Kataloğu
- **Hero Section:** Tam genişlik gradient banner, kampanya metni, CTA butonu
- **Filtreler:** Üstte arama barı + kategori dropdown + sıralama dropdown
- **Ürün Grid:** `3 kolon (lg)`, `2 kolon (md)`, `1 kolon (sm)` — kart yapısı (§5.4)
- **Sayfalama:** Alt merkez, numaralı sayfalama + önceki/sonraki oklar

#### Ürün Detay
- Sol: Ürün görseli (placeholder / mock)
- Sağ: Başlık, kategori badge, açıklama, fiyat (büyük ve bold)
- Miktar seçici (`-` `1` `+`) + "Sepete Ekle" butonu
- Alt: İlgili ürünler carousel (opsiyonel)

#### Sepet Drawer
- Sağdan kayan panel (`w: 400px` desktop, tam genişlik mobile)
- Her ürün satırı: küçük görsel + isim + miktar kontrol + fiyat + sil butonu
- Alt: Toplam tutar + "Siparişi Tamamla" butonu
- Boş sepet: empty state (§5.8)

#### Profilim
- Tab yapısı: "Bilgilerim" | "Şifre Değiştir"
- Bilgilerim: Ad, soyad, kullanıcı adı formları + "Güncelle" butonu
- Şifre: Mevcut şifre, yeni şifre, yeni şifre tekrar + "Değiştir" butonu

#### Siparişlerim
- Sipariş kartları listesi (tarih, durum badge, toplam tutar)
- Kart tıklanınca: sipariş detayı accordion veya modal (ürünler, miktarlar)

### 6.3. Admin Sayfaları

#### Dashboard
- 4 istatistik kartı (grid 4 kolon): Toplam Ürün, Kategori, Sipariş, Kullanıcı
- Her kart: büyük sayı + trend ikonu (opsiyonel) + ikon
- Alt: Son siparişler mini tablosu (5 satır)

#### CRUD Sayfaları (Ürünler, Kategoriler, Siparişler, Kullanıcılar, Roller)
Ortak pattern:
1. **Sayfa başlığı** + **"Yeni Ekle" butonu** (sağ üst)
2. **Arama + filtre toolbar**
3. **DataTable** (§5.3)
4. **Oluşturma/Düzenleme:** Modal veya ayrı sayfa (tercih: modal — bağlam kaybını engeller)
5. **Silme:** Danger modal onayı ("Bu kaydı silmek istediğinizden emin misiniz?")

#### Kullanıcı Detay (Admin)
- Kullanıcı bilgileri özet kartı
- Roller bölümü: Mevcut roller listesi (badge'ler) + "Rol Ekle" dropdown + atama butonu

---

## 7. Responsive Tasarım

### 7.1. Breakpoint'ler

| Token | Genişlik | Hedef Cihaz |
|---|---|---|
| `sm` | ≥ 640px | Büyük telefon (landscape) |
| `md` | ≥ 768px | Tablet (portrait) |
| `lg` | ≥ 1024px | Tablet (landscape), küçük laptop |
| `xl` | ≥ 1280px | Masaüstü |
| `2xl` | ≥ 1536px | Geniş ekran |

### 7.2. Responsive Davranışlar

| Bileşen | Mobile (< md) | Tablet (md-lg) | Desktop (≥ lg) |
|---|---|---|---|
| Admin Sidebar | Hamburger → overlay drawer | Daraltılmış (sadece ikon) | Tam genişlik (260px) |
| Ürün Grid | 1 kolon | 2 kolon | 3-4 kolon |
| DataTable | Kart görünümüne dönüşür | Yatay scroll | Tam tablo |
| Sepet | Tam ekran bottom sheet | Sağ drawer (360px) | Sağ drawer (400px) |
| Header | Logo + hamburger + avatar | Logo + arama + avatar | Logo + arama + nav + avatar |
| Modal | Tam ekran (bottom sheet) | Merkezi (max-w: 480px) | Merkezi (max-w: 480px) |
| İstatistik kartları | 2x2 grid | 4x1 grid | 4x1 grid |

---

## 8. Animasyon ve Geçiş Kuralları

### 8.1. Temel Zamanlama

| Hareket | Süre | Easing | Kullanım |
|---|---|---|---|
| Hover efekti | 150ms | `ease` | Buton, kart, link |
| Tooltip gösterme | 200ms | `ease-out` | Tooltip fade-in |
| Modal açma | 200ms | `ease-out` | Scale + opacity |
| Modal kapama | 150ms | `ease-in` | Scale + opacity |
| Sayfa geçişi | 300ms | `ease-out` | Opacity + translateY |
| Drawer slide | 300ms | `cubic-bezier(0.32, 0.72, 0, 1)` | Sidebar, sepet |
| Toast girişi | 300ms | `ease-out` | Sağdan kayma |
| Skeleton pulse | 2000ms | `ease-in-out` | Sürekli döngü |

### 8.2. Sayfa Geçişleri (Framer Motion)

```
Giriş:  opacity: 0 → 1,  y: 8px → 0px,  duration: 300ms
Çıkış:  opacity: 1 → 0,  duration: 150ms
```

### 8.3. Staggered Animation (Liste Öğeleri)

Kart grid'i veya tablo satırları ilk yüklendiğinde:
- Her öğe sırayla belirir (`stagger: 50ms`)
- `opacity: 0 → 1`, `y: 12px → 0`
- Max 8 öğeye kadar stagger, sonrası anında

### 8.4. Kullanıcı Tercih Saygısı

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

---

## 9. İkon Kullanım Rehberi

**Kütüphane:** Lucide React (tutarlılık için tek kaynak)

### 9.1. Sık Kullanılan İkonlar

| Aksiyon/Kavram | İkon | Boyut |
|---|---|---|
| Arama | `Search` | 18px |
| Sepet | `ShoppingCart` | 20px (header), 18px (inline) |
| Kullanıcı | `User` / `Users` | 18px |
| Düzenle | `Pencil` veya `Edit` | 16px |
| Sil | `Trash2` | 16px |
| Ekle | `Plus` | 16px (inline), 18px (buton) |
| Kapat | `X` | 18px |
| Geri | `ArrowLeft` | 18px |
| Sıralama (asc) | `ArrowUp` | 14px |
| Sıralama (desc) | `ArrowDown` | 14px |
| Başarı | `Check` veya `CircleCheck` | 18px |
| Hata | `AlertCircle` veya `CircleX` | 18px |
| Uyarı | `AlertTriangle` | 18px |
| Bilgi | `Info` | 18px |
| Göz (şifre göster) | `Eye` / `EyeOff` | 18px |
| Çıkış | `LogOut` | 18px |
| Ayarlar | `Settings` | 18px |
| Dashboard | `LayoutDashboard` | 18px |
| Ürünler | `Package` | 18px |
| Kategoriler | `FolderOpen` | 18px |
| Siparişler | `ShoppingBag` | 18px |
| Roller | `Shield` | 18px |
| Güneş/Ay (tema) | `Sun` / `Moon` | 18px |
| Menü (hamburger) | `Menu` | 22px |

### 9.2. İkon Kuralları
- Buton içinde ikon + metin: ikon solda, `--space-2` (8px) boşluk
- İkon-only buton: minimum `36x36px` dokunma alanı, `tooltip` zorunlu
- İkon rengi: Bulunduğu metnin rengiyle aynı (`currentColor`)
- Stroke genişliği: Varsayılan (2px), tutarlı tut

---

## 10. Erişilebilirlik (a11y) Standartları

| Kural | Uygulama |
|---|---|
| **Renk kontrastı** | Metin: min 4.5:1, büyük metin: min 3:1 (WCAG AA) |
| **Focus göstergesi** | Tüm interaktif öğelerde görünür `focus-visible` ring |
| **Keyboard navigasyon** | Tab sırası mantıksal, Enter/Space ile tetikleme, Escape ile kapatma |
| **ARIA etiketleri** | Modal: `role="dialog"`, `aria-labelledby`; Toast: `role="alert"` |
| **Alt text** | Tüm dekoratif olmayan görsellerde açıklayıcı `alt` metni |
| **Form etiketleri** | Her input'un `<label>` veya `aria-label` ile ilişkilendirilmesi |
| **Hata mesajları** | `aria-describedby` ile input'a bağlı, screen reader'ın anons etmesi |
| **Loading durumu** | `aria-busy="true"` ve `aria-live="polite"` region |
| **Touch target** | Minimum `44x44px` dokunma alanı (mobile) |

---

## 11. Tema Geçişi (Dark ↔ Light)

- Geçiş: CSS custom property'ler üzerinden, `transition: background-color 200ms, color 200ms`
- Toggle: Header'da `Sun`/`Moon` ikon butonu
- Tercih: `localStorage`'da saklanır, yoksa `prefers-color-scheme` takip edilir
- Uygulama: `<html>` elementine `class="dark"` veya `data-theme="dark"` eklenir
- Tüm renkler `--token` üzerinden çözümlenir, hiçbir yerde hard-coded renk kullanılmaz

---

## 12. Tasarım Kontrol Listesi (Her Ekran İçin)

Yeni bir ekran tasarlarken bu listeyi kontrol et:

- [ ] Dark ve Light mod'da doğru görünüyor mu?
- [ ] Mobile, tablet ve desktop'ta responsive mi?
- [ ] Loading state (skeleton) tanımlandı mı?
- [ ] Empty state tanımlandı mı?
- [ ] Error state tanımlandı mı?
- [ ] Focus göstergesi tüm interaktif öğelerde mevcut mu?
- [ ] Renk kontrastı WCAG AA'yı karşılıyor mu?
- [ ] Animasyonlar `prefers-reduced-motion`'a saygılı mı?
- [ ] Touch target'lar mobile için yeterli mi (44px)?
- [ ] Form hata mesajları inline gösteriliyor mu?
- [ ] Butonlarda loading state var mı?
- [ ] Sıralama ve sayfalama kontrolleri sezgisel mi?

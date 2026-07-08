---
name: dotnet-senior-code-review
description: Use this skill whenever the user shares a .NET / C# project, solution, repository, or codebase and asks for a code review, architecture review, technical debt assessment, production-readiness assessment, or a "senior developer" / "principal engineer" style critique. Trigger this any time the user pastes .NET source files, describes a .NET solution structure, or asks things like "bu projeyi incele", "kod review yap", "mimariyi değerlendir", "bu proje production'a hazır mı", or similar in any language — not just when they say "review" explicitly. Also trigger for requests to score a .NET project's architecture/quality/security/performance, to build a technical-debt table, or to produce a phased improvement roadmap for a .NET codebase. Do not use for non-.NET codebases, or for simple one-off questions about a single line of code.
---

# .NET Senior Code Review

Bu skill, 15+ yıllık deneyime sahip bir Principal Software Engineer / .NET Solution Architect / Senior Code Reviewer gibi davranarak bir .NET projesini uçtan uca, kurumsal yazılım geliştirme standartlarına göre analiz etmek için kullanılır.

Amaç sadece "kod çalışıyor mu" değil; mimari sağlamlık, kod kalitesi, güvenlik, performans, test edilebilirlik ve production readiness açısından **son derece eleştirel** bir değerlendirme sunmaktır. Sadece iyi yönleri övme — kötü tasarım kararlarını, gelecekte teknik borca dönüşecek noktaları ve kurumsal projelerde kabul edilmeyecek yaklaşımları açıkça belirt.

## Önce yapılacaklar

1. Projeye eriş: Kullanıcı dosya/klasör paylaştıysa `view` ile solution yapısını (klasörler, .csproj referansları, namespace organizasyonu) incele. Kod içeriği bağlamda zaten mevcutsa tekrar okumaya gerek yok.
2. Solution'ı bütün olarak tara: katmanlar arası bağımlılıklar, proje referansları, namespace yapısı, klasör organizasyonu — bunları analiz metninin en başında kısaca özetle.
3. Eğer proje/kod paylaşılmadıysa, analiz etmeden önce kullanıcıdan solution'ı (dosyaları, zip, veya en azından ana katmanların kodunu) paylaşmasını iste.

## Analiz Yapısı

Aşağıdaki 11 bölümü sırayla, başlıklar halinde işle. Her bölümde somut kod örnekleri ve varsa "önce / sonra" düzeltme örnekleri ver.

### 1. Mimari İnceleme
Katmanlı mimari uygulaması, Separation of Concerns, SOLID, Clean Architecture yakınlığı, Dependency Injection doğruluğu, katmanlar arası gereksiz bağımlılıklar, circular dependency riski, Repository Pattern, Unit of Work gerekliliği, CQRS uygunluğu, Domain Driven Design eksikleri. Her bulguyu **Critical / High / Medium / Low** olarak sınıflandır.

### 2. Kod Kalitesi Analizi
Readability, Maintainability, Extensibility, Reusability, Testability açısından değerlendir. Şunları tespit et: Code Smells, Anti-patterns, God Classes, Long Methods, Primitive Obsession, Feature Envy, Shotgun Surgery riski, Magic Strings/Numbers, Duplicate Code. Her bulgu için: sorun ne, neden problem, nasıl düzeltilir, düzeltilmiş örnek kod.

### 3. Entity Framework Core Analizi
DbContext tasarımı, Entity Configuration, Fluent API, migration yapısı, Lazy/Eager loading tercihleri, N+1 problemleri, query performansı, Tracking/NoTracking, eksik indexler, transaction ve concurrency yönetimi. Performans/bakım önerileri sun.

### 4. API Tasarımı Analizi (proje API içeriyorsa)
REST standartları, endpoint isimlendirme, HTTP status code kullanımı, versioning, pagination/filtering/sorting, DTO kullanımı, validation, ProblemDetails, error handling. Eksik ve iyileştirmeleri belirt.

### 5. Güvenlik Analizi
SQL Injection, XSS, authentication/authorization eksiklikleri, JWT kullanımı, secret yönetimi, connection string güvenliği, logging sırasında hassas veri sızıntısı, OWASP Top 10 riskleri. Her bulguyu önem seviyesine göre sınıflandır.

### 6. Performans Analizi
Gereksiz database sorguları, bellek kullanımı, LINQ optimizasyonları, async/await kullanımı, gereksiz allocation'lar, caching fırsatları, response sürelerini etkileyen noktalar. Somut örneklerle açıkla.

### 7. Logging ve Monitoring
Structured logging, Serilog kullanımı, exception logging, correlation id, health checks, observability, metrics, distributed tracing eksiklerini belirt.

### 8. Test Altyapısı
Unit/integration test kapsamı, mock kullanımı, test edilebilirlik, coverage eksiklikleri. Eklenmesi gereken testleri somut olarak listele.

### 9. Production Readiness
Ölçeklenebilirlik, bakım kolaylığı, deployment hazırlığı, configuration yönetimi, CI/CD uygunluğu, Docker uygunluğu, cloud readiness. Projeyi şu an production'a çıkarmanın riskini **0-10 arası puanla**.

### 10. Teknik Borç Analizi
Aşağıdaki tabloyu doldur:

| Öncelik | Sorun | Etki | Çözüm |
|---------|-------|------|-------|

### 11. Roadmap
Aşağıdaki fazlarda bir geliştirme planı hazırla; her faz için yapılacaklar, tahmini efor, beklenen kazanım:

- **Faz 1 (Kritik Düzeltmeler)**
- **Faz 2 (Mimari İyileştirmeler)**
- **Faz 3 (Performans ve Güvenlik)**
- **Faz 4 (Kurumsal Seviye Hazırlık)**

## Sonuç Bölümü (her analizin sonunda mutlaka yer almalı)

Aşağıdaki puanlama tablosunu doldur:

| Kategori | Puan (10 üzerinden) |
|----------|---------------------|
| Mimari | |
| Kod Kalitesi | |
| Güvenlik | |
| Performans | |
| Test Edilebilirlik | |
| Bakım Kolaylığı | |
| Production Readiness | |

Ardından şunları değerlendir:
- Proje Junior / Mid-level / Senior seviyesinde mi?
- "Bu projeyi bir iş görüşmesinde GitHub portföyü olarak paylaşsaydım, Senior .NET Developer gözüyle nasıl değerlendirirdin?" sorusuna detaylı cevap ver.

## Ton ve yaklaşım

- Son derece eleştirel ol. Sadece iyi yönleri söyleme.
- Her bulguyu somut kod örnekleriyle destekle.
- Genel geçer ifadelerden kaçın ("kod temiz görünüyor" gibi) — her zaman *neden* ve *nasıl düzeltilir* açıkla.
- Uzun bir analiz olacağı için, çok büyük projelerde bölüm bölüm ilerlemek ve kullanıcıya ilerlemeyi göstermek makul olabilir; ancak 11 bölümün hepsini atlamadan tamamla.

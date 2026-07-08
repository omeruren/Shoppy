# .NET Proje Analizi ve Roadmap Promptu

Sen 15+ yıllık deneyime sahip bir Principal Software Engineer, .NET Solution Architect ve Senior Code Reviewer rolündesin.

Görevin, aşağıda paylaşacağım .NET projesini profesyonel bir ekipte çalışan kıdemli bir geliştirici gibi analiz etmektir.

Kodları yalnızca çalışıyor olup olmadığı açısından değil, gerçek bir kurumsal yazılım geliştirme perspektifinden değerlendirmeni istiyorum.

## Analiz Kuralları

### 1. Mimari İnceleme

- Katmanlı mimari doğru uygulanmış mı?
- Separation of Concerns prensibine uyulmuş mu?
- SOLID prensipleri uygulanmış mı?
- Clean Architecture prensiplerine ne kadar yakın?
- Dependency Injection kullanımı doğru mu?
- Katmanlar arasında gereksiz bağımlılıklar var mı?
- Circular dependency riski var mı?
- Repository Pattern doğru uygulanmış mı?
- Unit of Work gerekli mi?
- CQRS uygulanmalı mı?
- Domain Driven Design açısından eksikler neler?

Her bulguyu önem derecesine göre sınıflandır:

- Critical
- High
- Medium
- Low

### 2. Kod Kalitesi Analizi

Kodları aşağıdaki açılardan değerlendir:

- Readability
- Maintainability
- Extensibility
- Reusability
- Testability

Ayrıca tespit et:

- Code Smells
- Anti-patterns
- God Classes
- Long Methods
- Primitive Obsession
- Feature Envy
- Shotgun Surgery riski
- Magic Strings
- Magic Numbers
- Duplicate Code

Her bulgu için:

- Sorunun ne olduğunu
- Neden problem oluşturduğunu
- Nasıl düzeltilmesi gerektiğini
- Düzeltilmiş örnek kodu

ayrıntılı olarak açıkla.

### 3. Entity Framework Core Analizi

Aşağıdaki konuları incele:

- DbContext tasarımı
- Entity Configuration
- Fluent API kullanımı
- Migration yapısı
- Lazy Loading / Eager Loading tercihleri
- N+1 problemleri
- Query performansı
- Tracking / NoTracking kullanımı
- Index eksiklikleri
- Transaction yönetimi
- Concurrency yönetimi

Performans ve bakım açısından öneriler sun.

### 4. API Tasarımı Analizi

Eğer proje API içeriyorsa incele:

- REST standartları
- Endpoint isimlendirmeleri
- HTTP Status Code kullanımı
- Versioning
- Pagination
- Filtering
- Sorting
- DTO kullanımı
- Validation
- ProblemDetails kullanımı
- Error Handling

Eksikleri ve iyileştirme önerilerini belirt.

### 5. Güvenlik Analizi

Tespit et:

- SQL Injection riskleri
- XSS riskleri
- Authentication eksiklikleri
- Authorization eksiklikleri
- JWT kullanımı
- Secret yönetimi
- Connection String güvenliği
- Logging sırasında hassas veri sızıntıları
- OWASP Top 10 riskleri

Her güvenlik bulgusunu önem seviyesine göre sınıflandır.

### 6. Performans Analizi

Aşağıdaki alanları incele:

- Gereksiz database sorguları
- Bellek kullanımı
- LINQ optimizasyonları
- Async/Await kullanımı
- Gereksiz allocation'lar
- Caching fırsatları
- Response sürelerini etkileyen noktalar

Mümkün olduğunca somut örneklerle açıkla.

### 7. Logging ve Monitoring

Kontrol et:

- Structured Logging
- Serilog kullanımı
- Exception Logging
- Correlation Id
- Health Checks
- Observability
- Metrics
- Distributed Tracing

Eksikleri belirt.

### 8. Test Altyapısı

Analiz et:

- Unit Test kapsamı
- Integration Test kapsamı
- Mock kullanımı
- Test edilebilirlik
- Test coverage eksiklikleri

Eklenmesi gereken testleri belirt.

### 9. Production Readiness

Projeyi gerçek bir şirkette canlıya çıkacakmış gibi değerlendir:

- Ölçeklenebilirlik
- Bakım kolaylığı
- Deployment hazırlığı
- Configuration yönetimi
- CI/CD uygunluğu
- Docker uygunluğu
- Cloud readiness

Bu projeyi şu an üretim ortamına çıkarmak ne kadar riskli?

0-10 arasında puan ver.

### 10. Teknik Borç Analizi

Projedeki teknik borçları listele.

Her madde için:

| Öncelik | Sorun | Etki | Çözüm |
|----------|---------|---------|---------|

şeklinde tablo oluştur.

### 11. Roadmap Oluştur

Analiz sonunda aşağıdaki formatta bir geliştirme planı hazırla:

## Faz 1 (Kritik Düzeltmeler)

- yapılacaklar
- tahmini efor
- beklenen kazanım

## Faz 2 (Mimari İyileştirmeler)

- yapılacaklar
- tahmini efor
- beklenen kazanım

## Faz 3 (Performans ve Güvenlik)

- yapılacaklar
- tahmini efor
- beklenen kazanım

## Faz 4 (Kurumsal Seviye Hazırlık)

- yapılacaklar
- tahmini efor
- beklenen kazanım

### Sonuç Bölümü

Aşağıdaki puanlamayı yap:

| Kategori | Puan (10 üzerinden) |
|-----------|-------------------|
| Mimari |
| Kod Kalitesi |
| Güvenlik |
| Performans |
| Test Edilebilirlik |
| Bakım Kolaylığı |
| Production Readiness |

Ardından:

- Bu proje Junior seviyesinde mi?
- Mid-level seviyesinde mi?
- Senior seviyesinde mi?

değerlendir.

Ayrıca:

> "Bu projeyi bir iş görüşmesinde GitHub portföyü olarak paylaşsaydım, Senior .NET Developer gözüyle nasıl değerlendirirdin?"

sorusunu detaylı cevapla.

Analizi son derece eleştirel yap. Sadece iyi yönleri söyleme. Potansiyel problemleri, kötü tasarım kararlarını, gelecekte teknik borca dönüşecek noktaları ve kurumsal projelerde kabul edilmeyecek yaklaşımları açıkça belirt.

Kod örnekleriyle destekle ve mümkün olduğunca somut geri bildirim ver.

Ek olarak bütün solution'ı tarayarak katmanlar arasındaki bağımlılıkları, namespace yapısını, proje referanslarını ve klasör organizasyonunu da analiz et.

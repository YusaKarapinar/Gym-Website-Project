# Gym Website Project

Bu proje, ASP.NET Core Web API ve MVC kullanılarak geliştirilmiş kapsamlı bir spor salonu yönetim platformudur.

## 🚀 Özellikler

- **Kullanıcı Yönetimi:** ASP.NET Core Identity ile güvenli üyelik sistemi, JWT tabanlı kimlik doğrulama.
- **Spor Salonu ve Hizmetler:** Spor salonlarını listeleme, detaylarını görüntüleme ve sunulan hizmetleri yönetme.
- **Randevu Sistemi:** Üyelerin antrenörlerden veya hizmetlerden randevu alabilmesi.
- **Yapay Zeka Desteği:** Google Gemini AI entegrasyonu ile kişiselleştirilmiş fitness ve beslenme önerileri.
- **Performans:** Redis ile sık erişilen verilerin önbelleklenmesi.
- **Loglama:** Serilog ile detaylı hata ve işlem logları.
- **Docker Desteği:** Kolay kurulum ve dağıtım için Docker Compose yapılandırması.

## 🛠 Teknolojiler

- **Backend:** ASP.NET Core Web API (.NET 9)
- **Frontend:** ASP.NET Core MVC (Razor Views)
- **Veritabanı:** PostgreSQL
- **ORM:** Entity Framework Core
- **Cache:** Redis
- **AI:** Google Gemini API
- **Container:** Docker & Docker Compose

## 📂 Proje Yapısı

- **Project.API:** RESTful API servislerini içerir. Veritabanı işlemleri, kimlik doğrulama ve iş mantığı burada yürütülür.
- **Project.web:** Kullanıcı arayüzünü sağlayan MVC projesidir. API ile haberleşerek verileri sunar.
- **Project.API.Tests & Project.web.Tests:** Birim test projeleri.

## 🔑 Varsayılan Kullanıcılar

Sistem ilk ayağa kalktığında aşağıdaki kullanıcılar otomatik olarak oluşturulur (Seed Data):

| Rol | E-posta | Şifre |
|---|---|---|
| **Admin** | `ogrencinumarasi@sakarya.edu.tr` | `sau` |
| **Trainer** | `ahmet@fittrack.com` | `Trainer123` |
| **Member** | `mehmet@test.com` | `Member123` |

## 🐳 Docker ile Çalıştırma

Projeyi Docker kullanarak hızlıca ayağa kaldırmak için:

```bash
docker compose up -d --build
```

- **API:** http://localhost:8080
- **Web Arayüzü:** http://localhost:8081

## 💻 Yerel Geliştirme Kurulumu

Gereksinimler: .NET 9 SDK, PostgreSQL, Redis.

1. **Bağımlılıkları Yükleyin:**
   ```bash
   dotnet restore
   ```

2. **Veritabanını Oluşturun (API projesi içinde):**
   ```bash
   cd Project.API
   dotnet ef database update
   ```

3. **API'yi Çalıştırın:**
   ```bash
   dotnet run --project Project.API/Project.API.csproj
   ```

4. **Web Arayüzünü Çalıştırın:**
   ```bash
   dotnet run --project Project.web/Project.web.csproj
   ```

## 📝 Doğrulama (Validation)

- **Sunucu Taraflı:** DTO'lar üzerinde DataAnnotations (Required, Range, Email vb.) kullanılarak veri bütünlüğü sağlanır.
- **İstemci Taraflı:** MVC tarafında `_ValidationScriptsPartial` ve aynı notasyonlar ile kullanıcı dostu hata mesajları gösterilir.

## ⚙️ Ortam Değişkenleri

- Bağlantı dizeleri `Project.API/appsettings.json` dosyasında yapılandırılır.
- Yerel çalıştırmalar için `.env` dosyası kullanılabilir.

## 🔗 Önemli API Uç Noktaları

- `POST /api/Users/login`, `POST /api/Users/register`
- `GET/POST/PUT/DELETE /api/Gyms`
- `GET/POST/PUT/DELETE /api/Services`
- `POST /api/Appointments/create`, `PUT /api/Appointments/{id}`, `DELETE /api/Appointments/{id}`
- `POST /api/AI/fitness-recommendation`

## 🧪 Testler

Testleri çalıştırmak için:

```bash
dotnet test
```

## 📌 Notlar

- Identity şifre politikası, varsayılan admin şifresine izin vermek için minimum uzunluk 3 olarak ayarlanmıştır.
- Başlangıçta roller, admin, örnek eğitmenler/üyeler, spor salonları ve hizmetler otomatik olarak eklenir.

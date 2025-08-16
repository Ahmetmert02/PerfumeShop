# PerfumeShop Kurulum Talimatları

Bu proje .NET 8 ve SQL Server kullanılarak geliştirilmiştir.  
Aşağıdaki adımları takip ederek kendi bilgisayarınızda projeyi çalıştırabilirsiniz.  

---

## 1) Gereksinimler
- .NET 8 SDK  
- SQL Server (veya uyumlu bir veritabanı)


## 2) Projeyi Klonlama
```bash
git clone https://github.com/<kullanıcı_adı>/PerfumeShop.git
cd PerfumeShop

3) Connection String Ayarlama

PerfumeShop.API/appsettings.json dosyasında: 

"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=PerfumeShopDb;Trusted_Connection=True;"
}


kısmını kendi veritabanı bilgilerinize göre güncelleyin.
(Gerekirse aynı işlemi PerfumeShop.Web/appsettings.json dosyasında da yapabilirsiniz.)


4) Migration ve Seed Data

Terminali açın ve aşağıdaki komutu çalıştırın:

dotnet ef database update \
  --project PerfumeShop.Repository \
  --startup-project PerfumeShop.API

Bu komut veritabanı tablolarını oluşturur ve SeedData içindeki admin + markaları ekler.

5) Uygulamayı Çalıştırma

API için: 

dotnet run --project PerfumeShop.API

Web arayüzü için:

dotnet run --project PerfumeShop.Web


6) Varsayılan Admin Girişi

E-posta: admin@perfumeshop.com

Şifre: Admin123


Özet:

Projeyi indir

Connection String ayarla

Migration/Seed çalıştır

API + Web başlat

Admin paneline giriş yap



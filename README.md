# Akıllı Fiyat Takip

E-ticaret sitelerindeki ürün linklerini kaydedip fiyatını takip eden bir web sitesi. Hedef fiyata düşünce kullanıcıya e-posta gider.

## Ne yapıyor?

- Hesap oluşturma / giriş
- Ürün linki + hedef fiyat ekleme
- Otomatik fiyat kontrolü
- Fiyat geçmişi grafiği
- TR / EN dil seçeneği

## Stack

- **API:** .NET 8, EF Core, PuppeteerSharp, MailKit  
- **Web:** Next.js 14, Tailwind, Recharts  
- **DB:** PostgreSQL  

## Demo

https://akilli-fiyat-takip.vercel.app

## Çalıştırma

```bash
cp .env.example .env
docker compose up -d --build
```

- Site: http://localhost:3001
- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger

Kapatmak için: `docker compose down`

## Canlı Versiyon

- Demo: https://akilli-fiyat-takip.vercel.app
- API: (Render üzerinde çalışıyor)
- Swagger: (Canlı versiyonda kapalı)

## Ortam değişkenleri

Örnek dosya: `.env.example`  
Canlı için: `deploy/env.production.example`

Şifre ve SMTP bilgilerini repoya koyma; sadece `.env` veya hosting paneli kullan.

## Yayınlama

Kısa notlar: [`deploy/DEPLOY.md`](deploy/DEPLOY.md)

Özet: Neon (DB) + Render (API) + Vercel (web).

## Klasörler

```
backend/    API
frontend/   arayüz
deploy/     yayın notları
```

## License

MIT

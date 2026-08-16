# Yayınlama

Bu proje yerelde Docker Compose ile çalışır. İnternete açmak için tipik kurulum:

1. **PostgreSQL** — Neon (bağlantıda **pooler olmayan** connection string kullan; Hangfire için gerekli)
2. **API** — Render (Docker, kökteki `backend/Dockerfile`)
3. **Web** — Vercel (`frontend` klasörü)

Değişken listesi: `env.production.example`

Frontend URL’ini aldıktan sonra API tarafında `Cors__Origins__0` değerini o adrese ayarla.

Kökteki `render.yaml` Render için başlangıç şablonudur. Oradaki `healthCheckPath: /health` hosting’in “servis ayakta mı?” kontrolüdür; kullanıcıya gösterilen demo adresi değildir.

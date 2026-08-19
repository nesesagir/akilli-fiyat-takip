# Yayınlama

1. **PostgreSQL** — Neon (Frankfurt). Connection string’i **Host=...;Database=...;SSL Mode=Require** formatında Render’a yapıştır. `postgresql://...?...=` URI kullanma (panelde bozuluyor).
2. **API** — Render Docker (`backend/Dockerfile`). `Hangfire__Enabled=false`, fiyat kontrolü `PriceCheck` BackgroundService.
3. **Web** — Vercel (`frontend`), `NEXT_PUBLIC_API_URL` = Render API adresi.

`healthCheckPath: /health` sadece hosting kontrolüdür; Demo adresi site URL’sidir.

Değişken şablonu: `env.production.example`

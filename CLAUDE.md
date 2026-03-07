# Tarot App — Claude Context

## Overview
Monorepo with a SvelteKit frontend and .NET 8 backend, deployed on Zeabur.

## Architecture

```
tarot-app/
├── frontend/   SvelteKit 2 + Svelte 5 (adapter-node, SPA mode)
├── backend/    ASP.NET Core 8 + EF Core + Npgsql
├── database/   Supabase migrations
└── docker-compose.yml  (local dev only)
```

## Zeabur Deployment
| Service | Name | Public URL | Port |
|---------|------|-----------|------|
| Frontend | tarot-app-uram | rtarot.zeabur.app | 8080 |
| Backend | tarot-app-ist | rtarot-api.zeabur.app | 8080 |

Health check: `GET https://rtarot-api.zeabur.app/api/health`

## Local Development
```bash
docker compose up --build
# Frontend → http://localhost:5173
# Backend  → http://localhost:5098
```

Or run individually:
```bash
# Frontend
cd frontend && pnpm dev

# Backend
cd backend && dotnet watch run --project TarotApi
```

## Environment Variables

### Frontend (build-time, baked in by Vite)
```
PUBLIC_SUPABASE_URL=
PUBLIC_SUPABASE_ANON_KEY=
```

### Backend (runtime)
```
PUBLIC_SUPABASE_URL=          # Also used for JWKS endpoint
SUPABASE_JWT_SECRET=          # Required at startup
SUPABASE_DB_CONNECTION_STRING= # PostgreSQL connection string
ALLOWED_ORIGINS=              # Comma-separated (e.g. https://rtarot.zeabur.app)
```

`.env` at repo root; `frontend/.env` is a symlink to `../.env`.

## Backend API Endpoints
All require `Authorization: Bearer <supabase-jwt>` except health.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/health | No | Health check |
| GET | /api/tarot/cards | Yes | List all 78 cards |
| GET | /api/tarot/cards/{id} | Yes | Card detail |
| GET | /api/readings | Yes | List user's readings |
| POST | /api/readings | Yes | Create reading |
| GET | /api/profile | Yes | Get profile |
| PUT | /api/profile | Yes | Update profile |

Swagger UI available at `/swagger` in Development environment only.

## Auth
- Supabase Auth on the frontend issues JWTs (ES256)
- Backend validates via JWKS: `{PUBLIC_SUPABASE_URL}/auth/v1/.well-known/openid-configuration`
- `SUPABASE_JWT_SECRET` is loaded but JWKS is the actual signing key source

## Key Source Files

### Backend
- `backend/TarotApi/Program.cs` — DI wiring, CORS, JWT config
- `backend/TarotApi/Controllers/` — 4 controllers (Health, Tarot, Reading, Profile)
- `backend/TarotApi/Services/` — TarotService, ReadingService, ProfileService
- `backend/TarotApi/Data/TarotDbContext.cs` — EF Core context
- `backend/TarotApi/Data/TarotCards.cs` — 78-card static data
- `backend/TarotApi/Middleware/ExceptionHandlingMiddleware.cs` — global error handling

### Frontend
- `frontend/src/lib/supabase.ts` — Supabase client
- `frontend/src/lib/tarot/` — Card data, spread logic
- `frontend/src/lib/components/` — UI components
- `frontend/src/routes/+layout.js` — `ssr = false`

## Common Debug Tips

### Backend won't start
Check all required env vars are set: `PUBLIC_SUPABASE_URL`, `SUPABASE_JWT_SECRET`, `SUPABASE_DB_CONNECTION_STRING`

### 401 Unauthorized from API
- Ensure frontend is sending `Authorization: Bearer <token>` header
- Token must be from Supabase Auth (ES256, audience: `authenticated`)
- Verify `PUBLIC_SUPABASE_URL` matches the Supabase project issuing tokens

### CORS errors
- Add the frontend origin to `ALLOWED_ORIGINS` env var on the backend
- Format: `https://rtarot.zeabur.app` (no trailing slash)

### Frontend build fails on Zeabur
- `PUBLIC_SUPABASE_URL` and `PUBLIC_SUPABASE_ANON_KEY` must be set as build-time env vars (ARGs) in Zeabur

### Database migrations
- Migrations live in `database/` and target the Supabase PostgreSQL instance
- EF Core uses `SUPABASE_DB_CONNECTION_STRING` at runtime

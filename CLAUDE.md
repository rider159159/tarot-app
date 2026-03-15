# Tarot App — Claude Context

## Overview
Monorepo with a SvelteKit frontend and .NET 8 backend, deployed on Zeabur.

## Architecture

```
tarot-app/
├── frontend/              SvelteKit 2 + Svelte 5 (adapter-node, ssr=false)
│   ├── src/
│   │   ├── hooks.server.ts       Auth middleware & route guards
│   │   ├── routes/               Pages (see Frontend Routes below)
│   │   └── lib/
│   │       ├── supabase.ts       Supabase browser client
│   │       ├── server/api.ts     Server-side API client
│   │       ├── components/       UI components (6 files)
│   │       ├── tarot/            Card data, spreads, images
│   │       ├── types/            TypeScript definitions
│   │       └── utils/            Reading helpers
│   └── svelte.config.js          adapter-node, builds to build/
├── backend/               ASP.NET Core 8 + EF Core + Npgsql
│   └── TarotApi/
│       ├── Program.cs            DI, CORS, JWT, middleware pipeline
│       ├── Controllers/          4 controllers (Health, Tarot, Reading, Profile)
│       ├── Services/             TarotService, ReadingService, ProfileService
│       ├── Models/               Entities, DTOs, enums
│       ├── Data/                 EF Core context, 78-card seed data
│       └── Middleware/           ExceptionHandlingMiddleware
├── database/              Supabase migrations
├── docker-compose.yml     Local dev only
└── .env                   Shared env vars (frontend/.env symlinks here)
```

## Zeabur Deployment
| Service | Name | Public URL | Port |
|---------|------|-----------|------|
| Frontend | tarot-app-uram | rtarot.zeabur.app | 8080 |
| Backend | tarot-app-ist | rtarot-api.zeabur.app | 8080 |

Health check: `GET https://rtarot-api.zeabur.app/api/health`

## Local Development

Prerequisites: Node.js 22.x (per `.nvmrc`), pnpm 9.15.4, .NET 8 SDK

```bash
# Option 1: Docker (recommended, starts both services)
docker compose up --build
# Frontend → http://localhost:5173  (Vite dev server, hot reload)
# Backend  → http://localhost:5098  (dotnet watch, live reload)
# Swagger  → http://localhost:5098/swagger
```

```bash
# Option 2: Run individually
cd frontend && pnpm install && pnpm dev
cd backend && dotnet watch run --project TarotApi
```

Useful commands:
```bash
cd frontend && pnpm check    # TypeScript verification
cd frontend && pnpm build    # Production build → build/ with 200.html SPA fallback
```

## Environment Variables

### Frontend (build-time, baked in by Vite)
```
PUBLIC_SUPABASE_URL=
PUBLIC_SUPABASE_ANON_KEY=
```

### Frontend (server-side, runtime)
```
INTERNAL_API_URL=            # Backend URL for server-side API calls (default: http://localhost:5098)
```

### Backend (runtime)
```
PUBLIC_SUPABASE_URL=          # Also used for JWKS endpoint
SUPABASE_JWT_SECRET=          # Required at startup
SUPABASE_DB_CONNECTION_STRING= # PostgreSQL connection string
ALLOWED_ORIGINS=              # Comma-separated (e.g. https://rtarot.zeabur.app)
```

`.env` at repo root; `frontend/.env` is a symlink to `../.env`.

## Frontend Routes

| Route | Page | Description |
|-------|------|-------------|
| `/` | Home | Tarot reading (spread selection, question, draw) |
| `/login` | Login | Email/password login |
| `/register` | Register | New account registration |
| `/history` | History | Paginated reading history (PAGE_SIZE=10) with delete |
| `/profile` | Profile | User info, reading stats, name editing |
| `/auth/callback` | — | OAuth callback (exchanges code for session) |
| `/auth/logout` | — | POST endpoint, signs out and redirects to /login |

Auth routing: unauthenticated users redirected to `/login`; authenticated users redirected away from `/login` and `/register`.

## Backend API Endpoints
All require `Authorization: Bearer <supabase-jwt>` except health.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/health | No | Health check |
| GET | /api/tarot/cards | Yes | List all 78 cards |
| GET | /api/tarot/cards/{id} | Yes | Card detail |
| GET | /api/readings | Yes | List user's readings (query: `page`, `pageSize`, max 50) |
| GET | /api/readings/{id} | Yes | Get specific reading |
| GET | /api/readings/stats | Yes | Reading statistics (top cards, spread usage) |
| POST | /api/readings | Yes | Create reading |
| DELETE | /api/readings/{id} | Yes | Delete reading |
| GET | /api/profile | Yes | Get profile |
| PUT | /api/profile | Yes | Update profile (displayName) |

Swagger UI available at `/swagger` in Development environment only.

## Auth
- Supabase Auth on the frontend issues JWTs (ES256)
- Backend validates via JWKS: `{PUBLIC_SUPABASE_URL}/auth/v1/.well-known/openid-configuration`
- `SUPABASE_JWT_SECRET` is loaded but JWKS is the actual signing key source
- Global `[Authorize]` filter on all controllers; `[AllowAnonymous]` on health only

## Key Source Files

### Backend
- `backend/TarotApi/Program.cs` — DI wiring, CORS, JWT config, middleware pipeline
- `backend/TarotApi/Controllers/` — HealthController, TarotController, ReadingController, ProfileController
- `backend/TarotApi/Services/TarotService.cs` — Card drawing (Fisher-Yates shuffle, cryptographic RNG), spread configs, feeling card logic
- `backend/TarotApi/Services/ReadingService.cs` — CRUD for readings, stats aggregation (raw SQL for JSONB)
- `backend/TarotApi/Services/ProfileService.cs` — Profile CRUD
- `backend/TarotApi/Data/TarotDbContext.cs` — EF Core context (profiles, readings tables)
- `backend/TarotApi/Data/TarotCards.cs` — 78-card static data (Chinese names, meanings, keywords)
- `backend/TarotApi/Models/` — Profile, Reading entities; SpreadType enum; 8 DTOs
- `backend/TarotApi/Middleware/ExceptionHandlingMiddleware.cs` — Global error handling
- `backend/TarotApi/Extensions/ClaimsPrincipalExtensions.cs` — GetUserId() from JWT sub claim

### Frontend
- `frontend/src/hooks.server.ts` — Auth middleware, session validation, route guards
- `frontend/src/lib/supabase.ts` — Supabase browser client (@supabase/ssr)
- `frontend/src/lib/server/api.ts` — Server-side API client (createServerApiClient with Bearer token)
- `frontend/src/lib/types/index.ts` — All TypeScript types (TarotCard, SpreadType, DTOs, API response types)
- `frontend/src/lib/utils/reading.ts` — mapApiResponse, getSpreadName, formatDate helpers
- `frontend/src/lib/tarot/cards.ts` — Combined 78-card data (allCards, cardById, getCardById)
- `frontend/src/lib/tarot/major-arcana.ts` — 22 major arcana definitions
- `frontend/src/lib/tarot/minor-arcana.ts` — 56 minor arcana definitions
- `frontend/src/lib/tarot/spread.ts` — 5 spread configs (single, three-card-time, three-card-problem, three-card-linear, celtic-cross)
- `frontend/src/lib/tarot/card-images.ts` — Wikimedia Commons image URLs for all 78 cards
- `frontend/src/lib/tarot/readings.ts` — saveReading() to Supabase
- `frontend/src/lib/components/Navbar.svelte` — Top navigation with auth-aware display
- `frontend/src/lib/components/SpreadSelector.svelte` — Spread type radio selector (5 options)
- `frontend/src/lib/components/QuestionInput.svelte` — Optional question textarea
- `frontend/src/lib/components/DrawButton.svelte` — Draw button with loading state
- `frontend/src/lib/components/CardResult.svelte` — Individual card display with image, meaning, orientation
- `frontend/src/lib/components/ReadingDisplay.svelte` — Full reading results with feeling card section

## Spread Types
| Key | Cards | Description |
|-----|-------|-------------|
| `single` | 1 | Daily guidance |
| `three-card-time` | 3 (+feeling) | Past / Present / Future |
| `three-card-problem` | 3 (+feeling) | Problem / Cause / Solution |
| `three-card-linear` | 3 (+feeling) | First / Second / Third |
| `celtic-cross` | 10 (+feeling) | Classic 10-card layout |

Non-single spreads include an extra "feeling card" for additional context.

## Custom Commands

| Command | Description |
|---------|-------------|
| `/pm` | 產品經理審查 — 檢查未提交變更的完整性（前後端型別一致性、API 合約、UI 狀態、認證、DB 遷移、牌陣邏輯、部署影響、文件更新） |
| `/pm <說明>` | 帶上下文的聚焦審查（如 `/pm 新增了 weekly-fortune 牌陣`） |

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

### Docker hot reload not working
- Frontend: Vite polling enabled in `vite.config.ts` (required for Docker volumes)
- Backend: `DOTNET_USE_POLLING_FILE_WATCHER=true` set in docker-compose.yml

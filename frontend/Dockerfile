# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM node:22-slim AS builder
RUN corepack enable
WORKDIR /app

COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile

COPY . .

# PUBLIC_* vars are inlined at build time by SvelteKit
ARG PUBLIC_SUPABASE_URL
ARG PUBLIC_SUPABASE_ANON_KEY
ENV PUBLIC_SUPABASE_URL=$PUBLIC_SUPABASE_URL
ENV PUBLIC_SUPABASE_ANON_KEY=$PUBLIC_SUPABASE_ANON_KEY

RUN pnpm build

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM node:22-slim
WORKDIR /app
RUN corepack enable

COPY package.json pnpm-lock.yaml* ./
# --ignore-scripts 跳過 prepare (svelte-kit sync)，只裝 runtime deps
RUN pnpm install --prod --frozen-lockfile --ignore-scripts

COPY --from=builder /app/build ./build

EXPOSE 3000
ENV PORT=3000
ENV HOST=0.0.0.0
ENV NODE_ENV=production

CMD ["node", "build/index.js"]

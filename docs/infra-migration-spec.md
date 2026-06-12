# tarot-app 基礎設施遷移 + CI/CD 規格文件

> **文件用途**：tarot-app 配合 oci-infra 架構整合所需的改動，以及未來 CI/CD 自動部署規劃。
>
> **文件版本**：v1.0
> **建立日期**：2026-06-12
> **關聯文件**：`oci-deploy` repo 的 `docs/caddy-to-nginx-and-oci-infra-spec.md`

---

## 1. 背景

tarot-app 目前的 production stack（`docker-compose.prod.yml`）包含 frontend、backend、caddy 三個服務，全部在同一個 compose 內。

即將進行的基礎設施變更：

1. **反向代理 Caddy → Nginx**：Nginx 由新的 `oci-infra` repo 獨立管理（Docker 容器）
2. **oci-infra 接管機器級基礎設施**：Nginx、CouchDB、iptables 等統一在 `oci-infra` repo
3. **跨 compose 通訊**：tarot-app 的服務透過 `web` external docker network 與 Nginx 容器互通

---

## 2. 現況

### 2.1 現有 docker-compose.prod.yml

```
services:
  frontend  (expose: 3000)  ─┐
  backend   (expose: 5098)  ─┼── 同一 compose 內部網路
  caddy     (ports: 80/443) ─┘
```

- Caddy 是唯一對外的服務，掛 Cloudflare Origin Cert
- frontend / backend 用 `expose`，只在 compose 內部可達
- 部署指令：`docker compose -f docker-compose.prod.yml up -d --build`

### 2.2 現有部署流程（純手動）

```
開發者 → ssh oci
       → cd ~/projects/tarot-app
       → git pull
       → docker compose -f docker-compose.prod.yml up -d --build
       → 等 5-10 分鐘（ARM 首次 build）
       → 手動驗證
```

無 CI/CD，無自動化。

---

## 3. Phase 1：compose 改造（配合 Nginx 遷移）

### 3.1 改動概覽

| 項目 | 現況 | 改成 |
|------|------|------|
| caddy service | 存在 | **移除** |
| caddy volumes | `caddy_data`, `caddy_config` | **移除** |
| Caddyfile | 存在（repo 根目錄） | **刪除**（由 oci-infra 的 nginx conf.d/ 取代） |
| frontend network | compose 預設 | 加入 `web` external network |
| backend network | compose 預設 | 加入 `web` external network |
| frontend container_name | 無（compose 自動生成） | 明確設定 `container_name: frontend` |
| backend container_name | 無 | 明確設定 `container_name: backend` |

### 3.2 新版 docker-compose.prod.yml

```yaml
# Production stack for self-hosting on the OCI ARM (aarch64) machine.
# Reverse proxy (Nginx) is managed by oci-infra repo, not this compose.
# All services join the shared 'web' docker network for Nginx to reach.
#
# Bring up:  docker compose -f docker-compose.prod.yml up -d --build
# Secrets come from .env (never committed). See .env.example for required keys.

services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
      args:
        PUBLIC_SUPABASE_URL: ${PUBLIC_SUPABASE_URL}
        PUBLIC_SUPABASE_ANON_KEY: ${PUBLIC_SUPABASE_ANON_KEY}
    container_name: frontend
    environment:
      - INTERNAL_API_URL=http://backend:5098
      - ORIGIN=https://tarot.rydercloud.cc
    expose:
      - "3000"
    restart: unless-stopped
    depends_on:
      - backend
    networks:
      - web
      - internal

  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: backend
    env_file:
      - .env
    expose:
      - "5098"
    restart: unless-stopped
    networks:
      - web
      - internal

networks:
  web:
    external: true
    name: web
  internal:
    # frontend ↔ backend 的內部通訊，不經 Nginx
```

> **設計要點**：
> - `web` (external)：讓 Nginx 容器能連到 frontend/backend
> - `internal`：frontend ↔ backend 的 SSR 呼叫（`INTERNAL_API_URL=http://backend:5098`）走內部網路，不經 Nginx
> - `container_name` 明確設定：Nginx conf 裡 `proxy_pass http://frontend:3000` 依賴這個名字
> - **沒有 `ports`**：所有對外流量由 Nginx 處理，app 服務不直接暴露
> - caddy 相關全部移除

### 3.3 刪除的檔案

- `Caddyfile`（反向代理設定移到 oci-infra）
- `caddy/` 目錄（Origin Cert 移到 oci-infra/ssl/）

### 3.4 不動的東西

- `frontend/Dockerfile` — 不變
- `backend/Dockerfile` — 不變
- `.env` / `.env.example` — 不變
- `ORIGIN` 環境變數 — 仍然需要（SvelteKit CSRF，跟反向代理是 Caddy 或 Nginx 無關）
- `docker-compose.yml`（dev 版）— 不變

---

## 4. Phase 2：CI/CD 自動部署（Nginx 遷移穩定後再做）

> **前置條件**：Phase 1 完成、Nginx 遷移穩定、手動部署流程已驗證。

### 4.1 目標

```
push to main → GitHub Actions → SSH 到 OCI → pull + build + up → 自動完成
```

開發者只需要 push code，不用 SSH 到機器。

### 4.2 架構

```
GitHub (tarot-app repo)
    │
    │  push to main
    ▼
GitHub Actions Runner
    │
    │  SSH (deploy key)
    ▼
OCI ARM 機器
    │
    ├── cd ~/projects/tarot-app
    ├── git pull origin main
    ├── docker compose -f docker-compose.prod.yml up -d --build
    └── health check（curl /api/health）
```

### 4.3 GitHub Actions workflow

```yaml
# .github/workflows/deploy.yml
name: Deploy to OCI

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.OCI_HOST }}
          username: ${{ secrets.OCI_USER }}
          key: ${{ secrets.OCI_SSH_KEY }}
          script: |
            cd ~/projects/tarot-app
            git pull origin main
            docker compose -f docker-compose.prod.yml up -d --build
            # 等容器起來
            sleep 10
            # Health check
            curl -sf http://localhost:5098/api/health || exit 1
```

### 4.4 需要設定的 GitHub Secrets

| Secret | 說明 |
|--------|------|
| `OCI_HOST` | `134.185.107.141` |
| `OCI_USER` | `ubuntu` |
| `OCI_SSH_KEY` | 部署專用的 SSH private key（不是你登入用的那把） |

> **安全建議**：為 CI/CD 另外生一把 SSH key pair，只授權 `git pull` + `docker compose` 操作。不要用你的個人 SSH key。

### 4.5 停機問題

Phase 2 的 CI/CD 仍然是「停舊起新」模式，會有幾秒斷線。這在目前流量下完全可接受。

### 4.6 Phase 3（更遠的未來）：零停機部署

等到真的需要時才做：

| 方案 | 說明 |
|------|------|
| `docker rollout` 插件 | 最簡單，一行指令做到 rolling update |
| 藍綠部署腳本 | 自己寫：起 v2 → health check → Nginx 切流量 → 停 v1 |

**目前不需要實作**，記錄在這裡供未來參考。

---

## 5. 實作順序

```
═══ Phase 1：compose 改造（配合 Nginx 遷移）═══
  [ ] 新版 docker-compose.prod.yml（§3.2）
  [ ] 刪除 Caddyfile
  [ ] 確認 web docker network 已建立
  [ ] docker compose up -d --build 驗證
  [ ] 外部經 Nginx + CF 驗證 https://tarot.rydercloud.cc 可達

═══ Phase 2：CI/CD（Phase 1 穩定後）═══
  [ ] 生 deploy SSH key pair
  [ ] GitHub Secrets 設定
  [ ] .github/workflows/deploy.yml
  [ ] push to main 觸發部署驗證
  [ ] 確認 health check 能攔住失敗的 build

═══ Phase 3：零停機（有需要時才做）═══
  [ ] 評估 docker rollout 或藍綠方案
```

---

## 6. 與 oci-infra 的分工

```
oci-infra repo                          tarot-app repo
├── Nginx 容器 + conf.d/tarot-app.conf  ├── frontend Dockerfile + 程式碼
├── Origin Cert 管理                     ├── backend Dockerfile + 程式碼
├── scripts/up-all.sh（全機啟動）         ├── docker-compose.prod.yml（app 服務）
└── docs/services-registry.md            ├── .github/workflows/deploy.yml（CI/CD）
                                         └── .env（secrets，不進版控）

        ╌╌╌╌ web docker network 連接兩邊 ╌╌╌╌
```

**契約**：tarot-app 保證 `container_name: frontend` / `container_name: backend` 在 `web` network 上可達。oci-infra 的 Nginx conf 依賴這個契約。任一邊改名要同步通知。

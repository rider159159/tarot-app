# App observability foundation

> Deployment status: **NOT_DEPLOYED**. This document describes the code and
> Compose changes in this branch. Merging it does not deploy containers or
> connect the app to Loki, Prometheus, or Grafana.

## What this change provides

- The .NET backend writes one JSON object per console log line with UTC
  timestamps and scopes.
- `X-Request-ID` is accepted only when it contains 1-128 ASCII letters,
  digits, `.`, `_`, or `-`; otherwise the frontend/backend generates a new ID.
- SvelteKit forwards that request ID on server-side calls to the backend.
- Backend request completion logs contain only method, route template, status,
  duration, and request ID. Query strings, request/response bodies, auth
  headers, cookies, user/email values, client IPs, and concrete URL paths are
  intentionally excluded.
- The backend exposes Prometheus metrics at internal endpoint `/metrics` using
  bounded framework labels. No user, request ID, or raw path is added as a
  metric label.
- Production containers use Docker `json-file` rotation: 10 MiB per file,
  three files per service.

## Health endpoints

| Endpoint | Meaning | Dependency |
| --- | --- | --- |
| Frontend `GET /health` | Public SvelteKit process status | None |
| Backend `GET /api/health` | Existing public compatibility endpoint | None |
| Backend `GET /_internal/health/live` | Internal backend process status | None |
| Backend `GET /_internal/health/ready` | Internal database readiness | Supabase database |

Readiness has a three-second database timeout and returns only status and UTC
timestamp. Connection strings and exception details are never returned.
Docker uses liveness, not readiness, so a temporary Supabase outage does not
cause a container restart loop. The frontend waits for the backend container
to become healthy before starting.

The frontend `/health` route is intentionally reachable through the existing
catch-all frontend proxy. It returns only status and timestamp; the correlation
middleware adds `X-Request-ID`. Nginx does not route `/_internal/*` or
`/metrics` to the backend; its catch-all sends them to the frontend.
The backend endpoints therefore remain reachable only from a private container
network unless infrastructure routing is changed later.

## Relationship to `oci-infra`

The companion `oci-infra` observability branch is expected to provide Loki,
Prometheus, and Grafana. This app branch deliberately does not add a new
external Docker network: doing so before the network exists on OCI would make
the current deployment fail. The live rollout must first verify the actual OCI
Compose project/network names, then give Prometheus a private route to the
backend `/metrics` endpoint and give Alloy access to Docker logs.

## Live rollout checks (requires OCI SSH)

1. Confirm the existing `web` network and running container names before
   changing any network attachment.
2. Build and start this stack, then verify both Docker health statuses.
3. Verify `/_internal/health/live` is `200` from inside the backend container
   network and temporarily validate that readiness changes to `503` when
   database access is unavailable.
4. Send a safe `X-Request-ID` through Nginx and confirm the same value appears
   in frontend response headers and backend JSON log scopes.
5. Configure Prometheus to scrape `/metrics` over a private Docker network and
   verify that neither `/metrics` nor backend `/_internal/*` is added to public
   Nginx routes. Public frontend `/health` should still return only status,
   timestamp, and `X-Request-ID`.

No database schema change or migration is required for this work.

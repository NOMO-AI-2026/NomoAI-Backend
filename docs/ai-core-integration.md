# AI Core Integration (ASP.NET ↔ FastAPI)

## Architecture flow

```text
Flutter / React  →  NomoAI ASP.NET Core API  →  FastAPI AI Core
                         │                         │
                    SQL Server                  OpenRouter / Qdrant / Whisper
```

- Clients never call FastAPI directly.
- ASP.NET attaches `X-AI-Service-Key` and forwards `X-Correlation-ID`.
- AI Core returns typed plans / evaluations / summaries; ASP.NET owns persistence (not stored by the temporary integration endpoints).

## Local URLs

| Service | URL |
|---------|-----|
| FastAPI AI Core | `http://localhost:8000` |
| ASP.NET API (http profile) | `http://localhost:5221` |
| ASP.NET Swagger | `http://localhost:5221/swagger` |
| ASP.NET AI readiness | `http://localhost:5221/health/ai` |

## Required configuration

Section name: `AiService`

```json
{
  "AiService": {
    "BaseUrl": "http://localhost:8000",
    "ServiceKey": "local-development-secret",
    "TimeoutSeconds": 180,
    "HealthTimeoutSeconds": 10,
    "MaxRetryAttempts": 2,
    "MaxAudioBytes": 10485760
  }
}
```

Committed `appsettings.json` / `appsettings.Production.json` keep empty `BaseUrl` and `ServiceKey` placeholders only.

Do not commit production secrets. Prefer user secrets locally and environment variables in deployment.

### User secrets (local)

```bash
cd NomoAI.API
dotnet user-secrets set "AiService:BaseUrl" "http://localhost:8000"
dotnet user-secrets set "AiService:ServiceKey" "local-development-secret"
```

### Production hosting environment variables

Set these on the ASP.NET Core host (IIS / Azure / Docker / VPS process env). Do **not** put the real secret in git.

```text
AiService__BaseUrl=http://191.218.161.183
AiService__ServiceKey=<same value as FastAPI AI_SERVICE_API_KEY on the VPS>
AiService__TimeoutSeconds=180
AiService__HealthTimeoutSeconds=10
AiService__MaxRetryAttempts=2
```

`BaseUrl` must be the FastAPI **host root only** (scheme + host[:port]). Do **not** include `/api/v1`.

Final request composition examples:

| ASP.NET call | Resolved FastAPI URL |
|--------------|----------------------|
| Plan | `http://191.218.161.183/api/v1/sessions/plan` |
| Evaluate | `http://191.218.161.183/api/v1/sessions/attempts/evaluate` |
| Summary | `http://191.218.161.183/api/v1/sessions/summary` |
| Ready health | `http://191.218.161.183/ready` |

### Future HTTPS / domain switch

When DNS and TLS are ready, change **only**:

```text
AiService__BaseUrl=https://ai.example.com
```

- Moving from IP → domain: change `AiService__BaseUrl` only.
- Rotating the secret: change `AiService__ServiceKey` (and FastAPI `AI_SERVICE_API_KEY`) together.
- **No application code change** is required.
- The frontend still calls ASP.NET Core only.

## Run both services locally

1. Start FastAPI (from the Ai-Service repository):

```bash
py -3.11 -m uvicorn app.main:app --reload --port 8000
```

2. Start ASP.NET (from Nomo-Backend):

```bash
cd NomoAI.API
dotnet run --launch-profile http
```

ASP.NET does **not** start or manage the FastAPI process.

## Smoke checks (local)

```bash
curl http://localhost:8000/health
curl -H "X-AI-Service-Key: local-development-secret" http://localhost:8000/ready
curl http://localhost:5221/health/ai
```

## Manual verification against deployed FastAPI VPS

Requires FastAPI to be running on the VPS first. ASP.NET build success alone is not end-to-end proof.

1. FastAPI liveness:

```bash
curl http://191.218.161.183/health
```

2. FastAPI readiness (service key required):

```bash
curl -H "X-AI-Service-Key: <secret>" http://191.218.161.183/ready
```

3. Point ASP.NET at the VPS via env vars (see above), restart ASP.NET, then:

```bash
curl http://localhost:5221/health/ai
```

4. Swagger (Doctor/Parent JWT) against ASP.NET:

- `POST /api/sessions/ai/plan`
- `POST /api/sessions/ai/evaluate` (multipart audio)
- `POST /api/sessions/ai/summary`

5. Optional: Doctor frontend test session (still talks to ASP.NET only).

Temporary authenticated integration routes (Doctor or Parent JWT):

- `POST /api/sessions/ai/plan`
- `POST /api/sessions/ai/evaluate` (multipart audio)
- `POST /api/sessions/ai/summary`

These endpoints proxy to AI Core and do not persist AI responses.

## Security boundaries

- Service key stays on the server; never returned to clients.
- BaseUrl is server configuration only.
- Logs must not include service keys, audio bytes, transcripts, targets, prompts, or personal identifiers.
- Audio is validated for size and content type before forwarding (default max 10 MB).
- TLS certificate validation is not disabled.

## Timeout and retry behavior

| Setting | Default | Purpose |
|---------|---------|---------|
| `TimeoutSeconds` | 180 | Plan / evaluate / summary (Whisper-friendly) |
| `HealthTimeoutSeconds` | 10 | `/health` and `/ready` |
| `MaxRetryAttempts` | 2 | Extra attempts after the first |

Retries apply only to transient failures (`429`, `502`, `503`, `504`, network errors) for JSON calls.

**EvaluateAttempt does not auto-retry** because multipart audio streams are not safely replayable.

Do not retry `400`, `401`, `403`, `404`, or `422`.

ASP.NET starts even when FastAPI is offline; `/health/ai` reports Unhealthy/Degraded clearly without leaking secrets.

## Evaluate endpoint (Swagger multipart)

`POST /api/sessions/ai/evaluate` accepts `multipart/form-data` with:

- `Audio` — required binary file (Swagger file picker)
- Form fields: `ChildId`, `ActivityId`, `ActivityType`, `TargetValue`, `SpeechLevel`, `Age`, `AttemptNumber`, plus optional metadata

Swagger metadata is corrected by `EvaluateAttemptFormOperationFilter` because Swashbuckle otherwise renders a single JSON-like `form` object for `[FromForm]` complex types on Minimal APIs.

### Antiforgery

This API authenticates with **Bearer JWT**, not cookies, and does not enable antiforgery middleware globally. The evaluate Minimal API endpoint calls `.DisableAntiforgery()` **only on that route** so multipart uploads work without antiforgery tokens. Do not disable antiforgery globally.

## Manual local integration checklist

1. FastAPI `/health` returns ok without a key.
2. FastAPI `/ready` requires `X-AI-Service-Key`.
3. ASP.NET `/health/ai` is Healthy when FastAPI is ready.
4. ASP.NET starts even if FastAPI is offline (health check fails; app still runs).
5. Plan / evaluate / summary succeed with a valid JWT and matching service key.
6. Switching `AiService__BaseUrl` / `AiService__ServiceKey` retargets the client without code changes.
7. Swagger evaluate shows a file picker for `Audio` and individual form fields (not a single JSON `form` object).

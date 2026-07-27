# Smoke-test FastAPI session endpoints with dummy external IDs (same wire format ASP.NET sends).
# Usage: set AI_SERVICE_API_KEY in env or pass as first arg.

import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

BASE = os.environ.get("AI_BASE_URL", "http://localhost:8000").rstrip("/")
KEY = os.environ.get("AI_SERVICE_API_KEY") or (sys.argv[1] if len(sys.argv) > 1 else "")
WAV = Path(__file__).resolve().parents[1] / "NomoAI.API.Tests" / "dummy-audio.wav"
OUT = Path(__file__).resolve().parent / "ai-demo-results.json"

# Dummy domain-like IDs as strings (FastAPI external refs)
SESSION_ID = "101"
CHILD_ID = "5"
ACTIVITY_ID = "9"


def call_json(method: str, path: str, payload: dict | None = None):
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        f"{BASE}{path}",
        data=data,
        method=method,
        headers={
            "X-AI-Service-Key": KEY,
            "X-Correlation-ID": "aspnet-demo-001",
            "Content-Type": "application/json",
            "Accept": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            body = resp.read().decode("utf-8")
            return resp.status, json.loads(body) if body else None
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            parsed = json.loads(raw)
        except json.JSONDecodeError:
            parsed = {"raw": raw}
        return exc.code, parsed


def call_evaluate():
    boundary = "----NomoDemoBoundary7MA4YWxk"
    audio = WAV.read_bytes()
    fields = {
        "childId": CHILD_ID,
        "activityId": ACTIVITY_ID,
        "activityType": "word",
        "targetValue": "بابا",
        "speechLevel": "vocalization",
        "age": "8",
        "attemptNumber": "1",
        "sessionId": SESSION_ID,
        "language": "ar",
        "consecutiveNoSpeechCount": "0",
        "previousAttemptScores": "[]",
    }
    parts: list[bytes] = []
    for name, value in fields.items():
        parts.append(
            (
                f"--{boundary}\r\n"
                f'Content-Disposition: form-data; name="{name}"\r\n\r\n'
                f"{value}\r\n"
            ).encode("utf-8")
        )
    parts.append(
        (
            f"--{boundary}\r\n"
            f'Content-Disposition: form-data; name="audio"; filename="dummy-audio.wav"\r\n'
            f"Content-Type: audio/wav\r\n\r\n"
        ).encode("utf-8")
        + audio
        + b"\r\n"
    )
    parts.append(f"--{boundary}--\r\n".encode("utf-8"))
    body = b"".join(parts)
    req = urllib.request.Request(
        f"{BASE}/api/v1/sessions/attempts/evaluate",
        data=body,
        method="POST",
        headers={
            "X-AI-Service-Key": KEY,
            "X-Correlation-ID": "aspnet-demo-001",
            "Content-Type": f"multipart/form-data; boundary={boundary}",
            "Accept": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            parsed = json.loads(raw)
        except json.JSONDecodeError:
            parsed = {"raw": raw}
        return exc.code, parsed


def main() -> int:
    if not KEY:
        print("Missing AI_SERVICE_API_KEY", file=sys.stderr)
        return 2

    results = {
        "baseUrl": BASE,
        "dummyIds": {
            "sessionId": SESSION_ID,
            "childId": CHILD_ID,
            "activityId": ACTIVITY_ID,
        },
        "note": "IDs are strings on the wire (FastAPI). Domain ints would be ToString()'d by ASP.NET.",
    }

    plan_payload = {
        "sessionId": SESSION_ID,
        "childId": CHILD_ID,
        "activityId": ACTIVITY_ID,
        "activityType": "word",
        "targetValue": "بابا",
        "speechLevel": "vocalization",
        "age": 8,
        "language": "ar",
        "maximumDurationMinutes": 15,
        "maximumSteps": 6,
        "additionalContext": "ASP.NET dummy integration smoke test",
    }
    status, body = call_json("POST", "/api/v1/sessions/plan", plan_payload)
    results["plan"] = {"httpStatus": status, "response": body}

    status, body = call_evaluate()
    results["evaluate"] = {"httpStatus": status, "response": body}

    # Build summary from evaluate when possible; otherwise use deterministic dummy attempt input.
    attempts = [
        {
            "attemptNumber": 1,
            "overallScore": 55,
            "speechOutcome": "speech_detected",
            "adaptiveAction": "retry_same",
            "reasonCodes": ["first_attempt"],
        }
    ]
    ev = results["evaluate"]["response"]
    if isinstance(ev, dict) and "adaptiveDecision" in ev:
        ad = ev.get("adaptiveDecision") or {}
        scores = ((ev.get("speechAnalysis") or {}).get("scores") or {})
        attempts = [
            {
                "attemptNumber": ev.get("attemptNumber", 1),
                "overallScore": scores.get("overallScore"),
                "accuracyScore": scores.get("accuracyScore"),
                "completenessScore": scores.get("completenessScore"),
                "fluencyScore": scores.get("fluencyScore"),
                "pronunciationProxyScore": scores.get("pronunciationProxyScore"),
                "speechOutcome": ev.get("speechOutcome", "scored"),
                "adaptiveAction": ad.get("action", "retry_same"),
                "reasonCodes": ad.get("reasonCodes") or [],
            }
        ]

    summary_payload = {
        "sessionId": SESSION_ID,
        "activityId": ACTIVITY_ID,
        "activityType": "word",
        "targetValue": "بابا",
        "speechLevel": "vocalization",
        "age": 8,
        "language": "ar",
        "childId": CHILD_ID,
        "attempts": attempts,
    }
    status, body = call_json("POST", "/api/v1/sessions/summary", summary_payload)
    results["summary"] = {"httpStatus": status, "response": body}

    OUT.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(results, ensure_ascii=False, indent=2))
    print(f"\nSaved: {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

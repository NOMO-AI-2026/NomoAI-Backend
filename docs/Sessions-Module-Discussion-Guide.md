# شرح موديول الـ Sessions + RAG بالتفصيل — دليل فهم للكود (NomoAI)

> الهدف: تفهم **إيه اللي بيحصل فعلياً** من لحظة ما الدكتور يعمل Activity لحد ما الجلسة تخلص ويتولد ملخص ويراجعها الدكتور.
>
> اقرأ الملف بالترتيب. الأقسام الأولى تشرح المفاهيم. بعدين كل Endpoint كقصة خطوة بخطوة.
>
> المصدر: كود `NomoAI.API/Features/Sessions/` و `Common/Ai/` و `Infrastructure/Ai/` و `docs/ai-core-integration.md`.

---

## فهرس

1. [إيه هو موديول Sessions؟](#1-إيه-هو-موديول-sessions)
2. [المفاهيم اللي لازم تفهمها قبل الكود](#2-المفاهيم-اللي-لازم-تفهمها-قبل-الكود)
3. [خريطة الخدمات: مين بيعمل إيه؟](#3-خريطة-الخدمات-مين-بيعمل-إيه)
4. [إيه اللي بيتخزن فين؟](#4-إيه-اللي-بيتخزن-فين)
5. [مين يقدر يفتح الجلسة؟ (Ownership)](#5-مين-يقدر-يفتح-الجلسة-ownership)
6. [جدول كل الـ Endpoints](#6-جدول-كل-الـ-endpoints)
7. [الفلو الكامل من أول لآخر](#7-الفلو-الكامل-من-أول-لآخر)
8. [Start Session بالتفصيل](#8-start-session-بالتفصيل)
9. [Get Runtime / Speech / Continue](#9-get-runtime--speech--continue)
10. [Submit Attempt — قلب الموديول](#10-submit-attempt--قلب-الموديول)
11. [القرار التكيّفي Adaptive Decision](#11-القرار-التكيفي-adaptive-decision)
12. [Get Attempts](#12-get-attempts)
13. [الملخص Summary](#13-الملخص-summary)
14. [مراجعة الدكتور للجلسة](#14-مراجعة-الدكتور-للجلسة)
15. [اندبوينتات الـ AI Proxy (مش Runtime المنتج)](#15-اندبوينتات-الـ-ai-proxy-مش-runtime-المنتج)
16. [RAG بالتفصيل الممل](#16-rag-بالتفصيل-الممل)
17. [IAiCoreClient: إزاي بنكلم FastAPI](#17-iaicoreclient-إزاي-بنكلم-fastapi)
18. [CanMakeSession و Wallet](#18-canmakesession-و-wallet)
19. [أخطاء شائعة بمعنى إنساني](#19-أخطاء-شائعة-بمعنى-إنساني)
20. [أسئلة مناقشة بإجابات جاهزة](#20-أسئلة-مناقشة-بإجابات-جاهزة)
21. [نقاط ضعف / تحسينات لو اتسألت](#21-نقاط-ضعف--تحسينات-لو-اتسألت)

---

## 1. إيه هو موديول Sessions؟

ده موديول **الجلسة العلاجية التفاعلية**.

الطفل بيتمرّن على نطق حرف / كلمة / جملة. الأفاتار بيتكلم، الطفل يسجّل صوته، الذكاء الاصطناعي يقيّم النطق ويقرر: نعيد؟ نسهّل؟ نعدّي للخطوة اللي بعدها؟ ولا نخلّص الجلسة؟

### الموديول بيعمل 4 حاجات كبيرة

1. **تخطيط الجلسة (Plan):** يطلب من AI Core خطة خطوات (مقدمة، تدريب، ختام...).
2. **تشغيل الجلسة (Runtime):** يحرّك مؤشر الخطوة، يستقبل تسجيل الطفل، يحفظ المحاولة.
3. **تقييم المحاولة (Evaluate):** يبعت الصوت لـ AI Core ويرجع درجات + كلام الأفاتار + قرار تكيّفي.
4. **الملخص + مراجعة الدكتور:** بعد ما الجلسة تخلص، يتولد ملخص، والدكتور يقيّم الجلسة.

---

## 2. المفاهيم اللي لازم تفهمها قبل الكود

لو المصطلحات دي مش واضحة، باقي الملف هيبقى صعب. اقرأها بهدوء.

### 2.1 Activity يعني إيه؟

**Activity** = تمرين جاهز عمله الدكتور للطفل.

مثال: الطفل يتمرن على كلمة `"بابا"`.

حقول مهمة:

| حقل | المعنى |
|---|---|
| `Content` | النص الهدف (بابا / حرف ب / جملة...) — ده اللي بيتبعت للـ AI كـ **prompt** |
| `ActivityTarget` | نوع التمرين: حرف / كلمة / جملة |
| `CanMakeSession` | هل مسموح نبدأ جلسة جديدة على التمرين ده؟ |

قاعدة المنتج: **جلسة مكتملة واحدة لكل Activity**، لحد ما الدكتور يسمح بإعادة التمرين (`repeatSession`).

### 2.2 Session يعني إيه؟

**Session** = مرة تشغيل فعلية للتمرين.

فيها:

- حالة: `InProgress` أو `Completed` (في Runtime غالباً دول اللي بيتستخدموا)
- خطة محفوظة JSON (`PlanJson`) — الخطة كلها اتولدت مرة واحدة في البداية
- مؤشر الخطوة الحالية `CurrentStepNumber` (يبدأ من 1)
- عدد محاولات **الخطوة الحالية** `CurrentAttemptNumber`
- بيانات منسوختة من الطفل/التمرين: `ActivityType`, `Prompt`, `SpeechLevel`, `Language`, `ChildAge`

فكرة مهمة جداً:

> بعد Start، الـ Backend **مش بيرجع يسأل AI يخطط من جديد**. كل خطوة جاية من `PlanJson` المحفوظ.

### 2.3 Step يعني إيه؟

الخطة عبارة عن قائمة خطوات. كل خطوة نوعها مثلاً:

- مقدمة (الأفاتار بيتكلم، الطفل مش لازم يرد)
- تدريب نطق (الطفل لازم يسجّل صوت)
- ختام / مراجعة

لو الخطوة **بتتوقع رد من الطفل** → الفرونت يسجّل صوت ويعمل Submit Attempt.  
لو **مش بتوقع رد** → الفرونت يعمل Continue Step من غير صوت.

### 2.4 Attempt يعني إيه؟

**Attempt** = محاولة نطق واحدة (ملف صوت واحد).

بعد كل محاولة بنحفظ 3 صفوف تقريباً:

1. `SessionAttempts` — رقم المحاولة
2. `AttemptTranscribtion` — النص اللي AI سمعه (transcription)
3. `AttemptEvaluation` — الدرجات + القرار التكيّفي + كلام الأفاتار

### 2.5 Command يعني إيه؟ (مهم للفرونت)

الـ Backend مش بيقول للفرونت "اعمل اللي نفسك فيه". بيرجّع **أمر واضح**:

| Command | الفرونت يعمل إيه؟ |
|---|---|
| `play_avatar_speech` | شغّل صوت الأفاتار (الخطوة الحالية) |
| `ready_to_record` | جاهز تسجّل صوت الطفل |
| `play_feedback` | شغّل فيدباك الأفاتار بعد التقييم |
| `take_break` | خد استراحة قصيرة |
| `session_completed` | الجلسة خلصت |

ده موجود في `SessionRuntimeCommand`.

### 2.6 AI Core يعني إيه؟

خدمة تانية مكتوبة بـ **FastAPI (Python)**، مش جوه الـ Backend ده.

الـ Backend (ASP.NET) هو **الوسيط**:

```text
Flutter/React  →  ASP.NET (NomoAI.API)  →  FastAPI AI Core
                      SQL Server              Whisper / LLM / Qdrant
```

العميل **عمرُه ما يكلّم FastAPI مباشرة**. ASP.NET بيحط هيدر سري `X-AI-Service-Key`.

### 2.7 RAG يعني إيه؟ (تعريف بسيط)

**RAG = Retrieval-Augmented Generation**

يعني قبل ما الـ LLM يكتب خطة أو فيدباك، النظام:

1. يحوّل السؤال/السياق لـ **embedding** (متجه أرقام)
2. يدور في قاعدة معرفة متجهة (عندنا حسب التوثيق: **Qdrant**)
3. يجيب قطع معرفة طبية/علاجية قريبة
4. يحط القطع دي جوه الـ Prompt
5. الـ LLM يولّد الرد وهو "شايف" المعرفة دي

**مهم للمناقشة — جملة احفظها:**

> RAG **مش متنفّذ جوه ASP.NET**. الاسترجاع بيحصل داخل FastAPI AI Core. الـ Backend بيبعت سياق العلاج، ويستقبل معرّفات المصادر (`knowledgeSourceIds` / `knowledgeChunkIds`) ويحفظها للمراجعة.

تفاصيل أعمق في [القسم 16](#16-rag-بالتفصيل-الممل).

---

## 3. خريطة الخدمات: مين بيعمل إيه؟

```text
┌─────────────┐     JWT      ┌─────────────────────┐     X-AI-Service-Key     ┌──────────────────┐
│  الموبايل / │ ───────────► │  NomoAI ASP.NET API │ ───────────────────────► │  FastAPI AI Core │
│  الفرونت    │ ◄─────────── │  (Sessions module)  │ ◄─────────────────────── │  Plan/Evaluate/  │
└─────────────┘   JSON/Audio └──────────┬──────────┘      Plan + Scores + TTS │  Speech + RAG    │
                                        │                                     └────────┬─────────┘
                                        ▼                                              │
                                   SQL Server                                    Qdrant + Whisper
                              Sessions / Attempts /                              + LLM (OpenRouter)
                              Evaluations / Summary
```

| الشغلانة | مين المسؤول؟ |
|---|---|
| مين المستخدم؟ صلاحيات؟ ملكية الطفل؟ | ASP.NET |
| حفظ الجلسة والمحاولات والملخص | ASP.NET + SQL Server |
| توليد خطة الخطوات | FastAPI (`POST /api/v2/sessions/plan`) |
| تحويل الصوت لنص + تقييم النطق + قرار تكيّفي | FastAPI (`POST /api/v2/sessions/attempts/evaluate`) |
| صوت الأفاتار (TTS) | FastAPI (`/api/v1/speech/synthesize`) |
| البحث في قاعدة المعرفة (RAG) | FastAPI + Qdrant |
| ملخص الجلسة في المنتج الحالي | **ASP.NET محلي (db_analytics)** مش AI |

---

## 4. إيه اللي بيتخزن فين؟

### SQL Server

| جدول / كيان | إيه فيه؟ |
|---|---|
| `Sessions` | الطفل، التمرين، الحالة، `PlanJson` كامل، مؤشر الخطوة، IDs المعرفة من الـ Plan، `RequiresDoctorReview`، تقييم الدكتور |
| `SessionAttempts` | رقم المحاولة. `AudioUrl` حالياً **دائماً null** (الصوت مش بيتخزن) |
| `AttemptTranscribtions` | النص المنطوق + اللغة + النص بعد التطبيع |
| `AttemptEvaluations` | الدرجات، matched، adaptive action، كلام الأفاتار، JSON التقييم كامل، IDs المعرفة من Evaluate |
| `SessionSummaries` | ملخص بعد اكتمال الجلسة (وضع المنتج: تحليل محلي) |
| `Activities.CanMakeSession` | قفل التمرين بعد جلسة مكتملة |

### مش بيتخزن عندنا

| حاجة | فين؟ |
|---|---|
| ملف صوت الطفل | بيتبعت لـ FastAPI للتقييم، ومش بيتسيف URL عندنا |
| embeddings / chunks | Qdrant داخل AI Core |
| JWT | stateless عند العميل |

### حقول Session المهمة للمناقشة

من `Domain/Entities/Session.cs`:

| حقل | دوره |
|---|---|
| `PlanJson` | صورة كاملة لخطة AI وقت البدء |
| `CurrentStepNumber` | إحنا فين في الخطة |
| `CurrentAttemptNumber` | كام محاولة اتعملت **على الخطوة دي** (بيتصفر لما نعدّي) |
| `KnowledgeSourceIdsJson` | مصادر RAG اللي الخطة استخدمتها (خرج AI) |
| `KnowledgeChunkIdsJson` | القطع نفسها |
| `RequiresDoctorReview` | AI طلب مراجعة إكلينيكية |
| `IsDoctorReviewed` + `DoctorRating` + `DoctorComment` | مراجعة بشرية بعد اكتمال الجلسة |

---

## 5. مين يقدر يفتح الجلسة؟ (Ownership)

الملف: `SessionOwnership.cs`

الـ JWT فيه `NameIdentifier` = `ApplicationUser.Id`.

القاعدة:

1. الطفل موجود وغير محذوف؟
2. هل في Doctor غير محذوف `UserId == jwt` و `Doctor.Id == child.DoctorId`؟ → **Owner**
3. وإلا: لو الطفل متعيّن لولي أمر، هل الـ Parent ده هو صاحب الـ JWT؟ → **Owner**
4. غير كده → **Forbidden**

يعني:

- دكتور الطفل يقدر يشغّل الجلسة
- ولي الأمر المعيَّن يقدر يشغّل نفس الجلسة
- دكتور طفل تاني / ولي أمر مش متعيّن → ممنوع

لو الجلسة مش موجودة، الدالة بترجع حالة `ChildNotFound` والـ handlers بيحولوها لـ `SessionNotFound`.

---

## 6. جدول كل الـ Endpoints

في المشروع آليتان للتسجيل (زي Auth):

- `MapSessionsEndpoints()` في `SessionsEndpoints.cs` → مجموعة `/api/sessions` و `/api/sessions/ai`
- `IEndpoint` discovery → attempts list + ملخصات الدكتور/الأهل + history + review

### أ) Runtime المنتج (يحفظ في DB)

| Method | Path | Roles | إيه بيعمل؟ |
|---|---|---|---|
| POST | `/api/sessions/` | Doctor, Parent | ابدأ جلسة + خطّط من AI |
| GET | `/api/sessions/{id}/runtime` | Doctor, Parent | حالة الجلسة الحالية والأمر التالي |
| POST | `/api/sessions/{id}/steps/continue` | Doctor, Parent | عدّي خطوة مش محتاجة صوت |
| GET | `/api/sessions/{id}/speech` | Doctor, Parent | صوت تعليم الأفاتار للخطوة |
| POST | `/api/sessions/{id}/attempts` | Doctor, Parent | ارفع صوت الطفل واتقيّم |
| GET | `/api/sessions/{id}/attempts/{attemptId}/feedback-speech` | Doctor, Parent | صوت فيدباك المحاولة |
| GET | `/api/sessions/{id}/attempts` | Doctor, Parent | قائمة كل المحاولات |
| POST | `/api/sessions/{id}/summary?force=` | Doctor, Parent | ولّد/ارجع ملخص تحليلي |

### ب) ملخصات ومراجعة (IEndpoint)

| Method | Path | Roles | إيه بيعمل؟ |
|---|---|---|---|
| GET | `/api/doctor/sessions/{id}/summary` | Doctor | ملخص إكلينيكي |
| GET | `/api/parent/sessions/{id}/summary` | Parent أو Doctor | ملخص مبسّط للأهل |
| GET | `/api/children/{childId}/sessions/history` | Doctor, Parent | تاريخ جلسات الطفل (آخر 50 مكتملة) |
| PUT | `/api/doctor/sessions/{id}/review` | Doctor | تقييم بشري + تعليق + إعادة فتح التمرين اختيارياً |

### ج) AI Proxy (تجربة/تكامل مباشر — **مش بيحفظ** نتيجة AI في الجلسة)

| Method | Path | Roles | إيه بيعمل؟ |
|---|---|---|---|
| POST | `/api/sessions/ai/plan` | Doctor, Parent | وكيل لتخطيط FastAPI فقط |
| POST | `/api/sessions/ai/evaluate` | Doctor, Parent | وكيل لتقييم صوت فقط |
| POST | `/api/sessions/ai/summary` | Doctor, Parent | وكيل لملخص AI (V1) |

لو اتسألت: **"ليه في plan مرتين؟"**  
قول: واحدة **منتج** (`POST /api/sessions/`) بتحفظ الجلسة. والتانية **proxy مؤقت** للاختبار/التكامل من غير persistence.

---

## 7. الفلو الكامل من أول لآخر

```text
الدكتور ينشئ Activity (CanMakeSession = true)
        │
ولي الأمر أو الدكتور:
POST /api/sessions
        │
        ├─ فحص ملكية الطفل + التمرين متاح؟
        ├─ جمع سياق: نوع النشاط، المحتوى، مستوى الكلام، العمر، اللغة
        ├─ FastAPI Plan V2  (+ RAG جوه FastAPI)
        ├─ حفظ Session + PlanJson + knowledge IDs
        └─ رجوع command = play_avatar_speech + صوت الخطوة 1
        │
الفرونت يشغّل صوت الأفاتار
        │
        ┌──────────────┴──────────────┐
        │                             │
 خطوة مش بتطلب نطق              خطوة بتطلب نطق
 POST .../steps/continue         الطفل يسجّل
        │                        POST .../attempts (audio)
        │                             │
        │                        FastAPI Evaluate V2
        │                        (Whisper + scores + RAG للفِيدباك)
        │                        حفظ Attempt + Transcription + Evaluation
        │                        تطبيق adaptiveDecision.action
        │                             │
        └──────────────┬──────────────┘
                       │
            لو خلصت الخطوات / AI قال end
                       │
            Status = Completed
            CanMakeSession = false
            توليد ملخص db_analytics
                       │
            GET ملخص دكتور / أهل
            PUT مراجعة الدكتور (اختياري)
```

احفظ الفلو ده كقصة. المناقشة غالباً هتمشي عليه.

---

## 8. Start Session بالتفصيل

**الملف:** `Features/Sessions/Runtime/StartSession/StartSessionFeature.cs`  
**Route:** `POST /api/sessions/`  
**Auth:** JWT + Role Doctor أو Parent

### Request مثال

```json
{
  "childId": 12,
  "activityId": 34,
  "durationMinutes": 15,
  "maxSteps": 8,
  "language": "ar"
}
```

### Validator

- `childId` و `activityId` أكبر من 0
- `durationMinutes` لو موجود: من 5 لـ 45
- `maxSteps` لو موجود: من 2 لـ 12
- اللغة أقصى 16 حرف
- `userId` من التوكن مش فاضي

لو `durationMinutes` مش اتبعت → افتراضي **15**  
لو `maxSteps` مش اتبعت → افتراضي **8**  
لو اللغة فاضية → `"ar"`

### خطوات الـ Handler بالعربي

1. **ملكية الطفل** عبر `SessionOwnership`. مش موجود / مش بتاعك → خطأ.
2. حمّل الطفل مع `SpeechLevel`. مفيش مستوى كلام → `SpeechLevelNotFound`.
3. حمّل الـ Activity. مش موجودة / مش بتاع الطفل ده → خطأ.
4. `CanMakeSession` لازم true، وإلا 409: التمرين ده خلاص اتعمل عليه جلسة مكتملة.
5. حوّل نوع التمرين:
   - حرف → `"character"`
   - كلمة → `"word"`
   - جملة → `"sentence"`
6. `prompt` = `activity.Content` (الهدف العلاجي)
7. العمر يتنظّم بين 2 و 18
8. **اتصل بـ AI Core:** `PlanSessionAsync` بالسياق ده فقط:

```text
activityType, prompt, speechLevel, age, language,
durationMinutes, maxSteps
```

> في مسار المنتج: **مبنبعتش** `previousSummary` ولا `doctorContext`.  
> (الـ proxy AI plan يقدر يبعتهم لو العميل ملاهم.)

9. لو التخطيط فشل → نرجّع خطأ AI (مثلاً 422 معرفة غير كافية، 503 الخدمة واقعة...) **من غير ما ننشئ Session**.
10. بعد الرد البطيء من AI: نعيد تحميل الـ Activity (عشان سباق: جلسة تانية متكمّلش في نفس الوقت) ونفحص `CanMakeSession` تاني.
11. نحفظ `Session`:
    - `Status = InProgress`
    - `PlanJson` = الخطة كلها
    - `CurrentStepNumber = 1`
    - `CurrentAttemptNumber = 0`
    - `KnowledgeSourceIdsJson` / `KnowledgeChunkIdsJson` من رد التخطيط
    - نسخ `ActivityType`, `Prompt`, `SpeechLevel`, `Language`, `ChildAge`
12. بالتوازي: TTS لصوت الخطوة الأولى (`purpose: instruction`)
13. الرد: `201` + `command = play_avatar_speech` + بيانات الخطوة + صوت base64 لو نجح TTS

### إيه اللي **مش** بيحصل في Start؟

- مش بنخصم دقايق من Wallet
- مش بنقفّل `CanMakeSession` لسه (القفلة بعد **اكتمال** الجلسة)
- مش بنبعت IDs الداتابيز لـ FastAPI (V2 متعمد: الهوية تفضل عند ASP.NET)

---

## 9. Get Runtime / Speech / Continue

### 9.1 GET `/api/sessions/{id}/runtime`

اقرأ الحالة الحالية من غير ما تغيّر حاجة.

1. ملكية الجلسة
2. فك `PlanJson`
3. لو الجلسة مش `InProgress` → `command = session_completed`
4. لو لسه شغالة:
   - لو الخطوة بتطلب نطق واتعمل عليها محاولات ولسه تحت الحد → `ready_to_record`
   - وإلا → `play_avatar_speech`
5. **مش** بيولّد TTS هنا (عشان كده في endpoint صوت منفصل)

استخدمه لما الفرونت يفتح الجلسة تاني (refresh / resume).

### 9.2 GET `/api/sessions/{id}/speech`

1. ملكية
2. هات نص الأفاتار للخطوة الحالية من الخطة
3. `SynthesizeSpeechAsync(text, "instruction")`
4. رجّع ستريم صوت

لو مفيش نص → 404.

### 9.3 POST `/api/sessions/{id}/steps/continue`

للخطوات **اللي الطفل مش مطالب ينطق فيها**.

1. ملكية + الجلسة InProgress
2. لو الخطوة `ExpectsChildResponse` → 409 `ChildResponseRequired` (يعني استخدم Submit Attempt مش Continue)
3. صفّر عداد محاولات الخطوة
4. لو مفيش خطوة بعد كده → Complete + قفل Activity + توليد ملخص
5. وإلا زوّد `CurrentStepNumber`
6. لو الأمر الجديد `play_avatar_speech` ممكن يضمّن TTS

---

## 10. Submit Attempt — قلب الموديول

**الملف:** `Features/Sessions/Runtime/SubmitAttempt/SubmitAttemptFeature.cs`  
**Route:** `POST /api/sessions/{sessionId}/attempts`  
**Body:** `multipart/form-data` حقل اسمه `audio`  
**Auth:** Doctor أو Parent  
**Antiforgery:** متعطّل هنا لأن المصادقة JWT مش كوكيز

ده أهم Endpoint في الموديول. لو فهمته، فهمت الجلسة.

### Validator على الملف الصوتي

- لازم ملف موجود ومش فاضي
- الحجم ≤ `AiService:MaxAudioBytes` (افتراضي 10 MB)
- الأنواع: wav / mp3 / m4a / webm / ogg
- الامتدادات بنفس القائمة

### خطوات الـ Handler بالتفصيل الممل

#### 1) تأكيد الجلسة
- ملكية
- لازم `Status == InProgress` وإلا 409

#### 2) تحميل الخطة والخطوة الحالية
- `Deserialize(PlanJson)`
- لو الخطة بايظة → 500 `PlanUnavailable`
- لو رقم الخطوة مش موجود في الخطة → 500 `StepNotFound`

#### 3) حد المحاولات على الخطوة
في الخطوات الكلامية في حد أدنى **5 محاولات** حتى لو الخطة قالت 2.

لو `CurrentAttemptNumber` وصل للحد → 409 `MaximumAttemptsExceeded`

#### 4) رقم المحاولة العام
```text
globalAttemptNumber = عدد محاولات الجلسة + 1
```
لو الرقم ده موجود قبل كده → 409 Duplicate.

#### 5) تاريخ الخطوة الحالية (لو مش أول محاولة عليها)
بنجيب آخر `CurrentAttemptNumber` محاولات عشان نجهّز لـ AI:

- `previousAttemptScores` (الـ overall)
- `previousDecision` (آخر adaptive action)
- `consecutiveNoSpeechCount` (كام مرة ورا بعض مفيش كلام)

لو أول محاولة على الخطوة: القيم دي فاضية/صفر.

#### 6) نداء التقييم
نبني `AiEvaluateAttemptV2Request`:

| حقل | مصدره |
|---|---|
| audio stream | ملف الفرونت |
| activityType | من الجلسة أو الخطة |
| prompt | الهدف العلاجي |
| speechLevel | مستوى الطفل |
| age | ChildAge أو 6 |
| attemptNumber | رقم محاولة **الخطوة** (مش العام) |
| maximumAttempts | الحد الفعّال |
| previousAttemptScores / previousDecision / consecutiveNoSpeechCount | من الخطوة 5 |
| language | لغة الجلسة |

**مش بيتبعت:** childId, sessionId, additionalContext, ملخص جلسات سابقة، IDs المعرفة.

FastAPI بيرجع: transcription + scores + `adaptiveDecision.action` + كلام الأفاتار + knowledge IDs.

لو التقييم فشل → نرجّع خطأ AI **ومنحفظش حاجة**.

#### 7) TTS للفِيدباك بالتوازي
`purpose: encouragement` عشان نوفّر round-trip للفرونت.

#### 8) الحفظ في DB (لسه من غير SaveChanges)
- صف `SessionAttempts` (`AudioUrl = null`)
- صف transcription
- صف evaluation (درجات + JSON كامل + knowledge IDs)

#### 9) تطبيق القرار التكيّفي
`ApplyAdaptiveTransition(session, plan, action)`  
(شرح كامل في القسم 11)

#### 10) لو الجلسة بقت Completed
- `ActivitySessionGate`: `CanMakeSession = false`
- `SessionSummaryPersister`: ملخص تحليلي محلي

#### 11) SaveChanges مرة واحدة
المحاولة + التقييم + مؤشر الجلسة (+ قفل التمرين والملخص إن اكتملت)

#### 12) الرد
غالباً `command = play_feedback` عشان الفرونت يشغّل كلام الأفاتار.  
استثناء: لو القرار `take_short_break` والجلسة لسه شغالة → `take_break`.

---

## 11. القرار التكيّفي Adaptive Decision

**الجملة الذهبية للمناقشة:**

> ASP.NET **مش هو اللي يقرر** نعدّي أو نعيد من الدرجات.  
> FastAPI يرجّع `adaptiveDecision.action` كنص، وASP.NET ينفّذ النص ده فقط.

### جدول القرارات

| action من AI | إيه اللي الـ Backend يعمله؟ |
|---|---|
| `advance` | روح للخطوة التالية، أو خلّص لو مفيش بعدها |
| `end` | أنهِ الجلسة فوراً حتى لو في خطوات باقية |
| `end_attempts` | ميزانية الخطوة خلصت → عدّي للخطوة التالية (مش إنهاء الجلسة لوحده) |
| `recommend_doctor_review` | `RequiresDoctorReview = true` ثم عدّي خطوة |
| `retry_same` | فضّل في نفس الخطوة +1 محاولة |
| `retry_with_hint` | نفس الفكرة مع تلميح (الكلام جاي من الأفاتار) |
| `simplify` | سهّل وحاول تاني |
| `ask_follow_up` | سؤال متابعة |
| `continue_conversation` | كمّل حوار |
| `take_short_break` | استراحة؛ الفرونت ياخد `take_break` |
| أي قيمة غريبة/مش معروفة | تتعامل زي retry |

في حالات الـ retry: لو وصلنا حد المحاولات برضو بنعدّي (`AdvanceOrComplete`) عشان الطفل ميفضلش واقف.

### AdvanceOrComplete

```text
CurrentAttemptNumber = 0
لو في خطوة رقم (الحالية+1) → CurrentStepNumber++
وإلا → Status = Completed + EndedAt = الآن
```

---

## 12. Get Attempts

**Route:** `GET /api/sessions/{sessionId}/attempts`  
**الملفات:** `Runtime/GetSessionAttempts/`

1. ملكية
2. كل محاولات الجلسة بالترتيب
3. معاها evaluation + transcription (من غير N+1 تقيل؛ Select فيه subqueries)

`OverallScore` هنا = مجموع الأربع درجات المخزّنة، **مش بالضرورة** overall بتاع AI (0–100). نقطة دقيقة لو اتسألت على الأرقام.

---

## 13. الملخص Summary

هنا فيه **مسارين** الناس بتخلط بينهم. افصلهم بوضوح في المناقشة.

### المسار أ — منتج فعلي (اللي الفرونت بيستخدمه)

`POST /api/sessions/{id}/summary`

- الجلسة لازم تكون **Completed**
- لازم يكون فيه محاولات
- الحساب من DB عبر `SessionSummaryAnalytics` (مش نداء LLM)
- الوضع: `SummaryGenerationMode = "db_analytics"`
- يولّد نص عربي تحليلي: نقاط قوة/ضعف، outcome، توصيات
- `?force=true` يعيد الحساب حتى لو الملخص موجود

أمثلة outcomes:

- `completed_successfully`
- `completed_with_progress`
- `completed_needs_practice`
- `ended_max_attempts`
- `ended_no_speech`
- `doctor_review_recommended`
- `inconclusive`

كمان بيتكتب أوتوماتيك لما الجلسة تكتمل من Submit/Continue (`SessionSummaryPersister`).

### المسار ب — ملخص AI عبر Proxy

`POST /api/sessions/ai/summary` → FastAPI `/api/v1/sessions/summary`

موجود للاختبار. **مفيش Handler منتج بيوصّل نتيجته على جدول الملخص.**  
في كود جاهز (`SessionSummaryRequestFactory`, `ApplyAiResponse`) بس **مش مستدعى** من Generate الحالي.

### قراءة الملخص حسب الدور

| Endpoint | مين | الشكل |
|---|---|---|
| `GET /api/doctor/sessions/{id}/summary` | Doctor | تفاصيل إكلينيكية + metrics |
| `GET /api/parent/sessions/{id}/summary` | Parent أو Doctor | نسخة ألطف، من غير أرقام تقيلة، وتنبيه متابعة لو في review flag |
| `GET /api/children/{id}/sessions/history` | Doctor, Parent | آخر 50 جلسة مكتملة + هل فيها ملخص |

لو مفيش صف ملخص → غالباً 404 بمعنى: ولّده أولاً.

---

## 14. مراجعة الدكتور للجلسة

**Route:** `PUT /api/doctor/sessions/{sessionId}/review`

ده **مش** ملخص AI. ده رأي بشري بعد ما الجلسة تخلص.

يحفظ على `Session`:

- `IsDoctorReviewed = true`
- `DoctorRating` (1–5)
- `DoctorComment`

وممكن `repeatSession` يرجّع `Activity.CanMakeSession = true` عشان الطفل يعيد التمرين.

لو الجلسة اتراجعت قبل كده → 409.

ملاحظة: فحص `IsApproved` للدكتور في الـ handler ده **معلّق بتعليق** حالياً.

الداشبورد بعدها يعد الجلسات المكتملة اللي `IsDoctorReviewed == false` كـ "في انتظار المراجعة".

---

## 15. اندبوينتات الـ AI Proxy (مش Runtime المنتج)

تحت `/api/sessions/ai`

### Plan proxy — `POST /api/sessions/ai/plan`

يبعت لـ FastAPI نفس تخطيط V2، **وكمان** يقدر يمرّر:

- `previousSessionSummary` → `previousSummary`
- `additionalContext` → `doctorContext`

الرد يترجع JSON للعميل **من غير حفظ Session**.

### Evaluate proxy — `POST /api/sessions/ai/evaluate`

multipart صوت + حقول السياق. يرجّع تقييم JSON. **مش بيحفظ Attempt.**  
حقل `AdditionalContext` في الفورم **بيتقبل ثم بيترمي** — V2 مفيهوش الحقل ده.

### Summary proxy — `POST /api/sessions/ai/summary`

يبعت محاولات جاهزة من العميل لـ FastAPI V1 summary.

**الفرق في جملة:**  
Proxy = كلّم الذكاء وارجع النتيجة.  
Runtime = كلّم الذكاء **واحفظ** وحرّك مؤشر الجلسة.

---

## 16. RAG بالتفصيل الممل

القسم ده مخصوص للمناقشة. اقرأه أكتر من مرة.

### 16.1 RAG ببساطة كأنك بتشرح لدكتور مش مبرمج

الـ LLM لوحده ممكن يهلوس أو ينسى بروتوكول العلاج.  
فإحنا بنخزّن معرفة معتمدة (تدخلات نطق، أمثلة، إرشادات) في قاعدة متجهة.

لما نطلب خطة أو فيدباك:

1. نأخد وصف الحالة (نوع التمرين + الكلمة الهدف + مستوى الطفل + العمر + اللغة)
2. نحول الوصف لـ vector
3. ندور أقرب قطع معرفة
4. ندي القطع دي للموديل مع الطلب
5. الموديل يكتب خطة/فيدباك مبني على المعرفة دي

### 16.2 فين بيعيش RAG في مشروعنا؟

| الطبقة | فيها RAG؟ |
|---|---|
| Flutter/React | لا — بيبعت صوت وبيانات جلسة لـ ASP.NET |
| ASP.NET NomoAI.API | **لا استرجاع.** مفيش Qdrant، مفيش embeddings، مفيش loop بحث |
| FastAPI AI Core | **نعم.** التوثيق يذكر Qdrant مع OpenRouter و Whisper |

جملة المناقشة:

> Backend بتاعنا **Orchestrator**. هو يجمع سياق علاجي نظيف، يبعته لخدمة الذكاء، ويحفظ معرّفات المعرفة اللي رجعت عشان التدقيق. محرّك RAG نفسه جوه AI Core.

### 16.3 إيه السياق اللي ASP.NET بيبعتته (مدخلات ممكن تُستخدم كـ query للـ RAG)؟

#### عند التخطيط (Plan V2)

مسار **المنتج StartSession** يبعت:

| حقل | مثال | ليه مهم للـ RAG؟ |
|---|---|---|
| `activityType` | `word` | يفلتر معرفة خاصة بالكلمات مش الجمل |
| `prompt` | `بابا` | الهدف العلاجي نفسه — غالباً محور البحث |
| `speechLevel` | اسم مستوى الطفل | التدخلات تختلف حسب المستوى |
| `age` | 6 | أمثلة مناسبة للعمر |
| `language` | `ar` | معرفة عربية |
| `durationMinutes` / `maxSteps` | 15 / 8 | حجم الخطة |

الحقول الاختيارية في العقد (`previousSummary`, `doctorContext`) **المسار المنتج مش بيملاها حالياً**.  
الـ proxy يقدر يبعتها لو الفرونت اداها.

**مش بيتبعت عمداً:** `childId`, `sessionId`, `activityId`, اسم الطفل.  
السبب: خصوصية + V2 مصمّم على **محتوى علاجي فقط**؛ الهوية تفضل في SQL.

#### عند التقييم (Evaluate V2)

بالإضافة للصوت:

- نفس `activityType` / `prompt` / `speechLevel` / `age` / `language`
- رقم المحاولة والحد الأقصى
- درجات المحاولات السابقة على الخطوة
- آخر قرار تكيّفي
- عدد مرات `no_speech` المتتالية

ده يخلي RAG/الـ LLM يكيّف الفيدباك: لو فشل مرتين، التلميح يتغيّر؛ لو مفيش كلام، القرار يختلف.

**مش بيتبعت:** ملخص جلسات قديمة، IDs المعرفة من الخطة، نص محاولات سابقة (بس أرقام وقرار).

### 16.4 إيه اللي بيرجع من RAG (مخرجات بنحفظها)؟

في رد التخطيط والتقييم:

```json
"knowledgeSourceIds": [ "...guids..." ],
"knowledgeChunkIds": [ "...guids..." ]
```

ASP.NET يحفظهم:

- بعد Plan → على `Session`
- بعد Evaluate → على `AttemptEvaluation`

فايدتهم:

- Audit: نقدر نقول الجلسة دي اتبنت على أنهي مصادر
- بحث لاحق / جودة
- مش بنرجع نستخدمهم كمدخل في النداء اللي بعده (الكود الحالي مش بيعمل كده)

لو FastAPI معندوش معرفة معتمدة كافية ممكن يرجع 422، والـ Backend يحوّله لخطأ `InsufficientKnowledge`.

### 16.5 فلو RAG مع التخطيط

```text
StartSession يجمع:
  "طفل 6 سنين، مستوى كذا، يتمرن على كلمة بابا، 8 خطوات"
        │
        ▼
FastAPI:
  1) embedding للسياق
  2) بحث Qdrant → chunks معتمدة
  3) Prompt = تعليمات النظام + chunks + السياق
  4) LLM يولّد خطوات الجلسة + كلام الأفاتار
  5) يرجع الخطة + IDs المصادر
        │
        ▼
ASP.NET يحفظ PlanJson + knowledge IDs
الفرونت يشغّل الخطوة 1
```

### 16.6 فلو RAG مع التقييم

```text
الطفل يسجّل "بابا"
        │
        ▼
FastAPI:
  1) Whisper / ASR → نص
  2) تطبيع النص ومقارنته بالهدف
  3) حساب درجات (accuracy, fluency, pronunciation, completeness...)
  4) (غالباً) استرجاع تدخل مناسب من Qdrant حسب المستوى والفشل السابق
  5) توليد كلام أفاتار مشجّع + adaptive action
  6) knowledge IDs
        │
        ▼
ASP.NET يحفظ كل ده وينفّذ الـ action
```

تفاصيل خطوات 2–5 داخل Python مش ظاهرة في ريبو الـ Backend. في المناقشة قول بصدق: **الاسترجاع والتنفيذ في AI Core؛ إحنا موثّقين العقد والـ IDs.**

### 16.7 ليه معرفناش نعمل RAG جوه ASP.NET؟

إجابة معمارية حلوة:

- فصل الاهتمامات: العلاج/الصلاحيات/البيانات في .NET، الذكاء/الصوت/المتجهات في Python
- نماذج Whisper والـ embeddings أثقل على خدمة مخصّصة
- Qdrant مناسب للبحث التقريبي، SQL مناسب للعلاقات والصلاحيات
- العميل يفضل ينادي API واحد موثّق بـ JWT

---

## 17. IAiCoreClient: إزاي بنكلم FastAPI

**الملف:** `Common/Ai/IAiCoreClient.cs` + `Infrastructure/Ai/AiCoreClient.cs`

| الدالة | مسار FastAPI | ملاحظات |
|---|---|---|
| `PlanSessionAsync` | `POST /api/v2/sessions/plan` | JSON، فيه retry |
| `EvaluateAttemptAsync` | `POST /api/v2/sessions/attempts/evaluate` | multipart صوت، **من غير retry** (الستريم يت consu me مرة) |
| `CreateSessionSummaryAsync` | `POST /api/v1/sessions/summary` | لسه V1 |
| `SynthesizeSpeechAsync` | `POST /api/v1/speech/synthesize` ثم تحميل الملف | TTS، فورمات wav |
| Health / Ready | `/health`, `/ready` | الجاهزية بمفتاح الخدمة |

هيدرز:

- `X-AI-Service-Key`
- `X-Correlation-ID` للتتبع

Timeout افتراضي 180 ثانية. Retry على 429/502/503/504 لنداءات JSON فقط.

لو FastAPI 401/403 → بنرجّع للعميل بمعنى إعدادات الخدمة مش مظبوطة، **من غير ما نسرّب المفتاح**.

---

## 18. CanMakeSession و Wallet

### CanMakeSession — القيد الحقيقي لبدء الجلسة

```text
إنشاء Activity → true
StartSession يتطلب true
جلسة Completed → false
مراجعة دكتور repeatSession → ممكن ترجع true
```

خطأ 409: `ActivitySessionAlreadyCreated`

### Wallet الدقايق

`DoctorCreditWallet.AvailableMinutes` بيتملّي عند تسجيل الدكتور ويتزاد مع الدفع.

**StartSession / SubmitAttempt حالياً لا يقرأون ولا ينقصون الدقايق.**  
لو اتسألت: المحفظة موجودة للدفع/الباقات، وربطها بالجلسات تحسين مستقبلي. القيد الحالي هو `CanMakeSession`.

---

## 19. أخطاء شائعة بمعنى إنساني

| الكود | يعني إيه؟ |
|---|---|
| SessionRuntime.Forbidden | مش دكتور/ولي أمر الطفل ده |
| SessionRuntime.ActivitySessionAlreadyCreated | التمرين اتقفل بعد جلسة مكتملة |
| SessionRuntime.SessionNotInProgress | بتحاول تسجّل على جلسة خلصت |
| SessionRuntime.MaximumAttemptsExceeded | استنفدت محاولات الخطوة |
| SessionRuntime.ChildResponseRequired | الخطوة دي محتاجة صوت مش Continue |
| SessionRuntime.AudioRequired | مفيش ملف صوت |
| InsufficientKnowledge (422) | AI Core معندوش معرفة معتمدة كافية (مرتبط بـ RAG) |
| 503 Unavailable / Timeout | FastAPI واقع أو بطيء |

---

## 20. أسئلة مناقشة بإجابات جاهزة

### س: اشرح موديول الجلسات في دقيقة.

**ج:** الدكتور يعمل تمرين للطفل. ولي الأمر أو الدكتور يبدأ جلسة، الـ Backend يطلب من AI Core خطة خطوات ويحفظها. الأفاتار بيتكلم، الطفل يسجّل، الصوت يتقيّم، والقرار التكيّفي يحرّك الجلسة. بعد الاكتمال يتولد ملخص تحليلي والدكتور يقدر يراجع.

### س: فين الذكاء الاصطناعي بالظبط؟

**ج:** مش جوه ASP.NET. خدمة FastAPI. إحنا Orchestration: صلاحيات، حفظ، وتحريك الحالة حسب قرارات AI.

### س: إيه هو RAG عندكم؟

**ج:** Retrieval-Augmented Generation في AI Core باستخدام Qdrant. ASP.NET بيبعت سياق العلاج (نوع/هدف/مستوى/عمر/لغة + تاريخ المحاولة)، ويستقبل ويحفظ IDs المصادر. مفيش كود بحث متجه في الـ Backend.

### س: ليه مش بتبعتوا childId لـ FastAPI؟

**ج:** عقد V2 محتوى علاجي فقط. هوية الطفل والجلسة مسؤولية SQL والصلاحيات. أقل بيانات شخصية تخرج لخدمة الذكاء.

### س: مين يقرر نعدّي للخطوة التالية؟

**ج:** AI يرجّع `adaptiveDecision.action`. ASP.NET مترجم للحالة: advance/end/retry/... الدرجات وحدها مش بتحرّك المؤشر.

### س: الملخص LLM ولا حساب؟

**ج:** في المنتج الحالي حساب من المحاولات (`db_analytics`). مسار ملخص AI موجود كـ proxy ومش متوصل على الحفظ.

### س: الصوت بيتخزن؟

**ج:** حالياً `AudioUrl = null`. الصوت يتبعت للتقييم ومش بيتسيف كملف عندنا.

### س: إيه الفرق بين RequiresDoctorReview و IsDoctorReviewed؟

**ج:** الأول فلاج من المحرّك التكيّفي (AI شايف الحالة محتاجة مختص). التاني إن الدكتور فعلاً فتح المراجعة وحط تقييم وتعليق.

### س: ليه في `/api/sessions/ai/plan` و `/api/sessions/`؟

**ج:** الأولى تكامل مباشر من غير حفظ. التانية مسار المنتج الكامل.

### س: TTS فين؟

**ج:** FastAPI. Start/Submit يحاولوا يضمّنوا الصوت base64. وفي GET منفصل لو الفرونت محتاج يعيد التشغيل.

---

## 21. نقاط ضعف / تحسينات لو اتسألت

كن صادق؛ ده بيبان نضج:

1. مسار المنتج مش بيبعت `previousSummary` / `doctorContext` للتخطيط → RAG مش مستفيد من تاريخ الجلسات السابقة أوتوماتيك.
2. ملخص المنتج مش ماشي على LLM؛ كود الربط موجود ومتساب.
3. الصوت مش متخزن؛ صعب إعادة التقييم لاحقاً.
4. Wallet الدقايق مش مربوط ببدء الجلسة.
5. `DoctorNotApproved` معرّف في أخطاء الجلسات ومش مستخدم في Start.
6. Evaluate proxy يقبل `AdditionalContext` وبيرميه.
7. Docs لسه كاتبة مسارات V1 للـ plan/evaluate بينما الكود V2.
8. مجموع الدرجات في Get Attempts ممكن يختلف عن overall بتاع AI.
9. AI proxy مفيهوش فحص ملكية طفل (لأن V2 أصلًا مش بيستخدم IDs)، أي Doctor/Parent JWT يقدر ينادي التخطيط/التقييم الخام.
10. TTS مش متخزن؛ كل GET يعيد التوليد.

---

## ملحق: خريطة ملفات للمذاكرة

```text
Features/Sessions/
  SessionsEndpoints.cs
  Runtime/
    StartSession/
    GetSessionRuntime/
    ContinueStep/
    SessionSpeech/
    AttemptFeedbackSpeech/
    SubmitAttempt/          ← الأهم
    GetSessionAttempts/
    SessionOwnership.cs
    ActivitySessionGate.cs
    SessionRuntimeContracts.cs
    SessionRuntimeErrors.cs
  Summary/
    GenerateSessionSummary/
    GetDoctorSessionSummary/
    GetParentSessionSummary/
    GetChildSessionHistory/
    SessionSummaryAnalytics.cs
  Ai/
    PlanSession/
    EvaluateAttempt/
    CreateSessionSummary/

Common/Ai/IAiCoreClient.cs
Common/Ai/Contracts/        ← Plan V2, Evaluate V2, Summary, Speech
Infrastructure/Ai/AiCoreClient.cs

Domain/Entities/Session.cs
docs/ai-core-integration.md
```

---

## نصيحة مذاكرة قبل المناقشة

1. ارسم على ورقة الفلو في القسم 7.
2. احفظ جملة RAG: الاسترجاع في FastAPI/Qdrant، الـ Backend سياق + حفظ IDs.
3. امشِ Start ثم Submit بالقلم: إيه الجداول اللي بتتكتب.
4. افصل بوضوح: Plan المنتج vs Plan الـ proxy، وملخص db_analytics vs ملخص AI.
5. لو اتلغّطت: **الفرونت → ASP.NET (ملكية + حالة) → FastAPI (ذكاء + RAG) → ASP.NET يحفظ وينفّذ الأمر.**

لو حابب ملف بنفس العمق لموديول تاني (Payment / Activities / Admin) قولّي.

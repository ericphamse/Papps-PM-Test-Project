
\# CV Tailoring Tool



Takes a candidate CV and a job description, both as `.docx`, and produces a tailored two-page CV in the client's template — with a provenance record for every value in it.


\---



\## 1. Running it



\### Prerequisites



\- Node.js (LTS) and npm

\- .NET SDK 8 or later

\- A PostgreSQL database (we used Supabase)

\- A Google Gemini API key



\### Backend



```bash

cd backend/CvPipeline.Api

\# create appsettings.Development.json (NOT committed) with:

\# {

\#   "ConnectionStrings": { "Default": "<your postgres connection string>" },

\#   "Gemini": { "ApiKey": "<your key>" }

\# }

dotnet restore

dotnet ef database update    # applies migrations from cold

dotnet run

```



The API listens on `http://localhost:5068`.


1\. Create a Supabase project; copy the connection string from Project Settings → Database.

2\. Put it in `ConnectionStrings:Default` as above.

3\. Run `dotnet ef database update` — this creates every table and constraint from the migrations in `backend/CvPipeline.Api/Migrations`. No manual SQL required.

4\. Verify: `\\dt` in the Supabase SQL editor should list `analyses`, `generations`, `generation\_fields`.



\### Frontend



```bash

cd web/recruitment

npm install

\# create .env.local (NOT committed) with:

\# NEXT\_PUBLIC\_API\_BASE\_URL=http://localhost:5068

npm run dev

```



Open `http://localhost:3000/editor`.



\### Tests



```bash

cd backend/CvPipeline.Api \&\& dotnet test    # pipeline suite, offline, fake client

cd web/recruitment \&\& npm test              # conformance + token tests

```



The pipeline suite runs entirely offline against a fake Gemini client. \*\*No API key is required to run the tests.\*\*



\---



\## 2. Architecture



```

.docx CV ─┐

&#x20;         ├─→ POST /api/analyses ─→ \[persist inputs]

.docx JD ─┘                         ↓

&#x20;                         Stage 1 SELECT   (Gemini call 1 — proposes)

&#x20;                         Gate 1           (code — verifies every quote)

&#x20;                               ↓

&#x20;                         Stage 2 NORMALISE (pure C#, no model)

&#x20;                               ↓

&#x20;                         Stage 3 TAILOR   (Gemini call 2 — 5 fields only)

&#x20;                               ↓

&#x20;                         CvDocument + provenance + warnings

&#x20;                               ↓

&#x20;                         \[React editor + live preview]

&#x20;                               ↓

&#x20;                   POST /api/generations → Gate 2 (server)

&#x20;                      201 → browser builds .docx    422 → violations, no file

```



\*\*Key split:\*\* the model proposes, the code decides. Stage 2 involves no model at all, and Gate 1 verifies every quote the model returns is a real substring of the source before anything downstream trusts it.



\*\*Why the .docx is built in the browser:\*\* the `docx` library is a Node library and cannot run inside .NET. So the server owns validation and the browser owns rendering — which is exactly why the download is gated by a server 201 rather than by a client-side check.



\*\*One source of truth for the template:\*\* every colour and measurement from section 6 lives in `web/recruitment/src/features/template/tokens.ts`, imported by both the HTML preview and the docx renderer. A Jest test fails the build if a hex literal appears anywhere else under `features/docx`, `features/preview` or `features/template`. The failure that guards against is not "wrong colour" but "right in one place, stale in the other".



\---



\## 3. Where our output deliberately differs from the supplied expected output



Required by the brief. Every row is a decision, not an accident.



| # | Supplied output | Ours | Why |

|---|---|---|---|

| 6.6.1 | `Professional Profile` heading uses `Heading3` with \*\*no\*\* bottom border; the others are direct-formatted \*\*with\*\* one | All seven headings identical, with a border | They are clearly meant to look the same on the page. Reproducing the inconsistency would mean encoding a Word accident as a rule. |

| 6.6.2 | `Professional` (11pt) and ` Profile` (10pt) in two runs at different sizes | Single run, 11pt | Same reasoning. Two point sizes inside one word pair is a formatting slip, not a design. |

| 6.6.3 | `QUALIFICATIONS` value cell 10pt, neighbours 9pt | \*\*Reproduced\*\* — 10pt | This one is plausibly deliberate: the qualifications string is the longest in the table and the emphasis reads as intentional. Where a wart could be a choice, we kept it. |

| 6.6.4 | Career Synopsis row 1 uses a hyphen `2024 - Current`; every other row an en dash | En dash throughout | One of them is wrong and it is row 1. Consistency beats fidelity to a typo. |

| 6.6.5 | Commendations ends with `Internal delivery and innovation recognitions \\| Various` | \*\*Row omitted\*\* | Nothing in the input CV grounds it — the strings `recognition`, `innovation`, `internal delivery` and `various` do not appear in the source. Under Inv-2 that row cannot be generated. Our output has three award rows where the supplied output has four. We believe we are right and the fixture is wrong. |

| Dated sections | Career Synopsis / Awards / Qualifications appear as aligned two-column rows | Right-aligned \*\*tab stops\*\*, not tables | 6.3 allows exactly one table in the document. Rendering these as borderless tables looked identical but produced four `<w:tbl>` elements. See §5. |



\---



\## 4. Decisions we were asked to defend



\### Provenance is per field, not per leaf



A user who fixes one character in one referee's phone number flips the whole `referees` field to `edited`. That is crude and we know it: our edit-rate reporting will overstate how often referees are wrong. The right answer is a tree of `FieldValue` rather than a record of them, with a UI that tracks and displays leaf-level provenance. \*\*That is the first thing we would build next.\*\*



\### Storing both `analyses.document` and `generations.document`



\_\[TEAM: pick one and defend it. Both are defensible; not noticing there was a choice is the failure.]\_



We store \*\*both\*\*. If the user edited nothing they are the same bytes, which is duplication — but they answer different questions. `analyses.document` is what the model produced; `generations.document` is what a human confirmed and what was actually sent to a client. Losing the first makes "how often do reviewers correct us?" unanswerable; losing the second makes the audit trail worthless. Storage is cheap; the questions are not.



\### Where J-rules came from



J1–J5 are read out of the \*\*job requirements document\*\*, not hardcoded as universal truths. A different RFQ says different things. We have not built a general RFQ-to-rules parser — we treat these five as constraints supplied by \*this\* engagement, encoded once, in one place, so a different RFQ means changing one module rather than hunting through the pipeline.



\### Rules enforced by machinery vs. by a human



| Rule | Enforced by |

|---|---|

| O1, J2, J4, J5 | Code, server-side, at Gate 2 |

| N1–N11, O1–O8 | Code, deterministically, Stage 2 |

| C1–C11 (docx conformance) | Automated test against the unzipped XML |

| \*\*J1 (max 2 pages)\*\* | \*\*A human, by eye\*\* |



J1 is exactly as binding as J4 and will get a response binned just as fast — but the server never sees the rendered file, so it cannot be enforced server-side. It is checked manually and recorded here.



\*\*Last manual page-count check:\*\* \_\[DATE]\_ — \_\[N]\_ pages. \_\[MUST BE 2]\_



\### What we would build first with another fortnight



\*\*Add, delete and reorder rows.\*\* The editing contract fixes the shape: a reviewer who learns on the phone that there is a third referee cannot add them, and one who thinks the sixth competency is irrelevant to this client cannot drop it. So the tool is good enough to draft and not good enough to finish — the reviewer still opens Word at the end and does the last ten minutes by hand. That is a real product failure and it was the right call for five weeks. Both are true.



Second would be leaf-level provenance, above.



\---



\## 5. Verifying the .docx — and the bug it caught



We do not check the generated document by eye. The renderer's output is unzipped and asserted against `word/document.xml`, covering C1–C11 from §6.7.



This caught something looking at the file never would have. The dated sections — Career Synopsis, Commendations and Awards, Qualifications — were rendering as borderless two-column tables. On screen and in Word that is indistinguishable from the template. In the XML it produced \*\*four `<w:tbl>` elements where 6.3 allows exactly one\*\*. Replaced with right-aligned tab stops.



Current status: \*\*14/14 automated checks pass.\*\* The page count is the one check a human still does.



The general point, which is why we test the XML rather than the rendering: nothing in the chain complained. The file built, `Packer` reported a healthy byte count, Word opened it without a murmur. The XML is the only thing in that pipeline that cannot shrug and carry on.



\---



\## 6. Tests: what we wrote and what we skipped



\### Written



\_\[TEAM: list what actually exists. Be specific.]\_



\### Skipped, and why



\_\[TEAM: list them. "We ran out of time and here is the list" is a professional answer. Twenty-nine half-written tests is not.]\_



Candidates we knowingly did not write: migrations applying cleanly from cold, Postgres `check` constraints firing, `generation\_fields` having one row per field, the Gate 1 retry path, the preview emitting all eight table rows.



\### Evals (not tests)



G15–G17 need a real API key and have no stable pass/fail, because the model has no stable output. They are \*\*not in CI\*\* — a red build on a Tuesday because Gemini picked a different adjective teaches nobody anything. Run deliberately.



\*\*Last eval run:\*\* \_\[DATE]\_ — \_\[RESULT]\_



\---



\## 7. Known gaps



\- \*\*Header logo\*\* — the template header carries a logo image. There is no image asset in the repo and we did not have one. The header text box is populated and correct; the logo is absent.

\- \_\[TEAM: add the rest, honestly. §15.4 invites this explicitly.]\_



\---



\## 8. Fixtures



| File | What it is |

|---|---|

| `fixtures/Jordan\_Reeve\_Resume\_ORIGINAL.docx` | Supplied input CV |

| `fixtures/JD\_Senior\_Software\_Engineer\_Level\_4.docx` | Supplied input job description |

| `fixtures/mock\_2\_Page\_CV\_SoftwareEngineer.docx` | Supplied expected output |

| `fixtures/Nam\_Nguyen\_Resume\_DEMO.docx` | Our own CV, used in the demo video (unseen by the client) |

| `fixtures/JD\_second\_role.md` | Our second RFQ, for the Inv-5 tailoring test |

| `output/Jordan\_Reeve\_CV\_GENERATED.docx` | Our output for the supplied pair, for side-by-side comparison |



\---



\## 9. Links



\- \*\*Repository:\*\* \_\[URL]\_

\- \*\*Project board:\*\* \_\[URL]\_

\- \*\*Demo video:\*\* \_\[URL]\_



# Inv-5 / T5: which suite the second RFQ belongs to

> Paste this into the README. Should 13 says: *"Say which suite it belongs to... picking without
> noticing there was a choice is the failure here."* This is that note.

## The choice

The brief offers two ways to use the second RFQ, and they prove different things:

| | Suite | What it asserts | What it cannot tell you |
|---|---|---|---|
| **Fake both model responses** | Pipeline test, 10.1a | Our *code* routes two different inputs to two different outputs, and the difference survives normalisation, assembly and persistence | Nothing about whether the model actually tailors — we wrote both answers |
| **Use a real API key** | Eval, 10.1b | The *model* genuinely responds to the job requirements | Has no stable pass/fail; the model has no stable output |

## What we chose, and why

**Both — but only the first goes in CI.**

**T5 is a pipeline test (10.1a), with a faked `IGeminiClient`.** The fake returns two deliberately different responses for the two RFQs, and the test asserts all five `generated` fields differ materially between them. That proves the thing CI can actually prove: that `jobRequirements` reaches the tailoring stage, that two inputs produce two outputs, and that nothing downstream flattens the difference. It runs offline, deterministically, with no key — which is what Inv-12 requires and why `IGeminiClient` exists at all.

**The real-key version is an eval (10.1b) and is deliberately NOT in CI.** Wiring it into CI buys a red build on a Tuesday because Gemini picked a different adjective, which tells us nothing and trains us to ignore the pipeline. It is run deliberately, by hand, and the result recorded below with a date.

## Why this is the right split, not a dodge

A faked T5 passing while the model ignores the RFQ entirely is a real failure mode, and the pipeline test cannot catch it. That is precisely why the eval exists and why we run it — but a check with no stable pass/fail does not belong in a gate that blocks merges. The distinction is between *tests* (deterministic, gate the build) and *evals* (probabilistic, inform a human).

## The second RFQ

`fixtures/JD_second_role.md` and `fixtures/JD_second_role.docx` — Senior Data Engineer, Kestrel Water Authority. Deliberately far from the Meridian software engineering RFQ:

- Different discipline (data engineering, not full-stack software engineering)
- Different essential criteria: E1 asks 8 years and 3 building production data pipelines, not 10 and 3 at senior level; E3 asks SQL and dimensional modelling, not a JavaScript front end; E5 asks data quality practice, not technical leadership
- Different location (Brisbane), availability window (8 weeks, not 6), and extension structure

If the same CV produces the same `coreCompetencies` against both, the tailoring is decorative and we would rather find that out now than in the demo.

## Last eval run

| Date | Result |
|---|---|
| _[DATE]_ | _[Record: did all five generated fields differ materially? Note anything that did not move.]_ |
=======
# Papps PM — CV Pipeline

An AI-powered CV tailoring system that transforms a candidate's raw CV into a polished, job-specific capability statement using a three-stage pipeline: extract, normalise, and tailor.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Supabase account](https://supabase.com) (free tier)
- [Google AI Studio account](https://aistudio.google.com) (free tier — Gemini API key)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/ericphamse/Papps-PM-Test-Project.git
cd Papps-PM-Test-Project/Apps
```

### 2. Set up secrets

Navigate to the backend project:

```bash
cd backend/CvPipeline.Api
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=db.xxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=yourpassword"
```

### 3. Apply database migrations

```bash
dotnet ef database update
```

---

## Running the Backend

```bash
cd Apps/backend/CvPipeline.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

API starts on `http://localhost:5000`

---

## Running the Frontend

```bash
cd Apps/web/recruitment
npm install
npm run dev
```

Frontend starts on `http://localhost:3000`

---

## Running the Tests

No API key or database required — runs fully offline.

```bash
cd Apps/CvPipeline.Tests
dotnet test
```

---

## How to Use

### Step 1 — Upload

1. Open `http://localhost:3000`
2. Upload the candidate CV (`.txt` file)
3. Paste the full job requirements (RFQ) text
4. Click **Generate CV**

### Step 2 — Review

The document displays with provenance badges on every field:
- 🟢 **Verbatim** — copied directly from the CV
- 🔵 **Normalised** — reformatted by a house-style rule
- 🟣 **Derived** — taken from the job requirements
- 🟡 **Generated** — written by AI
- 🔴 **Edited** — changed by you after generation
- ⚫ **Absent** — not found in the CV

### Step 3 — Edit

Use the **Edit Document** panel to modify:
- All detail-table fields (name, role, location etc.)
- Professional profile
- Role suitability (max 200 words — live word count shown)
- Core competency bullets (exactly 10)
- Career highlight headings and bullets (exactly 6)

Edited fields automatically show the **Edited** badge.

### Step 4 — Download

Click **Confirm & Download** to validate and download the `.docx` file.

Gate 2 checks before download:
- Referees present (J2)
- Role suitability ≤ 200 words (J4)
- All 8 detail-table rows present (J5)
- No candidate contact details in generated prose (O1)

---

## Architecture
POST /api/analyses
→ Persist raw inputs to database FIRST
→ Parse job requirements (C#): roleTitle, level, proposedRole

STAGE 1 — SELECT (Gemini call #1)
→ Extract verbatim CV spans per field
→ Tech votes: keep/drop per technology with JD citation
→ Gate 1: verify every quote is real substring of source

STAGE 2 — NORMALISE (pure C#)
→ N1–N11: format location, availability, dates, certifications
→ O1–O8: drop unrelated roles, pre-tertiary, irrelevant tech
→ S1, S2, S5: gapless timeline, top-4 qualifications, 2 referees

STAGE 3 — TAILOR (Gemini call #2)
→ Generate: professional profile, role suitability,
10 competency bullets, 6 career highlights, years of experience
→ Gate 2: structure, word count, grounding, voice rules

POST /api/generations
→ Gate 2 validation
→ Persist to database
→ Return .docx file


---

## Known Limitations

- **Generated content varies** between runs — AI output is non-deterministic. `FakeGeminiClient` provides deterministic output for CI.
- **Word file layout** — closely matches the template but does not replicate the gradient header image.
- **O2 (unrelated roles)** — uses a keyword list, not AI judgment. Extend `RelevanceFilter.cs` as needed.
- **JD parsing** — assumes RFQ table format (`Engagement title`, `Level required`, `Panel category`). Free-text RFQs return `Absent` for those fields.
- **Provenance is field-level** — one provenance tag per field, not per item within a list.

---

## Rule Decisions

| Rule | Decision |
|---|---|
| O1, O4, O5, O8 | Satisfied structurally — stage 1 never extracts contact details, personal interests, commercial terms, or extra referees |
| O2 | Keyword list in `RelevanceFilter.cs` — documented house policy, not model judgment |
| O7 | Stage 3 prompt instruction — hedging never appears in stage 2 fields |
| J1 | Manual check — max 2 pages verified by eye |
| J2, J4, J5 | Enforced at Gate 2 (`POST /api/generations`) |
| J3 | Structural — only one template exists |
| Provenance | Field-level, not leaf-level — one tag per field array |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 15, React |
| Backend | .NET 10 Web API |
| Database | PostgreSQL 15 via Supabase |
| ORM | EF Core + Npgsql |
| AI | Google Gemini (Google AI Studio free tier) |
| Word generation | DocumentFormat.OpenXml |
| Tests | xUnit, Jest |
>>>>>>> origin/main

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

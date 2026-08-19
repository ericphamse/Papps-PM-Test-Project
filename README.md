\# CV Tailoring Tool



Takes a candidate CV and a job description, both as `.docx`, and produces a tailored two-page CV in the client's template — with a provenance record for every value in it.



> \*\*Status:\*\* \_\[UPDATE BEFORE SUBMISSION]\_ — describe honestly what runs end to end and what does not.



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



\### Pointing at your own Supabase project from cold



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


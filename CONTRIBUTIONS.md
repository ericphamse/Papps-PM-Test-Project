\# Contributions



> The brief asks for who built what, who decided what, and where we disagreed.

> \_\[TEAM: fill in the placeholders honestly. "If you never disagreed across five weeks, one of you was not really there."]\_



\## Who we are



| | |

|---|---|

| \*\*Eric Pham\*\* | Backend — .NET API, the three-stage pipeline, Gemini integration, PostgreSQL persistence, Gates 1 and 2 |

| \*\*Ryan Dinh\*\* | Frontend — upload, editor, provenance UI, template tokens, HTML preview, docx renderer, download flow, history |



\## How we split the work



We divided along the API boundary: Eric owned everything server-side, Ryan owned everything in the browser. The seam is the `CvDocument` contract in section 4.2 of the brief, which both sides code against.



\_\[TEAM: was this the right split? Say so honestly. One real cost: neither of us reviewed the other's language closely at first, and the frontend was built against a fixture for longer than was ideal — which is how the JD-input mismatch in §"Disagreements" below went unnoticed for a fortnight.]\_



\## Who built what



\### Backend (Eric)



\- `POST /api/analyses` — input persistence before any model call

\- Stage 1 SELECT + Gate 1 quote verification with single retry

\- Stage 2 NORMALISE — rules N1–N11, O1–O8, S1–S5, in pure C#

\- Stage 3 TAILOR — the five generated fields, structural validation

\- `POST /api/generations` — Gate 2 (O1 sweep, J2, J4, J5)

\- EF Core schema, migrations, constraints

\- Pipeline test suite against a fake Gemini client



\### Frontend (Ryan)



\- `UploadForm` — two `.docx` inputs, lifecycle states, input preserved on failure

\- `DocumentEditor` + `FieldInput` — all 17 fields, the 5.4 editing contract

\- `ProvenanceBadge` — all six provenance classes, with source quotes shown and flagged when stale

\- `tokens.ts` — single source of truth for template colours and measurements, plus the hex-literal guard test

\- `PreviewRenderer` — template-faithful HTML, live against edits

\- `features/docx` — the `.docx` renderer, details table, header/footer, bullet numbering

\- `DownloadButton` — the 201/422 gate flow

\- `HistoryPanel` — list and reload previous generations

\- Conformance verification against the unzipped `word/document.xml`



\## Decisions, and who made them



| Decision | Who | Note |

|---|---|---|

| Model proposes, code decides — normalisation as deterministic C#, not prompts | \_\[?]\_ | Brief mandates it; the interpretation was ours |

| Build the `.docx` in the browser, gate it on a server 201 | \_\[?]\_ | Forced by `docx` being a Node library — but it made the gate design honest |

| One `tokens.ts` shared by both renderers, owned by neither | Ryan | Prevents the preview and the Word file drifting apart |

| Frontend built against a fixture first | Ryan | Unblocked frontend work while the backend was unrunnable. Cost: see disagreements |

| Store both `analyses.document` and `generations.document` | \_\[?]\_ | See README §4 |

| Omit the ungrounded awards row (6.6 wart 5) | \_\[?]\_ | Only defensible answer under Inv-2 |

| Reproduce wart 3, normalise warts 1, 2 and 4 | Ryan | See the differences table in the README |

| Dated sections as tab stops, not tables | Ryan | Found by asserting against the XML — four tables where 6.3 allows one |



\## Where we disagreed, and what happened



\_\[TEAM: these need to be real. Placeholders below are things that actually came up — keep the ones that are true, cut the rest, add your own.]\_



\*\*1. The job description input format.\*\*

The backend accepted job requirements as pasted text and the frontend sent it that way for two weeks. Ryan re-read Must 1 — \*"Same thing for job description, also `.docx`. There is no other input path: no PDF, no paste-the-CV, no plain-text upload"\* — and raised that both halves were non-conforming on the first item of the Must list. The frontend changed to send two files; the backend extractor was extended to the JD.



\*\*2. How long to stay on the fixture.\*\*

\_\[TEAM: fill in. The frontend ran against a mock while the backend ran against real Gemini calls, and the two were not run together until late. Andrew flagged it. Say what the disagreement actually was and how it resolved.]\_



\*\*3. \_\[Add a third if there was one.]\_\*\*



\## Process



\- Branches per stage, merged via pull request

\- Reviews: \_\[TEAM: be honest about review quality. A wall of "LGTM" tells the reader one of you was not reading.]\_

\- Board: \_\[URL]\_



\_\[TEAM: if the process was not clean the whole way — and ours was not; see the branch history around the merge that restored deleted backend files — say so. An honest account of a messy middle reads better than a claim of a tidy one that the commit log contradicts.]\_


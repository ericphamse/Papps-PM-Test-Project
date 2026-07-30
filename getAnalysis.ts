// web/recruitment/src/features/analysis/getAnalysis.ts
//
// One place that produces the analysis. Right now it returns the mock so you can
// build the UI with no backend. When the backend is reachable (see START_HERE
// Part 3), flip USE_MOCK to false and this calls the real endpoint instead —
// nothing else in the UI has to change.

import { mockAnalysis } from "./mockAnalysis";
import type { Analysis } from "./types";

const USE_MOCK = true; // <-- flip to false once the backend is reachable

export async function getAnalysis(): Promise<Analysis> {
  if (USE_MOCK) {
    return mockAnalysis;
  }

  const base = process.env.NEXT_PUBLIC_API_BASE_URL;

  // NOTE: /api/analyses currently expects TEXT, not a file:
  //   { rawCvText, jobDescription }. Wire these to real inputs later; the
  //   .docx upload path needs the backend to add a file endpoint first.
  const res = await fetch(`${base}/api/analyses`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      rawCvText: "REPLACE_WITH_REAL_CV_TEXT",
      jobDescription: "REPLACE_WITH_REAL_JOB_DESCRIPTION",
    }),
  });

  if (!res.ok) {
    throw new Error(`POST /api/analyses failed: ${res.status}`);
  }

  return (await res.json()) as Analysis;
}

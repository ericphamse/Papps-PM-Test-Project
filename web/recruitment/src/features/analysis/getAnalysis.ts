// web/recruitment/src/features/analysis/getAnalysis.ts
//
// The one place that produces an Analysis. Sends BOTH inputs as files to
// POST /api/analyses as multipart/form-data.
//
// Must 1: "Upload the candidate CV as a .docx ... Same thing for job
// description, also .docx. There is no other input path: no PDF, no
// paste-the-CV, no plain-text upload."
// So the job description is a FILE, not pasted text. The server extracts the
// text from both — extraction is a server-side concern (Must 1), and the same
// CvTextExtractor handles both documents.
//
// USE_MOCK stays true until Eric confirms his field names + CORS. Flip it to
// false for the integration session; nothing else in the UI has to change.

import { mockAnalysis } from "./mockAnalysis";
import type { Analysis } from "./types";

const USE_MOCK = true; // <-- flip to false for the real integration session

// The API address comes from config, never hardcoded (Andrew's point 3).
// Set NEXT_PUBLIC_API_BASE_URL in .env.local — e.g. http://localhost:5068
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL;

// CONFIRM WITH ERIC: the exact multipart field names his endpoint binds to.
// These two constants are the only thing that changes if his names differ.
const CV_FILE_FIELD = "cvFile";
const JD_FILE_FIELD = "jobDescriptionFile";

export async function getAnalysis(cvFile: File, jdFile: File): Promise<Analysis> {
  if (USE_MOCK) {
    // Small delay so the loading states are actually visible while developing.
    await new Promise((r) => setTimeout(r, 400));
    return mockAnalysis;
  }

  if (!API_BASE) {
    throw new Error("NEXT_PUBLIC_API_BASE_URL is not set. Add it to .env.local");
  }

  const form = new FormData();
  form.append(CV_FILE_FIELD, cvFile);
  form.append(JD_FILE_FIELD, jdFile);

  const res = await fetch(`${API_BASE}/api/analyses`, {
    method: "POST",
    // NO Content-Type header here — on purpose. The browser must set it itself
    // so it can add the multipart boundary marker. Setting it by hand produces
    // a request the server cannot parse, with a confusing error.
    body: form,
  });

  if (!res.ok) {
    // 422 = extraction/verification failed. Surface the server's reason if it
    // sent one, so the user sees why rather than a bare status code.
    let detail = "";
    try {
      const body = await res.json();
      detail = body?.error ?? body?.message ?? JSON.stringify(body);
    } catch {
      detail = await res.text().catch(() => "");
    }
    throw new Error(`Analysis failed (${res.status})${detail ? `: ${detail}` : ""}`);
  }

  return (await res.json()) as Analysis;
}
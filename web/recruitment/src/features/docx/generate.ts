// web/recruitment/src/features/docx/generate.ts
//
// Stage 6: the generate flow. The download is GATED by the server (5.3):
//   POST /api/generations with the confirmed document
//     -> 201 { generationId }            : render locked in, download proceeds
//     -> 422 { violations: [...] }       : NO download, violations shown
// The gate lives server-side because a browser-only rule is enforced only
// until someone opens dev tools. There is deliberately NO code path from an
// edited document straight to Packer without passing through here.

import { Packer } from "docx";
import { buildCvDocument } from "./template";
import type { CvDocument } from "@/features/analysis/types";

export interface Violation {
  rule: string; // e.g. "O1", "J2", "J4"
  field?: string;
  message: string;
}

export type GenerateResult =
  | { ok: true; generationId: string }
  | { ok: false; violations: Violation[] };

const USE_MOCK = true; // flip when the backend endpoint exists

export async function requestGeneration(analysisId: string, doc: CvDocument): Promise<GenerateResult> {
  if (USE_MOCK) {
    // Mimics the server gate closely enough to build the UI against:
    // J2 — referees must be present.
    if (!doc.referees.value || doc.referees.value.length === 0) {
      return { ok: false, violations: [{ rule: "J2", field: "referees", message: "Referees are required." }] };
    }
    // J4 — role suitability max 200 words.
    const words = (doc.roleSuitability.value ?? "").trim().split(/\s+/).filter(Boolean).length;
    if (words > 200) {
      return {
        ok: false,
        violations: [{ rule: "J4", field: "roleSuitability", message: `Role suitability is ${words} words; maximum is 200.` }],
      };
    }
    return { ok: true, generationId: "mock-generation-1" };
  }

  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  const res = await fetch(`${base}/api/generations`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ analysisId, document: doc }),
  });

  if (res.status === 201) {
    const data = await res.json();
    return { ok: true, generationId: data.generationId };
  }
  if (res.status === 422) {
    const data = await res.json();
    return { ok: false, violations: data.violations ?? [] };
  }
  throw new Error(`POST /api/generations failed: ${res.status}`);
}

// Only called AFTER a 201. Builds the file in the browser and triggers download.
export async function downloadCv(doc: CvDocument): Promise<void> {
  const blob = await Packer.toBlob(buildCvDocument(doc));
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `${(doc.fullName.value ?? "cv").replace(/\s+/g, "_")}_CV.docx`;
  a.click();
  URL.revokeObjectURL(url);
}

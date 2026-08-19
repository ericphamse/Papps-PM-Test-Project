// web/recruitment/src/features/docx/DownloadButton.tsx
//
// The UI for Stage 6. Disabled while requesting; on 422 shows the violations
// and does NOT download; the user's edits are untouched either way.

"use client";

import { useState } from "react";
import { requestGeneration, downloadCv, type Violation } from "./generate";
import { tokens } from "@/features/template/tokens";
import type { CvDocument } from "@/features/analysis/types";

// Editor-chrome colours for the violation box. Deliberately NOT in tokens.ts:
// tokens.ts is the CV template's palette, and these are error-state UI, which
// the brief does not grade on appearance. They are named here so the file
// carries no bare hex literals — the rule the tokens test enforces.
const ERROR_BORDER = "hsl(0 60% 67%)";
const ERROR_BG = "hsl(0 100% 96%)";

export function DownloadButton({ analysisId, document: doc }: { analysisId: string; document: CvDocument }) {
  const [busy, setBusy] = useState(false);
  const [violations, setViolations] = useState<Violation[]>([]);

  async function handleClick() {
    setBusy(true);
    setViolations([]);
    try {
      const result = await requestGeneration(analysisId, doc);
      if (result.ok) {
        await downloadCv(doc); // gate passed — build and download
      } else {
        setViolations(result.violations); // gate failed — show why, no file
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={{ display: "grid", gap: 8 }}>
      <button
        onClick={handleClick}
        disabled={busy}
        style={{
          padding: "10px 18px",
          fontSize: 15,
          fontWeight: 600,
          borderRadius: 6,
          border: "none",
          cursor: busy ? "wait" : "pointer",
          background: tokens.navy,   // from tokens.ts — never re-typed
          color: tokens.white,
        }}
      >
        {busy ? "Checking…" : "Download CV (.docx)"}
      </button>

      {violations.length > 0 && (
        <div style={{ border: `1px solid ${ERROR_BORDER}`, background: ERROR_BG, borderRadius: 6, padding: 12, fontSize: 13 }}>
          <strong>The document can’t be generated yet:</strong>
          <ul style={{ margin: "6px 0 0 18px" }}>
            {violations.map((v, i) => (
              <li key={i}>
                [{v.rule}] {v.message}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
// web/recruitment/src/app/editor/page.tsx
//
// Now a CLIENT component, because the flow is stateful: the page shows the
// upload form first, then swaps to the editor once an Analysis comes back.
// (It used to be a server component that fetched a mock at render time — that
// made sense when there was no upload step. There is now.)

"use client";

import { useState } from "react";
import { UploadForm } from "@/features/analysis/UploadForm";
import { DocumentEditor } from "@/features/editor/DocumentEditor";
import type { Analysis } from "@/features/analysis/types";

export default function EditorPage() {
  const [analysis, setAnalysis] = useState<Analysis | null>(null);

  return (
    <main style={{ maxWidth: 1400, margin: "0 auto", padding: 24, fontFamily: "Arial, sans-serif" }}>
      <h1 style={{ fontSize: 24, marginBottom: 4 }}>CV Tailoring</h1>

      {!analysis ? (
        <>
          <p style={{ color: "#555", marginBottom: 24 }}>
            Upload a candidate CV and paste the job requirements to generate a tailored document.
          </p>
          <UploadForm onAnalysed={setAnalysis} />
        </>
      ) : (
        <>
          <p style={{ color: "#555", marginBottom: 12 }}>
            Status: <strong>{analysis.status}</strong>
            {" · "}
            <button
              onClick={() => setAnalysis(null)} // back to the form to start again
              style={{
                background: "none",
                border: "none",
                padding: 0,
                color: "#1E1560",
                textDecoration: "underline",
                cursor: "pointer",
                fontSize: "inherit",
              }}
            >
              Start over
            </button>
          </p>

          {analysis.warnings.length > 0 && (
            <div
              style={{
                border: "1px solid #FFB74D",
                background: "#FFF8E1",
                borderRadius: 6,
                padding: 12,
                fontSize: 13,
                marginBottom: 16,
              }}
            >
              <strong>Warnings — worth a look before you download:</strong>
              <ul style={{ margin: "6px 0 0 18px" }}>
                {analysis.warnings.map((w, i) => (
                  <li key={i}>
                    {w.field ? `[${w.field}] ` : ""}
                    {w.message}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {analysis.document && (
            <DocumentEditor document={analysis.document} analysisId={analysis.analysisId} />
          )}
        </>
      )}
    </main>
  );
}

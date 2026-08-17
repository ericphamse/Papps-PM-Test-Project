// web/recruitment/src/features/analysis/UploadForm.tsx
//
// Must 1: upload a candidate CV (.docx) plus the job requirements text, and
// send them to POST /api/analyses. This is the ONLY input path — no PDF, no
// pasted CV text.
//
// A client component: it holds the chosen file, the typed text, and the
// lifecycle state. On success it hands the Analysis up to its parent, which
// swaps the form out for the editor.
//
// Lifecycle (5.2): idle -> extracting -> tailoring -> review | failed.
// On failure the user's file and text are DELIBERATELY kept, so a failed
// attempt never costs them their input (Should 15).

"use client";

import { useState } from "react";
import { getAnalysis } from "./getAnalysis";
import type { Analysis } from "./types";

type Phase = "idle" | "extracting" | "tailoring" | "failed";

export function UploadForm({ onAnalysed }: { onAnalysed: (a: Analysis) => void }) {
  const [file, setFile] = useState<File | null>(null);
  const [jobRequirements, setJobRequirements] = useState("");
  const [phase, setPhase] = useState<Phase>("idle");
  const [error, setError] = useState<string | null>(null);

  const busy = phase === "extracting" || phase === "tailoring";
  const canSubmit = file !== null && jobRequirements.trim().length > 0 && !busy;

  async function handleSubmit() {
    if (!file) return;
    setError(null);
    setPhase("extracting");

    try {
      // The server does extract -> tailor in one call, so we show 'tailoring'
      // shortly after starting rather than polling for a status we can't see.
      const timer = setTimeout(() => setPhase("tailoring"), 1200);
      const analysis = await getAnalysis(file, jobRequirements);
      clearTimeout(timer);

      if (analysis.status === "failed") {
        setPhase("failed");
        setError("The server could not extract this CV. Check the file and try again.");
        return; // input is kept — the user can retry without re-entering anything
      }

      onAnalysed(analysis); // hand the document up; parent shows the editor
    } catch (e) {
      setPhase("failed");
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  return (
    <div style={{ display: "grid", gap: 16, maxWidth: 620 }}>
      <div style={{ display: "grid", gap: 6 }}>
        <label htmlFor="cv" style={{ fontSize: 13, fontWeight: 600 }}>
          Candidate CV (.docx)
        </label>
        <input
          id="cv"
          type="file"
          accept=".docx" // Must 1: .docx only
          disabled={busy}
          onChange={(e) => {
            setFile(e.target.files?.[0] ?? null);
            setError(null);
          }}
        />
        {file && (
          <span style={{ fontSize: 12, color: "#555" }}>
            Selected: {file.name} ({Math.round(file.size / 1024)} KB)
          </span>
        )}
      </div>

      <div style={{ display: "grid", gap: 6 }}>
        <label htmlFor="jd" style={{ fontSize: 13, fontWeight: 600 }}>
          Job requirements
        </label>
        <textarea
          id="jd"
          rows={10}
          value={jobRequirements}
          disabled={busy}
          onChange={(e) => setJobRequirements(e.target.value)}
          placeholder="Paste the RFQ / job requirements text here…"
          style={{
            padding: 10,
            fontSize: 14,
            fontFamily: "inherit",
            border: "1px solid #ccc",
            borderRadius: 4,
            resize: "vertical",
          }}
        />
      </div>

      <button
        onClick={handleSubmit}
        disabled={!canSubmit}
        style={{
          padding: "10px 18px",
          fontSize: 15,
          fontWeight: 600,
          borderRadius: 6,
          border: "none",
          cursor: canSubmit ? "pointer" : "not-allowed",
          background: canSubmit ? "#1E1560" : "#9E9E9E",
          color: "#FFFFFF",
          justifySelf: "start",
        }}
      >
        {phase === "extracting"
          ? "Extracting…"
          : phase === "tailoring"
            ? "Tailoring…"
            : "Generate tailored CV"}
      </button>

      {busy && (
        <p style={{ fontSize: 13, color: "#555" }}>
          {phase === "extracting"
            ? "Reading the CV and verifying quotes against the source…"
            : "Generating the tailored sections…"}
        </p>
      )}

      {error && (
        <div
          style={{
            border: "1px solid #E57373",
            background: "#FFEBEE",
            borderRadius: 6,
            padding: 12,
            fontSize: 13,
          }}
        >
          <strong>Couldn’t process that CV.</strong>
          <div style={{ marginTop: 4 }}>{error}</div>
          <div style={{ marginTop: 6, color: "#666" }}>
            Your file and text are still here — fix the issue and try again.
          </div>
        </div>
      )}
    </div>
  );
}

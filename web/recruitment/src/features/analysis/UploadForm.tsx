// web/recruitment/src/features/analysis/UploadForm.tsx
//
// Must 1: upload the candidate CV as a .docx AND the job description as a
// .docx. "There is no other input path: no PDF, no paste-the-CV, no plain-text
// upload." Both inputs are files; the server extracts the text from each.
//
// A client component: holds the two chosen files and the lifecycle state.
// On success it hands the Analysis up to its parent, which swaps the form out
// for the editor.
//
// Lifecycle (5.2): idle -> extracting -> tailoring -> review | failed.
// On failure BOTH file selections are DELIBERATELY kept, so a failed attempt
// never costs the user their input (Should 15).

"use client";

import { useState } from "react";
import { getAnalysis } from "./getAnalysis";
import type { Analysis } from "./types";

type Phase = "idle" | "extracting" | "tailoring" | "failed";

// Reusable .docx picker. Rejects anything else before it reaches the server —
// the server still validates, but failing fast here is a better experience.
function DocxPicker({
  id,
  label,
  hint,
  file,
  disabled,
  onPick,
}: {
  id: string;
  label: string;
  hint: string;
  file: File | null;
  disabled: boolean;
  onPick: (f: File | null) => void;
}) {
  return (
    <div style={{ display: "grid", gap: 6 }}>
      <label htmlFor={id} style={{ fontSize: 13, fontWeight: 600 }}>
        {label}
      </label>
      <span style={{ fontSize: 12, color: "#777" }}>{hint}</span>
      <input
        id={id}
        type="file"
        accept=".docx" // Must 1: .docx only
        disabled={disabled}
        onChange={(e) => onPick(e.target.files?.[0] ?? null)}
      />
      {file && (
        <span style={{ fontSize: 12, color: "#555" }}>
          Selected: {file.name} ({Math.round(file.size / 1024)} KB)
        </span>
      )}
    </div>
  );
}

export function UploadForm({ onAnalysed }: { onAnalysed: (a: Analysis) => void }) {
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [jdFile, setJdFile] = useState<File | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const [error, setError] = useState<string | null>(null);

  const busy = phase === "extracting" || phase === "tailoring";
  const canSubmit = cvFile !== null && jdFile !== null && !busy;

  // Guard against a file picked through drag-drop or a renamed extension.
  function isDocx(f: File) {
    return f.name.toLowerCase().endsWith(".docx");
  }

  async function handleSubmit() {
    if (!cvFile || !jdFile) return;

    if (!isDocx(cvFile) || !isDocx(jdFile)) {
      setError("Both files must be .docx. No PDF or plain text.");
      setPhase("failed");
      return;
    }

    setError(null);
    setPhase("extracting");

    try {
      // The server does extract -> tailor in one call, so we show 'tailoring'
      // shortly after starting rather than polling for a status we can't see.
      const timer = setTimeout(() => setPhase("tailoring"), 1200);
      const analysis = await getAnalysis(cvFile, jdFile);
      clearTimeout(timer);

      if (analysis.status === "failed") {
        setPhase("failed");
        setError("The server could not extract these documents. Check the files and try again.");
        return; // both selections kept — retry without re-picking anything
      }

      onAnalysed(analysis); // hand the document up; parent shows the editor
    } catch (e) {
      setPhase("failed");
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  return (
    <div style={{ display: "grid", gap: 20, maxWidth: 620 }}>
      <DocxPicker
        id="cv"
        label="Candidate CV"
        hint="Word document (.docx) — the candidate's original CV"
        file={cvFile}
        disabled={busy}
        onPick={(f) => {
          setCvFile(f);
          setError(null);
        }}
      />

      <DocxPicker
        id="jd"
        label="Job description"
        hint="Word document (.docx) — the RFQ or job requirements"
        file={jdFile}
        disabled={busy}
        onPick={(f) => {
          setJdFile(f);
          setError(null);
        }}
      />

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
            ? "Reading both documents and verifying quotes against the source…"
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
          <strong>Couldn’t process those documents.</strong>
          <div style={{ marginTop: 4 }}>{error}</div>
          <div style={{ marginTop: 6, color: "#666" }}>
            Your file selections are still here — fix the issue and try again.
          </div>
        </div>
      )}
    </div>
  );
}
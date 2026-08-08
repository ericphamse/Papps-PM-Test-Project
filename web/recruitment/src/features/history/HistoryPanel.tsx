// web/recruitment/src/features/history/HistoryPanel.tsx
//
// Stage 7: list past generations and reload one into the editor (Must 9 UI).
// Persistence itself is the backend's job; this panel just reads it. Mocked
// until GET /api/generations exists — same toggle pattern as everywhere else.

"use client";

import { useEffect, useState } from "react";
import type { CvDocument } from "@/features/analysis/types";
import { mockAnalysis } from "@/features/analysis/mockAnalysis";

export interface HistoryEntry {
  generationId: string;
  createdAt: string; // ISO timestamp
  document: CvDocument;
}

const USE_MOCK = true;

async function fetchHistory(): Promise<HistoryEntry[]> {
  if (USE_MOCK) {
    return [
      {
        generationId: "mock-generation-1",
        createdAt: new Date().toISOString(),
        document: mockAnalysis.document!,
      },
    ];
  }
  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  const res = await fetch(`${base}/api/generations`);
  if (!res.ok) throw new Error(`GET /api/generations failed: ${res.status}`);
  return res.json();
}

export function HistoryPanel({ onLoad }: { onLoad: (doc: CvDocument) => void }) {
  const [entries, setEntries] = useState<HistoryEntry[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchHistory().then(setEntries).catch((e) => setError(String(e)));
  }, []);

  if (error) return <p style={{ color: "#B71C1C", fontSize: 13 }}>History unavailable: {error}</p>;
  if (entries.length === 0) return <p style={{ fontSize: 13, color: "#777" }}>No previous generations.</p>;

  return (
    <section>
      <h2 style={{ fontSize: 16, marginBottom: 8 }}>Previous generations</h2>
      <ul style={{ listStyle: "none", padding: 0, display: "grid", gap: 6 }}>
        {entries.map((e) => (
          <li key={e.generationId} style={{ display: "flex", alignItems: "center", gap: 10, fontSize: 13 }}>
            <span>{new Date(e.createdAt).toLocaleString()}</span>
            <button
              onClick={() => onLoad(e.document)} // hand the stored document back to the editor
              style={{ padding: "3px 10px", fontSize: 12, borderRadius: 4, border: "1px solid #ccc", cursor: "pointer", background: "#fff" }}
            >
              Load into editor
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}

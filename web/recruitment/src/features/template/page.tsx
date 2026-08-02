// web/recruitment/src/app/page.tsx
//
// A SERVER component: async, awaits the data, no state, no "use client".
// It hands the whole document to DocumentEditor, which is the client component
// that owns the editing. Fetch high, edit low.

import { getAnalysis } from "@/features/analysis/getAnalysis";
import { DocumentEditor } from "@/features/editor/DocumentEditor";

export default async function Home() {
  const analysis = await getAnalysis();

  return (
    <main style={{ maxWidth: 800, margin: "0 auto", padding: 24, fontFamily: "Arial, sans-serif" }}>
      <h1 style={{ fontSize: 24, marginBottom: 4 }}>CV Tailoring — draft</h1>
      <p style={{ color: "#555", marginBottom: 24 }}>
        Status: <strong>{analysis.status}</strong> (data source: mock)
      </p>

      {/* `analysis.document` is plain JSON, so it crosses the server -> client
          boundary fine. Functions and class instances would not. */}
      {analysis.document && <DocumentEditor document={analysis.document} />}
    </main>
  );
}

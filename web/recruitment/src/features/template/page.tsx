// web/recruitment/src/app/page.tsx
//
// A SERVER component: it can be async and await data directly. It does NOT get
// "use client" — it has no state and no event handlers. It fetches the analysis
// and hands the data down to DetailsEditor, which is the client component that
// owns the typing.
//
// Fetch high, edit low. This shape recurs: the upload form and the preview will
// follow it too.

import { getAnalysis } from "@/features/analysis/getAnalysis";
import { DetailsEditor } from "@/features/editor/DetailsEditor";

export default async function Home() {
  const analysis = await getAnalysis();
  const details = analysis.document?.personalDetails;

  return (
    <main style={{ maxWidth: 800, margin: "0 auto", padding: 24, fontFamily: "Arial, sans-serif" }}>
      <h1 style={{ fontSize: 24, marginBottom: 4 }}>CV Tailoring — draft</h1>
      <p style={{ color: "#555", marginBottom: 24 }}>
        Status: <strong>{analysis.status}</strong> (data source: mock)
      </p>

      {/* `details` is plain JSON, so it crosses the server -> client boundary
          fine. Functions and class instances would not — that is what a "not
          serializable" error means if you ever hit one. */}
      {details && <DetailsEditor details={details} />}

      <details>
        <summary style={{ cursor: "pointer", color: "#555" }}>Raw response (debug)</summary>
        <pre style={{ background: "#f6f6f6", padding: 12, borderRadius: 6, overflowX: "auto" }}>
          {JSON.stringify(analysis, null, 2)}
        </pre>
      </details>
    </main>
  );
}

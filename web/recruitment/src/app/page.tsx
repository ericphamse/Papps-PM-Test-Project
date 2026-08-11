// src/app/page.tsx
"use client";

import router from "next/dist/shared/lib/router/router";
import { useState } from "react";

export default function Home() {
  const [cvFile, setCvFile] = useState<File | null>(null);
  const [jobRequirements, setJobRequirements] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!cvFile) { setError("Please select a CV file."); return; }
    if (!jobRequirements.trim()) { setError("Please enter job requirements."); return; }

    setLoading(true);
    setError(null);
    setResult(null);

    const formData = new FormData();
    formData.append("cvFile", cvFile);
    formData.append("jobRequirements", jobRequirements);

    try {
      const response = await fetch("http://localhost:5068/api/analyses", {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const err = await response.json();
        setError(JSON.stringify(err, null, 2));
        return;
      }

      const data = await response.json();
      setResult(data);
    } catch (err) {
      setError("Failed to connect to backend. Make sure it is running on port 5068.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-gray-50 flex items-center justify-center p-8">
      <div className="bg-white rounded-2xl shadow-md w-full max-w-2xl p-8">
        <h1 className="text-2xl font-bold text-gray-800 mb-2">CV Pipeline</h1>
        <p className="text-gray-500 text-sm mb-8">Upload a candidate CV and paste the job requirements to generate a tailored document.</p>

        <form onSubmit={handleSubmit} className="space-y-6">

          {/* CV File Upload */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Candidate CV (.docx or .txt)
            </label>
            <input
              type="file"
              accept=".docx,.txt"
              onChange={(e) => setCvFile(e.target.files?.[0] ?? null)}
              className="block w-full text-sm text-gray-500
                file:mr-4 file:py-2 file:px-4
                file:rounded-lg file:border-0
                file:text-sm file:font-medium
                file:bg-blue-50 file:text-blue-700
                hover:file:bg-blue-100"
            />
            {cvFile && (
              <p className="mt-1 text-xs text-gray-400">Selected: {cvFile.name}</p>
            )}
          </div>

          {/* Job Requirements */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Job Requirements (RFQ text)
            </label>
            <textarea
              value={jobRequirements}
              onChange={(e) => setJobRequirements(e.target.value)}
              rows={10}
              placeholder="Paste the full job requirements / RFQ text here..."
              className="w-full rounded-lg border border-gray-300 p-3 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>

          {/* Error */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <p className="text-sm text-red-700 whitespace-pre-wrap">{error}</p>
            </div>
          )}

          {/* Submit */}
          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300
              text-white font-medium py-3 px-6 rounded-lg transition-colors"
          >
            {loading ? "Processing..." : "Generate CV"}
          </button>
        </form>

        {/* Result */}
        {result && (
          <div className="mt-8 space-y-8">
            
            {/* Raw JSON - collapsible */}
            <details>
              <summary className="cursor-pointer text-sm font-medium text-gray-500 hover:text-gray-700">
                View raw JSON
              </summary>
              <div className="mt-2 bg-gray-50 rounded-lg border border-gray-200 p-4 overflow-auto max-h-96">
                <pre className="text-xs text-gray-600 whitespace-pre-wrap">
                  {JSON.stringify(result, null, 2)}
                </pre>
              </div>
            </details>

            {/* Document display */}
            {result.tailoredDocument && <CvDocumentView doc={result.tailoredDocument} />}

            {result.tailoredDocument && (
              <EditableDocumentView
                doc={result.tailoredDocument}
                onDocumentChange={(updated) => setResult((prev: any) => ({ ...prev, tailoredDocument: updated }))}
              />
            )}
          </div>
        )}
      </div>
    </main>
  );
}

const NAVY = "#1B2A4A";
const ORANGE = "#C0592A";

function SectionHeading({ children }: { children: React.ReactNode }) {
  return (
    <h2 style={{ color: ORANGE, fontSize: 13, fontWeight: 700, marginTop: 24, marginBottom: 4, borderBottom: `1px solid #ccc`, paddingBottom: 4 }}>
      {children}
    </h2>
  );
}

function CvDocumentView({ doc }: { doc: any }) {
  const fullName = doc.fullName?.value ?? "";
  const roleTitle = doc.roleTitle?.value ?? "";
  const level = doc.level?.value ?? "";
  const referees = doc.referees?.value ?? [];
  const career = doc.careerSynopsis?.value ?? [];
  const competencies = doc.coreCompetencies?.value ?? [];
  const commendations = doc.commendationsAndAwards?.value ?? [];
  const qualifications = doc.qualificationsDetailed?.value ?? [];
  const highlights = doc.careerHighlights?.value ?? [];
  const qualList = doc.qualifications?.value ?? [];

  return (
    <div style={{ fontFamily: "Calibri, Arial, sans-serif", fontSize: 11, background: "white", boxShadow: "0 4px 24px rgba(0,0,0,0.15)", borderRadius: 8, color: "#111"  }}>

      {/* Header band */}
      <div style={{ background: `linear-gradient(135deg, #E8A87C 0%, #C0592A 40%, #5B7FA6 100%)`, padding: "18px 32px", borderRadius: "8px 8px 0 0" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 24 }}>
          <div style={{ background: "white", padding: "6px 12px", borderRadius: 4, fontWeight: 900, fontSize: 14, color: NAVY }}>
            Papps<span style={{ color: ORANGE }}>PM</span>
          </div>
          <div style={{ color: "white", fontWeight: 700, fontSize: 14 }}>
            {fullName} | {roleTitle}
          </div>
        </div>
      </div>

      <div style={{ padding: "24px 32px 32px" }}>

        {/* Title */}
        <h1 style={{ fontSize: 16, fontWeight: 700, color: NAVY, marginBottom: 12, borderBottom: `2px solid ${NAVY}`, paddingBottom: 6 }}>
          {fullName} | {roleTitle} | {level}
        </h1>

        {/* Details table */}
        <table style={{ width: "100%", borderCollapse: "collapse", marginBottom: 16 }}>
          <tbody>
            {[
              ["FULL NAME", fullName],
              ["PROPOSED ROLE", doc.proposedRole?.value],
              ["QUALIFICATIONS", qualList.join("; ")],
              ["SECURITY CLEARANCE", doc.securityClearance?.value],
              ["YEARS OF EXPERIENCE", doc.yearsOfExperience?.value],
              ["AVAILABILITY", doc.availability?.value],
              ["LOCATION", doc.location?.value],
            ].map(([label, value]) => (
              <tr key={label as string}>
                <td style={{ background: NAVY, color: "white", fontWeight: 700, padding: "6px 10px", width: "38%", fontSize: 10, letterSpacing: 0.5 }}>
                  {label}
                </td>
                <td style={{ background: "#EEF1F7", padding: "6px 10px" }}>
                  {(value as string) || "—"}
                </td>
              </tr>
            ))}
            <tr>
              <td style={{ background: NAVY, color: "white", fontWeight: 700, padding: "6px 10px", fontSize: 10, letterSpacing: 0.5, verticalAlign: "top" }}>
                REFEREES
              </td>
              <td style={{ background: "#EEF1F7", padding: "6px 10px" }}>
                {referees.map((r: any, i: number) => (
                  <div key={i} style={{ marginBottom: i < referees.length - 1 ? 8 : 0 }}>
                    {r.name} | {r.position}, {r.organisation} | {r.phone}
                  </div>
                ))}
              </td>
            </tr>
          </tbody>
        </table>

        {/* Professional Profile */}
        <SectionHeading>Professional Profile</SectionHeading>
        <p style={{ margin: "8px 0 0 0", lineHeight: 1.6 }}>{doc.professionalProfile?.value ?? "—"}</p>

        {/* Career Synopsis */}
        <SectionHeading>Career Synopsis</SectionHeading>
        <table style={{ width: "100%", borderCollapse: "collapse", marginTop: 6 }}>
          <tbody>
            {career.map((e: any, i: number) => (
              <tr key={i}>
                <td style={{ padding: "2px 0", width: "65%" }}>{e.title} ({e.organisation})</td>
                <td style={{ padding: "2px 0", textAlign: "right" }}>{e.startYear} – {e.endYear}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Role Suitability */}
        <SectionHeading>Role Suitability</SectionHeading>
        <p style={{ margin: "8px 0 0 0", lineHeight: 1.6 }}>{doc.roleSuitability?.value ?? "—"}</p>

        {/* Core Competencies */}
        <SectionHeading>Core Competencies</SectionHeading>
        <ul style={{ margin: "6px 0 0 0", paddingLeft: 20 }}>
          {competencies.map((c: any, i: number) => (
            <li key={i} style={{ marginBottom: 2 }}>{c.text}</li>
          ))}
        </ul>

        {/* Commendations and Awards */}
        {commendations.length > 0 && (
          <>
            <SectionHeading>Commendations and Awards</SectionHeading>
            <table style={{ width: "100%", borderCollapse: "collapse", marginTop: 6 }}>
              <tbody>
                {commendations.map((c: any, i: number) => (
                  <tr key={i}>
                    <td style={{ padding: "2px 0", width: "75%" }}>{c.description}</td>
                    <td style={{ padding: "2px 0", textAlign: "right" }}>{c.year}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}

        {/* Qualifications */}
        {qualifications.length > 0 && (
          <>
            <SectionHeading>Qualifications</SectionHeading>
            <table style={{ width: "100%", borderCollapse: "collapse", marginTop: 6 }}>
              <tbody>
                {qualifications.map((q: any, i: number) => (
                  <tr key={i}>
                    <td style={{ padding: "2px 0", width: "75%" }}>
                      {q.qualification}{q.institution ? `, ${q.institution}` : ""}
                    </td>
                    <td style={{ padding: "2px 0", textAlign: "right" }}>{q.year}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}

        {/* Career Highlights */}
        {highlights.length > 0 && (
          <>
            <SectionHeading>Career Highlights</SectionHeading>
            {highlights.map((h: any, i: number) => (
              <div key={i} style={{ marginTop: 10 }}>
                <div style={{ fontWeight: 700, marginBottom: 3 }}>{h.heading}</div>
                <ul style={{ margin: 0, paddingLeft: 20 }}>
                  <li style={{ lineHeight: 1.5 }}>{h.bullet}</li>
                </ul>
              </div>
            ))}
          </>
        )}

        {/* Footer */}
        <div style={{ marginTop: 40, borderTop: "1px solid #ccc", paddingTop: 6, display: "flex", justifyContent: "space-between", fontSize: 9, color: "#666" }}>
          <span>Papps Project Manager Pty Ltd | Capability Statement — Ref 17427</span>
          <span>Page 1</span>
        </div>

      </div>
    </div>
  );
}

function EditableDocumentView({ doc: initialDoc, onDocumentChange }: {
  doc: any;
  onDocumentChange: (updated: any) => void;
}) {
  const [doc, setDoc] = useState<any>(initialDoc);

  const update = (field: string, value: any) => {
    const updated = { ...doc, [field]: { ...doc[field], value } };
    setDoc(updated);
    onDocumentChange(updated);
  };

  const competencies = doc.coreCompetencies?.value ?? [];
  const highlights = doc.careerHighlights?.value ?? [];

  return (
    <div style={{ marginTop: 32, background: "white", borderRadius: 8, boxShadow: "0 2px 16px rgba(0,0,0,0.08)", padding: 32, color: "#111" }}>
      <h2 style={{ fontSize: 16, fontWeight: 700, color: "#1B2A4A", marginBottom: 24 }}>Edit Document</h2>

      {/* Professional Profile */}
      <div style={{ marginBottom: 20 }}>
        <label style={{ display: "block", fontWeight: 600, fontSize: 13, color: "#374151", marginBottom: 6 }}>
          Professional Profile
        </label>
        <textarea
          value={doc.professionalProfile?.value ?? ""}
          onChange={(e) => update("professionalProfile", e.target.value)}
          rows={5}
          style={{ width: "100%", border: "1px solid #d1d5db", borderRadius: 6, padding: "8px 12px", fontSize: 13, resize: "vertical", boxSizing: "border-box" }}
        />
      </div>

      {/* Role Suitability */}
      <div style={{ marginBottom: 20 }}>
        <label style={{ display: "block", fontWeight: 600, fontSize: 13, color: "#374151", marginBottom: 6 }}>
          Role Suitability <span style={{ fontWeight: 400, color: "#6b7280" }}>(max 200 words)</span>
        </label>
        <textarea
          value={doc.roleSuitability?.value ?? ""}
          onChange={(e) => update("roleSuitability", e.target.value)}
          rows={5}
          style={{ width: "100%", border: "1px solid #d1d5db", borderRadius: 6, padding: "8px 12px", fontSize: 13, resize: "vertical", boxSizing: "border-box" }}
        />
        <p style={{ fontSize: 12, color: "#6b7280", marginTop: 4 }}>
          Word count: {(doc.roleSuitability?.value ?? "").split(" ").filter(Boolean).length} / 200
        </p>
      </div>

      {/* Years of Experience */}
      <div style={{ marginBottom: 20 }}>
        <label style={{ display: "block", fontWeight: 600, fontSize: 13, color: "#374151", marginBottom: 6 }}>
          Years of Experience
        </label>
        <input
          type="text"
          value={doc.yearsOfExperience?.value ?? ""}
          onChange={(e) => update("yearsOfExperience", e.target.value)}
          style={{ width: "100%", border: "1px solid #d1d5db", borderRadius: 6, padding: "8px 12px", fontSize: 13, boxSizing: "border-box" }}
        />
      </div>

      {/* Core Competencies */}
      <div style={{ marginBottom: 20 }}>
        <label style={{ display: "block", fontWeight: 600, fontSize: 13, color: "#374151", marginBottom: 6 }}>
          Core Competencies <span style={{ fontWeight: 400, color: "#6b7280" }}>(exactly 10)</span>
        </label>
        {competencies.map((c: any, i: number) => (
          <div key={i} style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 8 }}>
            <span style={{ color: "#6b7280", fontSize: 12, minWidth: 20 }}>{i + 1}.</span>
            <input
              type="text"
              value={c.text}
              onChange={(e) => {
                const updated = [...competencies];
                updated[i] = { ...updated[i], text: e.target.value };
                update("coreCompetencies", updated);
              }}
              style={{ flex: 1, border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px", fontSize: 13 }}
            />
          </div>
        ))}
      </div>

      {/* Career Highlights */}
      <div style={{ marginBottom: 20 }}>
        <label style={{ display: "block", fontWeight: 600, fontSize: 13, color: "#374151", marginBottom: 6 }}>
          Career Highlights <span style={{ fontWeight: 400, color: "#6b7280" }}>(exactly 6)</span>
        </label>
        {highlights.map((h: any, i: number) => (
          <div key={i} style={{ marginBottom: 16, padding: 12, background: "#f9fafb", borderRadius: 6, border: "1px solid #e5e7eb" }}>
            <input
              type="text"
              value={h.heading}
              onChange={(e) => {
                const updated = [...highlights];
                updated[i] = { ...updated[i], heading: e.target.value };
                update("careerHighlights", updated);
              }}
              placeholder="Heading"
              style={{ width: "100%", border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px", fontSize: 13, fontWeight: 600, marginBottom: 6, boxSizing: "border-box" }}
            />
            <textarea
              value={h.bullet}
              onChange={(e) => {
                const updated = [...highlights];
                updated[i] = { ...updated[i], bullet: e.target.value };
                update("careerHighlights", updated);
              }}
              rows={3}
              style={{ width: "100%", border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px", fontSize: 13, resize: "vertical", boxSizing: "border-box" }}
            />
          </div>
        ))}
      </div>

      {/* Download button placeholder */}
      <button
        onClick={() => alert("Download not yet implemented")}
        style={{ width: "100%", background: "#1B2A4A", color: "white", fontWeight: 600, fontSize: 14, padding: "12px 0", borderRadius: 8, border: "none", cursor: "pointer" }}
      >
        Confirm & Download
      </button>
    </div>
  );
}
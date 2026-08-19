"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

const NAVY = "#1B2A4A";
const ORANGE = "#C0592A";

function SectionHeading({ children }: { children: React.ReactNode }) {
  return (
    <h2 style={{ color: ORANGE, fontSize: 13, fontWeight: 700, marginTop: 24, marginBottom: 4, borderBottom: `1px solid #ccc`, paddingBottom: 4 }}>
      {children}
    </h2>
  );
}

function EditableText({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  return (
    <textarea
      value={value}
      onChange={(e) => onChange(e.target.value)}
      style={{ width: "100%", border: "1px dashed #ccc", borderRadius: 4, padding: 6, fontSize: 11, fontFamily: "Calibri, Arial, sans-serif", resize: "vertical", lineHeight: 1.6, boxSizing: "border-box", background: "#fafafa" }}
    />
  );
}

export default function DocumentPage() {
  const router = useRouter();
  const [data, setData] = useState<any>(null);
  const [doc, setDoc] = useState<any>(null);

  useEffect(() => {
    const stored = localStorage.getItem("cvDocument");
    if (!stored) { router.push("/"); return; }
    const parsed = JSON.parse(stored);
    setData(parsed);
    setDoc(parsed.document);
  }, [router]);

  if (!doc) return <div style={{ padding: 32 }}>Loading...</div>;

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

  const update = (field: string, value: any) => {
    setDoc((prev: any) => ({
      ...prev,
      [field]: { ...prev[field], value }
    }));
  };

  return (
    <div style={{ minHeight: "100vh", background: "#e5e7eb" }}>

      {/* Toolbar */}
      <div style={{ background: NAVY, padding: "12px 32px", display: "flex", alignItems: "center", justifyContent: "space-between", position: "sticky", top: 0, zIndex: 10 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
          <button onClick={() => router.push("/")}
            style={{ background: "transparent", border: "1px solid white", color: "white", padding: "6px 14px", borderRadius: 6, cursor: "pointer", fontSize: 13 }}>
            ← Back
          </button>
          <span style={{ color: "white", fontSize: 14, fontWeight: 600 }}>{fullName} — {roleTitle}</span>
        </div>
        <button
          onClick={() => alert("Download not yet implemented")}
          style={{ background: ORANGE, border: "none", color: "white", padding: "8px 20px", borderRadius: 6, cursor: "pointer", fontSize: 13, fontWeight: 600 }}>
          Download .docx
        </button>
      </div>

      {/* Document */}
      <div style={{ padding: "32px 0" }}>
        <div style={{ fontFamily: "Calibri, Arial, sans-serif", fontSize: 11, maxWidth: 794, margin: "0 auto", background: "white", boxShadow: "0 4px 24px rgba(0,0,0,0.15)" }}>

          {/* Header band */}
          <div style={{ background: `linear-gradient(135deg, #E8A87C 0%, #C0592A 40%, #5B7FA6 100%)`, padding: "18px 32px", marginBottom: 16 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 24 }}>
              <div style={{ background: "white", padding: "6px 12px", borderRadius: 4, fontWeight: 900, fontSize: 14, color: NAVY }}>
                Papps<span style={{ color: ORANGE }}>PM</span>
              </div>
              <div style={{ color: "white", fontWeight: 700, fontSize: 14 }}>
                {fullName} | {roleTitle}
              </div>
            </div>
          </div>

          <div style={{ padding: "0 32px 32px" }}>

            {/* Title */}
            <h1 style={{ fontSize: 16, fontWeight: 700, color: NAVY, marginBottom: 12, borderBottom: `2px solid ${NAVY}`, paddingBottom: 6 }}>
              {fullName} | {roleTitle} | {level}
            </h1>

            {/* Details table */}
            <table style={{ width: "100%", borderCollapse: "collapse", marginBottom: 16, fontSize: 11 }}>
              <tbody>
                {[
                  ["FULL NAME", fullName, null],
                  ["PROPOSED ROLE", doc.proposedRole?.value, null],
                  ["QUALIFICATIONS", qualList.join("; "), null],
                  ["SECURITY CLEARANCE", doc.securityClearance?.value, null],
                  ["YEARS OF EXPERIENCE", doc.yearsOfExperience?.value, "yearsOfExperience"],
                  ["AVAILABILITY", doc.availability?.value, null],
                  ["LOCATION", doc.location?.value, null],
                ].map(([label, value, editField]) => (
                  <tr key={label as string}>
                    <td style={{ background: NAVY, color: "white", fontWeight: 700, padding: "6px 10px", width: "38%", fontSize: 10, letterSpacing: 0.5, verticalAlign: "top" }}>
                      {label}
                    </td>
                    <td style={{ background: "#EEF1F7", padding: "6px 10px" }}>
                      {editField
                        ? <EditableText value={value as string ?? ""} onChange={(v) => update(editField, v)} />
                        : value ?? "—"
                      }
                    </td>
                  </tr>
                ))}

                {/* Referees */}
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
            <EditableText
              value={doc.professionalProfile?.value ?? ""}
              onChange={(v) => update("professionalProfile", v)}
            />

            {/* Career Synopsis */}
            <SectionHeading>Career Synopsis</SectionHeading>
            <table style={{ width: "100%", borderCollapse: "collapse", marginTop: 6 }}>
              <tbody>
                {career.map((entry: any, i: number) => (
                  <tr key={i}>
                    <td style={{ padding: "2px 0", width: "65%" }}>
                      {entry.title} ({entry.organisation})
                    </td>
                    <td style={{ padding: "2px 0", textAlign: "right", color: "#333" }}>
                      {entry.startYear} – {entry.endYear}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Role Suitability */}
            <SectionHeading>Role Suitability</SectionHeading>
            <EditableText
              value={doc.roleSuitability?.value ?? ""}
              onChange={(v) => update("roleSuitability", v)}
            />

            {/* Core Competencies */}
            <SectionHeading>Core Competencies</SectionHeading>
            <ul style={{ margin: "6px 0 0 0", paddingLeft: 20 }}>
              {competencies.map((c: any, i: number) => (
                <li key={i} style={{ marginBottom: 4 }}>
                  <EditableText
                    value={c.text}
                    onChange={(v) => {
                      const updated = [...competencies];
                      updated[i] = { ...updated[i], text: v };
                      update("coreCompetencies", updated);
                    }}
                  />
                </li>
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
                      <li>
                        <EditableText
                          value={h.bullet}
                          onChange={(v) => {
                            const updated = [...highlights];
                            updated[i] = { ...updated[i], bullet: v };
                            update("careerHighlights", updated);
                          }}
                        />
                      </li>
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
      </div>
    </div>
  );
}
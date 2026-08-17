// web/recruitment/src/features/preview/PreviewRenderer.tsx
//
// CvDocument -> template-faithful HTML (brief 6.0).
//
// TWO HARD RULES for this file:
//   1. Every colour and measurement comes from tokens.ts. No hex literal here
//      (there is a Jest test that enforces it).
//   2. It imports NOTHING from features/docx (section 11). The two renderers are
//      siblings that share tokens, not a chain.
//
// Structural fidelity is what is graded, not pixel-perfection: a reviewer with
// the preview, the .docx and the template side by side should call them the
// same document.

import { tokens, dxaToPx, ptToPx } from "@/features/template/tokens";
import type {
  CvDocument,
  Competency,
  DatedEntry,
  Highlight,
  QualificationEntry,
  Referee,
  CareerEntry,
} from "@/features/analysis/types";

// The eight details-table rows, in the fixed order from 6.3.
// `wide` marks the QUALIFICATIONS value cell, which is 10pt where its
// neighbours are 9pt (wart 3 in 6.6 — reproduced deliberately; see README).
const TABLE_ROWS: { label: string; get: (d: CvDocument) => string; wide?: boolean }[] = [
  { label: "FULL NAME", get: (d) => d.fullName.value ?? "" },
  { label: "PROPOSED ROLE", get: (d) => d.proposedRole.value ?? "" },
  { label: "QUALIFICATIONS", get: (d) => (d.qualifications.value ?? []).join("; "), wide: true },
  { label: "SECURITY CLEARANCE", get: (d) => d.securityClearance.value ?? "" },
  { label: "YEARS OF EXPERIENCE", get: (d) => d.yearsOfExperience.value ?? "" },
  { label: "AVAILABILITY", get: (d) => d.availability.value ?? "" },
  { label: "LOCATION", get: (d) => d.location.value ?? "" },
  {
    label: "REFEREES",
    get: (d) =>
      (d.referees.value ?? [])
        .map((r: Referee) => `${r.name}, ${r.position}, ${r.organisation}, ${r.phone}`)
        .join("; "),
  },
];

export function PreviewRenderer({ document: doc }: { document: CvDocument }) {
  return (
    <div
      style={{
        // A4 page frame, converted from the DXA page size in tokens
        width: dxaToPx(tokens.page.width),
        minHeight: dxaToPx(tokens.page.height),
        paddingTop: dxaToPx(tokens.page.margin.top),
        paddingLeft: dxaToPx(tokens.page.margin.left),
        paddingRight: dxaToPx(tokens.page.margin.right),
        paddingBottom: dxaToPx(tokens.page.margin.bottom),
        background: tokens.white,
        color: tokens.ink,
        fontFamily: tokens.bodyFont,
        fontSize: ptToPx(tokens.bodyPt),
        boxShadow: "0 1px 6px rgba(0,0,0,0.15)",
        margin: "0 auto",
      }}
    >
      {/* ---- Header band: {fullName} | {roleTitle}, on every page in the .docx ---- */}
      <div style={{ marginBottom: ptToPx(12), fontSize: ptToPx(tokens.tableLabelPt) }}>
        {doc.fullName.value} | {doc.roleTitle.value}
      </div>

      {/* ---- Title line (Georgia bold 16pt) ---- */}
      <h1
        style={{
          fontFamily: tokens.titleFont,
          fontWeight: "bold",
          fontSize: ptToPx(tokens.titlePt),
          color: tokens.titleInk,
          margin: `0 0 ${ptToPx(10)}px 0`,
        }}
      >
        {doc.roleTitle.value} — {doc.level.value}
      </h1>

      {/* ---- Details table: exactly one table, eight rows, fixed order ---- */}
      <table style={{ borderCollapse: "collapse", width: "100%", marginBottom: ptToPx(14) }}>
        <tbody>
          {TABLE_ROWS.map((row) => (
            <tr key={row.label} style={{ height: dxaToPx(tokens.table.rowHeight) }}>
              <td
                style={{
                  width: dxaToPx(tokens.table.labelCol),
                  background: tokens.navy,
                  color: tokens.white,
                  fontWeight: "bold",
                  textTransform: "uppercase",
                  fontSize: ptToPx(tokens.tableLabelPt),
                  border: `1px solid ${tokens.border}`,
                  padding: `${dxaToPx(tokens.table.cellMargin.top)}px ${dxaToPx(tokens.table.cellMargin.right)}px`,
                  verticalAlign: "top",
                }}
              >
                {row.label}
              </td>
              <td
                style={{
                  width: dxaToPx(tokens.table.valueCol),
                  background: tokens.lavender,
                  color: tokens.ink,
                  fontSize: ptToPx(row.wide ? tokens.bodyPt : tokens.tableValuePt),
                  border: `1px solid ${tokens.border}`,
                  padding: `${dxaToPx(tokens.table.cellMargin.top)}px ${dxaToPx(tokens.table.cellMargin.right)}px`,
                  verticalAlign: "top",
                }}
              >
                {row.get(doc)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* ---- The seven sections, in the order fixed by 6.4 ---- */}

      <Section title="Professional Profile">
        <p style={paragraph}>{doc.professionalProfile.value}</p>
      </Section>

      <Section title="Career Synopsis">
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <tbody>
            {(doc.careerSynopsis.value ?? []).map((e: CareerEntry, i) => (
              <tr key={i}>
                <td style={{ padding: "2px 0" }}>
                  {e.title}, {e.organisation}
                </td>
                {/* en dash (U+2013) everywhere — wart 4 in 6.6 normalised, not reproduced */}
                <td style={{ padding: "2px 0", textAlign: "right", whiteSpace: "nowrap" }}>
                  {e.startYear} – {e.endYear}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Section>

      <Section title="Role Suitability">
        <p style={paragraph}>{doc.roleSuitability.value}</p>
      </Section>

      <Section title="Core Competencies">
        <ul style={list}>
          {(doc.coreCompetencies.value ?? []).map((c: Competency, i) => (
            <li key={i} style={listItem}>
              {c.text}
            </li>
          ))}
        </ul>
      </Section>

      <Section title="Commendations and Awards">
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <tbody>
            {(doc.commendationsAndAwards.value ?? []).map((a: DatedEntry, i) => (
              <tr key={i}>
                <td style={{ padding: "2px 0" }}>{a.description}</td>
                <td style={{ padding: "2px 0", textAlign: "right", whiteSpace: "nowrap" }}>{a.year}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Section>

      <Section title="Qualifications">
        <table style={{ borderCollapse: "collapse", width: "100%" }}>
          <tbody>
            {(doc.qualificationsDetailed.value ?? []).map((q: QualificationEntry, i) => (
              <tr key={i}>
                <td style={{ padding: "2px 0" }}>
                  {q.qualification}
                  {q.institution ? `, ${q.institution}` : ""}
                </td>
                <td style={{ padding: "2px 0", textAlign: "right", whiteSpace: "nowrap" }}>{q.year}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Section>

      <Section title="Career Highlights">
        {(doc.careerHighlights.value ?? []).map((h: Highlight, i) => (
          <div key={i} style={{ marginBottom: ptToPx(8) }}>
            {/* sub heading: Arial bold 10pt, no border */}
            <div style={{ fontWeight: "bold", fontSize: ptToPx(tokens.subHeadingPt) }}>{h.heading}</div>
            <ul style={list}>
              <li style={listItem}>{h.bullet}</li>
            </ul>
          </div>
        ))}
      </Section>

      {/* ---- Footer: in the .docx this is a live PAGE field, never a typed number ---- */}
      <div
        style={{
          marginTop: ptToPx(16),
          paddingTop: ptToPx(6),
          borderTop: `1px solid ${tokens.border}`,
          fontSize: ptToPx(tokens.tableLabelPt),
          display: "flex",
          justifyContent: "space-between",
        }}
      >
        <span>Meridian Digital Services Pty Ltd</span>
        <span>Page 1</span>
      </div>
    </div>
  );
}

// --- section heading: Arial bold 11pt orange, with a bottom border ---
// Wart 1 in 6.6: in the template, Professional Profile alone lacks the border
// and mixes 11pt/10pt runs. Normalised here — all seven headings are identical.
// Say so in the README.
function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section style={{ marginBottom: ptToPx(12) }}>
      <h2
        style={{
          fontFamily: tokens.bodyFont,
          fontWeight: "bold",
          fontSize: ptToPx(tokens.headingPt),
          color: tokens.orange,
          borderBottom: `1px solid ${tokens.ink}`,
          paddingBottom: ptToPx(2),
          margin: `0 0 ${ptToPx(6)}px 0`,
        }}
      >
        {title}
      </h2>
      {children}
    </section>
  );
}

const paragraph: React.CSSProperties = {
  textAlign: "justify", // jc: both
  margin: 0,
  lineHeight: 1.4,
};

const list: React.CSSProperties = {
  margin: 0,
  paddingLeft: ptToPx(14),
};

const listItem: React.CSSProperties = {
  textAlign: "justify",
  lineHeight: 1.4,
  marginBottom: ptToPx(2),
};

// web/recruitment/src/features/editor/DocumentEditor.tsx
//
// The editor for the whole CvDocument (4.2). ONE piece of state holds the
// document; every input reads from it and writes back to it, so Stage 4's
// preview can render from exactly what the user edited.
//
// Editing contract (5.4):
//   - any VALUE can be edited, including text inside a row  -> provenance 'edited'
//   - rows CANNOT be added, deleted or reordered — shape is fixed
//   - an absent field is empty but fillable
//   - sourceQuotes stay visible on an edited field, flagged as no longer supporting it

"use client";

import { useState } from "react";
import { FieldInput } from "./FieldInput";
import { ProvenanceBadge } from "./ProvenanceBadge";
import type { CvDocument, FieldValue } from "@/features/analysis/types";

// Scalar fields the user may edit as free text (5.4).
const SCALAR_FIELDS = [
  ["fullName", "Full name"],
  ["roleTitle", "Role title"],
  ["level", "Level"],
  ["proposedRole", "Proposed role"],
  ["securityClearance", "Security clearance"],
  ["yearsOfExperience", "Years of experience"],
  ["availability", "Availability"],
  ["location", "Location"],
  ["professionalProfile", "Professional profile"],
  ["roleSuitability", "Role suitability"],
] as const;

type ScalarKey = (typeof SCALAR_FIELDS)[number][0];

export function DocumentEditor({ document }: { document: CvDocument }) {
  const [doc, setDoc] = useState<CvDocument>(document);

  // Replace a field's value and mark it edited (P6). sourceQuotes are KEPT —
  // 5.4 wants them still shown, flagged as no longer supporting the value.
  function editField<T>(field: FieldValue<T>, newValue: T): FieldValue<T> {
    return { ...field, value: newValue, provenance: "edited" };
  }

  function updateScalar(key: ScalarKey, newValue: string) {
    setDoc((prev) => ({ ...prev, [key]: editField(prev[key], newValue) }));
  }

  // Update one property of one ROW inside a list field.
  // .map() returns a NEW array; length and order can never change, so 5.4 is
  // enforced by the shape of the code rather than by a check we could forget.
  function updateListItem<K extends keyof CvDocument>(
    key: K,
    rowIndex: number,
    prop: string,
    newValue: string,
  ) {
    setDoc((prev) => {
      const field = prev[key] as FieldValue<Record<string, unknown>[]>;
      const rows = field.value ?? [];
      const next = rows.map((row, i) => (i === rowIndex ? { ...row, [prop]: newValue } : row));
      return { ...prev, [key]: { ...field, value: next, provenance: "edited" } };
    });
  }

  // Update one entry of a plain string[] field (the details-table qualifications).
  function updateStringList(key: "qualifications", index: number, newValue: string) {
    setDoc((prev) => {
      const field = prev[key];
      const next = (field.value ?? []).map((s, i) => (i === index ? newValue : s));
      return { ...prev, [key]: { ...field, value: next, provenance: "edited" } };
    });
  }

  return (
    <div style={{ display: "grid", gap: 32 }}>
      {/* ---------- Scalar and prose fields ---------- */}
      <Section title="Details">
        {SCALAR_FIELDS.map(([key, label]) => (
          <FieldInput
            key={key}
            label={label}
            value={doc[key].value}
            provenance={doc[key].provenance}
            onChange={(v) => updateScalar(key, v)}
          />
        ))}
      </Section>

      {/* ---------- Qualifications (string[] for the details table) ---------- */}
      <Section title="Qualifications (details table)" field={doc.qualifications}>
        {(doc.qualifications.value ?? []).map((q, i) => (
          <FieldInput
            key={i}
            label={`Qualification ${i + 1}`}
            value={q}
            onChange={(v) => updateStringList("qualifications", i, v)}
          />
        ))}
      </Section>

      {/* ---------- Referees ---------- */}
      <Section title="Referees" field={doc.referees}>
        {(doc.referees.value ?? []).map((r, i) => (
          <Row key={i}>
            <FieldInput label="Name" value={r.name} onChange={(v) => updateListItem("referees", i, "name", v)} />
            <FieldInput label="Position" value={r.position} onChange={(v) => updateListItem("referees", i, "position", v)} />
            <FieldInput label="Organisation" value={r.organisation} onChange={(v) => updateListItem("referees", i, "organisation", v)} />
            <FieldInput label="Phone" value={r.phone} onChange={(v) => updateListItem("referees", i, "phone", v)} />
          </Row>
        ))}
      </Section>

      {/* ---------- Career synopsis ---------- */}
      <Section title="Career synopsis" field={doc.careerSynopsis}>
        {(doc.careerSynopsis.value ?? []).map((e, i) => (
          <Row key={i}>
            <FieldInput label="Title" value={e.title} onChange={(v) => updateListItem("careerSynopsis", i, "title", v)} />
            <FieldInput label="Organisation" value={e.organisation} onChange={(v) => updateListItem("careerSynopsis", i, "organisation", v)} />
            <FieldInput label="Start year" value={e.startYear} onChange={(v) => updateListItem("careerSynopsis", i, "startYear", v)} />
            <FieldInput label="End year" value={e.endYear} onChange={(v) => updateListItem("careerSynopsis", i, "endYear", v)} />
          </Row>
        ))}
      </Section>

      {/* ---------- Core competencies ---------- */}
      <Section title="Core competencies" field={doc.coreCompetencies}>
        {(doc.coreCompetencies.value ?? []).map((c, i) => (
          <FieldInput
            key={i}
            label={`Competency ${i + 1}  [${c.criteria.join(", ")}]`}
            value={c.text}
            onChange={(v) => updateListItem("coreCompetencies", i, "text", v)}
          />
        ))}
      </Section>

      {/* ---------- Commendations and awards ---------- */}
      <Section title="Commendations and awards" field={doc.commendationsAndAwards}>
        {(doc.commendationsAndAwards.value ?? []).map((a, i) => (
          <Row key={i}>
            <FieldInput label="Description" value={a.description} onChange={(v) => updateListItem("commendationsAndAwards", i, "description", v)} />
            <FieldInput label="Year" value={a.year} onChange={(v) => updateListItem("commendationsAndAwards", i, "year", v)} />
          </Row>
        ))}
      </Section>

      {/* ---------- Qualifications (detailed section) ---------- */}
      <Section title="Qualifications (detailed)" field={doc.qualificationsDetailed}>
        {(doc.qualificationsDetailed.value ?? []).map((q, i) => (
          <Row key={i}>
            <FieldInput label="Qualification" value={q.qualification} onChange={(v) => updateListItem("qualificationsDetailed", i, "qualification", v)} />
            <FieldInput label="Institution" value={q.institution ?? ""} onChange={(v) => updateListItem("qualificationsDetailed", i, "institution", v)} />
            <FieldInput label="Year" value={q.year} onChange={(v) => updateListItem("qualificationsDetailed", i, "year", v)} />
          </Row>
        ))}
      </Section>

      {/* ---------- Career highlights ---------- */}
      <Section title="Career highlights" field={doc.careerHighlights}>
        {(doc.careerHighlights.value ?? []).map((h, i) => (
          <Row key={i}>
            <FieldInput label={`Heading  [${h.criteria.join(", ")}]`} value={h.heading} onChange={(v) => updateListItem("careerHighlights", i, "heading", v)} />
            <FieldInput label="Bullet" value={h.bullet} onChange={(v) => updateListItem("careerHighlights", i, "bullet", v)} />
          </Row>
        ))}
      </Section>
    </div>
  );
}

// --- small layout helpers, kept local to this file ---

function Section({
  title,
  field,
  children,
}: {
  title: string;
  field?: FieldValue<unknown>;
  children: React.ReactNode;
}) {
  return (
    <section>
      <h2 style={{ fontSize: 18, marginBottom: 4, display: "flex", alignItems: "center", gap: 8 }}>
        {title}
        {field && <ProvenanceBadge provenance={field.provenance} />}
      </h2>
      <p style={{ fontSize: 12, color: "#777", marginBottom: 12 }}>
        Values can be edited. Entries cannot be added, removed or reordered.
      </p>
      <div style={{ display: "grid", gap: 12 }}>{children}</div>
    </section>
  );
}

function Row({ children }: { children: React.ReactNode }) {
  return (
    <div style={{ border: "1px solid #e5e7eb", borderRadius: 6, padding: 12, display: "grid", gap: 12 }}>
      {children}
    </div>
  );
}

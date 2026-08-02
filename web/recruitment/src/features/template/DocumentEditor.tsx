// web/recruitment/src/features/editor/DocumentEditor.tsx
//
// The editor for the whole CV document: personal details, work history and
// education. Replaces DetailsEditor (which only handled the flat fields —
// delete that file once this is in).
//
// ONE piece of state holds the entire document. Every input reads from it and
// writes back to it. That matters for Stage 4: the preview will render from
// this same object, so what the user sees on screen is always what they edited.
//
// Editing contract (brief 5.4):
//   - any VALUE can be edited, including text inside a row
//   - rows CANNOT be added, deleted or reordered — the shape is fixed
//   - an absent field is empty but fillable; typing makes it "edited"

"use client";

import { useState } from "react";
import { FieldInput } from "./FieldInput";
import type { CvDocument, PersonalDetails, ProvenanceValue } from "@/features/analysis/types";

// Helper: return a copy of a field with a new value, marked as edited.
function edit(field: ProvenanceValue<string>, newValue: string): ProvenanceValue<string> {
  return { ...field, value: newValue, provenance: "edited" };
}

export function DocumentEditor({ document }: { document: CvDocument }) {
  const [doc, setDoc] = useState<CvDocument>(document);

  // --- Personal details: replace one field on one object ---
  function updateDetail(key: keyof PersonalDetails, newValue: string) {
    setDoc((prev) => ({
      ...prev,
      personalDetails: {
        ...prev.personalDetails,
        [key]: edit(prev.personalDetails[key], newValue),
      },
    }));
  }

  // --- Work history: replace one field on ONE ROW of an array ---
  // .map() returns a NEW array. Rows that are not the one being edited are
  // passed through untouched; the matching row is replaced with a new object.
  // The array's LENGTH and ORDER never change — that is 5.4 enforced by design.
  function updateWorkField(
    rowIndex: number,
    key: "jobTitle" | "company" | "dates",
    newValue: string,
  ) {
    setDoc((prev) => ({
      ...prev,
      workHistory: prev.workHistory.map((row, i) =>
        i === rowIndex ? { ...row, [key]: edit(row[key], newValue) } : row,
      ),
    }));
  }

  // --- Bullet points: a nested array inside a row, so .map() twice ---
  function updateBullet(rowIndex: number, bulletIndex: number, newValue: string) {
    setDoc((prev) => ({
      ...prev,
      workHistory: prev.workHistory.map((row, i) =>
        i === rowIndex
          ? {
              ...row,
              bulletPoints: row.bulletPoints.map((b, j) => (j === bulletIndex ? newValue : b)),
            }
          : row,
      ),
    }));
  }

  // --- Education: same pattern as work history ---
  function updateEducation(rowIndex: number, key: "degree" | "institution", newValue: string) {
    setDoc((prev) => ({
      ...prev,
      education: prev.education.map((row, i) =>
        i === rowIndex ? { ...row, [key]: edit(row[key], newValue) } : row,
      ),
    }));
  }

  const details = doc.personalDetails;

  return (
    <div style={{ display: "grid", gap: 32 }}>
      {/* ---------- Personal details ---------- */}
      <section>
        <h2 style={{ fontSize: 18, marginBottom: 12 }}>Personal details</h2>
        <div style={{ display: "grid", gap: 12 }}>
          <FieldInput
            label="Full name"
            value={details.fullName.value}
            provenance={details.fullName.provenance}
            onChange={(v) => updateDetail("fullName", v)}
          />
          <FieldInput
            label="Location"
            value={details.location.value}
            provenance={details.location.provenance}
            onChange={(v) => updateDetail("location", v)}
          />
          <FieldInput
            label="Security clearance"
            value={details.securityClearance.value}
            provenance={details.securityClearance.provenance}
            onChange={(v) => updateDetail("securityClearance", v)}
          />
          <FieldInput
            label="Referees"
            value={details.referees.value}
            provenance={details.referees.provenance}
            onChange={(v) => updateDetail("referees", v)}
          />
        </div>
      </section>

      {/* ---------- Work history ---------- */}
      <section>
        <h2 style={{ fontSize: 18, marginBottom: 4 }}>Work history</h2>
        <p style={{ fontSize: 12, color: "#777", marginBottom: 12 }}>
          Values can be edited. Roles and bullet points cannot be added, removed or reordered.
        </p>

        <div style={{ display: "grid", gap: 20 }}>
          {doc.workHistory.map((row, i) => (
            // `key` tells React which row is which. Index is acceptable ONLY
            // because rows can never be added, removed or reordered here.
            <div key={i} style={{ border: "1px solid #e5e7eb", borderRadius: 6, padding: 12 }}>
              <div style={{ display: "grid", gap: 12 }}>
                <FieldInput
                  label="Job title"
                  value={row.jobTitle.value}
                  provenance={row.jobTitle.provenance}
                  onChange={(v) => updateWorkField(i, "jobTitle", v)}
                />
                <FieldInput
                  label="Company"
                  value={row.company.value}
                  provenance={row.company.provenance}
                  onChange={(v) => updateWorkField(i, "company", v)}
                />
                <FieldInput
                  label="Dates"
                  value={row.dates.value}
                  provenance={row.dates.provenance}
                  onChange={(v) => updateWorkField(i, "dates", v)}
                />

                {row.bulletPoints.map((bullet, j) => (
                  <FieldInput
                    key={j}
                    label={`Bullet ${j + 1}`}
                    value={bullet}
                    // No provenance: the backend DTO stores bullets as plain
                    // strings. Worth raising with your partner — the brief
                    // wants provenance on generated prose.
                    onChange={(v) => updateBullet(i, j, v)}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ---------- Education ---------- */}
      <section>
        <h2 style={{ fontSize: 18, marginBottom: 12 }}>Education</h2>
        <div style={{ display: "grid", gap: 20 }}>
          {doc.education.map((row, i) => (
            <div key={i} style={{ border: "1px solid #e5e7eb", borderRadius: 6, padding: 12 }}>
              <div style={{ display: "grid", gap: 12 }}>
                <FieldInput
                  label="Degree"
                  value={row.degree.value}
                  provenance={row.degree.provenance}
                  onChange={(v) => updateEducation(i, "degree", v)}
                />
                <FieldInput
                  label="Institution"
                  value={row.institution.value}
                  provenance={row.institution.provenance}
                  onChange={(v) => updateEducation(i, "institution", v)}
                />
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

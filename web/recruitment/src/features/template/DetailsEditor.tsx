// web/recruitment/src/features/editor/DetailsEditor.tsx
//
// The editable form for the personal-details fields.
//
// "use client" marks this as a CLIENT component: it runs in the browser, so it
// can hold state (useState) and respond to typing (onChange). Its parent
// (app/page.tsx) stays a SERVER component that fetches the data and passes it
// down. Fetch high, edit low.

"use client";

import { useState } from "react";
import { ProvenanceBadge } from "./ProvenanceBadge";
import type { PersonalDetails, ProvenanceValue } from "@/features/analysis/types";

// Which fields we show, and their labels. Driven off a list so we render the
// same row markup for each instead of copy-pasting it per field.
const FIELDS: { key: keyof PersonalDetails; label: string }[] = [
  { key: "fullName", label: "Full name" },
  { key: "location", label: "Location" },
  { key: "securityClearance", label: "Security clearance" },
  { key: "referees", label: "Referees" },
];

export function DetailsEditor({ details }: { details: PersonalDetails }) {
  // The prop is only the STARTING value. From here the state is the source of
  // truth for what is on screen, and typing updates it.
  const [fields, setFields] = useState<PersonalDetails>(details);

  // Runs on every keystroke in any field.
  function handleChange(key: keyof PersonalDetails, newValue: string) {
    setFields((previous) => ({
      // Build a NEW object. React re-renders when it sees a different object,
      // not by inspecting what changed inside — mutating `fields` directly
      // would update nothing on screen, silently.
      ...previous, // copy the other fields through untouched
      [key]: {
        ...previous[key], // keep ruleId and anything else on that field
        value: newValue, // what the user just typed
        provenance: "edited", // any user edit becomes "edited" (5.4)
      } as ProvenanceValue<string>,
    }));
  }

  return (
    <section style={{ marginBottom: 24 }}>
      <h2 style={{ fontSize: 18, marginBottom: 12 }}>Personal details</h2>

      <div style={{ display: "grid", gap: 12 }}>
        {FIELDS.map(({ key, label }) => {
          const field = fields[key];
          return (
            <label key={key} style={{ display: "grid", gap: 4 }}>
              <span style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, color: "#555" }}>
                {label}
                <ProvenanceBadge provenance={field.provenance} />
              </span>

              <input
                // `?? ""` because an absent field's value is null, and an input
                // must never be given null — React would treat it as
                // uncontrolled and warn.
                value={field.value ?? ""}
                onChange={(e) => handleChange(key, e.target.value)}
                // An absent field is empty but still fillable — that is the
                // point of Inv-2, not a hole in it (5.4).
                placeholder={field.provenance === "absent" ? "(empty — you can fill this in)" : undefined}
                style={{
                  padding: "6px 10px",
                  fontSize: 14,
                  fontFamily: "inherit",
                  border: "1px solid #ccc",
                  borderRadius: 4,
                }}
              />
            </label>
          );
        })}
      </div>
    </section>
  );
}

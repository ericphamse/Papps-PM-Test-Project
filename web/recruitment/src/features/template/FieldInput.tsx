// web/recruitment/src/features/editor/FieldInput.tsx
//
// One labelled input with its provenance badge.
//
// This component holds NO state of its own. It receives the value and an
// onChange handler from its parent, and calls back up when the user types.
// That is deliberate: the whole document lives in ONE piece of state in
// DocumentEditor, so the preview can later render from exactly the same data
// the user is editing. A component like this is called "controlled".

"use client";

import { ProvenanceBadge } from "./ProvenanceBadge";
import type { Provenance } from "@/features/analysis/types";

export function FieldInput({
  label,
  value,
  provenance,
  onChange,
}: {
  label: string;
  value: string | null;
  provenance?: Provenance; // optional: bullet points carry no provenance yet
  onChange: (newValue: string) => void;
}) {
  return (
    <label style={{ display: "grid", gap: 4 }}>
      <span style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12, color: "#555" }}>
        {label}
        {provenance && <ProvenanceBadge provenance={provenance} />}
      </span>

      <input
        // `?? ""` because an absent field's value is null, and React must never
        // be handed null for an input's value.
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value)}
        placeholder={provenance === "absent" ? "(empty — you can fill this in)" : undefined}
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
}

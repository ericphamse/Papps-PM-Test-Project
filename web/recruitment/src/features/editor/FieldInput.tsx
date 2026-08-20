// web/recruitment/src/features/editor/FieldInput.tsx
//
// One labelled input with its provenance badge and its source quotes.
//
// Holds NO state of its own: it receives the value and an onChange handler and
// calls back up. The whole document lives in ONE piece of state in
// DocumentEditor, so the preview renders from exactly what the user edited.

"use client";

import { ProvenanceBadge } from "./ProvenanceBadge";
import type { Provenance } from "@/features/analysis/types";

export function FieldInput({
  label,
  value,
  provenance,
  sourceQuotes,
  ruleIds,
  onChange,
}: {
  label: string;
  value: string | null;
  provenance?: Provenance;
  sourceQuotes?: string[];
  ruleIds?: string[];
  onChange: (newValue: string) => void;
}) {
  // 5.4: on an edited field the quotes are RETAINED and still shown, but
  // flagged as no longer supporting the current value.
  const stale = provenance === "edited";

  return (
    <label style={{ display: "grid", gap: 4 }}>
      <span style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12, color: "#555" }}>
        {label}
        {provenance && <ProvenanceBadge provenance={provenance} />}
        {ruleIds && ruleIds.length > 0 && (
          <span style={{ fontSize: 11, color: "#777" }}>rules: {ruleIds.join(", ")}</span>
        )}
      </span>

      <input
        // `?? ""` because an absent field's value is null, and React must never
        // be handed null for an input's value.
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value)}
        // An absent field is empty but still fillable — that is the point of
        // Inv-2, not a hole in it (5.4).
        placeholder={provenance === "absent" ? "(empty — you can fill this in)" : undefined}
        style={{
          padding: "6px 10px",
          fontSize: 14,
          fontFamily: "inherit",
          border: "1px solid #ccc",
          borderRadius: 4,
        }}
      />

      {sourceQuotes && sourceQuotes.length > 0 && (
        <div style={{ fontSize: 11, color: stale ? "#8D6E00" : "#777", paddingLeft: 2 }}>
          {stale && <strong>No longer supported by these quotes: </strong>}
          {!stale && <span>Source: </span>}
          {sourceQuotes.map((q, i) => (
            <span key={i} style={{ fontStyle: "italic" }}>
              {i > 0 && " · "}
              &ldquo;{q}&rdquo;
            </span>
          ))}
        </div>
      )}
    </label>
  );
}

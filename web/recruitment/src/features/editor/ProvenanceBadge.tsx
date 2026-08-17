// web/recruitment/src/features/editor/ProvenanceBadge.tsx
//
// A small coloured label showing where a field's value came from.
// Plain React + inline styles, so it does not depend on any Tailwind config.
//
// These colours are EDITOR CHROME — they style the editing UI, which is not
// graded on appearance (section 14). They are deliberately different from the
// CV template's colours (those live in tokens.ts and are the document's).

import type { Provenance } from "@/features/analysis/types";

const STYLES: Record<Provenance, { text: string; bg: string; fg: string }> = {
  verbatim: { text: "Verbatim", bg: "#EDE7F6", fg: "#4527A0" }, // purple — copied from the CV
  normalised: { text: "Normalised", bg: "#E8F5E9", fg: "#1B5E20" }, // green — rewritten by a rule
  derived: { text: "From RFQ", bg: "#E0F7FA", fg: "#006064" }, // cyan — from the job requirements
  generated: { text: "AI", bg: "#E3F2FD", fg: "#0D47A1" }, // blue — model-authored
  edited: { text: "Edited", bg: "#FFF8E1", fg: "#8D6E00" }, // amber — the user changed it
  absent: { text: "Absent", bg: "#F5F5F5", fg: "#616161" }, // grey — empty, never invented
};

export function ProvenanceBadge({ provenance }: { provenance: Provenance }) {
  const s = STYLES[provenance];
  return (
    <span
      title={`Provenance: ${provenance}`}
      style={{
        display: "inline-block",
        padding: "1px 8px",
        borderRadius: 9999,
        fontSize: 11,
        fontWeight: 600,
        lineHeight: 1.6,
        background: s.bg,
        color: s.fg,
      }}
    >
      {s.text}
    </span>
  );
}

// web/recruitment/src/features/template/tokens.ts
//
// THE single source of truth for the CV template's colours and measurements.
// Every value here comes straight from section 6 of the brief.
//
// This file is imported by BOTH renderers (the on-screen preview and the .docx
// builder) and owned by NEITHER — that is why it lives in features/template.
// If a colour or size is needed anywhere, it is read from here, never re-typed.
// (A Jest test checks that no hex literal appears under features/docx,
// features/preview or features/template outside this file.)

export const tokens = {
  // --- Colours (section 6.2, 6.3) ---
  navy: "#1E1560", // table label-cell fill; section headings
  lavender: "#EEF0FF", // table value-cell fill
  orange: "#E8600A", // section headings
  ink: "#0F0E1A", // body text
  titleInk: "#0D0B2B", // title line
  white: "#FFFFFF", // table label-cell text
  border: "#E5E7EB", // table cell borders

  // --- Fonts (section 6.2) ---
  bodyFont: "Arial",
  titleFont: "Georgia",

  // --- Type sizes in POINTS (section 6.2) ---
  // docx wants half-points (pt * 2); the preview wants pt or px.
  bodyPt: 10, //        body text            (docx sz 20)
  headingPt: 11, //     section heading      (docx sz 22)
  subHeadingPt: 10, //  career-highlight sub (docx sz 20)
  titlePt: 16, //       title line           (docx sz 32)
  tableLabelPt: 9, //   table label cell     (docx sz 18)
  tableValuePt: 9, //   table value cell (the QUALIFICATIONS row is 10 — see 6.6)

  // --- Layout in DXA (1440 DXA = 1 inch) (section 6.1, 6.3) ---
  page: {
    width: 11906, //  A4 pgSz width
    height: 16838, // A4 pgSz height
    margin: { top: 1710, left: 1440, right: 1106, bottom: 0 }, // pgMar
    header: 706,
    footer: 706,
  },
  table: {
    labelCol: 3120, // label column width (DXA)
    valueCol: 6240, // value column width (DXA)
    cellMargin: { top: 100, bottom: 100, left: 160, right: 160 },
    rowHeight: 300, // trHeight minimum
    borderSize: 4, //  0.5pt, single
  },
} as const;

// --- Unit converters ---
export const dxaToPx = (dxa: number) => (dxa / 1440) * 96; // for the HTML preview
export const ptToHalfPt = (pt: number) => pt * 2; //          for the .docx renderer
export const ptToPx = (pt: number) => (pt / 72) * 96; //      for the HTML preview

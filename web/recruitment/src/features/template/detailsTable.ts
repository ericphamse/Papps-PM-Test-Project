// web/recruitment/src/features/docx/shared/detailsTable.ts
//
// The eight-row details table (6.3). Exactly one table in the document.
// GOTCHA 1 (6.6a): ShadingType.CLEAR, never SOLID — SOLID renders black.

import {
  Table,
  TableRow,
  TableCell,
  Paragraph,
  TextRun,
  WidthType,
  ShadingType,
  BorderStyle,
} from "docx";
import { tokens, ptToHalfPt } from "@/features/template/tokens";
import type { CvDocument, Referee } from "@/features/analysis/types";

const noHash = (hex: string) => hex.replace("#", ""); // docx wants RRGGBB without '#'

const CELL_BORDER = {
  style: BorderStyle.SINGLE,
  size: tokens.table.borderSize, // sz 4 = 0.5pt
  color: noHash(tokens.border),
};
const BORDERS = { top: CELL_BORDER, bottom: CELL_BORDER, left: CELL_BORDER, right: CELL_BORDER };
const MARGINS = tokens.table.cellMargin;

function labelCell(text: string): TableCell {
  return new TableCell({
    width: { size: tokens.table.labelCol, type: WidthType.DXA },
    shading: { type: ShadingType.CLEAR, fill: noHash(tokens.navy) }, // CLEAR, not SOLID
    borders: BORDERS,
    margins: MARGINS,
    children: [
      new Paragraph({
        children: [
          new TextRun({
            text, // labels are typed in caps; template also uses bold white 9pt
            bold: true,
            color: noHash(tokens.white),
            font: tokens.bodyFont,
            size: ptToHalfPt(tokens.tableLabelPt),
          }),
        ],
      }),
    ],
  });
}

function valueCell(text: string, wide = false): TableCell {
  return new TableCell({
    width: { size: tokens.table.valueCol, type: WidthType.DXA },
    shading: { type: ShadingType.CLEAR, fill: noHash(tokens.lavender) },
    borders: BORDERS,
    margins: MARGINS,
    children: [
      new Paragraph({
        children: [
          new TextRun({
            text,
            color: noHash(tokens.ink),
            font: tokens.bodyFont,
            // wart 3 (6.6): QUALIFICATIONS value cell is 10pt, neighbours 9pt.
            // Reproduced deliberately — recorded in the README.
            size: ptToHalfPt(wide ? tokens.bodyPt : tokens.tableValuePt),
          }),
        ],
      }),
    ],
  });
}

export function buildDetailsTable(doc: CvDocument): Table {
  const referees = (doc.referees.value ?? [])
    .map((r: Referee) => `${r.name}, ${r.position}, ${r.organisation}, ${r.phone}`)
    .join("; ");

  const rows: [string, string, boolean?][] = [
    ["FULL NAME", doc.fullName.value ?? ""],
    ["PROPOSED ROLE", doc.proposedRole.value ?? ""],
    ["QUALIFICATIONS", (doc.qualifications.value ?? []).join("; "), true],
    ["SECURITY CLEARANCE", doc.securityClearance.value ?? ""],
    ["YEARS OF EXPERIENCE", doc.yearsOfExperience.value ?? ""],
    ["AVAILABILITY", doc.availability.value ?? ""],
    ["LOCATION", doc.location.value ?? ""],
    ["REFEREES", referees],
  ];

  return new Table({
    width: { size: tokens.table.labelCol + tokens.table.valueCol, type: WidthType.DXA },
    rows: rows.map(
      ([label, value, wide]) =>
        new TableRow({
          height: { value: tokens.table.rowHeight, rule: "atLeast" },
          children: [labelCell(label), valueCell(value, wide)],
        }),
    ),
  });
}

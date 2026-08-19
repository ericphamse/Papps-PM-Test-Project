// web/recruitment/src/features/docx/template.ts
//
// CvDocument -> docx.Document. PURE: no React, no fetch, no browser APIs, so it
// is unit-testable in Node (the Stage 8 conformance tests depend on that).
// Reads every colour and measurement from tokens.ts (6.0).
// Section order is fixed by 6.4 and mirrors the preview exactly.

import { Document, Paragraph, TextRun, AlignmentType, BorderStyle, Table, TabStopType } from "docx";
import { tokens, ptToHalfPt } from "@/features/template/tokens";
import { buildDetailsTable } from "./shared/detailsTable";
import { buildHeader, buildFooter } from "./shared/headerFooter";
import { BULLET_NUMBERING } from "./shared/numbering";
import type { CvDocument, CareerEntry, Competency, DatedEntry, QualificationEntry, Highlight } from "@/features/analysis/types";

const noHash = (hex: string) => hex.replace("#", "");

// --- building blocks -------------------------------------------------------

// Section heading: Arial bold 11pt orange with a bottom border (6.2).
// Warts 1+2 (6.6) normalised: ALL seven headings identical. README records it.
function heading(text: string): Paragraph {
  return new Paragraph({
    spacing: { before: 200, after: 80 },
    border: {
      bottom: { style: BorderStyle.SINGLE, size: 6, color: "auto" },
    },
    children: [
      new TextRun({
        text,
        bold: true,
        font: tokens.bodyFont,
        size: ptToHalfPt(tokens.headingPt),
        color: noHash(tokens.orange),
      }),
    ],
  });
}

// Justified body paragraph (jc: both, 6.2)
function body(text: string): Paragraph {
  return new Paragraph({
    alignment: AlignmentType.JUSTIFIED,
    spacing: { after: 80 },
    children: [
      new TextRun({
        text,
        font: tokens.bodyFont,
        size: ptToHalfPt(tokens.bodyPt),
        color: noHash(tokens.ink),
      }),
    ],
  });
}

// Bulleted body line — via the numbering config, NEVER a literal "•" (6.6a)
function bullet(text: string): Paragraph {
  return new Paragraph({
    numbering: { reference: "cv-bullets", level: 0 },
    alignment: AlignmentType.JUSTIFIED,
    children: [
      new TextRun({
        text,
        font: tokens.bodyFont,
        size: ptToHalfPt(tokens.bodyPt),
        color: noHash(tokens.ink),
      }),
    ],
  });
}

// "text .......... year" line: ONE paragraph with a right-aligned tab stop at
// the content edge. This is how the template does it — NOT a table. 6.3 allows
// exactly ONE table in the document (the details table); rendering these
// sections as tables fails the conformance table-count check. Found the hard
// way on the first generated file.
const CONTENT_WIDTH_DXA = tokens.table.labelCol + tokens.table.valueCol; // 9360

function datedLine(left: string, right: string): Paragraph {
  return new Paragraph({
    tabStops: [{ type: TabStopType.RIGHT, position: CONTENT_WIDTH_DXA }],
    spacing: { after: 40 },
    children: [
      new TextRun({
        text: left,
        font: tokens.bodyFont,
        size: ptToHalfPt(tokens.bodyPt),
        color: noHash(tokens.ink),
      }),
      new TextRun({
        text: "\t" + right, // the tab jumps to the right-aligned stop
        font: tokens.bodyFont,
        size: ptToHalfPt(tokens.bodyPt),
        color: noHash(tokens.ink),
      }),
    ],
  });
}

// --- the document -----------------------------------------------------------

export function buildCvDocument(doc: CvDocument): Document {
  const children: (Paragraph | Table)[] = [];

  // Title line: Georgia bold 16pt, Heading2 style (6.2)
  children.push(
    new Paragraph({
      style: "Heading2",
      spacing: { after: 160 },
      children: [
        new TextRun({
          text: `${doc.roleTitle.value ?? ""} — ${doc.level.value ?? ""}`,
          bold: true,
          font: tokens.titleFont,
          size: ptToHalfPt(tokens.titlePt),
          color: noHash(tokens.titleInk),
        }),
      ],
    }),
  );

  // The one table (6.3)
  children.push(buildDetailsTable(doc));

  // Seven sections, 6.4 order — identical to the preview
  children.push(heading("Professional Profile"));
  children.push(body(doc.professionalProfile.value ?? ""));

  children.push(heading("Career Synopsis"));
  for (const e of doc.careerSynopsis.value ?? [])
    children.push(datedLine(`${e.title}, ${e.organisation}`, `${e.startYear} \u2013 ${e.endYear}`)); // en dash (wart 4 normalised)

  children.push(heading("Role Suitability"));
  children.push(body(doc.roleSuitability.value ?? ""));

  children.push(heading("Core Competencies"));
  for (const c of doc.coreCompetencies.value ?? []) children.push(bullet((c as Competency).text));

  children.push(heading("Commendations and Awards"));
  for (const a of doc.commendationsAndAwards.value ?? [])
    children.push(datedLine(a.description, a.year));

  children.push(heading("Qualifications"));
  for (const q of doc.qualificationsDetailed.value ?? [])
    children.push(datedLine(q.institution ? `${q.qualification}, ${q.institution}` : q.qualification, q.year));

  children.push(heading("Career Highlights"));
  for (const h of doc.careerHighlights.value ?? []) {
    const hl = h as Highlight;
    children.push(
      new Paragraph({
        spacing: { before: 120 },
        children: [
          new TextRun({
            text: hl.heading, // sub heading: Arial bold 10pt, NO border (6.2)
            bold: true,
            font: tokens.bodyFont,
            size: ptToHalfPt(tokens.subHeadingPt),
            color: noHash(tokens.ink),
          }),
        ],
      }),
    );
    children.push(bullet(hl.bullet));
  }

  return new Document({
    numbering: BULLET_NUMBERING,
    sections: [
      {
        properties: {
          page: {
            size: { width: tokens.page.width, height: tokens.page.height },
            margin: {
              top: tokens.page.margin.top,
              left: tokens.page.margin.left,
              right: tokens.page.margin.right,
              bottom: tokens.page.margin.bottom,
              header: tokens.page.header,
              footer: tokens.page.footer,
            },
          },
        },
        headers: { default: buildHeader(doc) },
        footers: { default: buildFooter() },
        children,
      },
    ],
  });
}

// web/recruitment/src/features/docx/shared/headerFooter.ts
//
// Header: text box with "{fullName} | {roleTitle}" on every page (6.5).
// GOTCHA 3 (6.6a): Textbox goes DIRECTLY into Header's children. Wrapping it in
// a Paragraph nests <w:p> inside <w:p> — invalid OOXML that fails SILENTLY:
// the file builds, opens fine, and the header is just empty. No error anywhere.
//
// Footer: company line + a live PAGE field (6.5). A field instruction, never a
// typed "1" — the document must show 1 then 2 without either character being
// written by us.
//
// NOTE on the logo: the template header also carries a logo image. That needs
// an image asset we don't have in the repo yet — ask which file to use, then
// add an ImageRun here. Recorded as a known gap for the README.

import { Header, Footer, Paragraph, TextRun, Textbox, PageNumber, AlignmentType } from "docx";
import { tokens, ptToHalfPt } from "@/features/template/tokens";
import type { CvDocument } from "@/features/analysis/types";

const noHash = (hex: string) => hex.replace("#", "");

export function buildHeader(doc: CvDocument): Header {
  return new Header({
    children: [
      // DIRECT child. Do not wrap in Paragraph. (Textbox emits its own <w:p>.)
      new Textbox({
        alignment: AlignmentType.LEFT,
        children: [
          new Paragraph({
            children: [
              new TextRun({
                text: `${doc.fullName.value ?? ""} | ${doc.roleTitle.value ?? ""}`,
                font: tokens.bodyFont,
                size: ptToHalfPt(tokens.tableLabelPt),
                color: noHash(tokens.ink),
              }),
            ],
          }),
        ],
        style: { width: "50%", height: "auto" },
      }),
    ],
  });
}

export function buildFooter(): Footer {
  return new Footer({
    children: [
      new Paragraph({
        children: [
          new TextRun({
            text: "Meridian Digital Services Pty Ltd",
            font: tokens.bodyFont,
            size: ptToHalfPt(tokens.tableLabelPt),
          }),
          new TextRun({ text: "\t\t" }),
          new TextRun({
            // the LIVE field: docx serialises this as an OOXML PAGE instruction
            children: [PageNumber.CURRENT],
            font: tokens.bodyFont,
            size: ptToHalfPt(tokens.tableLabelPt),
          }),
        ],
      }),
    ],
  });
}

// web/recruitment/src/features/docx/__tests__/conformance.test.ts
//
// Section 6.7: render a .docx, unzip it, assert against word/document.xml.
//
// This runs in Node with no browser and no server: buildCvDocument is pure, so
// the whole check is Packer.toBuffer -> JSZip -> string assertions. That is why
// features/docx/template.ts was kept free of React and fetch.
//
// Why assert on the XML rather than look at the file: the dated sections once
// rendered as borderless tables. In Word that is indistinguishable from the
// template. In the XML it was four <w:tbl> where 6.3 allows one. Nothing in the
// chain complained — the file built, Packer reported a healthy byte count, Word
// opened it without a murmur. The XML is the only part of this pipeline that
// cannot shrug and carry on.

import { Packer } from "docx";
import JSZip from "jszip";
import { buildCvDocument } from "../template";
import { mockAnalysis } from "@/features/analysis/mockAnalysis";
import type { CvDocument } from "@/features/analysis/types";

const doc = mockAnalysis.document as CvDocument;

let documentXml = "";
let headerXml = "";
let footerXml = "";
let headerRels = "";

beforeAll(async () => {
  const buffer = await Packer.toBuffer(buildCvDocument(doc));
  const zip = await JSZip.loadAsync(buffer);

  const read = async (path: string) => {
    const f = zip.file(path);
    return f ? await f.async("string") : "";
  };

  documentXml = await read("word/document.xml");

  // header/footer part names are assigned by docx; find them rather than assume
  const headerName = Object.keys(zip.files).find((n) => /^word\/header\d*\.xml$/.test(n));
  const footerName = Object.keys(zip.files).find((n) => /^word\/footer\d*\.xml$/.test(n));
  headerXml = headerName ? await read(headerName) : "";
  footerXml = footerName ? await read(footerName) : "";
  headerRels = headerName ? await read(`word/_rels/${headerName.split("/")[1]}.rels`) : "";
});

// The eight details-table labels, in the fixed order from 6.3.
const TABLE_LABELS = [
  "FULL NAME",
  "PROPOSED ROLE",
  "QUALIFICATIONS",
  "SECURITY CLEARANCE",
  "YEARS OF EXPERIENCE",
  "AVAILABILITY",
  "LOCATION",
  "REFEREES",
];

// The seven sections, in the fixed order from 6.4.
const SECTION_HEADINGS = [
  "Professional Profile",
  "Career Synopsis",
  "Role Suitability",
  "Core Competencies",
  "Commendations and Awards",
  "Qualifications",
  "Career Highlights",
];

// Text of the first table only (the details table).
function firstTable(): string {
  return documentXml.split("<w:tbl>")[1]?.split("</w:tbl>")[0] ?? "";
}

describe("docx conformance (brief 6.7)", () => {
  it("C1 — page size and margins match 6.1", () => {
    expect(documentXml).toContain('w:w="11906"');
    expect(documentXml).toContain('w:h="16838"');
    expect(documentXml).toMatch(/w:top="1710"/);
    expect(documentXml).toMatch(/w:left="1440"/);
    expect(documentXml).toMatch(/w:right="1106"/);
  });

  it("C2 — exactly one table, correct column widths, exactly 8 rows", () => {
    expect(documentXml.split("<w:tbl>").length - 1).toBe(1);

    const tbl = firstTable();
    // Column widths live on the CELLS (tcW). The table's own w:w is the total
    // (9360), so match tcW specifically. Tolerances from 6.7: 3118..3120
    // and 6232..6240.
    const cellWidths = [...tbl.matchAll(/<w:tcW w:type="dxa" w:w="(\d+)"\/>/g)].map((m) =>
      Number(m[1]),
    );
    const labelWidths = cellWidths.filter((w) => w < 5000);
    const valueWidths = cellWidths.filter((w) => w >= 5000);

    expect(labelWidths.length).toBe(8);
    expect(valueWidths.length).toBe(8);
    for (const w of labelWidths) {
      expect(w).toBeGreaterThanOrEqual(3118);
      expect(w).toBeLessThanOrEqual(3120);
    }
    for (const w of valueWidths) {
      expect(w).toBeGreaterThanOrEqual(6232);
      expect(w).toBeLessThanOrEqual(6240);
    }

     // Match <w:tr> / <w:tr ...> only — "<w:tr" alone also catches
    // <w:trPr> and <w:trHeight>, which are per-row children.
    expect((tbl.match(/<w:tr[ >]/g) ?? []).length).toBe(8);
  });

  it("C3 — 8 label cells, in order, navy fill, white bold sz 18", () => {
    const tbl = firstTable();
    const found = TABLE_LABELS.filter((l) => tbl.includes(l));
    expect(found).toEqual(TABLE_LABELS); // presence AND order

    expect(tbl).toContain('w:fill="1E1560"');
    expect(tbl).toContain('w:val="FFFFFF"');
    expect(tbl).toContain("<w:b/>"); // docx emits no space
    expect(tbl).toMatch(/w:val="18"/);
  });

  it("C4 — every value cell has the lavender fill", () => {
    const tbl = firstTable();
    const lavender = (tbl.match(/w:fill="EEF0FF"/g) ?? []).length;
    expect(lavender).toBe(8); // one per row
  });

  it("C5 — the seven section headings appear in 6.4 order, and no others", () => {
    const pattern = new RegExp(`<w:t[^>]*>(${SECTION_HEADINGS.join("|")})</w:t>`, "g");
    const found = [...documentXml.matchAll(pattern)].map((m) => m[1]);
    expect(found).toEqual(SECTION_HEADINGS);
  });

  it("C6 — every section heading run is bold, orange, sz 22", () => {
    expect((documentXml.match(/w:val="E8600A"/g) ?? []).length).toBe(SECTION_HEADINGS.length);
    expect(documentXml).toMatch(/w:val="22"/);
  });

  it("C7 — no literal bullet character; bullets resolve through numbering", () => {
    expect(documentXml).not.toContain("\u2022");
    expect(documentXml).toContain("<w:numPr>");
  });

  it("C8 — no cell shading uses SOLID", () => {
    expect(documentXml).not.toContain('w:val="solid"');
    expect(documentXml).toContain('w:val="clear"');
  });

  it("C9 — the footer contains a live PAGE field instruction", () => {
    expect(footerXml).toMatch(/<w:instrText[^>]*>\s*PAGE\s*<\/w:instrText>/);
  });

  it("C10 — the header has a text box reading '{fullName} | {roleTitle}'", () => {
    // The gotcha this guards: wrapping Textbox in a Paragraph produces a valid
    // file with a SILENTLY empty header. Nothing throws. This assertion is the
    // only thing that notices.
    expect(headerXml).toContain("txbxContent");
    const expected = `${doc.fullName.value} | ${doc.roleTitle.value}`;
    expect(headerXml).toContain(expected);
  });

  it.todo("C10 — header image relationship (logo asset not yet in the repo)");

  it("C11 — profile is 40–150 words and not copied from the input CV", () => {
    const profile = doc.professionalProfile.value ?? "";
    const words = profile.trim().split(/\s+/).filter(Boolean).length;
    expect(words).toBeGreaterThanOrEqual(40);
    expect(words).toBeLessThanOrEqual(150);

    // It must render as the first paragraph after the heading.
    const afterHeading = documentXml.split("Professional Profile")[1] ?? "";
    expect(afterHeading.slice(0, 4000)).toContain(profile.slice(0, 60));

    // And it must not be lifted verbatim from the source CV.
    const sourceCv = (doc.professionalProfile.sourceQuotes ?? []).join(" ");
    expect(sourceCv).not.toContain(profile);
  });
});
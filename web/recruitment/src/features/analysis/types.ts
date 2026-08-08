// web/recruitment/src/features/analysis/types.ts
//
// The CvDocument contract, taken from the brief section 4.1 / 4.2.
// THIS is the source of truth for the shape — not the backend's stage-1 DTOs,
// which are an early sketch and are behind this. Your partner's C# records need
// to converge on this too; raise it with him.

// ---------------------------------------------------------------------------
// Provenance (4.1)
// ---------------------------------------------------------------------------

// All SIX classes. Every value in the document carries one.
export type Provenance =
  | "verbatim" // appears character-for-character in the CV
  | "normalised" // house-style rewrite of a verified CV span
  | "derived" // comes from the job requirements, not the CV
  | "generated" // model-authored synthesis, grounded in verified CV spans
  | "edited" // the user changed it after the model produced it
  | "absent"; // not supported by the CV. Renders empty. NEVER invented.

export interface FieldValue<T> {
  value: T | null;
  provenance: Provenance;
  sourceQuotes: string[]; // spans of the input supporting this value
  ruleIds?: string[]; // for 'normalised': which 4.8.1 rules were applied (plural)
  criteria?: string[]; // for 'generated' SCALARS only. List fields carry criteria per item.
}

// ---------------------------------------------------------------------------
// Item types (4.2)
// ---------------------------------------------------------------------------

export interface Referee {
  name: string;
  position: string;
  organisation: string;
  phone: string;
}

export interface CareerEntry {
  title: string;
  organisation: string;
  startYear: string;
  endYear: string; // may be 'Current'
}

export interface DatedEntry {
  description: string;
  year: string; // may be 'Various'
}

export interface QualificationEntry {
  qualification: string;
  institution?: string;
  year: string;
}

// criteria sit on the ITEM for these two, because the claim is about the
// distribution across items, not about the field as a whole (4.1).
export interface Competency {
  text: string;
  criteria: string[];
}

export interface Highlight {
  heading: string;
  bullet: string;
  criteria: string[];
}

// ---------------------------------------------------------------------------
// The document (4.2)
// ---------------------------------------------------------------------------

export interface CvDocument {
  schemaVersion: 1; // bumped only when a change makes old documents unreadable

  // Derived from the job requirements
  roleTitle: FieldValue<string>; // header band + title line
  level: FieldValue<string>; // title line
  proposedRole: FieldValue<string>; // details table

  // Extracted from the CV
  fullName: FieldValue<string>;
  qualifications: FieldValue<string[]>; // rendered semicolon-joined in the table
  securityClearance: FieldValue<string>;
  yearsOfExperience: FieldValue<string>;
  availability: FieldValue<string>;
  location: FieldValue<string>;
  referees: FieldValue<Referee[]>;
  careerSynopsis: FieldValue<CareerEntry[]>;

  // Generated, tailored to the job requirements
  professionalProfile: FieldValue<string>;
  roleSuitability: FieldValue<string>;

  // All mandatory — there is one template and it has all of these
  coreCompetencies: FieldValue<Competency[]>;
  commendationsAndAwards: FieldValue<DatedEntry[]>;
  qualificationsDetailed: FieldValue<QualificationEntry[]>;
  careerHighlights: FieldValue<Highlight[]>;
}

// ---------------------------------------------------------------------------
// The API envelope (4.3)
// ---------------------------------------------------------------------------

export type AnalysisStatus =
  | "extracting"
  | "tailoring"
  | "review"
  | "generated"
  | "failed";

export interface Warning {
  code: string;
  message: string;
  field?: string;
}

export interface Analysis {
  analysisId: string;
  status: AnalysisStatus;
  document: CvDocument | null;
  warnings: Warning[];
  failureDetail?: unknown;
}

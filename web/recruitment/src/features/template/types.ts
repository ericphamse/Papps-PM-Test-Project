// web/recruitment/src/features/analysis/types.ts
//
// TypeScript shapes for the analysis response the backend returns.
// These MIRROR the backend DTOs in
//   backend/CvPipeline.Api/Features/Analyses/CreateAnalysis/CreateAnalysisCommand.cs
//
// NOTE: this is the stage-1 EXTRACTION shape and it is incomplete. It will grow
// to cover the full document in section 6 (seven sections; the eight
// details-table rows). Keep it in sync with your partner's real response —
// confirm the exact top-level field names, since the backend currently returns
// `document` as opaque JSON.

// Where a field's value came from. Drives the ProvenanceBadge.
export type Provenance = "generated" | "normalised" | "edited" | "absent";

// Every field carries its value AND its provenance.
export type ProvenanceValue<T> = {
  value: T | null;
  provenance: Provenance;
  ruleId?: string; // e.g. "N1" when a normalisation rule produced the value
};

export type PersonalDetails = {
  fullName: ProvenanceValue<string>;
  location: ProvenanceValue<string>;
  securityClearance: ProvenanceValue<string>;
  referees: ProvenanceValue<string>;
};

export type WorkHistoryEntry = {
  jobTitle: ProvenanceValue<string>;
  company: ProvenanceValue<string>;
  dates: ProvenanceValue<string>;
  bulletPoints: string[]; // plain strings in the current DTO (no per-bullet provenance)
};

export type EducationEntry = {
  degree: ProvenanceValue<string>;
  institution: ProvenanceValue<string>;
};

// The tailored CV. (Composed from the DTOs above — confirm the nesting with your
// partner; the lists are assumed because a CV has many jobs / degrees.)
export type CvDocument = {
  personalDetails: PersonalDetails;
  workHistory: WorkHistoryEntry[];
  education: EducationEntry[];
};

export type AnalysisStatus =
  | "extracting"
  | "tailoring"
  | "review"
  | "generated"
  | "failed";

// The envelope around the document (matches the backend Analysis model:
// Status + Document + Warnings + FailureDetail).
export type Analysis = {
  status: AnalysisStatus;
  document: CvDocument | null;
  warnings: string[];
  failureDetail?: unknown;
};

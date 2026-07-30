// web/recruitment/src/features/analysis/mockAnalysis.ts
//
// A hand-written sample of what POST /api/analyses returns, so the whole
// frontend can be built and viewed WITHOUT the backend running.
// Data is drawn from the Jordan Reeve fixture. Grow this as the real
// document shape firms up (section 6: seven sections, eight table rows).

import type { Analysis } from "./types";

export const mockAnalysis: Analysis = {
  status: "review",
  document: {
    personalDetails: {
      fullName: { value: "Jordan Reeve", provenance: "generated" },
      // normalised: "Adelaide SA 5000" -> "Adelaide, SA" by rule N1
      location: { value: "Adelaide, SA", provenance: "normalised", ruleId: "N1" },
      // the CV never states a clearance, so it comes back absent (renders empty)
      securityClearance: { value: null, provenance: "absent" },
      referees: {
        value: "Marcus Delaney; Helena Voss",
        provenance: "generated",
      },
    },
    workHistory: [
      {
        jobTitle: { value: "Principal Software Engineer", provenance: "generated" },
        company: { value: "Northline Digital", provenance: "generated" },
        // normalised "Feb 2024 – present" -> "2024 – Current"
        dates: { value: "2024 – Current", provenance: "normalised", ruleId: "N4" },
        bulletPoints: [
          "Broke a Rails-era monolith into event-driven microservices on AWS with Kafka.",
          "Set up customer-data ingestion handling ~20M events per day at peak.",
        ],
      },
      {
        jobTitle: { value: "Engineering Lead", provenance: "generated" },
        company: { value: "Brightpath Software", provenance: "generated" },
        dates: { value: "2021 – 2024", provenance: "normalised", ruleId: "N4" },
        bulletPoints: [
          "Led a team of six engineers.",
          "Owned architecture for a customer platform with ~200k monthly active users.",
        ],
      },
    ],
    education: [
      {
        degree: { value: "Master of Software Engineering", provenance: "generated" },
        institution: { value: "Southern Cross Institute", provenance: "generated" },
      },
      {
        degree: { value: "Bachelor of Computer Science", provenance: "generated" },
        institution: { value: "Riverina Institute of Technology", provenance: "generated" },
      },
    ],
  },
  warnings: [],
};

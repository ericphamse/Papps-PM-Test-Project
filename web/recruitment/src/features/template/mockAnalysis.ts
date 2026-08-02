// web/recruitment/src/features/analysis/mockAnalysis.ts
//
// A hand-written sample of POST /api/analyses, so the frontend can be built
// without the backend running. Shaped to the real contract (4.2), with the
// Jordan Reeve fixture as the source CV and the Meridian RFQ as the job.
//
// Deliberately exercises every provenance class so the editor and preview can
// be seen handling all six:
//   verbatim   fullName
//   derived    roleTitle, level, proposedRole  (from the RFQ, not the CV)
//   normalised location, availability, referees, careerSynopsis, ...
//   generated  professionalProfile, roleSuitability, coreCompetencies,
//              careerHighlights, yearsOfExperience   (the only five allowed, 4.8.4)
//   absent     securityClearance                     (never invented)

import type { Analysis } from "./types";

export const mockAnalysis: Analysis = {
  analysisId: "00000000-0000-0000-0000-000000000001",
  status: "review",
  warnings: [],
  document: {
    schemaVersion: 1,

    // --- Derived from the job requirements ---
    roleTitle: {
      value: "Senior Software Engineer",
      provenance: "derived",
      sourceQuotes: ["Engagement title | Senior Software Engineer"],
    },
    level: {
      value: "Level 4",
      provenance: "derived",
      sourceQuotes: ["Level required | Level 4"],
    },
    proposedRole: {
      value: "Senior Software Engineer, Level 4",
      provenance: "derived",
      sourceQuotes: ["Engagement title | Senior Software Engineer", "Level required | Level 4"],
    },

    // --- Extracted from the CV ---
    fullName: {
      // the one field that survives character-for-character
      value: "Jordan Reeve",
      provenance: "verbatim",
      sourceQuotes: ["Jordan Reeve"],
    },
    qualifications: {
      // 4 entries for the table; the 5th lives in qualificationsDetailed (G9)
      value: [
        "Master of Software Engineering",
        "Bachelor of Computer Science",
        "AWS Certified Solutions Architect – Professional",
        "CKA – Certified Kubernetes Administrator",
      ],
      provenance: "normalised",
      sourceQuotes: [
        "Master of Software Engineering, Southern Cross Institute — finished 2016",
        "AWS Certified Solutions Architect – Professional (2023)",
      ],
      ruleIds: ["N7", "N8", "N9", "N11"],
    },
    securityClearance: {
      // the CV never mentions one, so it stays empty and is never invented (P5)
      value: null,
      provenance: "absent",
      sourceQuotes: [],
    },
    yearsOfExperience: {
      value: "15+ years - Software Engineering, Cloud Architecture and Technical Leadership",
      provenance: "generated",
      sourceQuotes: ["I've been doing this about 15 years now"],
      criteria: ["E1", "E2", "E5"],
    },
    availability: {
      value: "4 weeks notice",
      provenance: "normalised",
      sourceQuotes: ["Would need roughly a month to wrap up properly"],
      ruleIds: ["N3"],
    },
    location: {
      value: "Adelaide, SA",
      provenance: "normalised",
      sourceQuotes: ["Adelaide SA 5000"],
      ruleIds: ["N1"],
    },
    referees: {
      value: [
        {
          name: "Marcus Delaney",
          position: "Engineering Manager",
          organisation: "Northline Digital",
          phone: "0491 570 156",
        },
        {
          name: "Helena Voss",
          position: "Chief Technology Officer",
          organisation: "Brightpath Software",
          phone: "0491 570 157",
        },
      ],
      provenance: "normalised",
      sourceQuotes: [
        "Marcus Delaney — my current manager at Northline Digital (Engineering Manager). 0491 570 156.",
        "Helena Voss — was CTO at Brightpath Software while I was there",
      ],
      ruleIds: ["N5", "N10"],
    },
    careerSynopsis: {
      value: [
        {
          title: "Principal Software Engineer",
          organisation: "Northline Digital",
          startYear: "2024",
          endYear: "Current",
        },
        {
          title: "Engineering Lead",
          organisation: "Brightpath Software",
          startYear: "2021",
          endYear: "2024",
        },
        {
          title: "Senior Software Engineer",
          organisation: "Brightpath Software",
          startYear: "2018",
          endYear: "2021",
        },
        {
          title: "Software Engineer",
          organisation: "Cartwright Logistics Group",
          startYear: "2015",
          endYear: "2018",
        },
        {
          title: "Full-Stack Developer",
          organisation: "Redgum Interactive",
          startYear: "2009",
          endYear: "2015",
        },
      ],
      provenance: "normalised",
      sourceQuotes: [
        "Northline Digital — Principal Software Engineer (contract)",
        "Feb 2024 – present",
        "Engineering Lead, Mar 2021 – Feb 2024",
      ],
      ruleIds: ["N4", "N6", "N10"],
    },

    // --- Generated, tailored to the job requirements ---
    professionalProfile: {
      value:
        "A senior software engineer with over fifteen years delivering cloud-native services and customer-facing web applications. Combines deep full-stack capability across TypeScript, React, Python and Go with a track record of decomposing legacy monoliths into independently deployable services without customer downtime. Experienced in owning services end to end, from data modelling and API design through deployment, monitoring and incident response, and in providing technical leadership through design authority, code review and mentoring.",
      provenance: "generated",
      sourceQuotes: [
        "these days I mostly work on cloud stuff - TypeScript/React on the front, Python or Go on the back",
        "Broke it into event-driven microservices on AWS",
      ],
      criteria: ["E1", "E2", "E3", "E5"],
    },
    roleSuitability: {
      value:
        "Jordan's experience maps directly to this engagement. At Northline Digital he decomposed a payments monolith into event-driven microservices on AWS, cutting p95 latency by roughly sixty per cent while moving the team from monthly to on-demand deployment. At Cartwright Logistics he containerised and migrated a fifteen-year-old .NET application with zero customer downtime on cutover. As Engineering Lead at Brightpath he owned architecture and delivery for a platform serving 200,000 monthly active users, led a team of six, and authored the testing and release standards later adopted across three teams.",
      provenance: "generated",
      sourceQuotes: [
        "p95 latency went from about 900ms to under 400ms",
        "Zero customer downtime on cutover",
        "Led a team of six.",
      ],
      criteria: ["E2", "E4", "E5"],
    },

    // --- Mandatory list sections ---
    coreCompetencies: {
      value: [
        { text: "Cloud-native service design on AWS and Azure", criteria: ["E2"] },
        { text: "Containerisation and orchestration with Docker and Kubernetes", criteria: ["E2"] },
        { text: "Full-stack development in TypeScript, React, Python and Go", criteria: ["E3"] },
        { text: "Legacy system migration without service disruption", criteria: ["E4"] },
        { text: "Event-driven architecture and high-volume data pipelines", criteria: ["E2"] },
        { text: "Infrastructure as code and delivery pipeline automation", criteria: ["E2"] },
        { text: "Technical leadership, design authority and code review", criteria: ["E5"] },
        { text: "Mentoring and development of mid-level and junior engineers", criteria: ["E5"] },
        { text: "Communicating technical trade-offs to non-technical stakeholders", criteria: ["E6"] },
      ],
      provenance: "generated",
      sourceQuotes: ["TypeScript, JavaScript, React, Next.js, Node.js, Python (FastAPI, Django), Go"],
    },
    commendationsAndAwards: {
      value: [
        { description: "Engineering Excellence Award, Brightpath Software", year: "2023" },
        { description: "Winner, Adelaide Dev Summit Hackathon", year: "2019" },
        { description: "Employee of the Year, Redgum Interactive", year: "2014" },
      ],
      provenance: "normalised",
      sourceQuotes: [
        "Won the internal Engineering Excellence Award in 2023 for the release work.",
        "Employee of the Year 2014.",
      ],
      ruleIds: ["N10"],
    },
    qualificationsDetailed: {
      // exactly 5, including Professional Scrum Master I (G9)
      value: [
        {
          qualification: "Master of Software Engineering",
          institution: "Southern Cross Institute",
          year: "2016",
        },
        {
          qualification: "Bachelor of Computer Science",
          institution: "Riverina Institute of Technology",
          year: "2009",
        },
        {
          qualification: "AWS Certified Solutions Architect – Professional",
          year: "2023",
        },
        {
          qualification: "CKA – Certified Kubernetes Administrator",
          year: "2022",
        },
        {
          qualification: "Professional Scrum Master I",
          year: "2018",
        },
      ],
      provenance: "normalised",
      sourceQuotes: [
        "Master of Software Engineering, Southern Cross Institute — finished 2016",
        "Professional Scrum Master I — 2018",
      ],
      ruleIds: ["N7", "N8", "N9"],
    },
    careerHighlights: {
      // 6 entries; union covers E2, E3, E4, E5 (G11)
      value: [
        {
          heading: "Payments platform decomposition",
          bullet:
            "Decomposed a monolithic payments service into event-driven microservices on AWS with Kafka, reducing p95 latency from approximately 900ms to under 400ms and enabling zero-downtime deployment on any day.",
          criteria: ["E2", "E4"],
        },
        {
          heading: "Customer data ingestion pipeline",
          bullet:
            "Designed and delivered a Kafka-to-PostgreSQL ingestion pipeline sustaining approximately 20 million events per day at peak.",
          criteria: ["E2"],
        },
        {
          heading: "Legacy application migration",
          bullet:
            "Containerised and migrated a fifteen-year-old .NET application to cloud hosting with zero customer downtime at cutover and a 35% reduction in hosting cost.",
          criteria: ["E4"],
        },
        {
          heading: "Shared component library",
          bullet:
            "Built a shared React and TypeScript component library adopted by five product teams, measured to accelerate new feature UI delivery by approximately 40%.",
          criteria: ["E3"],
        },
        {
          heading: "Delivery pipeline automation",
          bullet:
            "Replaced a manual release process with GitHub Actions pipelines and Terraform, moving the team from monthly to daily releases.",
          criteria: ["E2"],
        },
        {
          heading: "Team leadership and mentoring",
          bullet:
            "Led a team of six engineers and authored the testing, code review and release standards subsequently adopted by two further teams; three mentored junior engineers reached mid-level within two years.",
          criteria: ["E5"],
        },
      ],
      provenance: "generated",
      sourceQuotes: [
        "Broke it into event-driven microservices on AWS, Kafka in the middle.",
        "handles about 20 million events a day at peak",
        "Three of the juniors I brought in got to mid-level inside two years",
      ],
    },
  },
};

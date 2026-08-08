// src/Cv.Infrastructure/Llm/FakeGeminiClient.cs
using CvPipeline.Api.Cv.Application.Abstractions;
using CvPipeline.Api.Cv.Application.Analysis.ExtractCv;
using CvPipeline.Api.Cv.Application.Analysis.TailorNarrative;
using CvPipeline.Api.Cv.Domain;

namespace CvPipeline.Api.Cv.Infrastructure.Llm;

public class FakeGeminiClient : IGeminiClient
{
    public Task<SelectionResult> SelectAsync(
        string cvText, string jobRequirements,
        CancellationToken ct, List<string>? previousErrors = null)
    {
        var result = new SelectionResult(
            FullName: new FieldQuotes(new[] { "Jordan Reeve" }),
            Qualifications: new FieldQuotes(new[]
            {
                "Master of Software Engineering, Southern Cross Institute — finished 2016, did it part time while working",
                "Bachelor of Computer Science, Riverina Institute of Technology — 2006 to 2009, graduated 2009",
                "AWS Certified Solutions Architect – Professional (2023)",
                "CKA – Certified Kubernetes Administrator (2022, need to renew this)",
                "Professional Scrum Master I — 2018"
            }),
            SecurityClearance: new FieldQuotes(new[] { "Current National Police Check, did it last year for the Northline contract." }),
            YearsOfExperience: new FieldQuotes(new[] { "about 15 years now" }),
            Availability: new FieldQuotes(new[] { "Would need roughly 2 months to wrap up properly." }),
            Location: new FieldQuotes(new[] { "Adelaide SA 5000" }),
            Referees: new List<ReferQuotes>
            {
                new ReferQuotes(
                    new FieldQuotes(new[] { "Marcus Delaney" }),
                    new FieldQuotes(new[] { "Engineering Manager" }),
                    new FieldQuotes(new[] { "Northline Digital" }),
                    new FieldQuotes(new[] { "0491 570 156" })),
                new ReferQuotes(
                    new FieldQuotes(new[] { "Helena Voss" }),
                    new FieldQuotes(new[] { "CTO" }),
                    new FieldQuotes(new[] { "Brightpath Software" }),
                    new FieldQuotes(new[] { "0491 570 157" }))
            },
            CareerSynopsis: new List<CareerEntryQuotes>
            {
                new CareerEntryQuotes(new FieldQuotes(new[] { "Principal Software Engineer (contract)" }), new FieldQuotes(new[] { "Northline Digital" }), new FieldQuotes(new[] { "Feb 2024" }), new FieldQuotes(new[] { "present" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Engineering Lead" }), new FieldQuotes(new[] { "Brightpath Software" }), new FieldQuotes(new[] { "Mar 2021" }), new FieldQuotes(new[] { "Feb 2024" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Senior Software Engineer" }), new FieldQuotes(new[] { "Brightpath Software" }), new FieldQuotes(new[] { "2018" }), new FieldQuotes(new[] { "2021" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Software Engineer" }), new FieldQuotes(new[] { "Cartwright Logistics Group" }), new FieldQuotes(new[] { "2015" }), new FieldQuotes(new[] { "2018" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Full-Stack Developer" }), new FieldQuotes(new[] { "Redgum Interactive" }), new FieldQuotes(new[] { "2012" }), new FieldQuotes(new[] { "2015" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Junior Developer" }), new FieldQuotes(new[] { "Redgum Interactive" }), new FieldQuotes(new[] { "2010" }), new FieldQuotes(new[] { "2012" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Intern" }), new FieldQuotes(new[] { "Redgum Interactive" }), new FieldQuotes(new[] { "Jan 09" }), new FieldQuotes(new[] { "Oct 10" })),
                new CareerEntryQuotes(new FieldQuotes(new[] { "Bar attendant" }), new FieldQuotes(new[] { "The Crown & Anchor Hotel" }), new FieldQuotes(new[] { "2005" }), new FieldQuotes(new[] { "2008" }))
            },
            CommendationsAndAwards: new List<DatedEntryQuotes>
            {
                new DatedEntryQuotes(
                    new FieldQuotes(new[] { "Engineering Excellence Award" }),
                    new FieldQuotes(new[] { "2023" })),
                new DatedEntryQuotes(
                    new FieldQuotes(new[] { "Won the Adelaide Dev Summit hackathon in 2019 with a team of three." }),  // ← exact CV text
                    new FieldQuotes(new[] { "2019" })),
                new DatedEntryQuotes(
                    new FieldQuotes(new[] { "Employee of the Year 2014" }),   // ← check exact CV text
                    new FieldQuotes(new[] { "2014" }))
            },
            QualificationsDetailed: new List<QualificationEntryQuotes>
            {
                new QualificationEntryQuotes(new FieldQuotes(new[] { "Master of Software Engineering" }), new FieldQuotes(new[] { "Southern Cross Institute" }), new FieldQuotes(new[] { "finished 2016" })),
                new QualificationEntryQuotes(new FieldQuotes(new[] { "Bachelor of Computer Science" }), new FieldQuotes(new[] { "Riverina Institute of Technology" }), new FieldQuotes(new[] { "2006 to 2009, graduated 2009" })),
                new QualificationEntryQuotes(new FieldQuotes(new[] { "AWS Certified Solutions Architect – Professional" }), new FieldQuotes(Array.Empty<string>()), new FieldQuotes(new[] { "2023" })),
                new QualificationEntryQuotes(new FieldQuotes(new[] { "CKA – Certified Kubernetes Administrator (2022, need to renew this)" }), new FieldQuotes(Array.Empty<string>()), new FieldQuotes(new[] { "2022" })),
                new QualificationEntryQuotes(new FieldQuotes(new[] { "Professional Scrum Master I" }), new FieldQuotes(Array.Empty<string>()), new FieldQuotes(new[] { "2018" })),
                new QualificationEntryQuotes(new FieldQuotes(new[] { "Adelaide High School" }), new FieldQuotes(Array.Empty<string>()), new FieldQuotes(new[] { "2004" }))
            },
            TechVotes: new List<TechVote>
            {
                new TechVote("TypeScript", true, "Strong full-stack capability across a modern JavaScript/TypeScript front end"),
                new TechVote("React", true, "Strong full-stack capability across a modern JavaScript/TypeScript front end"),
                new TechVote("Python", true, "at least one server-side language (Python, Go, Node.js, .NET or equivalent)"),
                new TechVote("Go", true, "at least one server-side language (Python, Go, Node.js, .NET or equivalent)"),
                new TechVote("Docker", true, "including containerisation and orchestration"),
                new TechVote("Kubernetes", true, "including containerisation and orchestration"),
                new TechVote("AWS", true, "Demonstrated delivery of production cloud-native services on AWS or Azure"),
                new TechVote("Terraform", true, "Experience with infrastructure as code (Terraform or equivalent)."),
                new TechVote("GitHub Actions", true, "Improve the delivery pipeline (build, test, release automation, infrastructure as code)."),
                new TechVote("PHP", false, null),
                new TechVote("jQuery", false, null),
                new TechVote("Jenkins", false, null),
                new TechVote("Figma", false, null),
                new TechVote("Redis", false, null)
            }
        );
        return Task.FromResult(result);
    }

    public Task<TailoredFields> TailorAsync(
        CvDocument partial, string jobRequirements,
        List<string> keptTechnologies, CancellationToken ct,
        List<string>? previousErrors = null)
    {
        var result = new TailoredFields(
            ProfessionalProfile: "Full-stack software engineer and technical lead with 15 years experience designing, building and running cloud-native web applications and distributed systems. Track record of modernising legacy platforms, lifting delivery throughput through automation, and mentoring engineers to work independently. Comfortable owning a service end to end, from data model and API design through to deployment, monitoring and incident response.",

            RoleSuitability: "Jordan has more than 15 years of hands-on software engineering experience across product, platform and integration work. As Engineering Lead at Brightpath Software, they owned the architecture and delivery of a customer-facing platform serving 200,000 monthly users, and set the standards for testing, code review and release management adopted across three teams. Their combination of deep technical delivery, cloud architecture and team leadership demonstrates the Level 4 expertise required to scope, build and support production systems with minimal supervision.",

            YearsOfExperience: "15+ years - Software Engineering, Cloud Architecture and Technical Leadership",

            CoreCompetencies: new List<TailoredCompetency>
            {
                new("Full-stack development with TypeScript, React and Node.js", new[] { "E3" }),
                new("Backend services in Python and Go", new[] { "E3" }),
                new("Cloud architecture on AWS and Azure", new[] { "E2" }),
                new("Containerisation and orchestration with Docker and Kubernetes", new[] { "E2" }),
                new("CI/CD pipelines and infrastructure as code (Terraform, GitHub Actions)", new[] { "E2" }),
                new("Relational and NoSQL data modelling (PostgreSQL, DynamoDB)", new[] { "E2" }),
                new("API design across REST, GraphQL and event-driven patterns", new[] { "E3" }),
                new("Automated testing, observability and code quality practices", new[] { "E2" }),
                new("Technical leadership, code review and mentoring", new[] { "E5" }),
                new("Clear communicator with technical and non-technical stakeholders", new[] { "E6" })
            },

            CareerHighlights: new List<TailoredHighlight>
            {
                new("Payments Platform Re-Architecture", "Led the migration of a monolithic payments service to event-driven microservices on AWS, cutting p95 latency by 60% and enabling zero-downtime releases.", new[] { "E2", "E4" }),
                new("Design System and Component Library", "Built a shared React and TypeScript component library adopted by five product teams, reducing UI build time for new features by roughly 40%.", new[] { "E3" }),
                new("CI/CD Modernisation", "Replaced a manual release process with automated pipelines and infrastructure as code, taking deployment frequency from monthly to daily.", new[] { "E2" }),
                new("Customer Data Platform", "Designed and delivered an ingestion pipeline processing 20 million events per day using Kafka and PostgreSQL.", new[] { "E2" }),
                new("Team Leadership and Mentoring", "Led a team of six engineers, ran hiring and onboarding, and mentored three junior developers through to mid-level within two years.", new[] { "E5" }),
                new("Legacy System Migration", "Migrated a 15-year-old .NET application to a containerised cloud-hosted service with no customer downtime and a 35% reduction in hosting cost.", new[] { "E4" })
            }
        );
        return Task.FromResult(result);
    }
}
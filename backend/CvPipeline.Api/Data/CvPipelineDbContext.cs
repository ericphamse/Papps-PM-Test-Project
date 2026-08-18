using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CvPipeline.Api.Models;

namespace CvPipeline.Api.Data;

public class CvPipelineDbContext : DbContext
{
    public CvPipelineDbContext(DbContextOptions<CvPipelineDbContext> options)
        : base(options)
    {
    }

    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<GenerationField> GenerationFields => Set<GenerationField>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.ToTable("analyses");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.CvText).HasColumnName("cv_text").IsRequired();
            entity.Property(e => e.JobRequirements).HasColumnName("job_requirements").IsRequired();
            entity.Property(e => e.CvSourceFilename).HasColumnName("cv_source_filename").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.FailureDetail).HasColumnName("failure_detail").HasColumnType("jsonb");
            entity.Property(e => e.Document).HasColumnName("document").HasColumnType("jsonb");
            entity.Property(e => e.Warnings).HasColumnName("warnings").HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");

            entity.ToTable(t => t.HasCheckConstraint("analyses_status_check", 
                "status IN ('extracting','tailoring','review','generated','failed')"));

            entity.HasIndex(e => e.CreatedAt, "analyses_created_at_idx").IsDescending();
        });

        modelBuilder.Entity<Generation>(entity =>
        {
            entity.ToTable("generations");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.OutputFilename).HasColumnName("output_filename").IsRequired();
            entity.Property(e => e.Document).HasColumnName("document").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(e => e.RoleTitle).HasColumnName("role_title").IsRequired();

            entity.HasOne(g => g.Analysis)
                .WithMany(a => a.Generations)
                .HasForeignKey(g => g.AnalysisId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedAt, "generations_created_at_idx").IsDescending();
            entity.HasIndex(e => e.AnalysisId, "generations_analysis_idx");
        });

        modelBuilder.Entity<GenerationField>(entity =>
        {
            entity.ToTable("generation_fields");

            entity.HasKey(e => new { e.GenerationId, e.FieldPath });

            entity.Property(e => e.GenerationId).HasColumnName("generation_id");
            entity.Property(e => e.FieldPath).HasColumnName("field_path");
            entity.Property(e => e.Provenance).HasColumnName("provenance").IsRequired();
            entity.Property(e => e.RuleIds).HasColumnName("rule_ids");
            entity.Property(e => e.Criteria).HasColumnName("criteria");
            entity.Property(e => e.SourceQuotes).HasColumnName("source_quotes").HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");

            entity.HasOne(gf => gf.Generation)
                .WithMany(g => g.GenerationFields)
                .HasForeignKey(gf => gf.GenerationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("generation_fields_provenance_check", 
                    "provenance IN ('verbatim','normalised','derived','generated','edited','absent')");
                
                t.HasCheckConstraint("normalised_cites_at_least_one_rule", 
                    "provenance <> 'normalised' OR (rule_ids IS NOT NULL AND array_length(rule_ids, 1) > 0)");
                
                t.HasCheckConstraint("absent_has_no_sources", 
                    "provenance <> 'absent' OR source_quotes = '[]'::jsonb");
            });

            entity.HasIndex(e => e.Provenance, "generation_fields_provenance_idx");
            entity.HasIndex(e => e.RuleIds, "generation_fields_rule_idx").HasMethod("gin");
        });

        // Npgsql has built-in JsonDocument <-> jsonb mapping; other providers
        // (e.g. the InMemory provider used in tests) don't, so give them a
        // JsonDocument <-> string converter instead.
        if (!Database.IsNpgsql())
        {
            var jsonDocumentConverter = new ValueConverter<JsonDocument, string>(
                v => v.RootElement.GetRawText(),
                v => JsonDocument.Parse(v, default));
            var nullableJsonDocumentConverter = new ValueConverter<JsonDocument?, string?>(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default));

            modelBuilder.Entity<Analysis>().Property(e => e.FailureDetail).HasConversion(nullableJsonDocumentConverter);
            modelBuilder.Entity<Analysis>().Property(e => e.Document).HasConversion(nullableJsonDocumentConverter);
            modelBuilder.Entity<Analysis>().Property(e => e.Warnings).HasConversion(jsonDocumentConverter);
            modelBuilder.Entity<Generation>().Property(e => e.Document).HasConversion(jsonDocumentConverter);
            modelBuilder.Entity<GenerationField>().Property(e => e.SourceQuotes).HasConversion(jsonDocumentConverter);
        }
    }
}
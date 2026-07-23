using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvPipeline.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    cv_text = table.Column<string>(type: "text", nullable: false),
                    job_requirements = table.Column<string>(type: "text", nullable: false),
                    cv_source_filename = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    failure_detail = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    document = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    warnings = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analyses", x => x.id);
                    table.CheckConstraint("analyses_status_check", "status IN ('extracting','tailoring','review','generated','failed')");
                });

            migrationBuilder.CreateTable(
                name: "generations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    output_filename = table.Column<string>(type: "text", nullable: false),
                    document = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    role_title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generations", x => x.id);
                    table.ForeignKey(
                        name: "FK_generations_analyses_analysis_id",
                        column: x => x.analysis_id,
                        principalTable: "analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "generation_fields",
                columns: table => new
                {
                    generation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_path = table.Column<string>(type: "text", nullable: false),
                    provenance = table.Column<string>(type: "text", nullable: false),
                    rule_ids = table.Column<List<string>>(type: "text[]", nullable: true),
                    criteria = table.Column<List<string>>(type: "text[]", nullable: true),
                    source_quotes = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_fields", x => new { x.generation_id, x.field_path });
                    table.CheckConstraint("absent_has_no_sources", "provenance <> 'absent' OR source_quotes = '[]'::jsonb");
                    table.CheckConstraint("generation_fields_provenance_check", "provenance IN ('verbatim','normalised','derived','generated','edited','absent')");
                    table.CheckConstraint("normalised_cites_at_least_one_rule", "provenance <> 'normalised' OR (rule_ids IS NOT NULL AND array_length(rule_ids, 1) > 0)");
                    table.ForeignKey(
                        name: "FK_generation_fields_generations_generation_id",
                        column: x => x.generation_id,
                        principalTable: "generations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "analyses_created_at_idx",
                table: "analyses",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "generation_fields_provenance_idx",
                table: "generation_fields",
                column: "provenance");

            migrationBuilder.CreateIndex(
                name: "generation_fields_rule_idx",
                table: "generation_fields",
                column: "rule_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "generations_analysis_idx",
                table: "generations",
                column: "analysis_id");

            migrationBuilder.CreateIndex(
                name: "generations_created_at_idx",
                table: "generations",
                column: "created_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generation_fields");

            migrationBuilder.DropTable(
                name: "generations");

            migrationBuilder.DropTable(
                name: "analyses");
        }
    }
}

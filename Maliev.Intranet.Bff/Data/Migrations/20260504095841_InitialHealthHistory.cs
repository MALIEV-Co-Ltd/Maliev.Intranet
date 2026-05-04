using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.Intranet.Bff.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialHealthHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "health_check_samples",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sampled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    service_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    domain_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    route_prefix = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_critical = table.Column<bool>(type: "boolean", nullable: false),
                    liveness_path = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    readiness_path = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    liveness_response_time_ms = table.Column<double>(type: "double precision", nullable: false),
                    readiness_response_time_ms = table.Column<double>(type: "double precision", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_check_samples", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_health_check_samples_domain_group_sampled_at_utc",
                table: "health_check_samples",
                columns: new[] { "domain_group", "sampled_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_health_check_samples_sampled_at_utc",
                table: "health_check_samples",
                column: "sampled_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_health_check_samples_service_name_sampled_at_utc",
                table: "health_check_samples",
                columns: new[] { "service_name", "sampled_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "health_check_samples");
        }
    }
}

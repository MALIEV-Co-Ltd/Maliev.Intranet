using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.Intranet.Bff.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    part_count = table.Column<int>(type: "integer", nullable: false),
                    process_types = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_read_receipts",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_read_receipts", x => new { x.notification_id, x.employee_id });
                    table.ForeignKey(
                        name: "FK_alert_read_receipts_alert_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "alert_notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_notifications_expires_at_utc",
                table: "alert_notifications",
                column: "expires_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_read_receipts");

            migrationBuilder.DropTable(
                name: "alert_notifications");
        }
    }
}

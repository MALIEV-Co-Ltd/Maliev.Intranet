using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.Intranet.Bff.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertNotificationEventDeduplicationKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "event_deduplication_key",
                table: "alert_notifications",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE alert_notifications
                SET event_deduplication_key = 'legacy:' || id::text
                WHERE event_deduplication_key IS NULL
                """);

            migrationBuilder.AlterColumn<string>(
                name: "event_deduplication_key",
                table: "alert_notifications",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_alert_notifications_event_deduplication_key",
                table: "alert_notifications",
                column: "event_deduplication_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_alert_notifications_event_deduplication_key",
                table: "alert_notifications");

            migrationBuilder.DropColumn(
                name: "event_deduplication_key",
                table: "alert_notifications");
        }
    }
}

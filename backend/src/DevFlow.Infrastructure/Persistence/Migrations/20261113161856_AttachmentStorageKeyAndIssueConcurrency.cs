using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevFlow.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Attachments now store a private storage key instead of a public blob URL. Existing rows are converted
    /// in place: the key is the (URL-decoded) last path segment of the old URL, which is the blob name the
    /// previous implementation used. Project.NextIssueNumber becoming a concurrency token needs no schema change.
    /// </summary>
    public partial class AttachmentStorageKeyAndIssueConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "Attachments",
                newName: "StorageKey");

            migrationBuilder.Sql(
                """
                UPDATE [Attachments]
                SET [StorageKey] = REPLACE(RIGHT([StorageKey], CHARINDEX('/', REVERSE([StorageKey])) - 1), '%20', ' ')
                WHERE [StorageKey] LIKE 'http%/%';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "Attachments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "Attachments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.RenameColumn(
                name: "StorageKey",
                table: "Attachments",
                newName: "FileUrl");
        }
    }
}

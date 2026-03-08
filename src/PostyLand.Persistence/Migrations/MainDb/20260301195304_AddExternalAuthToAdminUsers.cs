using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostyLand.Persistence.Migrations.MainDb
{
    /// <inheritdoc />
    public partial class AddExternalAuthToAdminUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "admin_users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderId",
                table: "admin_users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternalAccount",
                table: "admin_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_ExternalProvider_ExternalProviderId",
                table: "admin_users",
                columns: new[] { "ExternalProvider", "ExternalProviderId" },
                unique: true,
                filter: "\"ExternalProvider\" IS NOT NULL AND \"ExternalProviderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_users_ExternalProvider_ExternalProviderId",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "ExternalProviderId",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "IsExternalAccount",
                table: "admin_users");
        }
    }
}

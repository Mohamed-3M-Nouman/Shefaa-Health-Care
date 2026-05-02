using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShefaaHealthCare.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificatePath",
                table: "Doctors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SyndicateIdCardPath",
                table: "Doctors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificatePath",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "SyndicateIdCardPath",
                table: "Doctors");
        }
    }
}

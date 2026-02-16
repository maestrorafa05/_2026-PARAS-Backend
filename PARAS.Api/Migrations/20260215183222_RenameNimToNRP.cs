using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PARAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameNimToNRP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NIM",
                table: "Loans",
                newName: "NRP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NRP",
                table: "Loans",
                newName: "NIM");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PARAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanOwnershipAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                table: "LoanStatusHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "Loans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LoanStatusHistories_ChangedByUserId_ChangedAt",
                table: "LoanStatusHistories",
                columns: new[] { "ChangedByUserId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_RequestedByUserId_CreatedAt",
                table: "Loans",
                columns: new[] { "RequestedByUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoanStatusHistories_ChangedByUserId_ChangedAt",
                table: "LoanStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Loans_RequestedByUserId_CreatedAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "LoanStatusHistories");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "Loans");
        }
    }
}

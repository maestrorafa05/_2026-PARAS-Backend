using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PARAS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanScheduleIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_RoomId_StartTime_EndTime",
                table: "Loans");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Rooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Rooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Loans_RoomId_StartTime_EndTime_Status",
                table: "Loans",
                columns: new[] { "RoomId", "StartTime", "EndTime", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Loans_RoomId_StartTime_EndTime_Status",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Rooms");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_RoomId_StartTime_EndTime",
                table: "Loans",
                columns: new[] { "RoomId", "StartTime", "EndTime" });
        }
    }
}

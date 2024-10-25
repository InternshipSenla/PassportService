using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PassportService.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePassportModel_intSeriesAndNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Passports",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.AlterColumn<int>(
                name: "Series",
                table: "Passports",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "Passports",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLastRequest",
                table: "Passports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Passports_Series_Number",
                table: "Passports",
                columns: new[] { "Series", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Passports_Series_Number",
                table: "Passports");

            migrationBuilder.DropColumn(
                name: "DateLastRequest",
                table: "Passports");

            migrationBuilder.AlterColumn<string>(
                name: "Series",
                table: "Passports",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Passports",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.InsertData(
                table: "Passports",
                columns: new[] { "Id", "CreatedAt", "Number", "RemovedAt", "Series" },
                values: new object[,]
                {
                    { 1, new List<DateTime> { new DateTime(2020, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc) }, "567890", null, "1234" },
                    { 2, new List<DateTime> { new DateTime(2019, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc) }, "678901", new List<DateTime> { new DateTime(2022, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc) }, "2345" },
                    { 3, new List<DateTime> { new DateTime(2021, 11, 22, 0, 0, 0, 0, DateTimeKind.Utc) }, "789012", null, "3456" },
                    { 4, new List<DateTime> { new DateTime(2018, 8, 30, 0, 0, 0, 0, DateTimeKind.Utc) }, "890123", new List<DateTime> { new DateTime(2023, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc) }, "4567" },
                    { 5, new List<DateTime> { new DateTime(2022, 2, 28, 0, 0, 0, 0, DateTimeKind.Utc) }, "901234", null, "5678" },
                    { 6, new List<DateTime> { new DateTime(2020, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc) }, "012345", null, "6789" },
                    { 7, new List<DateTime> { new DateTime(2021, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }, "123456", new List<DateTime> { new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc) }, "7890" },
                    { 8, new List<DateTime> { new DateTime(2019, 9, 15, 0, 0, 0, 0, DateTimeKind.Utc) }, "234567", null, "8901" }
                });
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PassportService.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePassportModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Passports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Series = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<List<DateTime>>(type: "timestamp with time zone[]", nullable: false),
                    RemovedAt = table.Column<List<DateTime>>(type: "timestamp with time zone[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passports", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Passports");
        }
    }
}

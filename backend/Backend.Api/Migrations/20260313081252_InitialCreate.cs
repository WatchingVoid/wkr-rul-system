using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RulPredictions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MachineId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ToolId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RulMinutes = table.Column<float>(type: "real", nullable: false),
                    AlarmLevel = table.Column<int>(type: "integer", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulPredictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetrySpindle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MachineId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ToolId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SpindleRpm = table.Column<int>(type: "integer", nullable: false),
                    SpindleCurrentA = table.Column<float>(type: "real", nullable: false),
                    SpindlePowerKw = table.Column<float>(type: "real", nullable: false),
                    FeedMmMin = table.Column<int>(type: "integer", nullable: false),
                    Program = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CutFlag = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetrySpindle", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RulPredictions_MachineId_ToolId_Ts",
                table: "RulPredictions",
                columns: new[] { "MachineId", "ToolId", "Ts" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySpindle_MachineId_ToolId_Ts",
                table: "TelemetrySpindle",
                columns: new[] { "MachineId", "ToolId", "Ts" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RulPredictions");

            migrationBuilder.DropTable(
                name: "TelemetrySpindle");
        }
    }
}

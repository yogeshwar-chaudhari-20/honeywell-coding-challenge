using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StadiumAnalytics.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Gate = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Timestamp = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NumberOfPeople = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RawPayload = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ErrorDetails = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GateSensorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Gate = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Timestamp = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumberOfPeople = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateSensorEvents", x => x.Id);
                    table.CheckConstraint("CK_GateSensorEvents_Gate", "Gate IN ('GateA', 'GateB', 'GateC', 'GateD', 'GateE')");
                    table.CheckConstraint("CK_GateSensorEvents_NumberOfPeople", "NumberOfPeople > 0");
                    table.CheckConstraint("CK_GateSensorEvents_Type", "Type IN ('Enter', 'Leave')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GateSensorEvents_Gate_Timestamp_Type",
                table: "GateSensorEvents",
                columns: new[] { "Gate", "Timestamp", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GateSensorEvents_Gate_Type_Timestamp",
                table: "GateSensorEvents",
                columns: new[] { "Gate", "Type", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedEvents");

            migrationBuilder.DropTable(
                name: "GateSensorEvents");
        }
    }
}

using CareerSEA.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerSEA.Data.Migrations
{
    /// <inheritdoc />
    public partial class changeprediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Guess",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "InputVector",
                table: "Predictions");

            migrationBuilder.AddColumn<PredictionResult>(
                name: "Result",
                table: "Predictions",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Result",
                table: "Predictions");

            migrationBuilder.AddColumn<double>(
                name: "Guess",
                table: "Predictions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "InputVector",
                table: "Predictions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RS.Fahrzeugsystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleCatalogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Model = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Variant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    YearLabel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    BuildYearFrom = table.Column<int>(type: "integer", nullable: true),
                    BuildYearTo = table.Column<int>(type: "integer", nullable: true),
                    Engine = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EngineCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Transmission = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TransmissionCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EcuType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EcuManufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DriveType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Platform = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleCatalogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleCatalogEntries_Brand_Model_Variant_YearLabel_Engine_~",
                table: "VehicleCatalogEntries",
                columns: new[] { "Brand", "Model", "Variant", "YearLabel", "Engine", "EngineCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleCatalogEntries");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class EncodedLabelDataInlayConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InlayConfigDescription",
                table: "EncodedLabels",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InlayConfigID",
                table: "EncodedLabels",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InlayConfigDescription",
                table: "EncodedLabels");

            migrationBuilder.DropColumn(
                name: "InlayConfigID",
                table: "EncodedLabels");
        }
    }
}

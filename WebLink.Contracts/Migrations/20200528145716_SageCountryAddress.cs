using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class SageCountryAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AddressLine2",
                table: "Addresses",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AddressLine1",
                table: "Addresses",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine3",
                table: "Addresses",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryID",
                table: "Addresses",
                nullable: false,
                defaultValue: 210);

            migrationBuilder.AddColumn<string>(
                name: "SageCountryCode",
                table: "Addresses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SageProvinceCode",
                table: "Addresses",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Alpha2 = table.Column<string>(nullable: true),
                    Alpha3 = table.Column<string>(nullable: true),
                    NumericCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.ID);
                });


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropColumn(
                name: "AddressLine3",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "CountryID",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "SageCountryCode",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "SageProvinceCode",
                table: "Addresses");

            migrationBuilder.AlterColumn<string>(
                name: "AddressLine2",
                table: "Addresses",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AddressLine1",
                table: "Addresses",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}

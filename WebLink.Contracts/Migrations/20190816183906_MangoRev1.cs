using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class MangoRev1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GSTCity",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "GSTProvince",
                table: "Locations");

			migrationBuilder.DropColumn(
				name: "GSTCountry",
				table: "Locations");

			migrationBuilder.RenameColumn(
                name: "LocationCode",
                table: "Locations",
                newName: "FactoryCode");

			migrationBuilder.AddColumn<int>(
				name: "WorkingDays",
				table: "Locations",
				nullable: false,
				defaultValue:62);

			migrationBuilder.AddColumn<string>(
                name: "Holidays",
                table: "Locations",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionManager1",
                table: "Locations",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionManager2",
                table: "Locations",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowAddingArticlesToOrder",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowExtrasDuringValidation",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowOrderChangesAfterValidation",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowQuantityEdition",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowVariableDataEdition",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClientContact1",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientContact2",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostumerSupport1",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostumerSupport2",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFactory",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAuthorizationWorkflow",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableValidationWorkflow",
                table: "Projects",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FTPClients",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxExtras",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxExtrasPercentage",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantityPercentage",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SLADays",
                table: "Projects",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientContact1",
                table: "Companies",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientContact2",
                table: "Companies",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostumerSupport1",
                table: "Companies",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostumerSupport2",
                table: "Companies",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionManager1",
                table: "Companies",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionManager2",
                table: "Companies",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabelArtifacts",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    LabelID = table.Column<int>(nullable: false),
                    ConditionField = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelArtifacts", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LabelArtifacts_Labels_LabelID",
                        column: x => x.LabelID,
                        principalTable: "Labels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelArtifacts_LabelID",
                table: "LabelArtifacts",
                column: "LabelID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelArtifacts");

            migrationBuilder.AddColumn<string>(
                name: "GSTCity",
                table: "Locations",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GSTProvince",
                table: "Locations",
                maxLength: 6,
                nullable: true);

			migrationBuilder.AddColumn<int>(
				name: "GSTCountry",
				table: "Locations",
				nullable: true);

			migrationBuilder.RenameColumn(
				name: "FactoryCode",
				table: "Locations",
				newName: "LocationCode");

			migrationBuilder.DropColumn(
				name: "WorkingDays",
				table: "Locations");

			migrationBuilder.DropColumn(
				name: "Holidays",
				table: "Locations");

			migrationBuilder.DropColumn(
				name: "ProductionManager1",
				table: "Locations");

			migrationBuilder.DropColumn(
				name: "ProductionManager2",
				table: "Locations");

			migrationBuilder.DropColumn(
				name: "AllowAddingArticlesToOrder",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "AllowExtrasDuringValidation",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "AllowOrderChangesAfterValidation",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "AllowQuantityEdition",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "AllowVariableDataEdition",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "ClientContact1",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "ClientContact2",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "CostumerSupport1",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "CostumerSupport2",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "DefaultFactory",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "EnableAuthorizationWorkflow",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "EnableValidationWorkflow",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "FTPClients",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "MaxExtras",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "MaxExtrasPercentage",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "MaxQuantity",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "MaxQuantityPercentage",
				table: "Projects");

			migrationBuilder.DropColumn(
				name: "SLADays",
				table: "Projects");

			migrationBuilder.DropColumn(
                name: "ClientContact1",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ClientContact2",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CostumerSupport1",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CostumerSupport2",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ProductionManager1",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ProductionManager2",
                table: "Companies");
        }
    }
}

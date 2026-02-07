using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class UpdateOrderGroupWithSageInterface : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SageRef",
                table: "OrderGroups",
                newName: "SageReference");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedDate",
                table: "OrderGroups",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.AddColumn<int>(
                name: "CreditStatus",
                table: "OrderGroups",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "OrderGroups",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceStatus",
                table: "OrderGroups",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProjectPrefix",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredOn",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SageStatus",
                table: "OrderGroups",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOrderCancelled",
                table: "EmailServiceSettings",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("update OrderGroups SET CompletedDate = '0001-01-01 00:00:00.0000000' where CompletedDate is null");

            migrationBuilder.DropColumn(
                name: "CreditStatus",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "InvoiceStatus",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "ProjectPrefix",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "RegisteredOn",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "SageStatus",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "NotifyOrderCancelled",
                table: "EmailServiceSettings");

            migrationBuilder.RenameColumn(
                name: "SageReference",
                table: "OrderGroups",
                newName: "SageRef");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedDate",
                table: "OrderGroups",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class FTPFileWatcherServiceLastReadKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Server",
                table: "FtpLastReads",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "FtpLastReads",
                nullable: true);
            // fix for production database
            migrationBuilder.Sql("UPDATE [FtpLastReads] SET [Server] = 'extranet2.mango.es ', [UserName] = 'indet_spain' WHERE ID = 2 AND ProjectID = 46");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Server",
                table: "FtpLastReads");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "FtpLastReads");
        }
    }
}

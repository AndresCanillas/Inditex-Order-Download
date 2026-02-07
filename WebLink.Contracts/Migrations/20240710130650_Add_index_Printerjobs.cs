using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace WebLink.Contracts.Migrations
{
    public partial class Add_index_Printerjobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(
                @"CREATE NONCLUSTERED INDEX [IX_PrinterJob_ArticleID]
                ON [dbo].[PrinterJobs] ([ArticleId]) INCLUDE ([CompanyOrderID])"
            );
		}

		protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropIndex("dbo.PrinterJobs", "IX_PrinterJob_ArticleID");
		}
    }
}

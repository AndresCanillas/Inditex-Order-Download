using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class OrderLogIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"delete o
                                  from [dbo].[OrderLogs] o 
                                  left join [dbo].[CompanyOrders] c on c.id = o.OrderID
                                  where c.ID is null");

            migrationBuilder.Sql(@"ALTER TABLE [dbo].[OrderLogs]  WITH CHECK ADD  CONSTRAINT [FK_OrderLogs_CompanyOrders] FOREIGN KEY([OrderID])
                                REFERENCES [dbo].[CompanyOrders] ([ID])
                                ON DELETE CASCADE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE OrderLogs
                                   DROP CONSTRAINT  FK_OrderLogs_CompanyOrders");
        }
    }
}

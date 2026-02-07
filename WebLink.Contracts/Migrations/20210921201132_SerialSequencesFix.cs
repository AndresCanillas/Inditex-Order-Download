using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class SerialSequencesFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
				select ID, SubString(Filter, 1, 12) as Filter, max(NextValue) as NextValue into #tmps1 from SerialSequences 
				where len(Filter) between 12 and 13
				group by ID, SubString(Filter, 1, 12)

				update SerialSequences 
					set NextValue = t.NextValue
				from #tmps1 t 
				where 
					SerialSequences.ID = t.ID
					and substring(SerialSequences.Filter,1,12) = t.Filter

				delete from SerialSequences where len(filter) = 13

				drop table #tmps1
			");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

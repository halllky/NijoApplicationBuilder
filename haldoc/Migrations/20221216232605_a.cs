using Microsoft.EntityFrameworkCore.Migrations;

namespace haldoc.Migrations
{
    public partial class a : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "取引先",
                columns: table => new
                {
                    企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    企業名 = table.Column<string>(type: "TEXT", nullable: false),
                    てすと = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_取引先", x => x.企業ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "取引先");
        }
    }
}

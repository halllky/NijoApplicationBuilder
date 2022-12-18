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
                    企業名 = table.Column<string>(type: "TEXT", nullable: true),
                    重要度 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_取引先", x => x.企業ID);
                });

            migrationBuilder.CreateTable(
                name: "取引先支店",
                columns: table => new
                {
                    会社__企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    支店ID = table.Column<string>(type: "TEXT", nullable: false),
                    支店名 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_取引先支店", x => new { x.会社__企業ID, x.支店ID });
                });

            migrationBuilder.CreateTable(
                name: "営業所",
                columns: table => new
                {
                    営業所ID = table.Column<string>(type: "TEXT", nullable: false),
                    営業所名 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_営業所", x => x.営業所ID);
                });

            migrationBuilder.CreateTable(
                name: "担当者",
                columns: table => new
                {
                    ユーザーID = table.Column<string>(type: "TEXT", nullable: false),
                    氏名 = table.Column<string>(type: "TEXT", nullable: true),
                    所属__営業所ID = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_担当者", x => x.ユーザーID);
                });

            migrationBuilder.CreateTable(
                name: "請求情報",
                columns: table => new
                {
                    取引先__企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    宛名 = table.Column<string>(type: "TEXT", nullable: true),
                    住所__郵便番号 = table.Column<string>(type: "TEXT", nullable: true),
                    住所__都道府県 = table.Column<string>(type: "TEXT", nullable: true),
                    住所__市町村 = table.Column<string>(type: "TEXT", nullable: true),
                    住所__丁番地 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_請求情報", x => x.取引先__企業ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "取引先");

            migrationBuilder.DropTable(
                name: "取引先支店");

            migrationBuilder.DropTable(
                name: "営業所");

            migrationBuilder.DropTable(
                name: "担当者");

            migrationBuilder.DropTable(
                name: "請求情報");
        }
    }
}

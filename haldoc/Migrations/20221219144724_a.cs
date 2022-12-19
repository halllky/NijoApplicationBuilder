using Microsoft.EntityFrameworkCore.Migrations;

namespace haldoc.Migrations
{
    public partial class a : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "コメント",
                columns: table => new
                {
                    企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    At = table.Column<string>(type: "TEXT", nullable: true),
                    By__ユーザーID = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_コメント", x => x.企業ID);
                });

            migrationBuilder.CreateTable(
                name: "上場企業資本情報",
                columns: table => new
                {
                    企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    自己資本比率 = table.Column<decimal>(type: "TEXT", nullable: false),
                    利益率 = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_上場企業資本情報", x => x.企業ID);
                });

            migrationBuilder.CreateTable(
                name: "取引先",
                columns: table => new
                {
                    企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    企業名 = table.Column<string>(type: "TEXT", nullable: true),
                    重要度 = table.Column<int>(type: "INTEGER", nullable: true),
                    資本情報 = table.Column<int>(type: "INTEGER", nullable: true)
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
                    支店連番 = table.Column<string>(type: "TEXT", nullable: false),
                    支店名 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_取引先支店", x => new { x.会社__企業ID, x.支店連番 });
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
                    宛名 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_請求情報", x => x.取引先__企業ID);
                });

            migrationBuilder.CreateTable(
                name: "非上場企業資本情報",
                columns: table => new
                {
                    企業ID = table.Column<string>(type: "TEXT", nullable: false),
                    主要株主 = table.Column<string>(type: "TEXT", nullable: true),
                    安定性 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_非上場企業資本情報", x => x.企業ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "コメント");

            migrationBuilder.DropTable(
                name: "上場企業資本情報");

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

            migrationBuilder.DropTable(
                name: "非上場企業資本情報");
        }
    }
}

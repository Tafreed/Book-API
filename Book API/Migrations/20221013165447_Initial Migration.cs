using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Book_API.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    BookId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    YearOfPublish = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.BookId);
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "BookId", "Author", "Title", "YearOfPublish" },
                values: new object[] { 1, "Daniel Dafoe", "Robinson Crusoe", 1719 });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "BookId", "Author", "Title", "YearOfPublish" },
                values: new object[] { 2, "Charles Dickens", "A Tale of Two Cities", 1895 });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "BookId", "Author", "Title", "YearOfPublish" },
                values: new object[] { 3, "H.G. Wells", "The Invisible Man", 1897 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}

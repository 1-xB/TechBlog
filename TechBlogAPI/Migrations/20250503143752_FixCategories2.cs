using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechBlogAPI.Migrations
{
    public partial class FixCategories2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń ograniczenie klucza obcego w tabeli PostCategory
            migrationBuilder.DropForeignKey(
                name: "FK_PostCategory_Categories_CategoryId",
                table: "PostCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Category",
                table: "Category");

            migrationBuilder.RenameTable(
                name: "Category",
                newName: "Categories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "CategoryId");

            // Przywróć ograniczenie klucza obcego, wskazując na nową nazwę tabeli
            migrationBuilder.AddForeignKey(
                name: "FK_PostCategory_Categories_CategoryId",
                table: "PostCategory",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostCategory_Categories_CategoryId",
                table: "PostCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Category");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Category",
                table: "Category",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostCategory_Categories_CategoryId",
                table: "PostCategory",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
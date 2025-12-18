using Microsoft.EntityFrameworkCore.Migrations;

namespace Data.Context.Migrations
{
    /// <summary>
    /// Migration to add hierarchical support to Categories table.
    /// Adds ParentId column and self-referencing foreign key constraint.
    /// </summary>
    public partial class AddCategoryHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ParentId column to Categories table
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                schema: "dbo",
                table: "Categories",
                nullable: true);

            // Add foreign key constraint for self-referencing relationship
            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                schema: "dbo",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_ParentCategory",
                schema: "dbo",
                table: "Categories",
                column: "ParentId",
                principalSchema: "dbo",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_ParentCategory",
                schema: "dbo",
                table: "Categories");

            // Remove index
            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentId",
                schema: "dbo",
                table: "Categories");

            // Remove ParentId column
            migrationBuilder.DropColumn(
                name: "ParentId",
                schema: "dbo",
                table: "Categories");
        }
    }
}


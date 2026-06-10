using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MindedExample.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds global admin support, legal owner tracking, and tenant join request workflow.
    /// </summary>
    public partial class AddTenantGovernanceAndJoinRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants",
                schema: "dbo",
                table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                schema: "dbo",
                table: "Users",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobalAdmin",
                schema: "dbo",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LegalOwnerUserId",
                schema: "dbo",
                table: "Tenants",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantJoinRequests",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", nullable: false),
                    Surname = table.Column<string>(type: "varchar(100)", nullable: false),
                    Email = table.Column<string>(type: "varchar(250)", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(500)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(nullable: true),
                    Approved = table.Column<bool>(nullable: false),
                    ProcessedByUserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantJoinRequests_Tenants",
                        column: x => x.TenantId,
                        principalSchema: "dbo",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantJoinRequests_ProcessedByUser",
                        column: x => x.ProcessedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_LegalOwnerUserId",
                schema: "dbo",
                table: "Tenants",
                column: "LegalOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantJoinRequests_Tenant_Email_Processed",
                schema: "dbo",
                table: "TenantJoinRequests",
                columns: new[] { "TenantId", "Email", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantJoinRequests_ProcessedByUserId",
                schema: "dbo",
                table: "TenantJoinRequests",
                column: "ProcessedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_LegalOwnerUser",
                schema: "dbo",
                table: "Tenants",
                column: "LegalOwnerUserId",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants",
                schema: "dbo",
                table: "Users",
                column: "TenantId",
                principalSchema: "dbo",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Tenants_LegalOwnerUser", schema: "dbo", table: "Tenants");
            migrationBuilder.DropForeignKey(name: "FK_Users_Tenants", schema: "dbo", table: "Users");

            migrationBuilder.DropTable(name: "TenantJoinRequests", schema: "dbo");

            migrationBuilder.DropIndex(name: "IX_Tenants_LegalOwnerUserId", schema: "dbo", table: "Tenants");

            migrationBuilder.DropColumn(name: "LegalOwnerUserId", schema: "dbo", table: "Tenants");
            migrationBuilder.DropColumn(name: "IsGlobalAdmin", schema: "dbo", table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                schema: "dbo",
                table: "Users",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants",
                schema: "dbo",
                table: "Users",
                column: "TenantId",
                principalSchema: "dbo",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

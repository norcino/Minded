using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MindedExample.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds tenant-aware identity and onboarding entities.
    /// </summary>
    public partial class AddTenantAndAuth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(200)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [dbo].[Tenants] WHERE [Name] = 'Default Tenant') INSERT INTO [dbo].[Tenants] ([Name]) VALUES ('Default Tenant')");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                schema: "dbo",
                table: "Users",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TenantRole",
                schema: "dbo",
                table: "Users",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "Member");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "dbo",
                table: "Users",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "dbo",
                table: "Users",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                schema: "dbo",
                table: "Users",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants",
                schema: "dbo",
                table: "Users",
                column: "TenantId",
                principalSchema: "dbo",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.UserRoles', 'TenantId') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserRoles] ADD [TenantId] INT NOT NULL CONSTRAINT [DF_UserRoles_TenantId] DEFAULT 1;
    ALTER TABLE [dbo].[UserRoles] DROP CONSTRAINT [PK_UserRoles];
    ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [PK_UserRoles] PRIMARY KEY ([TenantId], [UserId], [RoleName]);
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.RolePermissions', 'TenantId') IS NULL
BEGIN
    ALTER TABLE [dbo].[RolePermissions] ADD [TenantId] INT NOT NULL CONSTRAINT [DF_RolePermissions_TenantId] DEFAULT 1;
    ALTER TABLE [dbo].[RolePermissions] DROP CONSTRAINT [PK_RolePermissions];
    ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([TenantId], [RoleName], [PermissionName]);
END");

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    Token = table.Column<string>(type: "varchar(200)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(nullable: false),
                    UsedAtUtc = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantInvites",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(nullable: false),
                    CreatedByUserId = table.Column<int>(nullable: false),
                    Email = table.Column<string>(type: "varchar(250)", nullable: true),
                    Code = table.Column<string>(type: "varchar(32)", nullable: false),
                    Token = table.Column<string>(type: "varchar(200)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(nullable: false),
                    UsedAtUtc = table.Column<DateTime>(nullable: true),
                    UsedByUserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvites_Tenants",
                        column: x => x.TenantId,
                        principalSchema: "dbo",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantInvites_CreatedByUser",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantInvites_UsedByUser",
                        column: x => x.UsedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                schema: "dbo",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                schema: "dbo",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_Code",
                schema: "dbo",
                table: "TenantInvites",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_Token",
                schema: "dbo",
                table: "TenantInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_TenantId",
                schema: "dbo",
                table: "TenantInvites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_CreatedByUserId",
                schema: "dbo",
                table: "TenantInvites",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_UsedByUserId",
                schema: "dbo",
                table: "TenantInvites",
                column: "UsedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TenantInvites", schema: "dbo");
            migrationBuilder.DropTable(name: "PasswordResetTokens", schema: "dbo");

            migrationBuilder.DropForeignKey(name: "FK_Users_Tenants", schema: "dbo", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_TenantId", schema: "dbo", table: "Users");

            migrationBuilder.DropColumn(name: "TenantId", schema: "dbo", table: "Users");
            migrationBuilder.DropColumn(name: "TenantRole", schema: "dbo", table: "Users");
            migrationBuilder.DropColumn(name: "PasswordHash", schema: "dbo", table: "Users");
            migrationBuilder.DropColumn(name: "IsActive", schema: "dbo", table: "Users");

            migrationBuilder.DropTable(name: "Tenants", schema: "dbo");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ProductComparison.Data.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompareList",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StoreId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompareList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompareListItem",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CompareListId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CategoryId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConfigurationKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompareListItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompareListItem_CompareList_CompareListId",
                        column: x => x.CompareListId,
                        principalTable: "CompareList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompareListItemSection",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CompareListItemId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    CustomText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompareListItemSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompareListItemSection_CompareListItem_CompareListItemId",
                        column: x => x.CompareListItemId,
                        principalTable: "CompareListItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompareList_IsAnonymous_ModifiedDate",
                table: "CompareList",
                columns: new[] { "IsAnonymous", "ModifiedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CompareList_UserId",
                table: "CompareList",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompareList_UserId_StoreId_OrganizationId",
                table: "CompareList",
                columns: new[] { "UserId", "StoreId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompareListItem_CompareListId_ProductId_ConfigurationKey",
                table: "CompareListItem",
                columns: new[] { "CompareListId", "ProductId", "ConfigurationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompareListItem_ProductId",
                table: "CompareListItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CompareListItemSection_CompareListItemId",
                table: "CompareListItemSection",
                column: "CompareListItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompareListItemSection");

            migrationBuilder.DropTable(
                name: "CompareListItem");

            migrationBuilder.DropTable(
                name: "CompareList");
        }
    }
}

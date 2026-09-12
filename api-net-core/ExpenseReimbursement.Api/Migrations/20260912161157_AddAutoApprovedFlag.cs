using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseReimbursement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoApprovedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoApproved",
                table: "ExpenseRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoApproved",
                table: "ExpenseRequests");
        }
    }
}

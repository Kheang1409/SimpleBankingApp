using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleBankingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTypeCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Account_AccountType",
                table: "Accounts",
                sql: "AccountType IN ('Savings', 'Checking', 'Business')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Account_Balance",
                table: "Accounts",
                sql: "Balance >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Account_AccountType",
                table: "Accounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Account_Balance",
                table: "Accounts");
        }
    }
}

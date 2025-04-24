using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeownersSubdivision.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountHolderName",
                table: "PaymentMethods",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "PaymentMethods",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "PaymentMethods",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingCountry",
                table: "PaymentMethods",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingState",
                table: "PaymentMethods",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BillingZip",
                table: "PaymentMethods",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                table: "PaymentMethods",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CardLastFour",
                table: "PaymentMethods",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "PaymentMethods",
                type: "varchar(19)",
                maxLength: 19,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Cvv",
                table: "PaymentMethods",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExpirationMonth",
                table: "PaymentMethods",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExpirationYear",
                table: "PaymentMethods",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NickName",
                table: "PaymentMethods",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountHolderName",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BillingCountry",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BillingState",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "BillingZip",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "CardHolderName",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "CardLastFour",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "Cvv",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "ExpirationMonth",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "ExpirationYear",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "NickName",
                table: "PaymentMethods");
        }
    }
}

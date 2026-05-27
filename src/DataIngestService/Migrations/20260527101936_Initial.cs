using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataIngestService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    external_transaction_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    transaction_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    source_channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ingested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_customer_id_transaction_date",
                table: "transactions",
                columns: new[] { "customer_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_source_channel_transaction_date",
                table: "transactions",
                columns: new[] { "source_channel", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "ux_transactions_deduplication_key",
                table: "transactions",
                column: "deduplication_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transactions");
        }
    }
}

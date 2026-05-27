using DataIngestService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataIngestService.Data.Configurations;

public class TransactionEntityConfiguration : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.CustomerId)
            .HasColumnName("customer_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(transaction => transaction.ExternalTransactionId)
            .HasColumnName("external_transaction_id")
            .HasMaxLength(128);

        builder.Property(transaction => transaction.TransactionDate)
            .HasColumnName("transaction_date")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(transaction => transaction.SourceChannel)
            .HasColumnName("source_channel")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(transaction => transaction.DeduplicationKey)
            .HasColumnName("deduplication_key")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(transaction => transaction.IngestedAt)
            .HasColumnName("ingested_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(transaction => transaction.DeduplicationKey)
            .IsUnique()
            .HasDatabaseName("ux_transactions_deduplication_key");

        builder.HasIndex(transaction => new { transaction.CustomerId, transaction.TransactionDate })
            .HasDatabaseName("ix_transactions_customer_id_transaction_date");

        builder.HasIndex(transaction => new { transaction.SourceChannel, transaction.TransactionDate })
            .HasDatabaseName("ix_transactions_source_channel_transaction_date");
    }
}

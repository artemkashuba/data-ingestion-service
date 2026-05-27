namespace DataIngestService.Services.Transactions;

public static class CsvTransactionHeaders
{
    public static readonly string[] CustomerId =
    [
        "customerid",
        "customer_id",
        "customeridentifier",
        "customer_identifier"
    ];

    public static readonly string[] ExternalTransactionId =
    [
        "externaltransactionid",
        "external_transaction_id",
        "transactionid",
        "transaction_id"
    ];

    public static readonly string[] TransactionDate =
    [
        "transactiondate",
        "transaction_date",
        "date"
    ];

    public static readonly string[] Amount = ["amount"];

    public static readonly string[] Currency = ["currency"];

    public static readonly string[] SourceChannel =
    [
        "sourcechannel",
        "source_channel",
        "channel"
    ];
}

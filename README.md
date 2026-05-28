# Data Ingestion Service

.NET 8 Web API for ingesting customer transaction data from both real-time JSON requests and daily CSV batch uploads. Data is validated, deduplicated, stored in PostgreSQL, and exposed through query and summary endpoints.

## Tech Stack

- .NET 8 Web API with classic controllers
- PostgreSQL
- Entity Framework Core with Npgsql
- CsvHelper for CSV parsing
- xUnit for unit tests
- Docker and Docker Compose

## Run With Docker

From the repository root:

```bash
docker compose up --build
```

The API will be available at:

```text
http://localhost:8080
```

Swagger UI:

```text
http://localhost:8080/swagger
```

PostgreSQL is exposed locally on:

```text
localhost:5432
```

Database credentials:

```text
Database: data_ingest
Username: postgres
Password: postgres
```

The API applies EF Core migrations on startup in Docker through:

```text
Database__ApplyMigrationsOnStartup=true
```

This keeps the assignment runnable with a single `docker compose up --build`. In a production system, migrations would usually run as a separate deployment step or one-off job.

## Run API Locally With PostgreSQL In Docker

Start only PostgreSQL:

```bash
docker compose up -d postgres
```

Run the API locally:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/DataIngestService/DataIngestService.csproj --launch-profile http
```

The local HTTP profile uses:

```text
http://localhost:5279
```

## Configuration

Main settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=data_ingest;Username=postgres;Password=postgres"
  },
  "Database": {
    "ApplyMigrationsOnStartup": true
  },
  "Ingestion": {
    "BatchSize": 1000
  }
}
```

`Ingestion:BatchSize` controls how many accepted CSV rows are checked and saved per database batch.

Environment variable example:

```bash
Ingestion__BatchSize=500 dotnet run --project src/DataIngestService/DataIngestService.csproj
```

## API Endpoints

### Real-Time Transaction Ingestion

```http
POST /api/ingest/transaction
```

Example:

```bash
curl -i -X POST http://localhost:8080/api/ingest/transaction \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-1",
    "transactionDate": "2026-05-27T10:15:00Z",
    "amount": 19.99,
    "currency": "USD",
    "sourceChannel": "web"
  }'
```

Duplicate transactions return `409 Conflict`.

### Batch CSV Ingestion

```http
POST /api/ingest/batch
```

Example:

```bash
curl -X POST http://localhost:8080/api/ingest/batch \
  -F "file=@transactions.csv"
```

A larger sample file is included for manual testing:

```bash
curl -X POST http://localhost:8080/api/ingest/batch \
  -F "file=@samples/transactions-10k.csv"
```

The sample contains 10,000 data rows: 9,500 valid unique rows, 300 duplicate rows, and 200 invalid rows.

Example CSV:

```csv
customerId,transactionDate,amount,currency,sourceChannel,externalTransactionId
customer-1,2026-05-27T10:00:00Z,19.99,USD,web,
customer-2,2026-05-27T10:05:00Z,29.99,USD,email,txn-123
```

Supported header aliases include:

```text
customerId, customer_id, customer identifier
transactionDate, transaction_date, transaction date
amount
currency
sourceChannel, source_channel, source channel
externalTransactionId, external_transaction_id, transactionId, transaction_id
```

The response includes total rows, accepted rows, rejected rows, duplicate rows, and per-row errors.

### Customer Transactions Query

```http
GET /customers/{customerId}/transactions
```

Query parameters:

```text
page
pageSize
from
to
currency
sourceChannel
```

Example:

```bash
curl "http://localhost:8080/customers/customer-1/transactions?page=1&pageSize=20&currency=USD&sourceChannel=web"
```

Transactions are returned newest first. Default page size is `50`, max page size is `200`.

### Summary Stats

```http
GET /stats/summary
```

Returns:

- total transactions
- total unique customers
- total amount
- breakdown by currency
- breakdown by source channel
- min and max transaction date

Example:

```bash
curl http://localhost:8080/stats/summary
```

## Validation And Deduplication

Validation is centralized in `TransactionValidator` and reused by both real-time and batch ingestion.

Current validation rules:

- customer id is required and limited to 128 characters
- transaction date is required and cannot be more than 5 minutes in the future
- amount must be greater than zero
- currency must be a 3-letter code
- source channel is required and limited to 64 characters
- external transaction id is optional and limited to 128 characters

Deduplication is based on a normalized deduplication key and enforced by a unique PostgreSQL index.

If `externalTransactionId` is present:

```text
external|{sourceChannel}|{externalTransactionId}
```

Otherwise:

```text
composite|{customerId}|{transactionDateUtc}|{amount}|{currency}|{sourceChannel}
```

The application checks for duplicates before insert for useful responses, while PostgreSQL remains the final guard through the unique index.

## Architecture

The API uses classic ASP.NET Core controllers because the assignment has multiple request and response shapes and controllers keep routing easy to scan.

Main areas:

```text
Controllers/
  IngestController.cs
  CustomersController.cs
  StatsController.cs

Contracts/
  Request and response DTOs

Data/
  EF Core DbContext, entities, and configurations

Services/
  Transactions/
    real-time ingestion
    batch CSV ingestion
    validation
    fingerprinting
  Customers/
    customer transaction queries
  Stats/
    aggregate summary queries

Infrastructure/
  Configuration/
    centralized configuration keys
  ExceptionHandling/
    global ProblemDetails exception handler
```

Controllers are intentionally thin. Business logic lives in services so it can be tested without ASP.NET plumbing.

## Error Handling

Expected API outcomes are handled explicitly:

- validation errors return `400 Bad Request`
- duplicates return `409 Conflict`
- missing records return `404 Not Found`

Unexpected exceptions are handled by a global exception handler and returned as `500 ProblemDetails` with a `traceId`.

## Tests

Run all tests:

```bash
dotnet test tests/DataIngestService.Tests/DataIngestService.Tests.csproj
```

The test suite covers:

- transaction validation
- deduplication key generation
- real-time ingestion
- batch CSV ingestion
- customer transaction query filtering and pagination
- summary statistics

## Trade-Offs

- Batch ingestion streams CSV rows and saves accepted rows in configurable chunks. This is enough for the expected 100K rows without adding background jobs.
- The batch endpoint returns all row errors. For much larger files, this could be capped or written to an external error report.
- Mixed-currency `totalAmount` is returned as a raw aggregate because the assignment asks for it. The breakdown by currency is the more meaningful financial view.
- Startup migrations are enabled in Docker to satisfy the single-command run requirement. For production, migrations should be run separately.
- Deduplication without a provider transaction id is heuristic. Two identical purchases at the same time could be treated as duplicates.

## What I Would Improve With More Time

- Add integration tests against a real PostgreSQL container.
- Add request/response examples directly into Swagger.
- Add structured logs around batch ingestion summaries.
- Add CSV upload limits and a configurable maximum error count.
- Move long-running batch ingestion to a background job if files grow beyond the assignment scope.
- Add authentication/authorization if this were exposed beyond a local assignment environment.
- Add another validation for currency. Accept only real ones

## AI Usage

CODEX
AI tools were used as a coding collaborator for planning, implementation support, and review of trade-offs.

Accepted with modification:

- initial project structure suggestions
- service and controller separation
- README wording and endpoint examples

Written and reviewed directly:

- validation rules
- deduplication behavior
- EF Core schema and indexes
- CSV ingestion behavior
- tests and Docker configuration

Things caught and corrected during the process:

- switched to usage of IOptions for some services
- top level statements were ignored and followed the old approach
- AI implemented automigration step on startup, I made it feature-flag based instead
- a handwritten EF migration was not discoverable by EF tooling, so it was removed and regenerated with `dotnet ef`
- Swagger does not support a bare `[FromForm] IFormFile`, so the upload was wrapped in a request DTO
- validation attributes were removed from request DTOs so `TransactionValidator` is the single validation path

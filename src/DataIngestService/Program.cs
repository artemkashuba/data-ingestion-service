using DataIngestService.Data;
using DataIngestService.Infrastructure.Configuration;
using DataIngestService.Infrastructure.ExceptionHandling;
using DataIngestService.Services.Customers;
using DataIngestService.Services.Stats;
using DataIngestService.Services.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString(ConfigurationKeys.ConnectionStrings.DefaultConnection)
                               ?? throw new InvalidOperationException(
                                   $"Connection string '{ConfigurationKeys.ConnectionStrings.DefaultConnection}' is not configured.");

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection(ConfigurationKeys.Sections.Ingestion));
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ITransactionFingerprintService, TransactionFingerprintService>();
        builder.Services.AddScoped<ITransactionValidator, TransactionValidator>();
        builder.Services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();
        builder.Services.AddScoped<IBatchTransactionIngestionService, BatchTransactionIngestionService>();
        builder.Services.AddScoped<ICustomerTransactionQueryService, CustomerTransactionQueryService>();
        builder.Services.AddScoped<IStatsSummaryService, StatsSummaryService>();

        builder.Services.AddAuthorization();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseAuthorization();
        app.MapControllers();

        await ApplyMigration(builder, app);
        await app.RunAsync();
    }

    private static async Task ApplyMigration(WebApplicationBuilder builder, WebApplication app)
    {
        if (builder.Configuration.GetValue<bool>(ConfigurationKeys.Database.ApplyMigrationsOnStartup))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}

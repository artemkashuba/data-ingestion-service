using DataIngestService.Data;
using DataIngestService.Services.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection(IngestionOptions.SectionName));
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ITransactionFingerprintService, TransactionFingerprintService>();
        builder.Services.AddScoped<ITransactionValidator, TransactionValidator>();
        builder.Services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();
        builder.Services.AddScoped<IBatchTransactionIngestionService, BatchTransactionIngestionService>();
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await app.RunAsync();
    }
}

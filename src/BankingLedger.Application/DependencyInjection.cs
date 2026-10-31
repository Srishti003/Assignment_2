using BankingLedger.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BankingLedger.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register application services
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransferService, TransferService>();

        return services;
    }
}

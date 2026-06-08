using LifeRPG.App.Controls;
using LifeRPG.Core.Rpg;
using LifeRPG.Data.Repositories;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LifeRPG.App.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddLifeRpgServices(this IServiceCollection services)
    {
        services.AddSingleton<RpgEngine>();
        services.AddSingleton<SqliteTransactionRunner>();
        services.AddSingleton<DatabaseMigrationService>();

        services.AddTransient<LifeRecordRepository>();
        services.AddTransient<LifeRecordImageRepository>();
        services.AddTransient<ActionTaskRepository>();
        services.AddTransient<ActionTaskImageRepository>();
        services.AddTransient<StatisticsRepository>();
        services.AddTransient<CharacterRepository>();
        services.AddTransient<EquipmentRepository>();
        services.AddTransient<SkillRepository>();
        services.AddTransient<NpcRepository>();
        services.AddTransient<MapRepository>();

        services.AddSingleton<ImageStorageService>();
        services.AddSingleton<ActionTaskImageStorageService>();
        services.AddSingleton<MarkdownExportService>();
        services.AddSingleton<BackupService>();
        services.AddTransient<RpgProgressionService>();

        services.AddTransient<MainWindow>();
        services.AddTransient<ActionTaskControl>();
        services.AddTransient<CharacterDashboardControl>();
        services.AddTransient<SystemStatusControl>();

        return services;
    }
}

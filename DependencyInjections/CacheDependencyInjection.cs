using System;
using Cache.Contract.Interfaces;
using Cache.Infrastructure.CacheService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;


namespace DependencyInjections;

public static class CacheDependencyInjection
{
    public static IServiceCollection AddCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection("Redis").Get<Redis>();

        // 1. Redis disabled → fallback immediately
        if (redisOptions?.Enabled != true)
        {
            Console.WriteLine("ℹ️ Redis disabled → using CacheDisabled");
            services.AddSingleton<ICache, CacheDisabled>();
            return services;
        }

        try
        {
            var config = ConfigurationOptions.Parse(
                redisOptions.ConnectionString ?? "localhost:6379"
            );

            config.AbortOnConnectFail = false;
            config.ConnectRetry = 3;
            config.SyncTimeout = 3000;

            // 🔥 Try connect BEFORE registering DI
            var mux = ConnectionMultiplexer.Connect(config);

            if (!mux.IsConnected)
                throw new Exception("Redis not connected");

            // ✅ Redis OK → register real cache
            services.AddSingleton<IConnectionMultiplexer>(mux);
            services.AddSingleton<ICache, CacheEnabled>();

            Console.WriteLine("✅ Redis connected → CacheEnabled active");
        }
        catch (Exception ex)
        {
            // ❗ Any failure → fallback safely
            Console.WriteLine("⚠️ Redis unavailable → using CacheDisabled");
            Console.WriteLine(ex.Message);

            services.AddSingleton<ICache, CacheDisabled>();
        }

        return services;
    }
}
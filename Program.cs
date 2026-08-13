using System.Net;
using Host.Middlewares;
using LicenseService.Data;
using LicenseService.Exceptions;
using LicenseService.Middlewares;
using LicenseService.Model;
using LicenseService.Service;
using LicenseService.Service.Impl;
using LicenseService.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection"),
        npgsqlOptions => npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        ));

builder.Services.Configure<AppConfigSetting>(
    builder.Configuration.GetSection("AppSettings")
    );

// Register redis 
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddSingleton(sp =>
{
    var mux = sp.GetRequiredService<IConnectionMultiplexer>();
    return mux.GetDatabase();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowSpecificOrigins",
//         builder =>
//         {
//             builder.WithOrigins("https://example.com", "https://www.example.com")
//                    .AllowAnyHeader()
//                    .AllowAnyMethod();
//         });
// });
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true; // optional
});

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .WriteTo.Console();
});

builder.Services.AddHostedService<KeyRotationWorker>();
builder.Services.AddScoped<ILicenseService, LicensingService>();
builder.Services.AddScoped<IKeyRotateService, KeyRotateService>();
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddTransient<GlobalException>();

builder.Services.AddControllers(options =>
        {
            options.Filters.Add<ApiResponseFilter>();
        }).ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                 .Where(x => x.Value?.Errors.Count > 0)
                 .ToDictionary(
                     x => x.Key,
                     x => x.Value!.Errors
                         .Select(e => e.ErrorMessage)
                         .ToArray()
                 );

                return new BadRequestObjectResult(
                    new ValidateBaseResponse<object>(
                        DateTime.UtcNow,
                        HttpStatusCode.BadRequest,
                        false,
                        "Validation failed.",
                        Errors: errors
                    )
                );
            };
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalException>();


app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();
// This logs every HTTP request automatically
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();


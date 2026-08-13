using System;
using LicenseService.Entities;
using LicenseService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LicenseService.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string Schema = "license";
    public DbSet<SignKeyAudit> sign_key { get; set; }
    public DbSet<License> license { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // ⭐ Module schema
        modelBuilder.HasDefaultSchema(Schema);

        // Make default datetime now
        var isSqlServer = Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";
        var isPostgres = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";

        string utcNowSql;
        string utcNowAdd1YearSql;
        string guidSql;

        if (isSqlServer)
        {
            utcNowSql = "GETUTCDATE()";
            utcNowAdd1YearSql = "DATEADD(year, 1, GETUTCDATE())";
            guidSql = "NEWSEQUENTIALID()";
        }
        else if (isPostgres)
        {
            utcNowSql = "NOW() AT TIME ZONE 'UTC'";
            utcNowAdd1YearSql = "(NOW() AT TIME ZONE 'UTC') + INTERVAL '1 year'";
            guidSql = "gen_random_uuid()";
        }
        else
        {
            throw new NotSupportedException($"Unsupported database provider: {Database.ProviderName}");
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {

            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                      .Property(nameof(BaseEntity.created_at))
                      .HasDefaultValueSql(utcNowSql)
                      .ValueGeneratedOnAdd();

                modelBuilder.Entity(entityType.ClrType)
                      .Property(nameof(BaseEntity.expire_at))
                      .HasDefaultValueSql(utcNowAdd1YearSql)
                      .ValueGeneratedOnAdd();

                modelBuilder.Entity(entityType.ClrType)
                      .Property(nameof(BaseEntity.guid))
                      .HasDefaultValueSql(guidSql)
                      .ValueGeneratedOnAdd();
            }

        }

        modelBuilder.Entity<SignKeyAudit>()
            .Property(e => e.sign_pub)
            .HasColumnType("bytea");

        modelBuilder.Entity<SignKeyAudit>()
            .Property(e => e.sign_priv)
            .HasColumnType("bytea");

        modelBuilder.Entity<License>()
            .Property(e => e.license)
            .HasColumnType("bytea");



        // put this inside OnModelCreating(ModelBuilder modelBuilder)
        // var datetimeInterface = typeof(IDatetime);

        // foreach (var et in modelBuilder.Model.GetEntityTypes()
        //              .Where(t => t.ClrType != null && datetimeInterface.IsAssignableFrom(t.ClrType)))
        // {
        //   // get the builder for the concrete CLR type (e.g. location, ArEvent, ...)
        //   var builder = modelBuilder.Entity(et.ClrType);

        //   // configure created_date
        //   builder.Property<DateTime>(nameof(IDatetime.created_date))
        //       .HasColumnType("timestamp without time zone")
        //       .HasDefaultValueSql("now()")
        //       .ValueGeneratedOnAdd();

        //   builder.Property<DateTime>(nameof(IDatetime.expire_date))
        //       .HasColumnType("timestamp without time zone")
        //       .HasDefaultValueSql("now()")
        //       .ValueGeneratedOnAdd();

        // }

        modelBuilder.Entity<SignKeyAudit>()
            .HasMany(k => k.licenses)
            .WithOne(s => s.sign_key)
            .HasForeignKey(s => s.sign_key_guid)
            .HasPrincipalKey(k => k.guid)
            .OnDelete(DeleteBehavior.Cascade);

    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LicenseService.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "license");

            migrationBuilder.CreateTable(
                name: "sign_key",
                schema: "license",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sign_pub = table.Column<byte[]>(type: "bytea", nullable: false),
                    sign_priv = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    expire_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "(NOW() AT TIME ZONE 'UTC') + INTERVAL '1 year'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sign_key", x => x.id);
                    table.UniqueConstraint("AK_sign_key_guid", x => x.guid);
                });

            migrationBuilder.CreateTable(
                name: "license",
                schema: "license",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company = table.Column<string>(type: "text", nullable: false),
                    customer_site = table.Column<string>(type: "text", nullable: false),
                    machine_id = table.Column<string>(type: "text", nullable: false),
                    license = table.Column<byte[]>(type: "bytea", nullable: false),
                    license_type = table.Column<int>(type: "integer", nullable: false),
                    sign_key_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    expire_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "(NOW() AT TIME ZONE 'UTC') + INTERVAL '1 year'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_license", x => x.id);
                    table.ForeignKey(
                        name: "FK_license_sign_key_sign_key_guid",
                        column: x => x.sign_key_guid,
                        principalSchema: "license",
                        principalTable: "sign_key",
                        principalColumn: "guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_license_sign_key_guid",
                schema: "license",
                table: "license",
                column: "sign_key_guid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "license",
                schema: "license");

            migrationBuilder.DropTable(
                name: "sign_key",
                schema: "license");
        }
    }
}

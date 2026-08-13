using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseService.Data;
using LicenseService.Entities;
using LicenseService.Helper;
using LicenseService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LicenseService.Service.Impl;

public class LicensingService(IOptions<AppConfigSetting> options, AppDbContext context, IDatabase redis) : ILicenseService
{
  private readonly AppConfigSetting _settings = options.Value;
  private readonly JsonSerializerOptions jopts = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public Task<EncryptedLicense> CreateLicenseAsync(LicensePayload payload)
  {
    throw new NotImplementedException();
  }

  public async Task<BaseDto<EncryptedLicense>> CreateLicenseDemoAsync(GenerateDemo request)
  {
    // Step 1 : Check and get DH Key from redis or database
    var authSession = await redis.StringGetAsync(request.sessionId);
    if (authSession.IsNullOrEmpty) return new BaseDto<EncryptedLicense>(System.Net.HttpStatusCode.Unauthorized, null, Guid.NewGuid(), "Invalid session", DateTime.UtcNow.ToLocalTime());

    var authJson = JsonSerializer.Deserialize<AuthSession>(authSession.ToString(), jopts);

    if (authJson is null) return new BaseDto<EncryptedLicense>(System.Net.HttpStatusCode.Unauthorized, null, Guid.NewGuid(), "Invalid session data", DateTime.UtcNow.ToLocalTime());

    if (authJson.expiresAt < DateTime.UtcNow) return new BaseDto<EncryptedLicense>(System.Net.HttpStatusCode.Unauthorized, null, Guid.NewGuid(), "Session expired", DateTime.UtcNow.ToLocalTime());

    // Step 2 : Checking demo license availability
    var isAvailable = await context.license.AsNoTracking().AnyAsync(x => x.machine_id.Equals(request.machineId));

    if (isAvailable) return new BaseDto<EncryptedLicense>(System.Net.HttpStatusCode.BadRequest, null, Guid.NewGuid(), "Demo license already exists", DateTime.UtcNow.ToLocalTime());

    // Step 3 : Get demo license details from settings

    var payload = new LicensePayload(
      Guid.NewGuid(),
      request.company,
      request.machineId,
      _settings.DemoLicense.nHardware,
      _settings.DemoLicense.nModule,
      _settings.DemoLicense.nOperator,
      _settings.DemoLicense.nLocation,
      _settings.DemoLicense.nControl,
      _settings.DemoLicense.nMonitor,
      _settings.DemoLicense.nMonitorGroup,
      _settings.DemoLicense.nDoor,
      _settings.DemoLicense.nAccessLevle,
      _settings.DemoLicense.nTimezone,
      _settings.DemoLicense.nCard,
      _settings.DemoLicense.nCardHolder,
      _settings.DemoLicense.nTrigger,
      _settings.DemoLicense.nHoliday,
      DateTime.Now,
      DateTime.Now.AddDays(_settings.DemoLicense.DurationInDays), // Demo license valid days based on nHardware count
      _settings.DemoLicense.GracePeriodInDays,
      false
    );

    var json = JsonSerializer.Serialize(payload);
    var data = Encoding.UTF8.GetBytes(json);

    // Step 4 : Get Signer from database
    var sign = await context.sign_key
    .AsNoTracking()
    .OrderByDescending(s => s.created_at)
    .FirstOrDefaultAsync(s => s.is_revoked == false);

    if (sign is null)
    {
      return new BaseDto<EncryptedLicense>(
            System.Net.HttpStatusCode.InternalServerError,
            null,
            Guid.NewGuid(),
            "No valid signing key found",
            DateTime.UtcNow.ToLocalTime()
      );
    }

    var serverSignPri = sign.sign_priv;
    var serverSignPub = sign.sign_pub;
    var signer = EncryptHelper.LoadSignerPrivateKey(serverSignPri);
    var secrets = EncryptHelper.DeriveSecretKey(EncryptHelper.LoadDhPrivateKey(serverSignPri), authJson.appDhPub);

    // Step 5 : Server sign and encrypt license
    var key = EncryptHelper.DeriveAesKey(secrets, _settings.Secret);
    var signature = EncryptHelper.SignData(signer, data);
    var pay = EncryptHelper.BuildPayload(data, signature);
    var enc = EncryptHelper.EncryptAes(key, pay);

    var license = new EncryptedLicense(
      request.sessionId,
      Convert.ToBase64String(enc),
      Convert.ToBase64String(signature),
      Convert.ToBase64String(serverSignPub)
    );

    var en = new License(
      request.company,
      request.customerSite,
      request.machineId,
      enc,
      Enums.LicenseType.Demo,
      sign.guid,
      DateTime.Now.AddDays(_settings.DemoLicense.DurationInDays)
    );

    // File.WriteAllText("license.json", JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true }));
    // Console.WriteLine("License generated.");

    await context.license.AddAsync(en);
    await context.SaveChangesAsync();


    return new BaseDto<EncryptedLicense>(
      System.Net.HttpStatusCode.OK,
      license,
      Guid.NewGuid(),
      "Demo license generation successful",
      DateTime.UtcNow.ToLocalTime()
    );
  }
}

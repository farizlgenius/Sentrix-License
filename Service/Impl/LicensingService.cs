using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseService.Data;
using LicenseService.Entities;
using LicenseService.Exceptions;
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

  public async Task<EncryptedLicense> CreateLicenseDemoAsync(GenerateDemo request)
  {
    // Step 1 : Check and get DH Key from redis or database
    var authSession = await redis.StringGetAsync(request.sessionId);
    if (authSession.IsNullOrEmpty)
      throw new UnauthorizedException("Invalid session");

    var authJson = JsonSerializer.Deserialize<AuthSession>(authSession.ToString(), jopts);

    if (authJson is null)
      throw new UnauthorizedException("Invalid session data");

    if (authJson.expiresAt < DateTime.UtcNow)
      throw new UnauthorizedException("Session expired");

    // Step 2 : Checking demo license availability
    var isAvailable = await context.license.AsNoTracking().AnyAsync(x => x.machine_id.Equals(request.machineId));

    if (isAvailable)
      throw new BadRequestException("Demo license already exists");

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
      throw new Exception("No valid signing key found");

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


    return license;
  }

  public async Task<ExchangeResponse> ExchangeAsync(ExchangeRequest request)
  {
    // Step 1 : Get Client Public Keys
    var appDhPub = Convert.FromBase64String(request.appDhPublic);
    var appSignPub = Convert.FromBase64String(request.appSignPublic);
    var appSignature = Convert.FromBase64String(request.signature);

    // Step 2 : Construct data to verify client signature
    var dataToVerify = appDhPub.Concat(appSignPub).ToArray();

    // Step 3 : Verify Client Signature
    if (!EncryptHelper.VerifyData(dataToVerify, appSignature, appSignPub))
      throw new UnauthorizedException("Client signature verification failed");

    // Step 4 : Get Signer from database
    var sign = await context.sign_key
    .AsNoTracking()
    .OrderByDescending(s => s.created_at)
    .FirstOrDefaultAsync(s => s.is_revoked == false);

    if (sign is null)
      throw new Exception("No valid signing key found");

    var serverSignPri = sign.sign_priv;
    var serverSignPub = sign.sign_pub;
    var signer = EncryptHelper.LoadSignerPrivateKey(serverSignPri);

    // Step 5 : Create Server Key Pair
    var serverDh = EncryptHelper.CreateDh();
    var serverDhPublic = EncryptHelper.ExportDhPublicKey(serverDh);

    var dataToSign = serverDhPublic.Concat(serverSignPub).ToArray();
    var signature = EncryptHelper.SignData(signer, dataToSign);

    // Step 4 : Store Auth Session in Redis
    var authSession = new AuthSession(
          serverDhPublic,
          appDhPub,
          appSignPub,
          serverDh,
          DateTime.UtcNow.AddMinutes(5)
    );

    if (await redis.KeyExistsAsync(request.sessionId))
    {
      await redis.KeyDeleteAsync(request.sessionId);
    }

    await redis.StringSetAsync(request.sessionId, JsonSerializer.Serialize(authSession), TimeSpan.FromMinutes(5));

    return new ExchangeResponse(
          request.sessionId,
          Convert.ToBase64String(serverDhPublic),
          Convert.ToBase64String(serverSignPub),
          Convert.ToBase64String(signature)
    );
  }
}

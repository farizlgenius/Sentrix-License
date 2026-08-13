using System;
using LicenseService.Data;
using LicenseService.Entities;
using LicenseService.Helper;
using LicenseService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicenseService.Service.Impl;

public sealed class KeyRotateService(AppDbContext context, IOptions<AppConfigSetting> options) : IKeyRotateService
{
  private readonly AppConfigSetting _settings = options.Value;

  public async Task CheckRotateNeededAsync()
  {
    var key = await context.sign_key
      .OrderByDescending(k => k.created_at)
      .Where(x => !x.is_revoked)
      .FirstOrDefaultAsync();

    if (key == null)
    {
      // Rotate key
      Console.WriteLine("Rotating keys...");

      // Generate new key
      var signer = EncryptHelper.CreateSigner();
      var en = new SignKeyAudit(
        signer.ExportSubjectPublicKeyInfo(),
        signer.ExportPkcs8PrivateKey(),
        DateTime.UtcNow.AddYears(1)
        );

      await context.sign_key.AddAsync(en);
      await context.SaveChangesAsync();

      Console.WriteLine("Key rotation completed.");
      return;

    }

    if (key.expire_at <= DateTime.UtcNow)
    {
      // Rotate key
      Console.WriteLine("Rotating keys...");

      key.is_revoked = true;
      context.sign_key.Update(key);

      var signer = EncryptHelper.CreateSigner();
      var en = new SignKeyAudit(
        signer.ExportSubjectPublicKeyInfo(),
        signer.ExportPkcs8PrivateKey(),
        DateTime.UtcNow.AddYears(1)
      );

      await context.sign_key.AddAsync(en);
      await context.SaveChangesAsync();

      Console.WriteLine("Key rotation completed.");
    }
  }
}

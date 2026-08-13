using LicenseService.Data;
using LicenseService.Entities;
using LicenseService.Helper;
using LicenseService.Service;
using Microsoft.EntityFrameworkCore;

namespace LicenseService.Worker;

public class StartupTask : IHostedService
{
  private readonly IServiceScopeFactory _scopeFactory;

  public StartupTask(IServiceScopeFactory scopeFactory)
  {
    _scopeFactory = scopeFactory;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await RunOnStartupAsync(cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private async Task RunOnStartupAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("Startup function executed");

    using var scope = _scopeFactory.CreateScope();

    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var hasKey = await context.sign_key.AnyAsync(x => !x.is_revoked, cancellationToken);

    if (!hasKey)
    {
      var signer = EncryptHelper.CreateSigner();
      var en = new SignKeyAudit(
        signer.ExportSubjectPublicKeyInfo(),
        signer.ExportPkcs8PrivateKey(),
        DateTime.UtcNow.AddYears(1)
      );

      await context.sign_key.AddAsync(en, cancellationToken);
      await context.SaveChangesAsync(cancellationToken);
    }
  }
}
using System;
using System.Text.Json;
using LicenseService.Data;
using LicenseService.Helper;
using LicenseService.Model;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LicenseService.Service.Impl;

public sealed class AuthService(AppDbContext context, IDatabase redis) : IAuthService
{

      public async Task<BaseDto<ExchangeResponse>> ExchangeAsync(ExchangeRequest request)
      {
            // Step 1 : Get Client Public Keys
            var appDhPub = Convert.FromBase64String(request.appDhPublic);
            var appSignPub = Convert.FromBase64String(request.appSignPublic);
            var appSignature = Convert.FromBase64String(request.signature);

            // Step 2 : Construct data to verify client signature
            var dataToVerify = appDhPub.Concat(appSignPub).ToArray();

            // Step 3 : Verify Client Signature
            if (!EncryptHelper.VerifyData(dataToVerify, appSignature, appSignPub))
            {
                  return new BaseDto<ExchangeResponse>(
                        System.Net.HttpStatusCode.Unauthorized,
                        null,
                        Guid.NewGuid(),
                        "Client signature verification failed",
                        DateTime.UtcNow.ToLocalTime()
                  );
            }

            // Step 4 : Get Signer from database
            var sign = await context.sign_key
            .AsNoTracking()
            .OrderByDescending(s => s.created_at)
            .FirstOrDefaultAsync(s => s.is_revoked == false);

            if (sign is null)
            {
                  return new BaseDto<ExchangeResponse>(
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

            var response = new ExchangeResponse(
                  request.sessionId,
                  Convert.ToBase64String(serverDhPublic),
                  Convert.ToBase64String(serverSignPub),
                  Convert.ToBase64String(signature)
            );
            return new BaseDto<ExchangeResponse>(
                  System.Net.HttpStatusCode.OK,
                  response,
                  Guid.NewGuid(),
                  "Exchange successful",
                  DateTime.UtcNow.ToLocalTime()
            );
      }

}

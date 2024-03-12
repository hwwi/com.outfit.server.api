using Api.Configuration;
using Api.Data.Models;
using Api.Repositories;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Args;
using Api.Data.Payload;
using Api.Errors;
using Api.Extension;
using Api.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;

namespace Api.Service
{
    public class AuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly SecuritySettings _securitySettings;
        private readonly OutfitDbContext _context;
        private readonly NotificationRepository _notificationRepository;

        private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
        private readonly EmailAddressAttribute _emailAddressAttribute = new EmailAddressAttribute();

        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            OutfitDbContext context,
            NotificationRepository notificationRepository,
            IOptions<SecuritySettings> securitySettings
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationRepository =
                notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _securitySettings = securitySettings.Value;
        }

        public string GenerateAccessCredentials(long personId)
        {
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, personId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(_securitySettings.SymmetricJwtKey,
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        private string GenerateRandomRefreshToken()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[64];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public async Task<AuthPostTokenPayload> NewToken(
            string appUuid,
            AuthPostTokenArgs args
        )
        {
            string? e164Number;
            try
            {
                e164Number = args.PhoneOrEmailOrName.format(PhoneNumberFormat.E164, args.Region);
            }
            catch (Exception)
            {
                e164Number = null;
            }
            
            Person? person = await _context.Persons
                .AsTracking()
                .Include(x => x.RefreshTokens)
                .Where(x =>
                    e164Number != null ? x.PhoneNumber == e164Number
                    : _emailAddressAttribute.IsValid(args.PhoneOrEmailOrName) ? x.Email == args.PhoneOrEmailOrName
                    : x.Name == args.PhoneOrEmailOrName
                )
                .FirstOrDefaultAsync();
            if (person == null || !VerifyHashedPassword(person.HashedPassword, args.Password))
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.Not_exist_user_or_password_incorrect_
                };

            // if (person.EmailVerificationId == null && person.PhoneNumberVerificationId == null)
            //     return Unauthorized(new ProblemDetails {Detail = Resources.There_is_no_verification_history_});

            var refreshToken = person.RefreshTokens.FirstOrDefault(x => x.AppUuid == appUuid);

            if (refreshToken == null || refreshToken.IsExpired || args.CloudMessagingTokens != null)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                if (refreshToken == null)
                {
                    refreshToken = new RefreshToken {
                        AppUuid = appUuid,
                        Token = GenerateRandomRefreshToken(),
                        ReissueCount = 0,
                        ExpiredAt = DateTimeOffset.UtcNow.AddDays(30)
                    };
                    person.RefreshTokens.Add(refreshToken);
                    await _context.SaveChangesAsync();
                }
                else if (refreshToken.IsExpired)
                {
                    refreshToken.Token = GenerateRandomRefreshToken();
                    refreshToken.ReissueCount += 1;
                    await _context.SaveChangesAsync();
                }

                if (args.CloudMessagingTokens != null)
                    await _notificationRepository.MergeTokenAsync(
                        person.Id,
                        args.CloudMessagingTokens.CurrentToken,
                        args.CloudMessagingTokens.ExpiredTokens
                    );

                await transaction.CommitAsync();
            }

            return new AuthPostTokenPayload {
                Id = person.Id,
                PhoneOrEmailOrName = e164Number ?? args.PhoneOrEmailOrName,
                AccessToken = $"Bearer {GenerateAccessCredentials(person.Id)}",
                RefreshToken = refreshToken.Token
            };
        }

        public async Task<AuthPostRefreshTokenPayload> RefreshToken(
            string appUuid,
            AuthPostRefreshTokenArgs args
        )
        {
            string token;
            if (args.AccessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = args.AccessToken.Substring("Bearer ".Length).Trim();
            else
                token = args.AccessToken;

            var payload = _tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters {
                    IssuerSigningKey = _securitySettings.SymmetricJwtKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                },
                out var securityToken
            );

            long personId = long.Parse(payload.Identity.Name, CultureInfo.InvariantCulture);

            var refreshToken = await _context.RefreshTokens
                .AsTracking()
                .Where(x =>
                    x.PersonId == personId
                    && x.AppUuid == appUuid
                )
                .FirstOrDefaultAsync();

            if (refreshToken == null || refreshToken.Token != args.RefreshToken)
                throw new ArgumentException("RefreshToken is not valid.");

            if (refreshToken.IsExpired)
            {
                // refreshToken 만료시 일단 갱신
                // throw new SecurityTokenExpiredException();
                refreshToken.Token = GenerateRandomRefreshToken();
                refreshToken.ReissueCount += 1;
                await _context.SaveChangesAsync();
            }

            return new AuthPostRefreshTokenPayload {
                AccessToken = $"Bearer {GenerateAccessCredentials(personId)}", RefreshToken = refreshToken.Token
            };
        }


        public string HashPassword(string password)
        {
            return HashPasswordV3(password);
        }

        public bool VerifyHashedPassword(string hashedBase64Password, string password)
        {
            int embeddedIterCount;
            //PasswordHasher.cs에서는 embeddedIterCount < _iterCount일시 SuccessRehashNeeded 로직이 있음. 
            return VerifyHashedPasswordV3(hashedBase64Password, password, out embeddedIterCount);
        }

        /* =======================
         * HASHED PASSWORD FORMATS
         * =======================
         * 
         * reference https://github.com/aspnet/AspNetCore/blob/master/src/Identity/Extensions.Core/src/PasswordHasher.cs Version 3:
         * PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit subkey, 10000 iterations.
         * Format: { 0x01, prf (UInt32), iter count (UInt32), salt length (UInt32), salt, subkey }
         * (All UInt32s are stored big-endian.)
         */
        private static string HashPasswordV3(
            string password,
            KeyDerivationPrf prf = KeyDerivationPrf.HMACSHA256,
            int iterCount = 10000,
            int saltSize = (128 / 8),
            int numBytesRequested = (256 / 8))
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[saltSize];
            rng.GetBytes(salt);
            byte[] subkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, numBytesRequested);

            var outputBytes = new byte[13 + salt.Length + subkey.Length];
            outputBytes[0] = 0x01; // format marker
            WriteNetworkByteOrder(outputBytes, 1, (uint)prf);
            WriteNetworkByteOrder(outputBytes, 5, (uint)iterCount);
            WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);


            return Convert.ToBase64String(outputBytes);
        }

        private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value >> 0);
        }

        private static bool VerifyHashedPasswordV3(string hashedBase64Password, string password, out int iterCount)
        {
            byte[] hashedPassword = Convert.FromBase64String(hashedBase64Password);

            iterCount = default;
            try
            {
                // Read header information
                KeyDerivationPrf prf = (KeyDerivationPrf)ReadNetworkByteOrder(hashedPassword, 1);
                iterCount = (int)ReadNetworkByteOrder(hashedPassword, 5);
                int saltLength = (int)ReadNetworkByteOrder(hashedPassword, 9);

                // Read the salt: must be >= 128 bits
                if (saltLength < 128 / 8)
                {
                    return false;
                }

                byte[] salt = new byte[saltLength];
                Buffer.BlockCopy(hashedPassword, 13, salt, 0, salt.Length);

                // Read the subkey (the rest of the payload): must be >= 128 bits
                int subkeyLength = hashedPassword.Length - 13 - salt.Length;
                if (subkeyLength < 128 / 8)
                {
                    return false;
                }

                byte[] expectedSubkey = new byte[subkeyLength];
                Buffer.BlockCopy(hashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

                // Hash the incoming password and verify it
                byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterCount, subkeyLength);

                return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
            }
            catch
            {
                // This should never occur except in the case of a malformed payload, where
                // we might go off the end of the array. Regardless, a malformed payload
                // implies verification failed.
                return false;
            }
        }

        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return ((uint)(buffer[offset + 0]) << 24)
                   | ((uint)(buffer[offset + 1]) << 16)
                   | ((uint)(buffer[offset + 2]) << 8)
                   | ((uint)(buffer[offset + 3]));
        }
    }
}
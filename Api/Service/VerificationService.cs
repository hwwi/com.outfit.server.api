using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Api.Data;
using Api.Data.Models;
using Api.Errors;
using Api.Extension;
using Api.Properties;
using Api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhoneNumbers;

namespace Api.Service
{
    public class VerificationService : AbstractEntityRepository<Verification>
    {
        private const int RE_REQUEST_COUNT_MODULO_DIVISOR = 3;
        private const int RE_REQUEST_AT_SUBTRACT_GAP = 3;

        private readonly ILogger<VerificationService> _logger;

        private readonly OutfitDbContext _dbContext;

        private readonly IAmazonSimpleNotificationService _simpleNotificationService;
        private readonly IAmazonSimpleEmailServiceV2 _simpleEmailServiceClient;

        public VerificationService(
            ILogger<VerificationService> logger,
            OutfitDbContext dbContext,
            IAmazonSimpleNotificationService simpleNotificationService,
            IAmazonSimpleEmailServiceV2 simpleEmailServiceClient) : base(dbContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _simpleNotificationService = simpleNotificationService ??
                                         throw new ArgumentNullException(nameof(simpleNotificationService));
            _simpleEmailServiceClient = simpleEmailServiceClient ??
                                        throw new ArgumentNullException(nameof(simpleEmailServiceClient));
        }

        public async Task<Verification?> VerifyCode(
            string appUuid,
            long verificationId,
            string verificationCode
        )
        {
            var verification = await _dbContext.Verifications.AsTracking()
                                   .FirstOrDefaultAsync(x => x.Id == verificationId)
                               ?? throw new ProblemDetailsException {
                                   StatusCode = StatusCodes.Status400BadRequest,
                                   Detail = Resources.Verification_request_not_exists_
                               };

            if (verification.AppUuid != appUuid || verification.VerifiedAt != null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.Verification_request_info_is_not_match_
                };

            if (!verification.Code.Equals(verificationCode, StringComparison.OrdinalIgnoreCase))
                return null;

            verification.VerifiedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
            return verification;
        }

        public async Task<Verification> RequestVerification(
            RouteAnonymousVerificationPurpose routePurpose,
            VerificationMethod method,
            string appUuid,
            string to
        )
        {
            checkRecentlyRequestTooOften(appUuid);
            var purpose = (VerificationPurpose)routePurpose;

            var verificationCode = CreateVerificationCode(purpose, method);

            string messageId = await SendVerificationCode(purpose, method, verificationCode, to);
            return await UpsertVerification(appUuid, null, method, purpose, to, verificationCode, messageId);
        }

        public async Task<Verification> RequestVerification(
            RouteVerificationPurpose routePurpose,
            VerificationMethod method,
            string appUuid,
            long requesterId
        )
        {
            checkRecentlyRequestTooOften(appUuid);
            var purpose = (VerificationPurpose)routePurpose;

            var person = _dbContext.Persons.First(x => x.Id == requesterId)
                         ?? throw new InvalidOperationException($@"Not exist personId({requesterId})");

            var to = (method == VerificationMethod.Email ? person.Email : person.PhoneNumber) ??
                     throw new InvalidParameterException(
                         $"Requested {method}, but Person({person.Id}) dose not have that address");
            var verificationCode = CreateVerificationCode(purpose, method);

            string messageId = await SendVerificationCode(purpose, method, verificationCode, to, person.Name);

            return await UpsertVerification(appUuid, requesterId, method, purpose, to, verificationCode, messageId);
        }

        private async Task<Verification> UpsertVerification(
            string appUuid,
            long? requesterId,
            VerificationMethod method,
            VerificationPurpose purpose,
            string to,
            string verificationCode,
            string messageId)
        {
            var verification = await _dbContext.Verifications.FirstOrDefaultAsync(x =>
                x.AppUuid == appUuid
                && x.RequesterId == requesterId
                && x.Purpose == purpose
                && x.Method == method
                && x.VerifiedAt == null
            );

            if (verification != null)
            {
                verification.To = to;
                verification.Code = verificationCode;
                verification.MessageId = messageId;
                verification.ReRequestCount += 1;
                verification.RequestedAt = DateTimeOffset.UtcNow;
                await _dbContext.UpdateAndSaveAsync(verification);

                return verification;
            }

            var entry = await _dbContext.AddAndSaveAsync(new Verification {
                AppUuid = appUuid,
                RequesterId = requesterId,
                Purpose = purpose,
                Method = method,
                To = to,
                Code = verificationCode,
                MessageId = messageId,
                ReRequestCount = 0,
                RequestedAt = DateTimeOffset.UtcNow
            });

            return entry.Entity;
        }


        public async Task RequestNewCode(
            string appUuid,
            long verificationId
        )
        {
            checkRecentlyRequestTooOften(appUuid);

            var verification = _dbContext.Verifications.AsTracking()
                                   .First(x => x.Id == verificationId)
                               ?? throw new ProblemDetailsException {
                                   StatusCode = StatusCodes.Status400BadRequest,
                                   Detail = Resources.Verification_request_not_exists_
                               };

            if (verification.AppUuid != appUuid || verification.VerifiedAt != null)
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources.Verification_request_info_is_not_match_
                };

            var verificationCode = CreateVerificationCode(verification.Purpose, verification.Method);

            string messageId = await SendVerificationCode(
                verification.Purpose,
                verification.Method,
                verificationCode,
                verification.To);

            verification.Code = verificationCode;
            verification.MessageId = messageId;
            verification.ReRequestCount += 1;
            verification.RequestedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        private void checkRecentlyRequestTooOften(string appUuid)
        {
            var toDateTimeOffset = DateTimeOffset.UtcNow;
            var fromDateTimeOffset = toDateTimeOffset.Subtract(TimeSpan.FromMinutes(RE_REQUEST_AT_SUBTRACT_GAP));
            if (_dbContext.Verifications.Any(x =>
                x.AppUuid == appUuid
                && (x.ReRequestCount != 0 && x.ReRequestCount % RE_REQUEST_COUNT_MODULO_DIVISOR == 0)
                && (fromDateTimeOffset <= x.RequestedAt && x.RequestedAt <= toDateTimeOffset))
            )
                throw new ProblemDetailsException {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = Resources
                        .Request_was_temporarily_blocked_due_to_a_large_number_of_confirmation_requests__Please_try_again_later_
                };
        }

        private String CreateVerificationCode(VerificationPurpose purpose, VerificationMethod method)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";
            var random = new Random();
            return new string(Enumerable.Range(1, method == VerificationMethod.Email ? 8 : 6)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }

        private async Task<string> SendVerificationCode(
            Enum verificationPurpose,
            VerificationMethod method,
            string verificationCode,
            string to,
            string? dearName = null
        )
        {
            switch (method)
            {
                case VerificationMethod.Email:
                {
                    var readablePurpose =
                        Regex.Replace(verificationPurpose.ToString(), "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", " $1");

                    var request = new SendEmailRequest {
                        FromEmailAddress = "no-reply@outfit.photos",
                        Destination = new Destination {ToAddresses = new List<string> {to}},
                        Content = new EmailContent {
                            Simple = new Message {
                                Subject = new Content {
                                    Charset = "UTF-8",
                                    Data = $@"[Outf!t] verify your email address to complete the {readablePurpose}"
                                },
                                Body = new Body {
                                    Text = new Content {
                                        Charset = "UTF-8",
                                        Data = $@"
Hello {dearName ?? ""},

A {readablePurpose} attempt requires further verification. To complete the {readablePurpose}, enter the verification code on the device.

Verification code: {verificationCode}

Please do not reply to this notification, this inbox is not monitored.

If you are having a problem with your account, please email contact@outfit.photos

Thanks,
The Outf!t Team
"
                                    }
                                }
                            }
                        }
                    };
                    var response = await _simpleEmailServiceClient.SendEmailAsync(request);
                    return response.MessageId;
                }
                case VerificationMethod.Sms:
                {
                    var request = new PublishRequest {
                        Message = $"Your Outf!t verification code is: {verificationCode}", PhoneNumber = to
                    };
                    request.MessageAttributes["AWS.SNS.SMS.SMSType"] =
                        new MessageAttributeValue {StringValue = "Transactional", DataType = "String"};

                    var pubResponse = await _simpleNotificationService.PublishAsync(request);
                    return pubResponse.MessageId;
                }
                default:
                    throw new InvalidOperationException($@"VerificationMethod({method}) is not yet supported");
            }
        }
    }
}
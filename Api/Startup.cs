using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Amazon.SimpleNotificationService;
using Api.Configuration;
using Api.Data;
using Api.Data.Dto;
using Api.Data.Models;
using Api.Data.Models.Relationships;
using Api.Data.Payload;
using Api.Documents;
using Api.Errors;
using Api.Extension;
using Api.Repositories;
using Api.Service;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using AutoMapper.Configuration.Conventions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        public ILifetimeScope AutofacContainer { get; private set; }

        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment environment
        )
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            });

            DatabaseSettings databaseSettings =
                services.ConfigurationSection<DatabaseSettings>(Configuration.GetSection("Database"));
            SecuritySettings securitySettings =
                services.ConfigurationSection<SecuritySettings>(Configuration.GetSection("Security"));

            FirebaseApp.Create(new AppOptions {
                Credential = GoogleCredential.FromJson(securitySettings.GoogleApplicationCredentialsJson)
            });

            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();
            var awsOptionsForSnsAndSes = Configuration.GetAWSOptions("AWS-sns-ses");
            services.AddAWSService<IAmazonSimpleNotificationService>(awsOptionsForSnsAndSes);
            services.AddAWSService<IAmazonSimpleEmailServiceV2>(awsOptionsForSnsAndSes);
            // https://github.com/aws/aws-logging-dotnet/issues/112
            services.AddLogging(logging =>
            {
                if (Environment.IsProduction())
                    logging.AddAWSProvider();
            });

            services.AddAutoMapper(options =>
                {
                    options.AddMemberConfiguration().AddName<ReplaceName>(_ => _.AddReplace("Image", "ImageUrl"));

                    long? viewerId = null;

                    options.CreateMap<Person, PersonDto>();
                    options.CreateMap<Person, PersonDetailDto>()
                        .ForMember(
                            x => x.IsViewerFollow,
                            opt => opt.MapFrom(src =>
                                src.Id == viewerId
                                    ? null
                                    : (bool?)src.Followers.Any(relation => relation.FollowerId == viewerId))
                        );
                    options.CreateMap<ItemTag, ItemTagDto>()
                        .ForMember(
                            x => x.Brand,
                            opt =>
                                opt.MapFrom(src => src.Product.Brand.Code)
                        )
                        .ForMember(
                            x => x.Product,
                            opt =>
                                opt.MapFrom(src => src.Product.Code)
                        );
                    options.CreateMap<Shot, ShotDto>()
                        .ForMember(
                            x => x.IsViewerBookmark,
                            opt =>
                                opt.MapFrom(src => src.PersonBookmarks.Any(relation => relation.PersonId == viewerId))
                        )
                        .ForMember(
                            x => x.IsViewerLike,
                            opt =>
                                opt.MapFrom(src => src.Likes.Any(relation => relation.PersonId == viewerId))
                        )
                        ;
                    options.CreateMap<PersonBookmarkShot, ShotDto>()
                        .IncludeMembers(x => x.Shot)
                        .ForMember(
                            x => x.Person,
                            opt => opt.MapFrom(src => src.Shot.Person)
                        )
                        .ForMember(
                            x => x.CreatedAt,
                            opt => opt.MapFrom(src => src.Shot.CreatedAt)
                        )
                        .ForMember(
                            x => x.UpdatedAt,
                            opt => opt.MapFrom(src => src.Shot.UpdatedAt)
                        )
                        .ForMember(
                            x => x.BookmarkedAt,
                            opt => opt.MapFrom(src => src.CreatedAt)
                        )
                        ;

                    options.CreateMap<Image, Uri>()
                        // ref. https://docs.automapper.org/en/stable/Queryable-Extensions.html#supported-mapping-options
                        //TODO .ConvertUsing<ClouldFileTypeConverter>() 
                        .ConvertUsing(x =>
                            x != null ? new Uri($"{CdnService.UrlCdn}/{x.Key}") : null);
                    options.CreateMap<Image, ImageDto>()
                        .ForMember(
                            x => x.Url,
                            opt =>
                                opt.MapFrom(src =>
                                    src != null ? new Uri($"{CdnService.UrlCdn}/{src.Key}") : null
                                )
                        )
                        ;
                    options.CreateMap<HashTag, SearchedHashTag>();
                    options.CreateMap<Comment, CommentDto>()
                        .ForMember(
                            x => x.IsViewerLike,
                            opt =>
                                opt.MapFrom(src => src.Likes.Any(likeRelation => likeRelation.PersonId == viewerId))
                        )
                        ;
                    options.CreateMap<Notification, NotificationDto>()
                        .ForMember(
                            x => x.ShotPreviewImageUrl,
                            opt =>
                                opt.MapFrom(src =>
                                    src.Shot != null
                                    && (
                                        src.Type == NotificationType.ShotPosted
                                        || src.Type == NotificationType.ShotIncludePersonTag
                                        || src.Type == NotificationType.ShotLiked
                                    )
                                        ? src.Shot.Images.OrderBy(x => x.Id).Take(1).ToList()[0]
                                        : null
                                )
                        )
                        .ForMember(
                            x => x.CommentText,
                            opt =>
                                opt.MapFrom(src =>
                                    src.Comment != null
                                    && (
                                        src.Type == NotificationType.Commented
                                        || src.Type == NotificationType.CommentIncludePersonTag
                                        || src.Type == NotificationType.CommentLiked
                                    )
                                        ? src.Comment.Text
                                        : null
                                )
                        )
                        ;
                },
                typeof(Startup));
            services.AddDbContextPool<OutfitDbContext, OutfitDbContext>((provider, builder) =>
            {
                builder
                    .EnableSensitiveDataLogging()
                    .UseNpgsql(databaseSettings.ConnectionString, options =>
                    {
                    })
                    .UseLazyLoadingProxies()
                    .UseSnakeCaseNamingConvention();
                ;
            });
            services.AddOptions();
            services.AddSingleton<ProblemDetailsFactory, OutfitProblemDetailsFactory>();
            services.AddControllers(options =>
                {
                    options.Conventions.Add(new ProblemDetailsClientErrorResultFilterConvention());
                    options.Conventions.Add(new ExceptionHandleActionFilterConvention());
                })
                .AddDataAnnotationsLocalization()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            services.PostConfigure<ApiBehaviorOptions>(options =>
            {
                options.SuppressMapClientErrors = true;
                var builtInFactory = options.InvalidModelStateResponseFactory;
                options.InvalidModelStateResponseFactory = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(context.ActionDescriptor.DisplayName);
                    IEnumerable<string> errorStrings = context.ModelState.Select(kv =>
                        $"{kv.Key} : {string.Join(", ", kv.Value.Errors.Select(error => error.ErrorMessage))}");
                    logger.LogError($"InvalidModelState \n=>  {string.Join("\n    ", errorStrings)}");

                    // Get an instance of ILogger (see below) and log accordingly.

                    IActionResult actionResult = builtInFactory(context);
                    if (actionResult is ObjectResult objectResult)
                    {
                        objectResult.ContentTypes.Clear();
                        objectResult.ContentTypes.Add("application/validation.problem+json");
                        objectResult.ContentTypes.Add("application/validation.problem+xml");
                    }

                    return actionResult;
                };
            });

            services
                .AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters {
                        IssuerSigningKey = securitySettings.SymmetricJwtKey,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                    };
                });
            if (!Environment.IsProduction())
                services.AddSwaggerDocument(config =>
                {
                    // TODO FromQueryAttribute에서 System.Text.Json를 이용하여 enum을 string으로 매칭시키는 방법찾기, 미지원??
                    // ref to https://github.com/RicoSuter/NSwag/issues/2243
                    config.DefaultEnumHandling = EnumHandling.String;
                    config.PostProcess = document =>
                    {
                        document.Info.Version = "v1";
                        document.Info.Title = "Outfit API";
                        document.Info.Description = "A ASP.NET Core web API";
                        document.Info.TermsOfService = "None";
                        document.Info.Contact = new OpenApiContact {
                            Name = "Shayne Boyer", Email = string.Empty, Url = "https://twitter.com/spboyer"
                        };
                        document.Info.License =
                            new OpenApiLicense {Name = "Use under LICX", Url = "https://example.com/license"};
                    };

                    // ref to https://github.com/RicoSuter/NSwag/issues/2431 (Support preauthorizeApiKey in SwaggerUi3)
                    config.AddSecurity("JWT", Enumerable.Empty<string>(),
                        new OpenApiSecurityScheme {
                            Type = OpenApiSecuritySchemeType.ApiKey,
                            Name = "Authorization",
                            In = OpenApiSecurityApiKeyLocation.Header,
                            Description = "Type into the textbox: Bearer {your JWT token}."
                        });
                    config.OperationProcessors.Add(new ExampleOperationProcessor());
                    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
                    config.OperationProcessors.Add(new FormJsonStringOperationProcessor());
                    config.OperationProcessors.Add(
                        new ReplaceConsumesAndProducesOperationProcessor("application/*+json"));
                });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<CommentRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<BrandRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<ProductRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<ShotRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<PersonRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<FollowPersonRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<SearchRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<NotificationRepository>()
                .InstancePerLifetimeScope();

            builder.RegisterType<AuthenticationService>()
                .InstancePerLifetimeScope();
            builder.RegisterType<CloudStorageService>()
                .InstancePerLifetimeScope();
            builder.RegisterType<CdnService>()
                .InstancePerLifetimeScope();
            builder.RegisterType<VerificationService>()
                .InstancePerLifetimeScope();
            builder.RegisterType<ImageService>()
                .InstancePerLifetimeScope();
            builder.RegisterType<CloudMessagingService>()
                .InstancePerLifetimeScope();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            AutofacContainer = app.ApplicationServices.GetAutofacRoot();

            if (!Environment.IsProduction())
            {
                app.UseOpenApi();
                app.UseSwaggerUi3(config =>
                {
                });
            }

            var supportedCultures = new[] {new CultureInfo("en-US"), new CultureInfo("ko-KR"),};
            app.UseRequestLocalization(new RequestLocalizationOptions {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
            });


            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("health");
                endpoints.MapFallback(async context =>
                {
                    logger.LogInformation(
                        $"Request not matched {context.Request.Protocol} {context.Request.Method} {context.Request.Path} by {context.Connection.RemoteIpAddress}"
                    );
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                });
            });
        }
    }
}
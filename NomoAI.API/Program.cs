using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Ai;
using NomoAI.API.Common.Behaviors;
using NomoAI.API.Common.DoctorDocuments;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Common.Options;
using NomoAI.API.Common.Redis;
using NomoAI.API.Common.Redis;
using NomoAI.API.Common.Roles;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Activities;
using NomoAI.API.Features.Admin;
using NomoAI.API.Features.Auth;
using NomoAI.API.Features.Auth.Register_User;
using NomoAI.API.Features.Children;
using NomoAI.API.Features.Parents;
using NomoAI.API.Features.Profile.UpdateDoctorDocuments;
using NomoAI.API.Features.Sessions;
using NomoAI.API.Infrastructure;
using NomoAI.API.Infrastructure.Ai;
using NomoAI.API.Infrastructure.Email;
using NomoAI.API.Infrastructure.BackgroundJobs;
using NomoAI.API.Infrastructure.PayMob.Services;
using NomoAI.API.Persistence;
using StackExchange.Redis;
using StackExchange.Redis;
using System.Reflection;
using System.Text;

namespace NomoAI.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // .NET 8 defaults to :8080; Elastic Beanstalk nginx proxies to :5000.
            if (!builder.Environment.IsDevelopment())
            {
                builder.WebHost.UseUrls("http://127.0.0.1:5000");
            }

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            //Swagger Configuration
            builder.Services.AddSwaggerGen(
            options =>
            {
                options.MapType<IFormFile>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });

                options.OperationFilter<EvaluateAttemptFormOperationFilter>();
                options.OperationFilter<RegisterFormOperationFilter>();
                options.OperationFilter<UpdateDoctorDocumentsFormOperationFilter>();

                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description =
                            "Enter your JWT access token."
                    });

                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
            });
            //Hangfire
            builder.Services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddHangfireServer();

            // Database
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString(
                        "DefaultConnection"));
            });

            // Identity
            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 4;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;

                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // CORS
            builder.Services
                .AddOptions<CorsOptions>()
                .Bind(builder.Configuration.GetSection(CorsOptions.SectionName));

            builder.Services
                .AddOptions<PublicApiOptions>()
                .Configure(options =>
                {
                    options.BaseUrl = builder.Configuration[PublicApiOptions.ConfigKey] ?? string.Empty;
                });

            string[] allowedOrigins = builder.Configuration
                .GetSection(CorsOptions.SectionName)
                .GetSection(nameof(CorsOptions.AllowedOrigins))
                .Get<string[]>() ?? [];

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "MyPolicy",
                    policy =>
                    {
                        if (allowedOrigins.Length > 0)
                        {
                            // Explicit allow-list: credentials are safe because origins are known.
                            policy
                                .WithOrigins(allowedOrigins)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        }
                        else if (builder.Environment.IsDevelopment())
                        {
                            // No allow-list configured locally: keep permissive DX.
                            policy
                                .AllowAnyMethod()
                                .AllowAnyOrigin()
                                .AllowAnyHeader();
                        }
                        else
                        {
                            // Non-Development without an allow-list: deny cross-origin browser calls
                            // rather than silently defaulting to AllowAnyOrigin.
                            policy
                                .WithOrigins(Array.Empty<string>())
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        }
                    });
            });

            //Auth
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
            });

            // Email service
            builder.Services
                .AddOptions<EmailOptions>()
                .Bind(
                    builder.Configuration.GetSection(
                        EmailOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddSingleton<
                IValidateOptions<EmailOptions>,
                EmailOptionsValidator>();

            builder.Services.AddScoped<
                IEmailSender,
                SmtpEmailSender>();

            builder.Services.AddSingleton<
                IEmailTemplateBuilder,
                EmailTemplateBuilder>();

            builder.Services.AddScoped<
                IEmailOtpDispatcher,
                EmailOtpDispatcher>();

           
            //////////////////////
            ///

            // Upstash Redis options
            builder.Services
                .AddOptions<UpstashRedisOptions>()
                .Bind(
                    builder.Configuration.GetSection(
                        UpstashRedisOptions.SectionName))
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.Endpoint),
                    "Upstash Redis endpoint is required.")
                .Validate(
                    options =>
                        options.Port > 0,
                    "Upstash Redis port must be valid.")
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.Password),
                    "Upstash Redis password is required.")
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.InstancePrefix),
                    "Redis instance prefix is required.")
                .ValidateOnStart();

            // Email OTP options
            builder.Services
                .AddOptions<EmailOtpOptions>()
                .Bind(
                    builder.Configuration.GetSection(
                        EmailOtpOptions.SectionName))
                .Validate(
                    options =>
                        options.Length is >= 6 and <= 9,
                    "OTP length must be between 6 and 9 digits.")
                .Validate(
                    options =>
                        options.ExpirationMinutes > 0,
                    "OTP expiration must be greater than zero.")
                .Validate(
                    options =>
                        options.MaxAttempts > 0,
                    "OTP maximum attempts must be greater than zero.")
                .Validate(
                    options =>
                        options.ResendCooldownSeconds > 0,
                    "OTP resend cooldown must be greater than zero.")
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.HashKey) &&
                        options.HashKey.Length >= 32,
                    "OTP HashKey must contain at least 32 characters.")
                .ValidateOnStart();

            ///////////////



            //////////////تسجيل ConnectionMultiplexer
            ///

            builder.Services.AddSingleton<
    IConnectionMultiplexer>(
    serviceProvider =>
    {
        var redisOptions =
            serviceProvider
                .GetRequiredService<
                    IOptions<UpstashRedisOptions>>()
                .Value;

        var logger =
            serviceProvider
                .GetRequiredService<
                    ILogger<Program>>();

        var configuration =
            new ConfigurationOptions
            {
                User =
                    string.IsNullOrWhiteSpace(
                        redisOptions.User)
                        ? null
                        : redisOptions.User,

                Password =
                    redisOptions.Password,

                Ssl =
                    redisOptions.UseSsl,

                AbortOnConnectFail = false,

                ConnectRetry = 3,

                ConnectTimeout = 10_000,

                KeepAlive = 60
            };

        configuration.EndPoints.Add(
            redisOptions.Endpoint,
            redisOptions.Port);

        var connection =
            ConnectionMultiplexer.Connect(
                configuration);

        connection.ConnectionFailed +=
            (_, eventArgs) =>
            {
                logger.LogError(
                    eventArgs.Exception,
                    "Redis connection failed. " +
                    "Endpoint: {Endpoint}, " +
                    "FailureType: {FailureType}.",
                    eventArgs.EndPoint,
                    eventArgs.FailureType);
            };

        connection.ConnectionRestored +=
            (_, eventArgs) =>
            {
                logger.LogInformation(
                    "Redis connection restored. " +
                    "Endpoint: {Endpoint}.",
                    eventArgs.EndPoint);
            };

        return connection;
    });
            /////////////////////
            ///

            builder.Services.AddScoped<IEmailOtpService,UpstashEmailOtpService>();


            // FluentValidation
            builder.Services.AddValidatorsFromAssembly(
                Assembly.GetExecutingAssembly());

            // MediatR and validation pipeline
            builder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(
                    Assembly.GetExecutingAssembly());

                configuration.AddOpenBehavior(
                    typeof(ValidationBehavior<,>));
            });

            builder.Services.AddEndpoints(
                Assembly.GetExecutingAssembly());

            //jwt 
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            //Auto Mapper
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

            //Role Manger 
            builder.Services.AddScoped<IRoleManger , RoleManger>();
            builder.Services.AddSingleton<DoctorDocumentStorage>();

            // AI Core (FastAPI) integration
            builder.Services.AddAiService(builder.Configuration);

            builder.WebHost.ConfigureKestrel(options =>
            {
                const long doctorDocumentsMaxBytes =
                    (DoctorDocumentLimits.MaxFileBytes * 2) + (1024 * 1024);

                options.Limits.MaxRequestBodySize = Math.Max(
                    AiServiceOptions.DefaultMaxAudioBytes + (1024 * 1024),
                    doctorDocumentsMaxBytes);
            });
            //Payment 
            builder.Services.AddScoped<IPayMobService, PayMobService>();
            builder.Services.AddScoped<ExpirePaymentQuickLinksBackgroundJob>();

            var app = builder.Build();

            // Elastic Beanstalk / reverse proxies terminate TLS; honor X-Forwarded-* from nginx/ALB.
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            app.UseSwagger();
            app.UseSwaggerUI();

            // Do not force HTTPS redirects behind Elastic Beanstalk HTTP → nginx → Kestrel.
            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("MyPolicy");
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHangfireDashboard("/jobs");

            RecurringJob.AddOrUpdate<ExpirePaymentQuickLinksBackgroundJob>(
                ExpirePaymentQuickLinksBackgroundJob.RecurringJobId,
                job => job.ExecuteAsync(),
                "*/30 * * * *"); // every 30 minutes

            app.MapAuthEndpoints();
            app.MapEndpoints();
            app.MapParentsEndpoints();
            app.MapChildrenEndpoints();
            app.MapActivitiesEndpoints();
            app.MapAdminEndpoints();
            app.MapSessionsEndpoints();

            // Liveness for load balancer / EB health checks (does not depend on AI Core).
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            app.MapHealthChecks("/health/ai", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ai")
            });

            app.MapControllers();

            app.Run();
        }
    }
}
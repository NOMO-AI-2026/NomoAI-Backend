using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Behaviors;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Common.Redis;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Activities;
using NomoAI.API.Features.Admin;
using NomoAI.API.Features.Auth;
using NomoAI.API.Features.Children;
using NomoAI.API.Features.Parents;
using NomoAI.API.Infrastructure;
using NomoAI.API.Infrastructure.Email;
using NomoAI.API.Persistence;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using NomoAI.API.Common.EmailOtp;
using NomoAI.API.Common.Redis;
using StackExchange.Redis;
using NomoAI.API.Common.Roles;

namespace NomoAI.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            //Swagger Configuration
            builder.Services.AddSwaggerGen(
            options =>
            {
               
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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "MyPolicy",
                    policy =>
                    {
                        policy
                            .AllowAnyMethod()
                            .AllowAnyOrigin()
                            .AllowAnyHeader();
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

            //Auto Mapper
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

            //Role Manger 
            builder.Services.AddScoped<IRoleManger , RoleManger>();

            var app = builder.Build();

           

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

        
            app.UseCors("MyPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapAuthEndpoints();
            app.MapEndpoints();
            app.MapParentsEndpoints();
            app.MapChildrenEndpoints();
            app.MapActivitiesEndpoints();
            app.MapAdminEndpoints();


            app.MapControllers();

            app.Run();
        }
    }
}
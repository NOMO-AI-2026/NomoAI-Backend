using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Behaviors;
using NomoAI.API.Common.Email;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Activities;
using NomoAI.API.Features.Auth;
using NomoAI.API.Features.Children;
using NomoAI.API.Features.Parents;
using NomoAI.API.Infrastructure.Email;
using NomoAI.API.Persistence;
using System.Reflection;
using System.Text;

namespace NomoAI.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Swagger configuration
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile =
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

                var xmlPath =
                    Path.Combine(AppContext.BaseDirectory, xmlFile);

                options.IncludeXmlComments(xmlPath);

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

            // JWT authentication
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer =
                                builder.Configuration["Jwt:Issuer"],

                            ValidateAudience = true,
                            ValidAudience =
                                builder.Configuration["Jwt:Audience"],

                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(
                                        builder.Configuration["Jwt:Key"]!))
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

            builder.Services.Configure<FrontendOptions>(
                builder.Configuration.GetSection("Frontend"));

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

            builder.Services.AddScoped<IJwtService, JwtService>();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            // استخدام اسم الـ CORS policy المسجلة
            app.UseCors("MyPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapAuthEndpoints();
            app.MapEndpoints();
            app.MapParentsEndpoints();
            app.MapChildrenEndpoints();
            app.MapActivitiesEndpoints();
            app.MapControllers();

            app.Run();
        }
    }
}
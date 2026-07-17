using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

namespace NomoAI.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder =
                WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile =
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

                var xmlPath =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        xmlFile);

                options.IncludeXmlComments(xmlPath);
            });

            builder.Services.AddDbContext<AppDbContext>(
                options =>
                {
                    options.UseSqlServer(
                        builder.Configuration
                            .GetConnectionString(
                                "DefaultConnection"));
                });

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(
                    options =>
                    {
                        options.Password.RequireDigit = false;
                        options.Password.RequiredLength = 4;
                        options.Password
                            .RequireNonAlphanumeric = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireLowercase = false;

                        options.User.RequireUniqueEmail = true;

                        options.SignIn
                            .RequireConfirmedEmail = true;
                    })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

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
                builder.Configuration.GetSection(
                    "Frontend"));

            // Register all FluentValidation validators
            builder.Services.AddValidatorsFromAssembly(
                Assembly.GetExecutingAssembly());

            // Register MediatR and validation pipeline
            builder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(
                    Assembly.GetExecutingAssembly());

                configuration.AddOpenBehavior(
                    typeof(ValidationBehavior<,>));
            });

            builder.Services.AddEndpoints(
                Assembly.GetExecutingAssembly());

            builder.Services.AddScoped<
                IJwtService,
                JwtService>();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapEndpoints();
            app.MapAuthEndpoints();
            app.MapParentsEndpoints();
            app.MapChildrenEndpoints();
            app.MapActivitiesEndpoints();
            app.MapControllers();

            app.Run();
        }
    }
}
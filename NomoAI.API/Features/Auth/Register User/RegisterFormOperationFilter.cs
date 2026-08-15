using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NomoAI.API.Features.Auth.Register_User;

/// <summary>
/// Documents POST /api/auth/register as JSON (Parent) or multipart/form-data (Doctor).
/// </summary>
public sealed class RegisterFormOperationFilter : IOperationFilter
{
    public const string OperationId = "Register";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(operation.OperationId, OperationId, StringComparison.Ordinal))
        {
            return;
        }

        operation.Parameters?.Clear();

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description =
                "Parent: application/json. Doctor: multipart/form-data with three verification files " +
                "and syndicateRegistrationNumber in a single request.",
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = BuildJsonSchema()
                },
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = BuildMultipartSchema(),
                    Encoding = new Dictionary<string, OpenApiEncoding>
                    {
                        ["identityDocument"] = new OpenApiEncoding { Style = ParameterStyle.Form },
                        ["practiceLicense"] = new OpenApiEncoding { Style = ParameterStyle.Form },
                        ["syndicateCard"] = new OpenApiEncoding { Style = ParameterStyle.Form }
                    }
                }
            }
        };
    }

    private static OpenApiSchema BuildJsonSchema()
    {
        return new OpenApiSchema
        {
            Type = "object",
            Description = "Parent registration. Role must be 1 (Parent). Doctor JSON is rejected.",
            Required = new HashSet<string>
            {
                "fullName",
                "email",
                "password",
                "age",
                "gender",
                "role"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["fullName"] = new OpenApiSchema { Type = "string" },
                ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                ["phoneNumber"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["password"] = new OpenApiSchema { Type = "string", Format = "password" },
                ["age"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                ["gender"] = new OpenApiSchema { Type = "integer", Description = "0 = Male, 1 = Female" },
                ["role"] = new OpenApiSchema { Type = "integer", Description = "1 = Parent" },
                ["yearsOfExperience"] = new OpenApiSchema { Type = "integer", Format = "int32", Nullable = true },
                ["clinicName"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["professionalBio"] = new OpenApiSchema { Type = "string", Nullable = true }
            }
        };
    }

    private static OpenApiSchema BuildMultipartSchema()
    {
        return new OpenApiSchema
        {
            Type = "object",
            Description = "Doctor registration. Role must be 0 (Doctor).",
            Required = new HashSet<string>
            {
                "fullName",
                "email",
                "password",
                "age",
                "gender",
                "role",
                "identityDocument",
                "practiceLicense",
                "syndicateCard",
                "syndicateRegistrationNumber"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["fullName"] = new OpenApiSchema { Type = "string" },
                ["email"] = new OpenApiSchema { Type = "string", Format = "email" },
                ["phoneNumber"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["password"] = new OpenApiSchema { Type = "string", Format = "password" },
                ["age"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                ["gender"] = new OpenApiSchema { Type = "integer", Description = "0 = Male, 1 = Female" },
                ["role"] = new OpenApiSchema { Type = "integer", Description = "0 = Doctor" },
                ["yearsOfExperience"] = new OpenApiSchema { Type = "integer", Format = "int32", Nullable = true },
                ["clinicName"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["professionalBio"] = new OpenApiSchema { Type = "string", Nullable = true },
                ["syndicateRegistrationNumber"] = new OpenApiSchema
                {
                    Type = "string",
                    Description = "Doctors' Syndicate registration number."
                },
                ["identityDocument"] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "National ID or passport (JPEG, PNG, or PDF, max 5MB)."
                },
                ["practiceLicense"] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "Professional practice license (JPEG, PNG, or PDF, max 5MB)."
                },
                ["syndicateCard"] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "Doctors' Syndicate membership card (JPEG, PNG, or PDF, max 5MB)."
                }
            }
        };
    }
}

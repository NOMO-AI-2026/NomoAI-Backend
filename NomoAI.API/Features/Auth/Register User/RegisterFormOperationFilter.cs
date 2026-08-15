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

        // Put multipart first so Swagger UI defaults to Doctor file uploads (Browse buttons).
        // Parent JSON remains available from the media-type dropdown.
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description =
                "Doctor (default): multipart/form-data — use Browse for practiceLicense and syndicateCard.\n" +
                "Parent: switch media type to application/json (no files).",
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = BuildMultipartSchema(),
                    Encoding = new Dictionary<string, OpenApiEncoding>
                    {
                        ["practiceLicense"] = new OpenApiEncoding
                        {
                            Style = ParameterStyle.Form,
                            ContentType = "application/octet-stream"
                        },
                        ["syndicateCard"] = new OpenApiEncoding
                        {
                            Style = ParameterStyle.Form,
                            ContentType = "application/octet-stream"
                        }
                    }
                },
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = BuildJsonSchema()
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
                "practiceLicense",
                "syndicateCard"
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

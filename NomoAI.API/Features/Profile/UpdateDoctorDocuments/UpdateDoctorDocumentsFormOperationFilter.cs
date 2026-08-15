using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NomoAI.API.Features.Profile.UpdateDoctorDocuments;

/// <summary>
/// Documents PUT /api/profile/doctor-documents as multipart/form-data with Browse buttons.
/// </summary>
public sealed class UpdateDoctorDocumentsFormOperationFilter : IOperationFilter
{
    public const string OperationId = "UpdateDoctorDocuments";

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
                "Submit replacement documents for admin review. Active documents stay in use until accepted. " +
                "Use Browse for practiceLicense and syndicateCard.",
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = BuildSchema(),
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
                }
            }
        };
    }

    private static OpenApiSchema BuildSchema()
    {
        return new OpenApiSchema
        {
            Type = "object",
            Required = new HashSet<string>
            {
                "practiceLicense",
                "syndicateCard"
            },
            Properties = new Dictionary<string, OpenApiSchema>
            {
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

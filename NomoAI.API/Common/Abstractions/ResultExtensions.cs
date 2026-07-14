using Microsoft.AspNetCore.Mvc;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Common.Abstractions
{
    public static class ResultExtensions
    {
        public static IResult ToProblem(this Result result)
        {
            if (result.IsSuccess)
                throw new InvalidOperationException(
                    "Cannot convert success result into problem");

            return Results.Problem(
                detail: result.Error.Description,
                title: "An error occurred",
                statusCode: result.Error.StatusCode,
                type: result.Error.Code);
        }
    }
}

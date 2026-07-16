using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.DeleteActivity;

public sealed record DeleteActivityCommand(
    int ActivityId,
    string DoctorUserId)
    : IRequest<Result>;
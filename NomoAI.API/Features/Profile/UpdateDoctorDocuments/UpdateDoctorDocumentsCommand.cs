using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.DoctorDocuments;

namespace NomoAI.API.Features.Profile.UpdateDoctorDocuments;

public sealed record UpdateDoctorDocumentsCommand(
    string UserId,
    DoctorDocumentFile? PracticeLicense,
    DoctorDocumentFile? SyndicateCard,
    string PublicApiBaseUrl) : IRequest<Result<UpdateDoctorDocumentsResponse>>;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.ToggleDoctorApproval
{
    internal sealed class ToggleDoctorApprovalCommandHandler : IRequestHandler<ToggleDoctorApprovalCommand, Result>
    {
        private readonly AppDbContext _db;
        public ToggleDoctorApprovalCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(ToggleDoctorApprovalCommand request, CancellationToken cancellationToken)
        {
            var doctor = await _db.Doctor
                .Where(d => d.UserId == request.UserId && !d.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure(AdminDashboardErrors.DoctorNotFound);
            }

            doctor.IsApproved = request.ApproveStatus;
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.DeleteDoctor
{
    internal sealed class DeleteDoctorCommandHandler : IRequestHandler<DeleteDoctorCommand, Result>
    {
        private readonly AppDbContext _db;
        public DeleteDoctorCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(DeleteDoctorCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Where(u => u.Id == request.UserId && !u.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure(AdminDashboardErrors.DoctorNotFound);
            }

            user.IsDeleted = true;

            var doctor = await _db.Doctor
                .Where(d => d.UserId == request.UserId && !d.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is not null)
            {
                doctor.IsDeleted = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

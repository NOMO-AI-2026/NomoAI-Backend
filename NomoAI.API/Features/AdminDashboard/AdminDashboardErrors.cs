using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard
{
    public static class AdminDashboardErrors
    {
      public static Error DoctorNotFound => new Error("Admin.DoctorNotFound", "Doctor not found.", 404);
        public static Error ParentNotFound => new Error("Admin.ParentNotFound", "Parent not found.", 404);
    }
}

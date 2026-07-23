namespace NomoAI.API.Features.AdminDashboard.ToggleDoctorApproval
{
 public class ToggleApprovalRequest
 {
    public string UserId { get; set; }
    public bool ApproveStatus { get; set; }
 }
}
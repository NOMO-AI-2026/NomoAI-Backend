using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorNotes
{
    public static class DoctorNotesErrors
    {
        public static Error ChildNotFound = new Error("DoctorNotes.ChildNotFound", "Child not found.", 404);
        public static Error DoctorNotFound = new Error("DoctorNotes.DoctorNotFound", "Doctor not found or not approved.", 404);
        public static Error NoteNotFound = new Error("DoctorNotes.NoteNotFound", "this note is not found.", 404);
        public static Error UnathorizedAccess = new Error("DoctorNotes.UnauthorizedAccess", "User does not has the access to update this note.", 403);
        public static Error UpdateFailed = new Error("DoctorNotes.UpdateFailed", "The Update Failed.", 400);
        public static Error DeleteFailed = new Error("DoctorNotes.DeleteFailed", "The Delete Failed.", 400);
    }
}

using MimeKit.Tnef;

namespace NomoAI.API.Features.DoctorNotes.GetAllChildNotes
{
    public class GetAllNotesResponse
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

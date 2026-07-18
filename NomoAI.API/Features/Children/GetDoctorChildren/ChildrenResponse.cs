using MimeKit.Tnef;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    public class ChildrenResponse
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }
    }
}

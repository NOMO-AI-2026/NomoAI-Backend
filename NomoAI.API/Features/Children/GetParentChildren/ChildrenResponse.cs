using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Children.GetParentChildren
{
    public class ChildrenResponse
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }
    }
}

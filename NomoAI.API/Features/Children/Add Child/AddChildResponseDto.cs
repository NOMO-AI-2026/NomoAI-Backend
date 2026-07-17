namespace NomoAI.API.Features.Children.Add_Child
{
    public class AddChildResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public int SpeechLevelId { get; set; }
    }
}

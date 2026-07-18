namespace NomoAI.API.Features.SpeechLevels.GetAllLevels
{
    public class SpeechLevelResponse
    {
        public int Id { get; set; } 

        public required string LevelName { get; set; }
    }
}

namespace keyraces.Core.Dtos
{
    public class SessionCompleteRequestDto
    {
        public int SessionId { get; set; }
        public double Wpm { get; set; }
        public int Errors { get; set; }
        public double Accuracy { get; set; }
        public TimeSpan Duration { get; set; }
    }
}

namespace keyraces.Server.Dtos
{
    public class SessionDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }

        public SessionDto(int id, DateTime startTime)
        {
            Id = id;
            StartTime = startTime;
        }
    }
}

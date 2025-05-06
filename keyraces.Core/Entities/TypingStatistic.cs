public class TypingStatistic
{
    public TypingStatistic(int userId, int sessionId, double wpm, double accuracy)
    {
        UserId = userId;
        SessionId = sessionId;
        WPM = wpm;
        Accuracy = accuracy;
    }

    protected TypingStatistic() { }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int SessionId { get; private set; }
    public double WPM { get; private set; }
    public double Accuracy { get; private set; }

    public void Update(double newWpm, double newAccuracy)
    {
        WPM = newWpm;
        Accuracy = newAccuracy;
    }
}

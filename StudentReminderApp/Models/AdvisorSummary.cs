namespace StudentReminderApp.Models
{
    public class AdvisorSummary
    {
        public int TotalAccumulatedCredits { get; set; }
        public int RegisteredCreditsThisTerm { get; set; }
        public int RemainingCredits { get; set; }
        public string GPAFormatted { get; set; }
        public string GPALevel { get; set; }
        public int MaxCreditsAllowed { get; set; }
    }
}
namespace LTUDAPI.DTOs
{
    public class ReminderRequest
    {
        public long IdAcc { get; set; }
        public int MinsBefore { get; set; }
        public bool IsEnabled { get; set; }
        public string Channel { get; set; } = "App";
    }
}
namespace StudentReminderApp.Models
{
    public class EventTag
    {
        public long IdTag { get; set; }
        public long IdAcc { get; set; }
        public string TagType { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
    }
}
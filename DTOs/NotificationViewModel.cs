namespace AIResumeScreeningSystem.DTOs
{
    public class NotificationViewModel
    {
        public int NotificationID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string? ReferenceURL { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

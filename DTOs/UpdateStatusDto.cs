namespace AIResumeScreeningSystem.DTOs
{
    public class UpdateStatusDto
    {
        public int ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

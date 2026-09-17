namespace AIResumeScreeningSystem.DTOs
{
    /// <summary>
    /// ViewModel for displaying featured premium candidates on the homepage.
    /// Premium subscribers with complete profiles get featured placement.
    /// </summary>
    public class FeaturedCandidateViewModel
    {
        public int ApplicantID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfileHeadline { get; set; }
        public string? Skills { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Location { get; set; }
        public int ProfileCompletion { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }
}

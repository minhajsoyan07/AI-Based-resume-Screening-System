# Data Transfer Objects (DTOs) & ViewModels

This directory contains the objects used for transferring data between the frontend and backend, and for rendering views.

## 📂 Categories

### 🆕 Request DTOs
Used for capturing user input from forms and API requests.
- `RegisterDTO.cs`, `LoginDTO.cs`: Authentication flows.
- `JobCreateDTO.cs`, `ApplicationCreateDTO.cs`: Resource creation.
- `CompanySetupDTO.cs`: Onboarding for recruiters.

### 📊 ViewModels
Used for passing data to Razor Views.
- `AdminDashboardViewModel.cs`, `ApplicantDashboardViewModel.cs`: Portal homepages.
- `CandidateViewModel.cs`, `JobCardViewModel.cs`: List and card displays.
- `ProfileAnalyticsViewModel.cs`: Complex data for charts and AI insights.

### 🤖 AI-Specific DTOs
Used specifically for structured communication with AI services.
- `AIMatchingDTOs.cs`: Encapsulates match results and reasoning.

## 💡 Best Practices
- Keep these classes "anemic" (data only, no business logic).
- Use these to decouple the database models (`Models/`) from the external API/UI.

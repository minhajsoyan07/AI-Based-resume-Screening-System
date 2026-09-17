# AI-Based Resume Screening System (RecruitAI)

RecruitAI is an advanced, AI-powered job recruitment management system built with .NET 8. It streamlines the hiring process by automating resume parsing, skill extraction, and candidate matching using cutting-edge AI models like Google Gemini.

## 🚀 Key Features

### 👤 For Applicants
- **Smart Job Search:** Browse and search for jobs tailored to your skills.
- **AI Job Recommendations:** Get personalized job suggestions based on your resume.
- **Profile Analytics:** Visualize your skill gaps and get suggestions for improvement.
- **Application Tracking:** Track the status of your job applications in real-time.

### 🏢 For Recruiters
- **AI-Powered Screening:** Automatically parse resumes (PDF, Word, Images/OCR) and extract structured data.
- **Candidate Matching:** Match candidates against job requirements with detailed scoring and gap analysis.
- **Interview Management:** Schedule and manage interviews seamlessly.
- **Company Branding:** Customize your recruiter profile and company details.

### 🛠 For Admins
- **Comprehensive Dashboard:** Monitor platform activity, recruiter performance, and applicant trends.
- **Analytics & Reporting:** Access detailed reports on revenue, user growth, and system usage.
- **User Management:** Manage recruiters and applicants across the platform.

## 💻 Tech Stack

- **Backend:** [ASP.NET Core 8 MVC](https://dotnet.microsoft.com/en-us/apps/aspnet/mvc)
- **Database:** [SQL Server](https://www.microsoft.com/en-us/sql-server/) with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- **AI Models:** [Google Gemini 2.0 Flash](https://ai.google.dev/models/gemini), OpenAI Compatible APIs (OpenRouter, Modal, OllamaCloud)
- **Resume Parsing:** 
  - [PdfPig](https://github.com/UglyToad/PdfPig) (PDF extraction)
  - [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) (Word documents)
  - [Tesseract OCR](https://github.com/charlesw/tesseract) (Image-based resumes)
- **Machine Learning:** [ML.NET](https://dotnet.microsoft.com/en-us/apps/machinelearning-ai/ml-dotnet) for specialized analysis.
- **Communication:** [Mailjet](https://www.mailjet.com/) & [MailKit](https://github.com/jstedfast/MailKit) for automated email notifications.
- **Authentication:** Cookie-based Auth with RBAC (Role-Based Access Control).

## 🏗 Project Structure

```text
├───Controllers/    # MVC Controllers (Admin, Applicant, Recruiter, AI APIs)
├───Services/       # Business logic (AI Matching, Resume Parsing, Automation)
├───Models/         # Database entities
├───DTOs/           # Data Transfer Objects & ViewModels
├───Interfaces/     # Service abstractions and AI data contracts
├───Data/           # Entity Framework DbContext and Migrations
├───Views/          # Razor Views for the web interface
└───wwwroot/        # Static assets (CSS, JS, Uploaded Resumes)
```

## 🛠 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or localdb)
- Tesseract OCR data (`tessdata/eng.traineddata` included)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/AIResumeScreeningSystem.git
   cd AIResumeScreeningSystem
   ```

2. **Configure the database:**
   Update the `DefaultConnection` in `appsettings.json` with your SQL Server connection string.

3. **Set Environment Variables:**
   Ensure the following are set or configured in `appsettings.json`:
   - `JWT_SECRET_KEY`: A secure key for token generation.
   - `EMAIL_SENDER`: The email address used for notifications.
   - `AISettings:Gemini:ApiKey`: Your Google Gemini API key.

4. **Apply Migrations:**
   ```bash
   dotnet ef database update
   ```

5. **Run the application:**
   ```bash
   dotnet run
   ```

## 🛡 Security & Best Practices
- **Password Hashing:** Uses BCrypt for secure storage.
- **CSRF Protection:** Integrated Anti-Forgery tokens.
- **Panel Access Guard:** Custom middleware ensures users can only access their respective portals (Admin/Recruiter/Applicant).

## 📄 License
This project is licensed under the MIT License - see the LICENSE file for details.

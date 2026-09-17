# AI-Based Resume Screening System (RecruitAI)

RecruitAI is an enterprise-ready, AI-driven recruitment and applicant tracking platform built with ASP.NET Core 8 MVC. The platform automates resume ingestion, multi-format text extraction (PDF, DOCX, and OCR for scanned images), structured candidate profiling, and multi-factor job compatibility matching using Google Gemini and machine learning inference.

---

## System Architecture

```text
├── Controllers/         # MVC & REST API Controllers (Admin, Applicant, Recruiter, Scanner, Auth)
├── Services/            # Core business logic (AI matching, resume parsing, automation, email)
├── Interfaces/          # Abstractions and data contracts for services and parsers
├── Models/              # Entity Framework Core database models
├── DTOs/                # Data Transfer Objects, view models, and request contracts
├── Data/                # ApplicationDbContext and EF Core database migrations
├── Views/               # Razor Views and partials for all portals (Admin, Recruiter, Applicant)
├── Helpers/             # Validation attributes and file handling utilities
├── Constants/           # Session keys and application constants
├── tessdata/            # Tesseract OCR language training data (eng.traineddata)
└── wwwroot/             # Static web assets (CSS, JS, images, upload directories)
```

---

## Core Features and Capabilities

### 1. Role-Based Portals and Access Control

- **Separate Authentication Flows**: Dedicated portals and login interfaces for Applicants, Recruiters, and Platform Administrators.
- **Dual Authentication**: Secure cookie authentication for web sessions with sliding expiration, alongside JWT bearer authentication for REST API endpoints.
- **Security Guard Middleware**: Role-based access enforcement (RBAC) ensuring users are restricted to their authorized domain.
- **Credential Protection**: Password hashing via BCrypt, anti-forgery CSRF validation tokens, email verification tokens, and secure password reset workflows.

### 2. Applicant Portal

- **Job Discovery and Search**: Browse active job openings with filters for keywords, department, employment type, and experience level.
- **AI Job Recommendations**: Automated matching engine recommends relevant job openings based on the applicant's parsed resume and skill profile.
- **Automated Profile Generation**: Uploading a resume automatically parses education, work experience, technical skills, soft skills, and bio into the candidate profile.
- **Profile Analytics**: Visual skill gap analysis, profile completion indicators, and career development suggestions.
- **Application Tracking**: Real-time status monitoring (Submitted, Under Review, Shortlisted, Interview Scheduled, Accepted, Rejected).
- **Saved Jobs**: Bookmark job listings for quick application later.

### 3. Recruiter Portal

- **Company Profile and Branding**: Manage organization profile, description, contact details, and company branding.
- **Job Lifecycle Management**: Create, edit, publish, and close job postings with detailed criteria (required degree, major, minimum experience, mandatory and optional skills).
- **Automated Resume Screening**: Automatically parses candidate applications upon submission and ranks applicants using weighted multi-factor scoring.
- **Deep Candidate Analysis**:
  - Match Verdicts (Strong Match, Good Match, Partial Match, Low Match).
  - Component breakdown: Skills Match (25%), Experience (30%), Education Alignment (30%), Keywords & Certifications (15%).
  - Detailed list of matching skills, missing requirements, and candidate strengths/weaknesses.
- **Interview Scheduling**: Schedule interviews directly from the dashboard (date, time, format, location/meeting link, and notes) with automated candidate email notifications.
- **Application Workflow**: Move candidates across recruitment stages with automated notification triggers.

### 4. Administrator Portal

- **Platform Analytics Dashboard**: High-level overview of system metrics, active jobs, registered candidates, recruiters, and application volume.
- **User Management**: Oversee recruiter accounts, candidate registrations, and account statuses.
- **Revenue and Monetization Tracking**: Monitor credit package transactions, sales volume, and system earnings.
- **System Monitoring**: Track background automation tasks, error logs, and AI parsing activities.

### 5. AI Resume Scanner and Credit System

- **Standalone Resume Scanner**: Direct evaluation tool allowing users or recruiters to upload a resume against an arbitrary job description without creating an active job posting.
- **Credit/Token Economy**: Users utilize platform credits for running deep AI analyses and scans.
- **Package Purchasing**: Built-in checkout system featuring Starter, Professional, and Power User credit packages with simulated payment gateway integration.
- **Transaction History**: Audit ledger tracking all credit purchases, deductions, and balance adjustments.

### 6. Document Parsing and AI Matching Engine

- **Hybrid Document Extraction**:
  - **PDF Documents**: Layout-aware structured text extraction using PdfPig.
  - **Word Documents (DOCX)**: Table and text extraction using DocumentFormat.OpenXml.
  - **Scanned Resumes / Images**: Optical Character Recognition (OCR) via Tesseract 5.2 and Google Gemini Vision for complex image layouts.
- **Multi-Model AI Integration**:
  - Primary AI service powered by Google Gemini 2.0 Flash using the official `Google.GenAI` SDK.
  - Pluggable provider support for OpenAI-compatible endpoints (OpenRouter, custom inference servers).
  - Machine learning classification and scoring via ML.NET (`Microsoft.ML`).
- **Deterministic Skill Inference Engine**: Synonym mapping, framework relationships, and hierarchical education level evaluation.
- **In-Memory Caching**: Concurrent caching for match scores and job requirements to optimize response latency and minimize external API token consumption.

### 7. Communication System

- Automated email notifications via MailKit/MimeKit over SMTP.
- System templates for account verification, password resets, application confirmations, status transitions, and interview invitations.

### 8. RESTful API Endpoints

In addition to Razor UI views, the platform provides dedicated API controllers:
- `/api/admin` - Administrative data access and metrics.
- `/api/applications` - Application submission and status tracking.
- `/api/auth` - Programmatic authentication and token issuance.
- `/api/jobs` - Job posting, querying, and search endpoints.
- `/api/resume` - Resume parsing and evaluation endpoints.
- `/api/config` - Public system configuration parameters.

---

## Technology Stack

| Category | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 MVC & Web API |
| Language | C# (.NET 8.0) |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core 8.0 |
| AI Service | Google Gemini 2.0 Flash (`Google.GenAI`) |
| Machine Learning | ML.NET (`Microsoft.ML`, `Microsoft.ML.FastTree`) |
| Document Extraction | PdfPig, DocumentFormat.OpenXml, Tesseract OCR 5.2 |
| Email Service | MailKit, MimeKit |
| Security | BCrypt.Net-Next, JWT Bearer, ASP.NET Core Cookie Auth |
| Frontend | Razor Views, Vanilla JavaScript, CSS3 |

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Express, Developer, or LocalDB)
- Tesseract OCR language file (included under `tessdata/eng.traineddata`)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/minhajsoyan07/AI-Based-resume-Screening-System.git
   cd AI-Based-resume-Screening-System
   ```

2. **Configure Application Settings:**
   Open `appsettings.json` (or create an `appsettings.Development.json` for local overrides) and configure your database connection string, email settings, and AI credentials:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=JanalaAIBasedResume;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;"
     },
     "JwtSettings": {
       "SecretKey": "YOUR_SECURE_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS"
     },
     "EmailSettings": {
       "From": "your-email@gmail.com",
       "SmtpServer": "smtp.gmail.com",
       "Port": 587,
       "DisplayName": "Janala | AI Recruitment",
       "Username": "your-email@gmail.com",
       "Password": "YOUR_GMAIL_APP_PASSWORD"
     },
     "AISettings": {
       "Provider": "Gemini",
       "Gemini": {
         "ApiKey": "YOUR_GEMINI_API_KEY",
         "Model": "gemini-2.0-flash"
       },
       "OpenAI": {
         "ApiKey": "YOUR_OPENAI_API_KEY",
         "BaseUrl": "https://api.openai.com/v1",
         "Model": "gpt-4o"
       }
     }
   }
   ```

3. **Apply Database Migrations:**
   Run the following command to create the database and seed required tables:
   ```bash
   dotnet ef database update
   ```

4. **Build and Run:**
   ```bash
   dotnet run
   ```
   Open your browser and navigate to `http://localhost:5000` (or the URL indicated in your console output).

---

## Security and Best Practices

- **Sanitized Repositories**: Sensitive production keys, passwords, and user-uploaded resumes are excluded from version control via `.gitignore`.
- **Credential Storage**: Supports environment variables (`JWT_SECRET_KEY`, `EMAIL_SENDER`, `GOOGLE_API_KEY`) for secure production deployments.
- **Input Validation**: Rigorous file type, file size, and extension validation for all resume and circular uploads.
- **Data Protection**: Prepared Entity Framework queries prevent SQL injection, and ASP.NET Core anti-forgery tokens protect state-changing requests against CSRF attacks.

---

## License

This project is licensed under the MIT License.

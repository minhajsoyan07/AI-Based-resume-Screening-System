# Interfaces & Data Contracts

This directory contains the service abstractions (interfaces) and the data contracts used for AI operations and cross-service communication.

## 📡 Service Interfaces

These define the contracts for the business and AI services, allowing for dependency injection and easier testing.

- `IAIResumeService.cs`: Contract for AI-based resume analysis.
- `IAIMatchingService.cs`: Contract for matching algorithms.
- `IResumeParserService.cs`: Contract for document parsing logic.
- `IJobService.cs`, `IApplicationService.cs`, `IAuthService.cs`: Standard business operation contracts.

## 📊 Data Contracts (AI & Analytics)

These classes/interfaces define the structure of data returned by AI models or used in analytical calculations.

### Resume & Matching
- `ParsedResumeData.cs`: The base structure for extracted resume information (name, contact, education, experience).
- `EnhancedParsedResume.cs`: Adds deeper insights like inferred skills and project summaries.
- `SkillsGapAnalysis.cs`: Represents the comparison between a candidate's skills and job requirements.
- `WorkExperienceAnalysis.cs`: Provides a qualitative assessment of a candidate's career progression.

### Recommendations & Outcomes
- `JobRecommendation.cs`: Data structure for personalized job suggestions.
- `HiringOutcome.cs`: Defines the possible results and reasoning for a candidate's application journey.

### Monitoring
- `ModelPerformanceMetrics.cs`: Used to track and evaluate the performance and latency of the integrated AI models.

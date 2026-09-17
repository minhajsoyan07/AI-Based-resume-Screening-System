using System;
using System.Collections.Generic;
using System.Linq;

namespace AIResumeScreeningSystem.Services
{
    public static class SkillInferenceEngine
    {
        private static readonly Dictionary<string, List<(string skill, double confidence)>> InferenceRules = new(StringComparer.OrdinalIgnoreCase)
        {
            { "React", new() { ("JavaScript", 0.95), ("HTML", 0.9), ("CSS", 0.85), ("TypeScript", 0.6), ("JSX", 0.9), ("Webpack", 0.5), ("NPM", 0.8) } },
            { "Angular", new() { ("TypeScript", 0.95), ("JavaScript", 0.9), ("HTML", 0.9), ("CSS", 0.85), ("RxJS", 0.8) } },
            { "Vue", new() { ("JavaScript", 0.95), ("HTML", 0.9), ("CSS", 0.85), ("TypeScript", 0.5), ("Node.js", 0.5) } },
            { "Vue.js", new() { ("JavaScript", 0.95), ("HTML", 0.9), ("CSS", 0.85), ("TypeScript", 0.5), ("Node.js", 0.5) } },
            { "Next.js", new() { ("React", 0.95), ("JavaScript", 0.9), ("TypeScript", 0.7), ("Node.js", 0.8) } },
            { "Node.js", new() { ("JavaScript", 0.95), ("NPM", 0.9), ("Express", 0.6), ("REST API", 0.7) } },
            { "Express", new() { ("Node.js", 0.95), ("JavaScript", 0.9), ("REST API", 0.8) } },
            { "Django", new() { ("Python", 0.95), ("SQL", 0.8), ("REST API", 0.7) } },
            { "Flask", new() { ("Python", 0.95), ("REST API", 0.7), ("SQL", 0.6) } },
            { "FastAPI", new() { ("Python", 0.95), ("REST API", 0.8), ("SQL", 0.6), ("Async", 0.7) } },
            { "Spring Boot", new() { ("Java", 0.95), ("SQL", 0.7), ("REST API", 0.8), ("Maven", 0.6) } },
            { "ASP.NET Core", new() { ("C#", 0.95), (".NET", 0.9), ("SQL", 0.7), ("REST API", 0.8) } },
            { "ASP.NET", new() { ("C#", 0.95), (".NET", 0.9), ("SQL", 0.7), ("REST API", 0.7) } },
            { "Entity Framework", new() { ("C#", 0.9), ("SQL", 0.85), (".NET", 0.8), ("LINQ", 0.8) } },
            { "TensorFlow", new() { ("Python", 0.9), ("Machine Learning", 0.95), ("Deep Learning", 0.9), ("NumPy", 0.8) } },
            { "PyTorch", new() { ("Python", 0.9), ("Machine Learning", 0.95), ("Deep Learning", 0.9), ("NumPy", 0.8) } },
            { "Kubernetes", new() { ("Docker", 0.9), ("DevOps", 0.85), ("Linux", 0.7), ("YAML", 0.7) } },
            { "Docker", new() { ("Linux", 0.7), ("DevOps", 0.75), ("CI/CD", 0.6) } },
            { "Terraform", new() { ("AWS", 0.6), ("Azure", 0.5), ("DevOps", 0.8), ("Cloud", 0.8) } },
            { "AWS", new() { ("Cloud", 0.9), ("Linux", 0.6), ("DevOps", 0.5) } },
            { "Azure", new() { ("Cloud", 0.9), (".NET", 0.5), ("DevOps", 0.5) } },
            { "GCP", new() { ("Cloud", 0.9), ("Linux", 0.6), ("DevOps", 0.5) } },
            { "Google Cloud", new() { ("Cloud", 0.9), ("Linux", 0.6), ("DevOps", 0.5) } },
            { "Machine Learning", new() { ("Python", 0.85), ("Statistics", 0.7), ("Data Science", 0.8) } },
            { "Deep Learning", new() { ("Machine Learning", 0.95), ("Python", 0.8), ("Neural Network", 0.9) } },
            { "NLP", new() { ("Machine Learning", 0.85), ("Python", 0.8), ("Deep Learning", 0.7) } },
            { "Computer Vision", new() { ("Machine Learning", 0.85), ("Deep Learning", 0.8), ("Python", 0.8) } },
            { "Data Science", new() { ("Python", 0.85), ("SQL", 0.7), ("Statistics", 0.8), ("Machine Learning", 0.7) } },
            { "Power BI", new() { ("SQL", 0.8), ("Data Analysis", 0.8), ("DAX", 0.7) } },
            { "Tableau", new() { ("SQL", 0.7), ("Data Analysis", 0.8), ("Data Visualization", 0.9) } },
            { "MongoDB", new() { ("NoSQL", 0.95), ("JavaScript", 0.6), ("REST API", 0.5) } },
            { "PostgreSQL", new() { ("SQL", 0.95), ("Database", 0.9) } },
            { "MySQL", new() { ("SQL", 0.95), ("Database", 0.9) } },
            { "Redis", new() { ("NoSQL", 0.8), ("Caching", 0.9), ("Database", 0.7) } },
            { "GraphQL", new() { ("REST API", 0.7), ("JavaScript", 0.6), ("Node.js", 0.5) } },
            { "Jenkins", new() { ("CI/CD", 0.95), ("DevOps", 0.9), ("Git", 0.7) } },
            { "GitHub Actions", new() { ("CI/CD", 0.95), ("DevOps", 0.85), ("Git", 0.9) } },
            { "GitLab CI", new() { ("CI/CD", 0.95), ("DevOps", 0.85), ("Git", 0.9) } },
            { "Scrum", new() { ("Agile", 0.95) } },
            { "Kanban", new() { ("Agile", 0.9) } },
            { "SAFe", new() { ("Agile", 0.9), ("Scrum", 0.7) } },
            { "Tailwind", new() { ("CSS", 0.95), ("HTML", 0.9) } },
            { "Bootstrap", new() { ("CSS", 0.95), ("HTML", 0.9) } },
            { "Svelte", new() { ("JavaScript", 0.95), ("HTML", 0.85), ("CSS", 0.8) } },
            { "Blazor", new() { ("C#", 0.95), (".NET", 0.9), ("HTML", 0.7) } },
            { "Rust", new() { ("Linux", 0.6), ("Systems Programming", 0.8) } },
            { "Go", new() { ("Linux", 0.6), ("Cloud", 0.5), ("Docker", 0.5) } },
            { "Swift", new() { ("iOS", 0.9), ("Xcode", 0.8), ("Objective-C", 0.5) } },
            { "Kotlin", new() { ("Android", 0.9), ("Java", 0.7) } },
            { "XGBoost", new() { ("Python", 0.8), ("Machine Learning", 0.9), ("Data Science", 0.85) } },
            { "Scikit-learn", new() { ("Python", 0.9), ("Machine Learning", 0.9), ("NumPy", 0.8) } },
            { "Pandas", new() { ("Python", 0.95), ("Data Science", 0.8), ("NumPy", 0.85) } },
            { "NumPy", new() { ("Python", 0.95), ("Data Science", 0.7) } },
            { "Kafka", new() { ("Distributed Systems", 0.8), ("Java", 0.6), ("Scala", 0.5) } },
            { "Spark", new() { ("Python", 0.7), ("Scala", 0.6), ("Big Data", 0.9) } },
            { "Hadoop", new() { ("Big Data", 0.95), ("Java", 0.6), ("Linux", 0.5) } },
            { "Ansible", new() { ("DevOps", 0.9), ("Linux", 0.8), ("Automation", 0.8) } },
            { "Nginx", new() { ("Linux", 0.8), ("DevOps", 0.6), ("Web Server", 0.9) } },
            { "Apache", new() { ("Linux", 0.7), ("Web Server", 0.9), ("DevOps", 0.5) } },
            { "Salesforce", new() { ("CRM", 0.9), ("Apex", 0.6), ("Cloud", 0.5) } },
            { "SAP", new() { ("ERP", 0.9), ("Business Process", 0.7) } },
            { "Workday", new() { ("HRIS", 0.9), ("HCM", 0.8) } },
            { "Penetration Testing", new() { ("Security", 0.95), ("Networking", 0.7), ("Linux", 0.7) } },
            { "SIEM", new() { ("Security", 0.9), ("Networking", 0.6), ("Log Analysis", 0.8) } },
            { "Cryptography", new() { ("Security", 0.9), ("Mathematics", 0.6) } },
            { "TypeScript", new() { ("JavaScript", 0.95) } },
            { "JavaScript", new() { ("HTML", 0.7), ("CSS", 0.6) } },
            { "C#", new() { (".NET", 0.9), ("SQL", 0.5) } },
            { "Java", new() { ("SQL", 0.5), ("Spring Boot", 0.4), ("Maven", 0.4) } },
            { "Python", new() { ("SQL", 0.5), ("Git", 0.5) } },
            { "C++", new() { ("C", 0.7), ("Linux", 0.5) } },
        };

        public static List<(string skill, double confidence)> InferSkills(IEnumerable<string> knownSkills)
        {
            var inferred = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var skill in knownSkills)
            {
                if (InferenceRules.TryGetValue(skill, out var rules))
                {
                    foreach (var (inferredSkill, confidence) in rules)
                    {
                        if (!knownSkills.Any(s => s.Equals(inferredSkill, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (inferred.TryGetValue(inferredSkill, out var existingConf))
                            {
                                inferred[inferredSkill] = Math.Max(existingConf, confidence);
                            }
                            else
                            {
                                inferred[inferredSkill] = confidence;
                            }
                        }
                    }
                }
            }

            return inferred
                .Where(x => x.Value >= 0.5)
                .OrderByDescending(x => x.Value)
                .Select(x => (x.Key, x.Value))
                .ToList();
        }

        public static HashSet<string> GetExpandedSkillSet(IEnumerable<string> originalSkills, double minConfidence = 0.6)
        {
            var expanded = new HashSet<string>(originalSkills, StringComparer.OrdinalIgnoreCase);
            var inferred = InferSkills(originalSkills);

            foreach (var (skill, confidence) in inferred)
            {
                if (confidence >= minConfidence)
                {
                    expanded.Add(skill);
                }
            }

            return expanded;
        }
    }
}

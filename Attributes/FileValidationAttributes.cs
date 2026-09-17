using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AIResumeScreeningSystem.Attributes
{
    /// <summary>
    /// Validates that an uploaded file does not exceed a maximum size.
    /// </summary>
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxBytes;

        public MaxFileSizeAttribute(int maxBytes) : base()
        {
            _maxBytes = maxBytes;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxBytes)
                {
                    var sizeMB = _maxBytes / (1024.0 * 1024.0);
                    return new ValidationResult(ErrorMessage ?? $"File size cannot exceed {sizeMB:F0} MB.");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that an uploaded file has an allowed extension.
    /// </summary>
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions) : base()
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_extensions.Contains(ext))
                {
                    return new ValidationResult(ErrorMessage ?? $"Allowed file types: {string.Join(", ", _extensions)}");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that an uploaded file has a valid magic number signature for its type.
    /// Prevents malware disguised by file extension spoofing ("Hacker" protection).
    /// </summary>
    public class FileSignatureAttribute : ValidationAttribute
    {
        private static readonly Dictionary<string, byte[][]> _fileSignatures = new Dictionary<string, byte[][]>
        {
            { ".pdf", new byte[][] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
            { ".docx", new byte[][] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // PK.. (Zip header)
            { ".doc", new byte[][] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
            { ".png", new byte[][] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".jpeg", new byte[][] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpg", new byte[][] { new byte[] { 0xFF, 0xD8, 0xFF } } }
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_fileSignatures.ContainsKey(ext)) return ValidationResult.Success;

                using var stream = file.OpenReadStream();
                using var reader = new BinaryReader(stream);
                
                var signatures = _fileSignatures[ext];
                var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

                var isValid = signatures.Any(signature => 
                    headerBytes.Take(signature.Length).SequenceEqual(signature));

                if (!isValid)
                {
                    return new ValidationResult(ErrorMessage ?? $"Security Error: The internal content of '{file.FileName}' does not match its '{ext}' extension. Upload blocked for safety.");
                }
            }

            return ValidationResult.Success;
        }
    }
}

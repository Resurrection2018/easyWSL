using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace easyWslLib
{
    public static class InputValidator
    {
        private static readonly char[] InvalidDistroNameChars =
            Path.GetInvalidFileNameChars()
                .Concat(new[] { ' ', '/', '\\', ':', '*', '?', '"', '<', '>', '|' })
                .Distinct()
                .ToArray();

        public static (bool IsValid, string Error) ValidateDistroName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                SecurityLogger.LogValidationFailure("DistroName", name, "Empty name");
                return (false, "Distribution name cannot be empty");
            }

            if (name.Length > 255)
            {
                SecurityLogger.LogValidationFailure("DistroName", name, "Name too long");
                return (false, "Distribution name is too long (max 255 characters)");
            }

            if (name.IndexOfAny(InvalidDistroNameChars) >= 0)
            {
                SecurityLogger.LogValidationFailure("DistroName", name, "Contains invalid characters");
                return (false, "Distribution name contains invalid characters");
            }

            // Reserved names
            var reserved = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3",
                                   "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                                   "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6",
                                   "LPT7", "LPT8", "LPT9" };
            if (reserved.Contains(name.ToUpperInvariant()))
            {
                SecurityLogger.LogValidationFailure("DistroName", name, "Reserved system name");
                return (false, "Distribution name is a reserved system name");
            }

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return (false, "Username cannot be empty");

            if (userName.Length > 32)
                return (false, "Username is too long (max 32 characters)");

            // Linux username rules
            if (!char.IsLetter(userName[0]))
                return (false, "Username must start with a letter");

            if (!userName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                return (false, "Username can only contain letters, digits, underscores, and hyphens");

            return (true, string.Empty);
        }

        public static (bool IsValid, string Error) ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return (false, "Path cannot be empty");

            try
            {
                var fullPath = Path.GetFullPath(path);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Invalid path: {ex.Message}");
            }
        }

        public static (bool IsValid, string Error) ValidateDockerImage(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                SecurityLogger.LogValidationFailure("DockerImage", image, "Empty image name");
                return (false, "Docker image name cannot be empty");
            }

            // Prevent malicious registry URLs
            if (image.Contains("://"))
            {
                SecurityLogger.LogValidationFailure("DockerImage", image, "Contains URL protocol");
                return (false, "Full URLs are not allowed. Use registry/image:tag format");
            }
            
            // Prevent excessively long names (possible DoS)
            if (image.Length > 256)
            {
                SecurityLogger.LogValidationFailure("DockerImage", image, "Name too long");
                return (false, "Docker image name is too long (max 256 characters)");
            }

            // Basic Docker image name validation
            // Format: [registry/]name[:tag]
            var parts = image.Split(':');
            if (parts.Length > 2)
            {
                SecurityLogger.LogValidationFailure("DockerImage", image, "Invalid format - multiple colons");
                return (false, "Invalid Docker image format");
            }

            // Validate image name part
            var namePattern = @"^[a-z0-9]+([._-][a-z0-9]+)*(/[a-z0-9]+([._-][a-z0-9]+)*)*$";
            if (!Regex.IsMatch(parts[0], namePattern))
            {
                SecurityLogger.LogValidationFailure("DockerImage", image, "Invalid name format");
                return (false, "Invalid Docker image name format");
            }
            
            // Validate tag if present
            if (parts.Length == 2)
            {
                var tagPattern = @"^[a-zA-Z0-9_][a-zA-Z0-9._-]{0,127}$";
                if (!Regex.IsMatch(parts[1], tagPattern))
                {
                    SecurityLogger.LogValidationFailure("DockerImage", image, "Invalid tag format");
                    return (false, "Invalid Docker image tag format");
                }
            }

            return (true, string.Empty);
        }

        public static string EscapeShellArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return "\"\"";

            // Escape single quotes for bash/sh
            return $"'{argument.Replace("'", "'\\''")}'";
        }

        public static string SanitizePathForWSL(string windowsPath)
        {
            // Validate it's a valid Windows path first
            var (isValid, _) = ValidatePath(windowsPath);
            if (!isValid)
                throw new ArgumentException("Invalid path", nameof(windowsPath));

            // Escape backslashes and quotes
            return windowsPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Validates that a path doesn't escape the base directory (path traversal attack)
        /// </summary>
        public static (bool IsValid, string Error) ValidatePathWithinBase(string path, string baseDirectory)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                string fullBase = Path.GetFullPath(baseDirectory);
                
                // Ensure path starts with base directory
                if (!fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
                {
                    SecurityLogger.LogPathTraversalAttempt(path, baseDirectory);
                    return (false, "Path attempts to access location outside allowed directory");
                }
                
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Invalid path: {ex.Message}");
            }
        }

        /// <summary>
        /// Sanitizes a filename by removing path traversal attempts
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename cannot be empty", nameof(fileName));
            
            // Remove any path components
            fileName = Path.GetFileName(fileName);
            
            // Remove path traversal attempts
            fileName = fileName.Replace("..", "");
            fileName = fileName.Replace("/", "");
            fileName = fileName.Replace("\\", "");
            
            // Remove other dangerous characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), "");
            }
            
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename is invalid after sanitization", nameof(fileName));
            
            return fileName;
        }
    }
}
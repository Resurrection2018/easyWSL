using System;
using System.IO;

namespace easyWslLib
{
    /// <summary>
    /// Provides security event logging for audit trail
    /// </summary>
    public static class SecurityLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "easyWSL", "security.log"
        );
        
        private static readonly object _lockObject = new object();
        
        static SecurityLogger()
        {
            // Ensure log directory exists
            try
            {
                var logDir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
            }
            catch
            {
                // If we can't create log directory, logging will gracefully fail
            }
        }
        
        /// <summary>
        /// Logs a security event
        /// </summary>
        public static void LogSecurityEvent(string eventType, string details, bool isSuccess = true)
        {
            try
            {
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                string status = isSuccess ? "SUCCESS" : "FAILURE";
                string logEntry = $"[{timestamp}] [{status}] {eventType}: {details}\n";
                
                lock (_lockObject)
                {
                    File.AppendAllText(LogPath, logEntry);
                }
            }
            catch
            {
                // Don't throw if logging fails - logging should never break functionality
            }
        }
        
        /// <summary>
        /// Logs an input validation failure
        /// </summary>
        public static void LogValidationFailure(string inputType, string value, string reason)
        {
            LogSecurityEvent(
                "VALIDATION_FAILURE",
                $"Type={inputType}, Value={SanitizeForLog(value)}, Reason={reason}",
                isSuccess: false
            );
        }
        
        /// <summary>
        /// Logs a command execution (without sensitive data)
        /// </summary>
        public static void LogCommandExecution(string command, string distroName)
        {
            // Don't log full command (may contain sensitive data)
            string cmdPreview = command.Length > 50 ? command.Substring(0, 50) + "..." : command;
            LogSecurityEvent(
                "COMMAND_EXECUTION",
                $"Distro={distroName}, Command={cmdPreview}"
            );
        }
        
        /// <summary>
        /// Logs a file operation
        /// </summary>
        public static void LogFileOperation(string operation, string path, bool isSuccess = true)
        {
            LogSecurityEvent(
                "FILE_OPERATION",
                $"Operation={operation}, Path={SanitizeForLog(path)}",
                isSuccess
            );
        }
        
        /// <summary>
        /// Logs a path traversal attempt
        /// </summary>
        public static void LogPathTraversalAttempt(string attemptedPath, string baseDirectory)
        {
            LogSecurityEvent(
                "PATH_TRAVERSAL_ATTEMPT",
                $"Attempted={SanitizeForLog(attemptedPath)}, Base={SanitizeForLog(baseDirectory)}",
                isSuccess: false
            );
        }
        
        /// <summary>
        /// Sanitizes values for safe logging (prevents log injection, limits length)
        /// </summary>
        private static string SanitizeForLog(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "[empty]";
                
            // Truncate long values
            if (value.Length > 100)
                value = value.Substring(0, 100) + "...";
                
            // Remove newlines and carriage returns to prevent log injection
            value = value.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            
            return value;
        }
    }
}
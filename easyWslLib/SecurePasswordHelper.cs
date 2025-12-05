using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace easyWslLib
{
    /// <summary>
    /// Provides secure handling of passwords for WSL user creation
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class SecurePasswordHelper
    {
        /// <summary>
        /// Creates a temporary file with secure permissions and writes password for chpasswd
        /// </summary>
        /// <param name="userName">The username</param>
        /// <param name="password">The password (will be securely deleted after use)</param>
        /// <returns>Path to the temporary password file</returns>
        public static string CreateSecurePasswordFile(string userName, string password)
        {
            // Create temp file with unique name in secure location
            string tempPath = Path.Combine(Path.GetTempPath(), $"wsl_pass_{Guid.NewGuid()}.tmp");
            
            try
            {
                // Write password in format expected by chpasswd: username:password
                File.WriteAllText(tempPath, $"{userName}:{password}\n");
                
                // Set restrictive permissions (Windows ACL) - only current user can access
                var fileInfo = new FileInfo(tempPath);
                var fileSecurity = fileInfo.GetAccessControl();
                
                // Disable inheritance and remove all existing permissions
                fileSecurity.SetAccessRuleProtection(true, false);
                
                // Add permission only for current user
                var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
                fileSecurity.AddAccessRule(
                    new System.Security.AccessControl.FileSystemAccessRule(
                        currentUser.User,
                        System.Security.AccessControl.FileSystemRights.FullControl,
                        System.Security.AccessControl.AccessControlType.Allow
                    )
                );
                
                fileInfo.SetAccessControl(fileSecurity);
                
                return tempPath;
            }
            catch
            {
                // Clean up on error
                if (File.Exists(tempPath))
                    SecureDelete(tempPath);
                throw;
            }
        }
        
        /// <summary>
        /// Securely deletes a file by overwriting with random data first
        /// </summary>
        /// <param name="filePath">Path to the file to securely delete</param>
        public static void SecureDelete(string filePath)
        {
            if (!File.Exists(filePath))
                return;
                
            try
            {
                // Overwrite file with random data before deletion
                var fileInfo = new FileInfo(filePath);
                long length = fileInfo.Length;
                
                if (length > 0)
                {
                    using (var stream = File.OpenWrite(filePath))
                    {
                        byte[] buffer = new byte[4096];
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            long written = 0;
                            while (written < length)
                            {
                                int toWrite = (int)Math.Min(buffer.Length, length - written);
                                rng.GetBytes(buffer, 0, toWrite);
                                stream.Write(buffer, 0, toWrite);
                                written += toWrite;
                            }
                        }
                        stream.Flush();
                    }
                }
                
                // Now delete the file
                File.Delete(filePath);
            }
            catch
            {
                // Even if secure delete fails, still try regular delete
                try { File.Delete(filePath); } catch { }
            }
        }
    }
}
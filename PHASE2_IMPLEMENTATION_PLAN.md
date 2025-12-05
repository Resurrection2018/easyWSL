# Phase 2: Security Improvements - Implementation Plan

> **Timeline**: Week 2 (5 days)  
> **Effort**: 2-3 days  
> **Priority**: P0 - Critical Security

---

## 🎯 Objectives

Enhance security posture by addressing password handling, path traversal risks, and implementing comprehensive security measures.

---

## 📋 Task Breakdown

### Task 1: Implement Secure Password Handling
**Files**: 
- `easyWslLib/SecurePasswordHelper.cs` - NEW FILE
- `easyWSL/RegisterNewDistro_Page.xaml.cs`

**Effort**: 2 hours  
**Risk**: Medium

**Current Issue**:
```csharp
// Line 220 - Password visible in process arguments
var escapedPassword = InputValidator.EscapeShellArgument(password);
await helpers.ExecuteCommandInWSLAsync(distroName, 
    $"echo {escapedUserName}:{escapedPassword} | chpasswd");
```

**Problems**:
- Password passed as command-line argument (visible in process list)
- Password stored in memory as plain string
- No secure cleanup of password data

**Implementation**:

1. Create `SecurePasswordHelper.cs`:
```csharp
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace easyWslLib
{
    public static class SecurePasswordHelper
    {
        /// <summary>
        /// Creates a temporary file with secure permissions and writes password hash
        /// </summary>
        public static string CreateSecurePasswordFile(string userName, string password)
        {
            // Create temp file in secure location
            string tempPath = Path.Combine(Path.GetTempPath(), $"wsl_pass_{Guid.NewGuid()}.tmp");
            
            try
            {
                // Write password in format expected by chpasswd
                File.WriteAllText(tempPath, $"{userName}:{password}\n");
                
                // Set restrictive permissions (Windows ACL)
                var fileInfo = new FileInfo(tempPath);
                var fileSecurity = fileInfo.GetAccessControl();
                fileSecurity.SetAccessRuleProtection(true, false); // Disable inheritance
                
                // Only current user can read
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
        public static void SecureDelete(string filePath)
        {
            if (!File.Exists(filePath))
                return;
                
            try
            {
                // Overwrite file with random data
                var fileInfo = new FileInfo(filePath);
                long length = fileInfo.Length;
                
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
```

2. Update `RegisterNewDistro_Page.xaml.cs`:
```csharp
// Replace lines 226-228
string tempPassFile = null;
try
{
    // Create secure temp file with password
    tempPassFile = SecurePasswordHelper.CreateSecurePasswordFile(userName, password);
    
    // Use file input instead of command line argument
    await helpers.ExecuteCommandInWSLAsync(distroName, 
        $"chpasswd < $(wslpath '{tempPassFile}')");
}
finally
{
    // Always clean up the password file
    if (tempPassFile != null)
        SecurePasswordHelper.SecureDelete(tempPassFile);
}
```

**Testing**:
- Create user with password
- Verify process list doesn't show password
- Verify temp file is deleted after use
- Test with special characters in password

---

### Task 2: Add Path Traversal Prevention
**Files**: 
- `easyWslLib/InputValidator.cs` - UPDATE
- `easyWSL/RegisterNewDistro_Page.xaml.cs` - UPDATE
- `easyWSL/ManageSnapshotsPage.xaml.cs` - UPDATE

**Effort**: 1 hour  
**Risk**: Low

**Current Issue**:
```csharp
// No validation that paths stay within allowed boundaries
var distroStoragePath = Path.Combine(storageDirectory.Path, distroName);
```

**Implementation**:

1. Add to `InputValidator.cs`:
```csharp
/// <summary>
/// Validates that a path doesn't escape the base directory (path traversal attack)
/// </summary>
public static (bool IsValid, string Error) ValidatePathWithinBase(string path, string baseDirectory)
{
    try
    {
        string fullPath = Path.GetFullPath(path);
        string fullBase = Path.GetFullPath(baseDirectory);
        
        if (!fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
        {
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
    
    return fileName;
}
```

2. Update `RegisterNewDistro_Page.xaml.cs`:
```csharp
// After line 413 - Validate path before creating directory
var distroStoragePath = Path.Combine(storageDirectory.Path, distroName);

var (pathValid, pathError) = InputValidator.ValidatePathWithinBase(
    distroStoragePath, 
    storageDirectory.Path
);

if (!pathValid)
{
    await showErrorModal($"Security error: {pathError}");
    return;
}

Directory.CreateDirectory(distroStoragePath);
```

**Testing**:
- Try distro name with `../../../etc/passwd`
- Try distro name with absolute path `C:\Windows`
- Verify paths stay within storage directory

---

### Task 3: Add Security Logging
**Files**: 
- `easyWslLib/SecurityLogger.cs` - NEW FILE
- Various files - Add logging calls

**Effort**: 2 hours  
**Risk**: Low

**Implementation**:

1. Create `SecurityLogger.cs`:
```csharp
using System;
using System.IO;

namespace easyWslLib
{
    public static class SecurityLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "easyWSL", "security.log"
        );
        
        static SecurityLogger()
        {
            // Ensure log directory exists
            var logDir = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
        }
        
        public static void LogSecurityEvent(string eventType, string details, bool isSuccess = true)
        {
            try
            {
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                string status = isSuccess ? "SUCCESS" : "FAILURE";
                string logEntry = $"[{timestamp}] [{status}] {eventType}: {details}\n";
                
                File.AppendAllText(LogPath, logEntry);
            }
            catch
            {
                // Don't throw if logging fails
            }
        }
        
        public static void LogValidationFailure(string inputType, string value, string reason)
        {
            LogSecurityEvent(
                "VALIDATION_FAILURE",
                $"Type={inputType}, Value={SanitizeForLog(value)}, Reason={reason}",
                isSuccess: false
            );
        }
        
        public static void LogCommandExecution(string command, string distroName)
        {
            // Don't log full command (may contain sensitive data)
            LogSecurityEvent(
                "COMMAND_EXECUTION",
                $"Distro={distroName}, Command={command.Substring(0, Math.Min(50, command.Length))}..."
            );
        }
        
        private static string SanitizeForLog(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "[empty]";
                
            // Truncate long values
            if (value.Length > 100)
                value = value.Substring(0, 100) + "...";
                
            // Remove newlines
            return value.Replace("\n", " ").Replace("\r", " ");
        }
    }
}
```

2. Add logging calls:
```csharp
// In InputValidator.cs
public static (bool IsValid, string Error) ValidateDistroName(string name)
{
    // ... existing validation ...
    
    if (!isValid)
    {
        SecurityLogger.LogValidationFailure("DistroName", name, error);
    }
    
    return (isValid, error);
}

// In RegisterNewDistro_Page.xaml.cs
await helpers.ExecuteCommandInWSLAsync(distroName, command);
SecurityLogger.LogCommandExecution(command, distroName);
```

---

### Task 4: Review File Operations Security
**Files**: All files with file operations

**Effort**: 1 hour  
**Risk**: Low

**Audit Points**:

1. **Check all `File.Delete` operations**:
   - Verify path is validated before deletion
   - Add try-catch for permission errors

2. **Check all `Directory.Delete` operations**:
   - Verify recursive deletes are intentional
   - Confirm user consent for destructive operations

3. **Check all file/directory creation**:
   - Validate paths don't escape base directories
   - Check for race conditions (TOCTOU)

4. **Check all file reads**:
   - Validate file extensions for safety
   - Size limits for user-provided files

**Example fixes**:
```csharp
// Before (unsafe)
File.Delete(distroTarballPath);

// After (safer)
if (File.Exists(distroTarballPath))
{
    var (isValid, _) = InputValidator.ValidatePathWithinBase(
        distroTarballPath,
        App.tmpDirectory.Path
    );
    
    if (isValid)
    {
        try
        {
            File.Delete(distroTarballPath);
        }
        catch (Exception ex)
        {
            SecurityLogger.LogSecurityEvent(
                "FILE_DELETE_FAILED",
                $"Path={distroTarballPath}, Error={ex.Message}",
                isSuccess: false
            );
        }
    }
}
```

---

### Task 5: Add Docker Image Name Validation
**Files**: 
- `easyWslLib/InputValidator.cs` - ENHANCE
- `easyWSL/RegisterNewDistro_Page.xaml.cs` - ADD VALIDATION

**Effort**: 30 minutes  
**Risk**: Low

**Implementation**:

Enhance existing `ValidateDockerImage()`:
```csharp
public static (bool IsValid, string Error) ValidateDockerImage(string image)
{
    if (string.IsNullOrWhiteSpace(image))
        return (false, "Docker image name cannot be empty");

    // Prevent malicious registry URLs
    if (image.Contains("://"))
        return (false, "Full URLs are not allowed. Use registry/image:tag format");
    
    // Prevent excessively long names (possible DoS)
    if (image.Length > 256)
        return (false, "Docker image name is too long");
    
    // Basic Docker image name validation
    var parts = image.Split(':');
    if (parts.Length > 2)
        return (false, "Invalid Docker image format");

    // Validate image name part
    var namePattern = @"^[a-z0-9]+([._-][a-z0-9]+)*(/[a-z0-9]+([._-][a-z0-9]+)*)*$";
    if (!Regex.IsMatch(parts[0], namePattern))
        return (false, "Invalid Docker image name format");
    
    // Validate tag if present
    if (parts.Length == 2)
    {
        var tagPattern = @"^[a-zA-Z0-9_][a-zA-Z0-9._-]{0,127}$";
        if (!Regex.IsMatch(parts[1], tagPattern))
            return (false, "Invalid Docker image tag format");
    }

    return (true, string.Empty);
}
```

Add validation in `RegisterNewDistro_Page.xaml.cs`:
```csharp
// Line 149 - Before downloading
image = dockerImageTextBox.Text;

var (imageValid, imageError) = InputValidator.ValidateDockerImage(image);
if (!imageValid)
{
    await showErrorModal(imageError);
    return;
}
```

---

### Task 6: Add Resource Limits
**Files**: 
- `easyWslLib/DockerDownloader.cs` - ADD LIMITS

**Effort**: 1 hour  
**Risk**: Low

**Implementation**:

Add download size limits to prevent DoS:
```csharp
public class DockerDownloader
{
    private const long MAX_LAYER_SIZE = 5L * 1024 * 1024 * 1024; // 5 GB
    private const long MAX_TOTAL_SIZE = 20L * 1024 * 1024 * 1024; // 20 GB
    private const int MAX_LAYERS = 100;
    
    public async Task DownloadImage(string distroImage)
    {
        // ... existing code ...
        
        var layersSizeList = layersSizeRegex.Cast<Match>()
            .Select(match => Convert.ToInt64(match.Value.Remove(0, 8)))
            .ToList();
        
        // Validate layer count
        if (layersList.Count > MAX_LAYERS)
        {
            throw new DockerException($"Image has too many layers ({layersList.Count}). Maximum is {MAX_LAYERS}.");
        }
        
        // Validate total size
        long totalSize = layersSizeList.Sum();
        if (totalSize > MAX_TOTAL_SIZE)
        {
            throw new DockerException($"Image too large ({totalSize / 1024 / 1024 / 1024} GB). Maximum is {MAX_TOTAL_SIZE / 1024 / 1024 / 1024} GB.");
        }
        
        // Validate individual layer sizes
        foreach (long layerSize in layersSizeList)
        {
            if (layerSize > MAX_LAYER_SIZE)
            {
                throw new DockerException($"Layer too large ({layerSize / 1024 / 1024 / 1024} GB). Maximum is {MAX_LAYER_SIZE / 1024 / 1024 / 1024} GB.");
            }
        }
        
        // ... rest of existing code ...
    }
}
```

---

## 🧪 Testing Strategy

### Security Tests

1. **Password Security**:
   - [ ] Create user with password
   - [ ] Check process list doesn't show password
   - [ ] Verify temp file is created with restricted permissions
   - [ ] Verify temp file is deleted after use
   - [ ] Test failure scenarios (exception during user creation)

2. **Path Traversal**:
   - [ ] Try `../../../Windows/System32` as distro name
   - [ ] Try absolute paths
   - [ ] Try UNC paths `\\server\share`
   - [ ] Verify all attempts are blocked

3. **Docker Image Validation**:
   - [ ] Try URL: `https://evil.com/malicious:latest`
   - [ ] Try excessively long name (300+ chars)
   - [ ] Try invalid characters
   - [ ] Verify legitimate images still work

4. **Resource Limits**:
   - [ ] Try downloading very large image (if available)
   - [ ] Verify limits are enforced
   - [ ] Verify error messages are clear

5. **Security Logging**:
   - [ ] Generate validation failures
   - [ ] Check security.log file created
   - [ ] Verify timestamps are correct
   - [ ] Verify sensitive data isn't logged

---

## 📊 Success Criteria

1. ✅ Passwords never visible in process arguments
2. ✅ Password temp files always cleaned up
3. ✅ Path traversal attempts blocked
4. ✅ All security events logged
5. ✅ Docker image names validated
6. ✅ Resource limits enforced
7. ✅ No degradation in functionality
8. ✅ All tests pass

---

## 🔍 Security Audit Checklist

- [ ] Review all user inputs for validation
- [ ] Check all file operations for path traversal
- [ ] Verify all sensitive data is handled securely
- [ ] Confirm all exceptions are caught appropriately
- [ ] Review all external command executions
- [ ] Verify logging doesn't contain sensitive data
- [ ] Check for race conditions (TOCTOU)
- [ ] Verify error messages don't leak information

---

## 📝 Implementation Order

**Day 1** (3-4 hours):
1. Create `SecurePasswordHelper.cs`
2. Update password handling in `RegisterNewDistro_Page.xaml.cs`
3. Add path traversal validation to `InputValidator.cs`
4. Test password security and path validation

**Day 2** (3-4 hours):
1. Create `SecurityLogger.cs`
2. Add logging throughout codebase
3. Enhance Docker image validation
4. Add resource limits to `DockerDownloader.cs`

**Day 3** (2 hours):
1. Security audit of all file operations
2. Comprehensive security testing
3. Fix any discovered issues
4. Documentation

---

## 🚀 Next Steps After Phase 2

Once Phase 2 is complete:
- All critical security vulnerabilities addressed
- Ready for Phase 3: Package updates and modernization
- Consider external security audit
- Prepare security documentation for users

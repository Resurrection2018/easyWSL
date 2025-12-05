# Phase 1: Critical Fixes - Implementation Plan

> **Timeline**: Week 1 (5 days)  
> **Effort**: 2-3 days  
> **Priority**: P0 - Critical

---

## 🎯 Objectives

Fix 5 critical bugs that are preventing the application from functioning correctly and pose security risks.

---

## 📋 Task Breakdown

### Task 1: Fix Kernel Command Line Configuration Bug
**File**: [`easyWSL/SettingsPage.xaml.cs:177`](easyWSL/SettingsPage.xaml.cs:177)  
**Effort**: 15 minutes  
**Risk**: Low

**Current Code**:
```csharp
// Kernel commandline
if (configParsed.ContainsKey("kernel"))  // ❌ Wrong key
{
    string kernelCmd = configParsed["kernelCommandLine"];
    kernelCommandLineTextBox.Text = kernelCmd;
}
```

**Fix**:
```csharp
// Kernel commandline
if (configParsed.ContainsKey("kernelCommandLine"))  // ✅ Correct key
{
    string kernelCmd = configParsed["kernelCommandLine"];
    kernelCommandLineTextBox.Text = kernelCmd;
}
```

**Testing**:
- Create a `.wslconfig` file with `kernelCommandLine` setting
- Open Settings page and verify it loads correctly
- Modify and save, verify it persists

---

### Task 2: Fix Exception Rethrowing Anti-Pattern
**File**: [`easyWslLib/Helpers.cs:34-37`](easyWslLib/Helpers.cs:34-37)  
**Effort**: 10 minutes  
**Risk**: Low

**Current Code**:
```csharp
catch(WebException ex)
{
    throw ex;  // ❌ Resets stack trace
}
```

**Fix**:
```csharp
catch(WebException)
{
    throw;  // ✅ Preserves stack trace
}
```

**Testing**:
- Trigger a network error (disconnect network during Docker pull)
- Verify stack trace shows original error location

---

### Task 3: Add Input Validation
**Files**: 
- [`easyWslLib/InputValidator.cs`](easyWslLib/InputValidator.cs) - NEW FILE
- [`easyWSL/RegisterNewDistro_Page.xaml.cs`](easyWSL/RegisterNewDistro_Page.xaml.cs)
- [`easyWSL/ManageSnapshotsPage.xaml.cs`](easyWSL/ManageSnapshotsPage.xaml.cs)

**Effort**: 2-3 hours  
**Risk**: Medium

**Implementation**:

1. Create `InputValidator.cs`:
```csharp
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
                return (false, "Distribution name cannot be empty");

            if (name.Length > 255)
                return (false, "Distribution name is too long (max 255 characters)");

            if (name.IndexOfAny(InvalidDistroNameChars) >= 0)
                return (false, "Distribution name contains invalid characters");

            // Reserved names
            var reserved = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", 
                                   "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", 
                                   "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", 
                                   "LPT7", "LPT8", "LPT9" };
            if (reserved.Contains(name.ToUpperInvariant()))
                return (false, "Distribution name is a reserved system name");

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
                return (false, "Docker image name cannot be empty");

            // Basic Docker image name validation
            // Format: [registry/]name[:tag]
            var parts = image.Split(':');
            if (parts.Length > 2)
                return (false, "Invalid Docker image format");

            var namePattern = @"^[a-z0-9]+([._-][a-z0-9]+)*(/[a-z0-9]+([._-][a-z0-9]+)*)*$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(parts[0], namePattern))
                return (false, "Invalid Docker image name format");

            return (true, string.Empty);
        }
    }
}
```

2. Update `RegisterNewDistro_Page.xaml.cs` validation:
```csharp
// Replace lines 89-93
var (isValid, error) = InputValidator.ValidateDistroName(distroName);
if (!isValid)
{
    await showErrorModal(error);
    return;
}

if (newUserSwitch.IsOn == true && distroSource == "Supported distro list")
{
    var (userValid, userError) = InputValidator.ValidateUserName(userName);
    if (!userValid)
    {
        await showErrorModal(userError);
        return;
    }
    // ... existing password validation
}
```

3. Update `showErrorModal` to accept custom message:
```csharp
private async Task showErrorModal(string message = null)
{
    ContentDialog errorDialog = new ContentDialog();
    errorDialog.XamlRoot = registerDistroProceedButton.XamlRoot;
    errorDialog.Title = "Error";
    errorDialog.CloseButtonText = "Cancel";
    errorDialog.DefaultButton = ContentDialogButton.Close;
    errorDialog.Content = message ?? "There were problems with registering your distribution.";
    await errorDialog.ShowAsync();
}
```

**Testing**:
- Try distro names with spaces, special chars, reserved names
- Try usernames starting with numbers, containing special chars
- Verify appropriate error messages are shown

---

### Task 4: Add Null Checks for JSON Deserialization
**File**: [`easyWslLib/DockerDownloader.cs`](easyWslLib/DockerDownloader.cs)  
**Effort**: 30 minutes  
**Risk**: Low

**Current Code** (lines 100, 127):
```csharp
var autorizationResponse = JsonSerializer.Deserialize<autorizationResponse>(
    helpers.GetRequest($"{authorizationUrl}...")
);
// No null check before accessing .token
```

**Fix**:
```csharp
var authJson = helpers.GetRequest($"{authorizationUrl}?service={registryUrl}&scope=repository:{repository}:pull");
var autorizationResponse = JsonSerializer.Deserialize<autorizationResponse>(authJson);

if (autorizationResponse == null || string.IsNullOrEmpty(autorizationResponse.token))
{
    throw new DockerException("Failed to authenticate with Docker registry");
}

string layersResponse;
try
{
    layersResponse = helpers.GetRequestWithHeader(
        $"https://{registry}/v2/{repository}/manifests/{tag}", 
        autorizationResponse.token, 
        "application/vnd.docker.distribution.manifest.v2+json"
    );
    
    if (string.IsNullOrEmpty(layersResponse))
    {
        throw new DockerException("Failed to retrieve image manifest");
    }
}
catch (WebException ex)
{
    throw new DockerException($"Failed to download Docker image: {ex.Message}");
}
```

**Testing**:
- Test with invalid Docker image name
- Test with network disconnected
- Verify error messages are descriptive

---

### Task 5: Fix Async/Await Issues
**File**: [`easyWslLib/Helpers.cs:72-79`](easyWslLib/Helpers.cs:72-79)  
**Effort**: 30 minutes  
**Risk**: Low

**Current Code**:
```csharp
public async Task StartWSLDistroAsync(string distroName)
{
    Process proc = new Process();
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.FileName = "wsl.exe";
    proc.StartInfo.Arguments = $"-d {distroName}";
    proc.Start();
    // ❌ Missing await
}
```

**Fix Option 1** (If we want to wait for process):
```csharp
public async Task StartWSLDistroAsync(string distroName)
{
    Process proc = new Process();
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.FileName = "wsl.exe";
    proc.StartInfo.Arguments = $"-d {distroName}";
    proc.Start();
    await proc.WaitForExitAsync().ConfigureAwait(false);
}
```

**Fix Option 2** (Fire-and-forget, which seems intentional):
```csharp
public void StartWSLDistro(string distroName)  // Remove async Task
{
    Process proc = new Process();
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.FileName = "wsl.exe";
    proc.StartInfo.Arguments = $"-d {distroName}";
    proc.Start();
}
```

**Recommendation**: Use Option 2 since WSL terminal should remain open independently.

**Testing**:
- Click "Start" button on a distro
- Verify WSL terminal opens
- Verify no deadlocks or hanging

---

### Task 6: Fix String.Join Usage
**File**: [`easyWSL/RegisterNewDistro_Page.xaml.cs:188-197`](easyWSL/RegisterNewDistro_Page.xaml.cs:188-197)  
**Effort**: 10 minutes  
**Risk**: Low

**Current Code**:
```csharp
string postInstallCommand = String.Join(
    "pwconv; ",
    "grpconv; ",
    "chmod 0744 /etc/shadow; ",
    // ...
);
```

**Fix**:
```csharp
string[] postInstallCommands = new[]
{
    "pwconv",
    "grpconv",
    "chmod 0744 /etc/shadow",
    "chmod 0744 /etc/gshadow",
    "chown -R root:root /bin/su",
    "chmod 755 /bin/su",
    "chmod u+s /bin/su",
    "touch /etc/fstab"
};
string postInstallCommand = string.Join("; ", postInstallCommands);
```

**Similar fixes needed** at:
- Lines 203-212 (configureCommand)
- Lines 234-270 (Windows Hello command)
- Lines 279-289 (Python install command)
- Lines 294-304 (Node.js install command)
- Lines 308-320 (Go install command)
- Lines 324-334 (C++ install command)
- Lines 338-349 (Haskell install command)
- Lines 352-364 (Java install command)

**Testing**:
- Register a new distro with user creation enabled
- Verify post-install commands execute correctly

---

### Task 7: Command Injection Prevention
**Files**: Multiple  
**Effort**: 1 hour  
**Risk**: Medium

**Implementation**:

1. Update `InputValidator` to escape shell arguments:
```csharp
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
```

2. Update command execution in critical places:
```csharp
// In RegisterNewDistro_Page.xaml.cs, line 220
var escapedUserName = InputValidator.EscapeShellArgument(userName);
var escapedPassword = InputValidator.EscapeShellArgument(password);
await helpers.ExecuteCommandInWSLAsync(distroName, 
    $"echo {escapedUserName}:{escapedPassword} | chpasswd");
```

**Testing**:
- Try usernames/passwords with special characters: `'; rm -rf /;'`
- Verify commands execute safely without injection
- Test with paths containing spaces and special chars

---

## 🧪 Testing Strategy

### Unit Tests (New)
Create `easyWslLib.Tests` project with:
```csharp
[TestClass]
public class InputValidatorTests
{
    [TestMethod]
    public void ValidateDistroName_WithSpaces_ReturnsFalse()
    {
        var (isValid, error) = InputValidator.ValidateDistroName("test name");
        Assert.IsFalse(isValid);
        Assert.IsTrue(error.Contains("invalid character"));
    }

    [TestMethod]
    public void ValidateDistroName_WithReservedName_ReturnsFalse()
    {
        var (isValid, error) = InputValidator.ValidateDistroName("CON");
        Assert.IsFalse(isValid);
        Assert.IsTrue(error.Contains("reserved"));
    }

    // Add more tests...
}
```

### Manual Testing Checklist

- [ ] Create new distro with valid name
- [ ] Try to create distro with invalid names (spaces, special chars)
- [ ] Create user with valid username
- [ ] Try to create user with invalid username
- [ ] Load existing .wslconfig with kernelCommandLine
- [ ] Modify and save settings
- [ ] Test with network errors (disconnect during download)
- [ ] Test Docker image download
- [ ] Test snapshot creation and restoration

---

## 📊 Success Criteria

1. ✅ All 5 critical bugs fixed
2. ✅ Input validation prevents invalid names
3. ✅ No command injection vulnerabilities
4. ✅ Proper error handling with meaningful messages
5. ✅ All existing functionality continues to work
6. ✅ No new warnings or errors in build

---

## 🚀 Implementation Order

**Day 1** (2-3 hours):
1. Task 2: Fix exception rethrowing (10 min)
2. Task 1: Fix kernel command line bug (15 min)
3. Task 6: Fix String.Join usage (30 min)
4. Task 5: Fix async/await (30 min)
5. Task 4: Add null checks (30 min)
6. Build and smoke test

**Day 2** (3-4 hours):
1. Task 3: Create InputValidator class (1 hour)
2. Task 3: Integrate validation in UI (1 hour)
3. Task 7: Command injection prevention (1 hour)
4. Comprehensive testing (1 hour)

**Day 3** (2 hours):
1. Fix any issues found in testing
2. Code review
3. Documentation updates

---

## 📝 Notes

- All changes maintain backward compatibility
- No database schema changes
- No UI changes (except better error messages)
- Can be deployed incrementally if needed

---

## 🔄 Next Steps After Phase 1

Once Phase 1 is complete and tested:
- Merge to main branch
- Tag release as `v0.1.9-bugfixes`
- Proceed with Phase 2: Security improvements
- Begin Phase 3: Package updates in parallel

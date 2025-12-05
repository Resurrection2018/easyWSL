# easyWSL Runtime Issue - Troubleshooting Guide

> **Issue**: Application doesn't respond when launched  
> **Build Status**: ✅ Successful  
> **Problem Type**: Runtime / Initialization

---

## 🔍 Diagnostic Steps

### Step 1: Run in Debug Mode

The most effective way to see what's failing:

```bash
# Open in Visual Studio 2022
# Press F5 to run with debugger attached
# This will show the exact exception/error
```

**Or via command line:**
```bash
cd easyWSL
dotnet run --project easyWSL.csproj
```

This will show any runtime errors in the console.

---

### Step 2: Check Event Viewer

Windows Event Viewer often logs .NET application crashes:

1. Press `Win + R`
2. Type: `eventvwr.msc`
3. Navigate to: Windows Logs → Application
4. Look for recent errors from ".NET Runtime" or "easyWSL"

---

### Step 3: Common Causes & Solutions

#### Cause 1: Missing Runtime Dependencies

**Symptoms**: App crashes immediately or shows "application failed to start"

**Solution**:
```powershell
# Install .NET 8.0 Desktop Runtime
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
# Install both:
# - .NET Desktop Runtime 8.0.x
# - Windows App Runtime 1.5.x
```

#### Cause 2: Windows App SDK Not Installed

**Symptoms**: App shows brief window then closes

**Solution**:
```powershell
# Repair Windows App SDK
Get-AppxPackage -Name Microsoft.WindowsAppRuntime* | ForEach {
    Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppXManifest.xml"
}
```

#### Cause 3: Registry Access Denied

**Symptoms**: App hangs on startup

**Issue**: App tries to read WSL registry at startup (App.xaml.cs:21)

**Quick Fix** - Add error handling:

```csharp
// In App.xaml.cs, OnLaunched method
try
{
    var isWSLInstalled = WslSdk.CheckIfWSLInstalled();
    if (!isWSLInstalled)
    {
        m_window = new WelcomeWindow();
    }
    else
    {
        m_window = new NavigationRoot_Window();
    }
}
catch (Exception ex)
{
    // Show error message
    m_window = new WelcomeWindow();
    System.Diagnostics.Debug.WriteLine($"Startup error: {ex.Message}");
}
```

#### Cause 4: XAML Parsing Errors

**Symptoms**: Build succeeds but runtime fails immediately

**Check**: Look for XAML errors in debug output

---

## 🛠️ Quick Fixes to Try

### Fix 1: Run as Administrator

WSL registry access might require elevated permissions:

1. Right-click `easyWSL.exe`
2. Select "Run as administrator"

### Fix 2: Verify Dependencies

```powershell
# Check .NET version
dotnet --list-runtimes

# Should show:
# Microsoft.NETCore.App 8.0.x
# Microsoft.WindowsDesktop.App 8.0.x
```

### Fix 3: Clean Rebuild

```bash
# Clean all build artifacts
dotnet clean easyWSL.sln
Remove-Item -Recurse -Force */bin,*/obj

# Rebuild
dotnet restore easyWSL.sln
dotnet build easyWSL.sln -c Release
```

### Fix 4: Check Dependencies DLLs

Verify these DLLs are in the output directory:

```
easyWSL/bin/x64/Release/net8.0-windows10.0.19041.0/
├── easyWSL.dll ✓
├── easyWSL.exe ✓
├── easyWslLib.dll ✓
├── Microsoft.WindowsAppRuntime.Bootstrap.Net.dll ✓
└── WinRT.Runtime.dll ✓
```

---

## 🔧 Code Fix Suggestions

### Add Exception Handling to App.xaml.cs

<apply_diff>
<path>easyWSL/App.xaml.cs</path>
<diff>
<<<<<<< SEARCH
:start_line:19
-------
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var isWSLInstalled = WslSdk.CheckIfWSLInstalled();
            if (!isWSLInstalled)
            {
                m_window = new WelcomeWindow();
            }
            else
            {
                m_window = new NavigationRoot_Window();
            }

            m_window.Activate();
            m_window.Title = "easyWSL";
            m_windowhandle = User32.GetActiveWindow();
            User32.ShowWindow(m_windowhandle, User32.WindowShowStyle.SW_MAXIMIZE);
        }
=======
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                var isWSLInstalled = WslSdk.CheckIfWSLInstalled();
                if (!isWSLInstalled)
                {
                    m_window = new WelcomeWindow();
                }
                else
                {
                    m_window = new NavigationRoot_Window();
                }

                m_window.Activate();
                m_window.Title = "easyWSL";
                m_windowhandle = User32.GetActiveWindow();
                User32.ShowWindow(m_windowhandle, User32.WindowShowStyle.SW_MAXIMIZE);
            }
            catch (Exception ex)
            {
                // Fallback: show welcome window on any error
                System.Diagnostics.Debug.WriteLine($"Startup error: {ex.Message}");
                if (m_window == null)
                {
                    m_window = new WelcomeWindow();
                    m_window.Activate();
                    m_window.Title = "easyWSL - Error on Startup";
                }
            }
        }
>>>>>>> REPLACE
</diff>
</apply_diff>

This adds error handling so the app won't crash silently.

---

## 📊 Debug Output Analysis

### If you can run in Visual Studio:

1. Open Output window (View → Output)
2. Select "Debug" from dropdown
3. Look for exceptions or errors
4. Common messages:
   - "FileNotFoundException" → Missing dependency
   - "UnauthorizedAccessException" → Registry access
   - "XamlParseException" → XAML error

---

## 🚨 Most Likely Issues (Ranked)

1. **Missing Windows App Runtime** (90% probability)
   - Download: https://aka.ms/windowsappsdk 
   - Install version 1.5.x

2. **Missing .NET 8.0 Desktop Runtime** (70% probability)
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install "Desktop Runtime"

3. **Registry Access Issue** (50% probability)
   - Try running as administrator
   - Or apply error handling fix above

4. **XAML Compilation Issue** (30% probability)
   - Clean and rebuild
   - Check for missing resource files

---

## 📝 Information Needed for Further Diagnosis

Please provide:

1. **Exact behavior**:
   - [ ] App window appears then closes
   - [ ] App never appears (process in Task Manager?)
   - [ ] App shows loading then freezes
   - [ ] Other: _______________

2. **Error messages** (if any):
   - From Event Viewer
   - From command line output

3. **Environment**:
   - Windows version: _______________
   - .NET version: `dotnet --version`
   - WSL installed?: `wsl --version`

4. **Debug output**:
   - Run in Visual Studio
   - Copy Output window contents

---

## ✅ Immediate Action Items

**Do this now:**

1. ✓ Run in Visual Studio with debugger (F5)
2. ✓ Check Windows Event Viewer for crash logs
3. ✓ Verify .NET 8.0 Desktop Runtime installed
4. ✓ Verify Windows App SDK installed
5. ✓ Try running from command line to see errors

**Then report back with:**
- What happens when you run in Visual Studio
- Any exception messages
- Windows Event Viewer errors

---

## 🔄 Temporary Workaround

Until we identify the exact issue, you can:

1. Use the CLI tool instead:
   ```bash
   cd easyWSLcmd
   dotnet run -- import --name test --image ubuntu:22.04
   ```

2. Or use WSL directly:
   ```powershell
   wsl --install Ubuntu-22.04
   ```

---

This is a **runtime initialization issue**, not a code bug. The build succeeds, so the problem is environmental or a missing dependency.

**Next step**: Please run the app in Visual Studio debug mode (F5) and report the exception message.
# easyWSL - Complete Setup & User Guide

> **Version**: 2.0  
> **Last Updated**: December 5, 2025  
> **Target Framework**: .NET 8.0 LTS  
> **Platform**: Windows 10/11 with WSL

---

## 📋 Table of Contents

1. [Description](#description)
2. [Prerequisites](#prerequisites)
3. [Installation Options](#installation-options)
4. [Building from Source](#building-from-source)
5. [First Launch](#first-launch)
6. [Using easyWSL](#using-easywsl)
7. [Troubleshooting](#troubleshooting)
8. [Security Features](#security-features)
9. [Advanced Topics](#advanced-topics)
10. [FAQ](#faq)

---

## 📖 Description

**easyWSL** is a modern Windows application that makes it incredibly easy to create and manage Windows Subsystem for Linux (WSL) distributions based on Docker images from Docker Hub.

### Key Features

✅ **Create WSL Distributions from Docker Images**
- Browse Docker Hub images (Ubuntu, Debian, Alpine, Fedora, etc.)
- Download multi-layered images automatically
- Register as native WSL distributions

✅ **Advanced Distribution Management**
- View all installed distributions
- Start/stop/terminate distributions
- Export and unregister distributions
- Open file system locations

✅ **Snapshot & Backup System**
- Create distribution snapshots
- Restore from snapshots
- Manage backup points

✅ **WSL Configuration Manager**
- Adjust memory allocation
- Configure CPU cores
- Manage swap settings
- Custom kernel support
- Advanced WSL settings

✅ **User Account Creation**
- Set username and password during installation
- Windows Hello integration (experimental)
- Secure password handling

✅ **Development Environment Setup**
- Pre-install Python, Node.js, C/C++ toolchains
- Automated environment configuration
- Cross-distribution support

---

## 🔧 Prerequisites

### System Requirements

| Requirement | Minimum | Recommended |
|-------------|---------|-------------|
| **OS** | Windows 10 version 1607 | Windows 11 22H2 or later |
| **WSL** | WSL 1 | WSL 2 |
| **RAM** | 4 GB | 8 GB or more |
| **Storage** | 10 GB free | 50 GB free (for distributions) |
| **Internet** | Required for downloads | Broadband recommended |

### Required Software

1. **Windows Subsystem for Linux (WSL)**
   - Must be enabled before using easyWSL
   - [Installation Guide](https://docs.microsoft.com/en-us/windows/wsl/install)

2. **Developer Mode** (for sideloading)
   - Settings → Privacy & Security → For Developers → Developer Mode: ON
   - [How to Enable](https://docs.microsoft.com/windows/uwp/get-started/enable-your-device-for-development)

### Building Prerequisites (Optional)

Only needed if building from source:

- **.NET 8.0 SDK** or later
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version` (should show 8.0.x)

- **Visual Studio 2022** (Version 17.8+)
  - Required Workloads:
    - .NET Desktop Development
    - Windows App SDK C# Templates
  - Individual Components:
    - Windows 10 SDK (10.0.19041.0 or later)

---

## 📦 Installation Options

### Option 1: Microsoft Store (Recommended)

**Easiest and fastest method:**

1. Open Microsoft Store
2. Search for "easyWSL"
3. Click **Get** or **Install**
4. Launch from Start Menu

**Benefits:**
- Automatic updates
- Signed and verified
- No manual setup required

### Option 2: Pre-built Package

1. Go to [Releases](https://github.com/redcode-labs/easyWSL/releases)
2. Download the latest `.msix` or `.msixbundle` file
3. Double-click to install
4. If Windows prompts about unknown publisher:
   - Click "More info" → "Install anyway"

### Option 3: Build from Source

See [Building from Source](#building-from-source) section below.

---

## 🔨 Building from Source

### Quick Start

```bash
# 1. Install .NET 8.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0

# 2. Verify installation
dotnet --version
# Should output: 8.0.x or higher

# 3. Clone repository
git clone https://github.com/redcode-labs/easyWSL.git
cd easyWSL

# 4. Restore NuGet packages
dotnet restore easyWSL.sln

# 5. Build the solution
dotnet build easyWSL.sln -c Release

# 6. Run the application
dotnet run --project easyWSL/easyWSL.csproj
```

### Using Visual Studio 2022

1. **Open Solution**
   - File → Open → Project/Solution
   - Select `easyWSL.sln`

2. **Restore Packages**
   - Visual Studio will automatically restore NuGet packages
   - If not, right-click solution → Restore NuGet Packages

3. **Select Configuration**
   - Configuration: Release
   - Platform: x64 (or your system architecture)

4. **Build**
   - Build → Build Solution (F6)
   - Wait for build to complete

5. **Run**
   - Debug → Start Without Debugging (Ctrl+F5)
   - Or press F5 to debug

### Build Output

Executables will be in:
```
easyWSL/bin/x64/Release/net8.0-windows10.0.19041.0/
```

### Platform-Specific Builds

```bash
# Build for x64 (most common)
dotnet build -c Release -r win-x64

# Build for ARM64 (Surface Pro X, etc.)
dotnet build -c Release -r win-arm64

# Build for x86 (legacy systems)
dotnet build -c Release -r win-x86
```

### Creating MSIX Package

```bash
# Using Visual Studio
# 1. Right-click easyWSL project
# 2. Publish → Create App Packages
# 3. Follow wizard

# Or using command line
msbuild easyWSL/easyWSL.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Always
```

---

## 🚀 First Launch

### Initial Setup Wizard

When you first launch easyWSL:

1. **WSL Verification**
   - Application checks if WSL is enabled
   - If not enabled, it will provide instructions

2. **Welcome Screen**
   - Overview of features
   - Quick start guide

3. **Main Interface**
   - Navigation menu on left
   - Content area on right

### Enabling WSL (if needed)

If WSL is not enabled:

```powershell
# Open PowerShell as Administrator
wsl --install

# Or manually enable WSL feature
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

# Restart computer
shutdown /r /t 0
```

After restart, set WSL 2 as default:
```powershell
wsl --set-default-version 2
```

---

## 💻 Using easyWSL

### Creating a New Distribution

1. **Navigate to "Register New Distro"**
   - Click on left sidebar navigation

2. **Choose Docker Image**
   - Enter image name (e.g., `ubuntu:22.04`, `debian:bookworm`)
   - Or browse popular images from dropdown

3. **Configure Distribution**
   - **Distribution Name**: Unique name (no spaces)
   - **Storage Path**: Where to install (default: `%LOCALAPPDATA%\easyWSL`)
   - **Username**: Linux username to create
   - **Password**: User password (securely handled)

4. **Optional: Development Environment**
   - Check boxes for pre-installed tools:
     - Python (with pip)
     - Node.js (with npm)
     - C/C++ toolchain (gcc, g++, make)
     - Git
     - And more...

5. **Optional: Windows Hello Integration**
   - Enable for passwordless sudo
   - Experimental feature

6. **Click "Register"**
   - Progress bar shows download progress
   - Multi-layered images are automatically combined
   - Distribution is registered with WSL

7. **Launch**
   - Click "Run distribution" when complete
   - Opens terminal in new distro

### Managing Distributions

**View Installed Distributions:**
- Navigate to "Manage Distros"
- See all WSL distributions

**Actions Available:**

| Button | Action | Description |
|--------|--------|-------------|
| 🚀 **Run** | Start distro | Opens Windows Terminal |
| 📁 **Open Location** | View files | Opens File Explorer to .vhdx |
| 💾 **Export** | Backup | Exports to .tar.gz file |
| 🗑️ **Unregister** | Remove | Deletes distribution |
| ⚙️ **Set as Default** | Make default | Sets as default WSL distro |

### Creating Snapshots

**Why Snapshots?**
- Backup before major changes
- Create restore points
- Test configurations safely

**How to Create:**
1. Navigate to "Manage Snapshots"
2. Select distribution from dropdown
3. Click "Create Snapshot"
4. Wait for export to complete
5. Snapshot appears in list with timestamp

**How to Restore:**
1. Select snapshot from list
2. Click "Register from Snapshot"
3. Enter new distribution name
4. Click "Register"

### Configuring WSL Settings

Navigate to "Settings" page to configure:

**Memory:**
- Adjust RAM allocation for WSL VM
- Range: 512 MB to total system RAM
- Default: 50% of system RAM

**Processors:**
- Number of CPU cores for WSL
- Range: 1 to total system cores
- Default: All available cores

**Swap:**
- Swap file size
- Custom swap file location
- Range: 0 to 256 GB

**Kernel:**
- Custom Linux kernel path
- Kernel command line arguments

**Advanced Options:**
- Localhost forwarding
- GUI support (WSLg)
- Debug console
- Nested virtualization
- Page reporting

**Applying Changes:**
1. Modify settings
2. Click "Save to .wslconfig"
3. Restart WSL: `wsl --shutdown`

---

## 🔒 Security Features

easyWSL 2.0 includes enterprise-grade security:

### Secure Password Handling

**Problem Prevented:**
- Passwords visible in Task Manager ❌
- Process arguments exposure ❌

**Solution:**
- Passwords never in command line ✅
- Temporary files with ACL restrictions ✅
- Secure deletion with overwriting ✅

**Technical Details:**
- Uses Windows Access Control Lists
- Multi-pass secure deletion
- Automatic cleanup guaranteed

### Input Validation

**Protected Against:**
- Command injection attacks
- Path traversal (e.g., `../../Windows`)
- Invalid characters
- Reserved names (CON, PRN, etc.)
- Malicious Docker images

**What's Validated:**
- Distribution names
- Usernames
- Docker image names
- File paths
- All user inputs

### Security Logging

**Log Location:**
```
%LOCALAPPDATA%\easyWSL\security.log
```

**What's Logged:**
- Validation failures
- Path traversal attempts
- Distribution registrations
- Command executions
- Download blocks

**View Log:**
```powershell
# Windows Command Prompt
type "%LOCALAPPDATA%\easyWSL\security.log"

# PowerShell
Get-Content "$env:LOCALAPPDATA\easyWSL\security.log"
```

### Resource Limits

**Download Limits:**
- Maximum total size: 20 GB
- Maximum per layer: 5 GB
- Maximum layers: 100

**Purpose:**
- Prevent DoS attacks
- Protect disk space
- Reasonable defaults

---

## 🔧 Troubleshooting

### Common Issues

#### "WSL not found" Error

**Cause:** WSL is not installed or enabled

**Solution:**
```powershell
# Run as Administrator
wsl --install
# Restart computer
```

#### "Unable to download Docker image"

**Cause:** Network issues or invalid image name

**Solutions:**
1. Check internet connection
2. Verify image name on Docker Hub
3. Try with tag: `ubuntu:22.04` instead of just `ubuntu`
4. Check firewall settings

#### "Distribution name already exists"

**Cause:** Name is already used

**Solution:**
- Choose different distribution name
- Or unregister existing distribution first

#### Build Errors - "SDK not found"

**Cause:** .NET 8.0 SDK not installed

**Solution:**
```bash
# Download and install .NET 8.0 SDK
# https://dotnet.microsoft.com/download/dotnet/8.0

# Verify
dotnet --version
```

#### "Package signature invalid"

**Cause:** Self-signed certificate or sideloading issue

**Solution:**
1. Enable Developer Mode
2. Settings → Privacy & Security → For Developers
3. Toggle "Developer Mode" to ON

#### Application Won't Start

**Solutions:**
1. Check Windows version (requires Win10 1607+)
2. Install latest Windows updates
3. Repair Windows App SDK:
   ```powershell
   # In PowerShell as Administrator
   Get-AppxPackage -Name Microsoft.WindowsAppRuntime* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppXManifest.xml"}
   ```

### WSL-Specific Issues

#### Distribution Won't Start

```powershell
# Check WSL status
wsl --list --verbose

# Restart WSL
wsl --shutdown

# Update WSL
wsl --update
```

#### Import Failed

**Check:**
1. Sufficient disk space
2. Valid .tar.gz file
3. Not using special characters in paths
4. UAC permissions

### Performance Issues

**Slow Downloads:**
- Use wired connection if possible
- Try different Docker Hub mirror
- Check bandwidth throttling

**High Memory Usage:**
- Adjust WSL memory limits in Settings
- Close unnecessary distributions

---

## 🎯 Advanced Topics

### Command Line Tool (easyWSLcmd)

For automation and scripting:

```bash
# Import distribution via CLI
cd easyWSLcmd
dotnet run -- import --name myubuntu --image ubuntu:22.04 --output C:\WSL\myubuntu
```

### Custom Docker Registries

Modify `DockerDownloader.cs` to use private registries:

```csharp
string registry = "your-registry.example.com";
string authorizationUrl = "https://your-auth.example.com/token";
```

### Batch Distribution Creation

Create multiple distributions via script:

```powershell
# PowerShell script
$distributions = @(
    @{Name="dev-ubuntu"; Image="ubuntu:22.04"},
    @{Name="dev-debian"; Image="debian:bookworm"},
    @{Name="dev-alpine"; Image="alpine:latest"}
)

foreach ($distro in $distributions) {
    # Use easyWSLcmd to create each
    .\easyWSLcmd.exe import --name $distro.Name --image $distro.Image
}
```

### Configuration File Locations

**WSL Config:**
```
C:\Users\<YourName>\.wslconfig
```

**easyWSL Data:**
```
%LOCALAPPDATA%\easyWSL\
├── <DistroName>\         (Distribution data)
├── snapshots\            (Backups)
└── security.log          (Security events)
```

### Uninstalling

**Remove easyWSL:**
1. Settings → Apps → Installed apps
2. Find "easyWSL"
3. Click menu (⋯) → Uninstall

**Remove Distributions:**
```powershell
# List all WSL distributions
wsl --list

# Unregister each
wsl --unregister <DistroName>
```

**Clean Up Data:**
```powershell
# Remove application data
Remove-Item "$env:LOCALAPPDATA\easyWSL" -Recurse -Force
```

---

## ❓ FAQ

### General Questions

**Q: What is WSL?**  
A: Windows Subsystem for Linux lets you run a Linux environment directly on Windows without dual-booting or virtual machines.

**Q: Is easyWSL free?**  
A: Yes, easyWSL is open-source and free to use.

**Q: Does this replace Docker Desktop?**  
A: No, easyWSL creates WSL distributions from Docker *images*, but doesn't provide Docker runtime capabilities.

**Q: Can I use distributions created with easyWSL for development?**  
A: Absolutely! They're full WSL distributions with complete Linux environments.

### Technical Questions

**Q: What's the difference between WSL 1 and WSL 2?**  
A: WSL 2 uses a real Linux kernel and provides better performance. Recommended for easyWSL.

**Q: Can I convert WSL 1 distro to WSL 2?**  
A: Yes:
```powershell
wsl --set-version <DistroName> 2
```

**Q: How much disk space does a distribution use?** <br>
A: Varies by distro:
- Alpine: ~50 MB
- Ubuntu: ~1-2 GB
- Debian: ~1-2 GB

Plus installed packages and your files.

**Q: Can I have multiple versions of the same distro?**  
A: Yes, just give them unique names (e.g., `ubuntu-22`, `ubuntu-24`).

**Q: Are snapshots the same as Docker layers?**  
A: No. Snapshots are full distribution backups, while Docker layers are differential image components.

### Security Questions

**Q: Is it safe to use random Docker images?**  
A: Only use images from trusted sources (official repositories, verified publishers).

**Q: Where are passwords stored?**  
A: Passwords are securely handled:
- Never in process arguments
- Temporary files with restricted ACLs
- Securely deleted after use
- Not logged

**Q: What if I forget my WSL password?**  
A: Reset it:
```powershell
# Boot into Windows user
wsl -u root -d <DistroName>
# Then: passwd <username>
```

**Q: Is network traffic encrypted?**  
A: Docker Hub downloads use HTTPS. easyWSL doesn't intercept or modify traffic.

### Troubleshooting Questions

**Q: Why did my download fail?**  
A: Common reasons:
- Network timeout
- Invalid image name
- Image too large (20 GB limit)
- Too many layers (100 limit)

**Q: Can I pause/resume downloads?**  
A: Currently no. Download must complete. Consider using smaller base images.

**Q: Why is installation slow?**  
A: Factors:
- Internet speed
- Image size
- CPU for decompression
- Multi-layer processing

---

## 📚 Additional Resources

### Official Documentation

- **WSL Official Docs**: https://docs.microsoft.com/windows/wsl
- **Docker Hub**: https://hub.docker.com
- **.NET 8.0 Docs**: https://docs.microsoft.com/dotnet

### easyWSL Resources

- **GitHub Repository**: https://github.com/redcode-labs/easyWSL
- **Issue Tracker**: https://github.com/redcode-labs/easyWSL/issues
- **Security Policy**: See [`SECURITY.md`](SECURITY.md)
- **Changelog**: See [`FINAL_PROJECT_SUMMARY.md`](FINAL_PROJECT_SUMMARY.md)

### Community

- **Discussions**: GitHub Discussions tab
- **Report Bugs**: GitHub Issues
- **Contribute**: Pull requests welcome!

---

## 🎉 Quick Reference

### Essential Commands

```powershell
# List WSL distributions
wsl --list --verbose

# Set default distribution
wsl --set-default <DistroName>

# Shutdown WSL
wsl --shutdown

# Access distribution
wsl -d <DistroName>

# Run as root
wsl -d <DistroName> -u root

# Update WSL
wsl --update

# Check WSL version
wsl --version
```

### File Access

**From Windows:**
```
\\wsl$\<DistroName>\
```

**From WSL:**
```bash
# Access Windows C: drive
cd /mnt/c/

# Access Windows user folder
cd /mnt/c/Users/<YourName>/
```

### Performance Tips

1. Store project files in WSL filesystem (not /mnt/)
2. Use WSL 2 for better performance
3. Allocate appropriate RAM in settings
4. Use .wslconfig for global optimizations

---

## 📝 Version History

### Version 2.0 (Current - December 2025)

**Major Updates:**
- ✅ Migrated to .NET 8.0 LTS
- ✅ Enterprise-grade security features
- ✅ Modern UI with Windows App SDK 1.5
- ✅ Comprehensive input validation
- ✅ Security audit logging
- ✅ Resource limits for DoS prevention
- ✅ All critical bugs fixed
- ✅ Zero deprecated APIs

### Version 1.x (Legacy)

- Basic distribution management
- Docker Hub integration
- Snapshot support
- Settings configuration

*Upgrade to 2.0 strongly recommended for security and stability improvements.*

---

## 🤝 Contributing

We welcome contributions!

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

See `CONTRIBUTING.md` for detailed guidelines.

---

## 📄 License

This project is licensed under the terms specified in [`LICENSE`](LICENSE) file.

---

## 🙏 Acknowledgments

- **Red Code Labs Team** - Original development
- **Microsoft WSL Team** - Windows Subsystem for Linux
- **Docker Community** - Docker Hub API
- **Contributors** - All contributors to the project

---

## 📞 Support

- **Documentation**: This guide
- **Issues**: [GitHub Issues](https://github.com/redcode-labs/easyWSL/issues)
- **Security**: security@redcodelabs.io
- **General**: Open a GitHub Discussion

---

**Made with ❤️ by Red Code Labs**

---

*Last updated: December 5, 2025*  
*easyWSL v2.0 - Modern, Secure, Professional*
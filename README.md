<h1 align="center"> easyWSL</h1> <br>
<p align="center">
  <a>
    <img src="./assets/img/easyWSL-Logo.png" width="450">
  </a>
</p>

<p align="center">
  Create WSL distros based on Docker Images.
</p>

> Made with ❤ by @redcode-labs team.

## What does this project do?

easyWSL makes it way easier to use the wonders of WSL functionality on Windows 10 and 11 systems. Thanks to our efforts, easyWSL grants you an access to use most (almost all) system images from Docker Hub as WSL distros. Our C# sorcery allows us to use Docker Hub API in order to get .tar or .tar.gz images from there. After getting an image, single or multi-layered, we turn it into single image (multi-layered Docker image case) which we easily import as WSL distro.

https://user-images.githubusercontent.com/40501814/161391038-10bf9360-429d-4a47-887f-63b35fae90cd.mp4

## Features

In our latest release, we've added even more features than ever before. All of them wrapped around GUI app using a beauty of WinUI framework.

We've managed to add management features to WSL. With a single click, you can unregister, open .vhdx location as well as see the filesystem of the given distro.

https://user-images.githubusercontent.com/40501814/161391082-101aeaa4-48e1-4cc6-bac3-9bc251efe469.mp4

While this sounds so good so far, we've also made a separate 'Settings' page, to manage your WSL in general. There you can easily adjust things like memory, the number of cores or the swap size assigned with WSL VM. You can also point to different swap file, custom Linux kernel and provide it with custom command line arguments. The given page has also a number of switches, with which you can manage things like localhost forwarding, page reporing, GUI applications (WSLg) support, debug console, nested virtualization. Don't worry if you get lost - you can always just 'Revert to defaults'.

We've added a functionality to register currently or previously used WSL distros using their .vhdx file. Going further down the rabbit hole, we've turned it into more advanced feature - easyWSL can now make snapshots, which can easily be used as a backups/restore points for your distros.

https://user-images.githubusercontent.com/40501814/161391108-42a4e891-99da-4d36-a49d-3960dff28410.mp4

We've added several experimental features as well. One of them, is creating a new user at the time of installing a distro. You now can set the username and password, and, if you want to, use experimental integration of [WSL-Hello-sudo](https://github.com/nullpo-head/WSL-Hello-sudo).

Furthermore, you can use an experimental feature to install development environments during the install process. This includes environments such as Python, Node.js, C/C++ and more. Not to mention, that it works cross-distro and more can be added along the way.

https://user-images.githubusercontent.com/40501814/161391121-e76dc012-b819-434b-acb2-0d2696862911.mp4

## How to get it?

Just go to our Microsoft Store page, which you can find [here](https://www.microsoft.com/store/apps/9NHBTMKS47RB).
Get it, install, and voilà!

## Building on your own

### Prerequisites

* Windows 10 1607 or later (for WSL1) or Windows 10 1903 (18362) or later
   * Note: you might want to check instructions on how to enable WSL [here](https://docs.microsoft.com/en-us/windows/wsl/install-manual)
* [Developer Mode enabled](https://docs.microsoft.com/windows/uwp/get-started/enable-your-device-for-development)
* **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later** (Required)
* [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) (17.8 or later recommended)
* The following workloads:
   * .NET Desktop Development
   * Windows App SDK C# Templates
* Individual components:
   * Windows 10 SDK (10.0.19041.0 or later)
   * Windows App SDK (automatically installed with workload)

More detailed info can be found in the [Windows App SDK documentation](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment)

(Upon opening the repo in Visual Studio, it will prompt you to install any missing workloads and features.)

### Building

#### Using Visual Studio
1. Open `easyWSL.sln` in Visual Studio 2022
2. Restore NuGet packages (automatic)
3. Build the solution (F6 or Build → Build Solution)
4. Run the project (F5 or Debug → Start Debugging)

#### Using Command Line
```bash
# Clone the repository
git clone https://github.com/redcode-labs/easyWSL.git
cd easyWSL

# Restore NuGet packages
dotnet restore easyWSL.sln

# Build the solution
dotnet build easyWSL.sln -c Release

# Run the UI application
dotnet run --project easyWSL/easyWSL.csproj

# Or run the CLI tool
cd easyWSLcmd
dotnet run -- import --name test-distro --image ubuntu:22.04
```

### Build Configurations
- **Debug**: Development builds with debug symbols
- **Release**: Optimized builds for production
- **Platforms**: x86, x64, ARM64

## Recent Updates

### Version 2.0 (December 2025) - Major Security & Modernization Release

**Framework Modernization**:
- ✅ Migrated to .NET 8.0 LTS (supported until November 2026)
- ✅ Updated all NuGet packages to latest stable versions
- ✅ Removed all deprecated APIs (WebRequest → HttpClient)
- ✅ Modern Windows App SDK 1.5

**Security Enhancements**:
- ✅ **Secure password handling** - Passwords never exposed in process arguments
- ✅ **Path traversal prevention** - Blocks directory escape attacks
- ✅ **Input validation** - Comprehensive validation of all user inputs
- ✅ **Security logging** - Complete audit trail at `%LOCALAPPDATA%\easyWSL\security.log`
- ✅ **Resource limits** - Protection against DoS attacks (20GB max downloads)
- ✅ **Command injection prevention** - Shell argument escaping

**Bug Fixes**:
- ✅ Fixed kernel command line configuration loading
- ✅ Fixed exception stack trace preservation
- ✅ Fixed async/await issues
- ✅ Added null reference checks
- ✅ Fixed string concatenation bugs

**For detailed changelog, see**: `PHASE*_COMPLETION_SUMMARY.md` files

**For security information**, see: [`SECURITY.md`](SECURITY.md)

---

## Security

easyWSL 2.0 includes enterprise-grade security features. For details on our security implementation, vulnerability reporting, and best practices, please see our [Security Policy](SECURITY.md).

---

## Future plans

### CLI

A command-line tool (`easyWSLcmd`) is included in this repository for automation and scripting scenarios.

For legacy CLI support, older version (easyWSL 1.2) is available [here](https://github.com/redcode-labs/easyWSL/releases/tag/1.2).

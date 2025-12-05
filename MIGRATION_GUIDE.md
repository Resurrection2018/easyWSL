# easyWSL Migration Guide

## Overview

This guide provides step-by-step instructions for migrating the easyWSL codebase from its current state to a modernized, secure, and maintainable architecture. The migration is designed to be incremental to minimize disruption and allow for thorough testing at each stage.

## Pre-Migration Checklist

### 1. Environment Setup
- [ ] Backup current codebase
- [ ] Install .NET 8.0 SDK
- [ ] Update Visual Studio to latest version (2022 17.8+)
- [ ] Install required Visual Studio workloads:
  - .NET Desktop Development
  - Windows App SDK development
- [ ] Set up version control branching strategy
- [ ] Create test environment for validation

### 2. Dependencies Analysis
- [ ] Document current package versions
- [ ] Identify breaking changes in package updates
- [ ] Plan rollback strategy for each dependency update

## Phase 1: Foundation Updates (Week 1-2)

### Step 1.1: Update Target Framework

#### Action Items:
1. **Update easyWSL.csproj**
   ```xml
   <!-- Change from -->
   <TargetFramework>net6.0-windows10.0.19041.0</TargetFramework>
   
   <!-- To -->
   <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
   ```

2. **Update easyWslLib.csproj**
   ```xml
   <!-- Change from -->
   <TargetFramework>net6.0</TargetFramework>
   
   <!-- To -->
   <TargetFramework>net8.0</TargetFramework>
   ```

3. **Update easyWSLcmd.csproj**
   ```xml
   <!-- Change from -->
   <TargetFramework>net6.0</TargetFramework>
   
   <!-- To -->
   <TargetFramework>net8.0</TargetFramework>
   ```

#### Validation:
- [ ] Project builds successfully
- [ ] All existing functionality works
- [ ] No new compiler warnings

### Step 1.2: Enable Nullable Reference Types

#### Action Items:
1. **Add to all project files**
   ```xml
   <PropertyGroup>
     <Nullable>enable</Nullable>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
     <WarningsNotAsErrors>NU1701</WarningsNotAsErrors>
   </PropertyGroup>
   ```

2. **Fix nullable warnings incrementally**
   - Start with easyWslLib project
   - Then easyWSLcmd project
   - Finally easyWSL main project

#### Expected Issues:
- Nullable reference warnings throughout codebase
- Need to add null checks and null-forgiving operators

#### Validation:
- [ ] Zero nullable reference warnings
- [ ] All projects build without warnings

### Step 1.3: Update Core Dependencies

#### Action Items:
1. **Update Microsoft.WindowsAppSDK**
   ```xml
   <!-- From -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.0.1" />
   
   <!-- To -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.240923002" />
   ```

2. **Update System.CommandLine**
   ```xml
   <!-- From -->
   <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
   
   <!-- To -->
   <PackageReference Include="System.CommandLine" Version="2.0.0" />
   ```

3. **Update other packages** (see TECHNICAL_SPECIFICATIONS.md for complete list)

#### Validation:
- [ ] All packages restore successfully
- [ ] No package conflicts
- [ ] Application starts and basic functionality works

## Phase 2: Security Improvements (Week 2-3)

### Step 2.1: Replace HttpWebRequest with HttpClient

#### Action Items:
1. **Create IHttpService interface** (see TECHNICAL_SPECIFICATIONS.md)
2. **Implement HttpService class**
3. **Replace Helpers.GetRequest() calls**

#### Migration Pattern:
```csharp
// Before
var response = helpers.GetRequest(url);

// After
var response = await _httpService.GetAsync(url, cancellationToken);
```

#### Files to Update:
- [ ] `easyWslLib/Helpers.cs` - Remove HTTP methods
- [ ] `easyWslLib/DockerDownloader.cs` - Use IHttpService
- [ ] All calling code

#### Validation:
- [ ] All HTTP requests use new service
- [ ] Proper error handling implemented
- [ ] Cancellation tokens supported

### Step 2.2: Implement Input Validation

#### Action Items:
1. **Create InputValidator class** (see TECHNICAL_SPECIFICATIONS.md)
2. **Add validation to all user inputs**

#### Files to Update:
- [ ] `RegisterNewDistro_Page.xaml.cs` - Validate distro names, usernames
- [ ] `ManageDistrosPage.xaml.cs` - Validate operations
- [ ] `SettingsPage.xaml.cs` - Validate configuration values

#### Validation Pattern:
```csharp
// Before
if (distroName == "" || distroName.Contains(" "))
{
    await showErrorModal();
    return;
}

// After
var validationResult = InputValidator.ValidateDistroName(distroName);
if (!validationResult.IsValid)
{
    await _dialogService.ShowErrorAsync("Validation Error", validationResult.ErrorMessage!);
    return;
}
```

#### Validation:
- [ ] All user inputs validated
- [ ] Consistent error messages
- [ ] No injection vulnerabilities

### Step 2.3: Secure Credential Handling

#### Action Items:
1. **Implement CredentialManager** (see TECHNICAL_SPECIFICATIONS.md)
2. **Replace plain text password handling**

#### Files to Update:
- [ ] `RegisterNewDistro_Page.xaml.cs` - Secure password handling
- [ ] Any configuration storage

#### Validation:
- [ ] Passwords encrypted at rest
- [ ] Secure memory handling
- [ ] No plain text credentials in logs

## Phase 3: Architecture Refactoring (Week 3-5)

### Step 3.1: Implement Dependency Injection

#### Action Items:
1. **Add DI packages to easyWSL.csproj**
   ```xml
   <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
   <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
   <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
   <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.1" />
   ```

2. **Update App.xaml.cs**
   ```csharp
   public partial class App : Application
   {
       private IHost? _host;
       
       public App()
       {
           InitializeComponent();
           _host = CreateHostBuilder().Build();
       }
       
       private static IHostBuilder CreateHostBuilder()
       {
           return Host.CreateDefaultBuilder()
               .ConfigureServices((context, services) =>
               {
                   // Register services
                   services.AddSingleton<IHttpService, HttpService>();
                   services.AddSingleton<IDockerService, DockerService>();
                   services.AddSingleton<IWslService, WslService>();
                   services.AddSingleton<IFileService, FileService>();
                   // Add other services
               });
       }
   }
   ```

#### Validation:
- [ ] DI container configured
- [ ] All services registered
- [ ] Constructor injection working

### Step 3.2: Create Service Layer

#### Action Items:
1. **Create Services folder structure**
   ```
   easyWslLib/
   ├── Services/
   │   ├── IDockerService.cs
   │   ├── DockerService.cs
   │   ├── IWslService.cs
   │   ├── WslService.cs
   │   ├── IFileService.cs
   │   └── FileService.cs
   ```

2. **Implement service interfaces** (see TECHNICAL_SPECIFICATIONS.md)

3. **Refactor existing code to use services**

#### Migration Pattern:
```csharp
// Before
private Helpers helpers = new();
var result = helpers.ExecuteProcessAsynch("wsl.exe", args);

// After
private readonly IWslService _wslService;
var result = await _wslService.ExecuteCommandAsync(args, cancellationToken);
```

#### Validation:
- [ ] All business logic moved to services
- [ ] UI classes only handle presentation
- [ ] Services are testable

### Step 3.3: Implement MVVM Pattern

#### Action Items:
1. **Add CommunityToolkit.Mvvm package**
   ```xml
   <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
   ```

2. **Create ViewModels folder**
3. **Implement ViewModels** (see TECHNICAL_SPECIFICATIONS.md)
4. **Update XAML to use data binding**

#### Files to Create:
- [ ] `ViewModels/RegisterDistroViewModel.cs`
- [ ] `ViewModels/ManageDistrosViewModel.cs`
- [ ] `ViewModels/SettingsViewModel.cs`
- [ ] `ViewModels/ManageSnapshotsViewModel.cs`

#### Validation:
- [ ] All UI logic moved to ViewModels
- [ ] Data binding working correctly
- [ ] Commands implemented properly

## Phase 4: Performance and Quality (Week 5-7)

### Step 4.1: Modernize Async Patterns

#### Action Items:
1. **Add CancellationToken support to all async methods**
2. **Replace Task.Run with proper async implementations**
3. **Implement ConfigureAwait(false) where appropriate**

#### Migration Pattern:
```csharp
// Before
public async Task ExecuteProcessAsynch(string exe, string arguments)
{
    Process proc = new Process();
    // ... setup
    proc.Start();
    await proc.WaitForExitAsync().ConfigureAwait(false);
}

// After
public async Task ExecuteProcessAsync(string exe, string arguments, CancellationToken cancellationToken = default)
{
    using var proc = new Process();
    // ... setup
    proc.Start();
    await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
}
```

#### Validation:
- [ ] All async methods have CancellationToken support
- [ ] No blocking calls on UI thread
- [ ] Proper exception handling

### Step 4.2: Implement Structured Logging

#### Action Items:
1. **Create logging extensions** (see TECHNICAL_SPECIFICATIONS.md)
2. **Add logging to all services**
3. **Configure log levels and outputs**

#### Files to Update:
- [ ] All service classes
- [ ] Error handling code
- [ ] Performance-critical sections

#### Validation:
- [ ] Comprehensive logging implemented
- [ ] Log levels configured correctly
- [ ] No sensitive data in logs

### Step 4.3: Add Error Handling

#### Action Items:
1. **Create custom exception types** (see TECHNICAL_SPECIFICATIONS.md)
2. **Implement global error handling**
3. **Add retry mechanisms for network operations**

#### Validation:
- [ ] Specific exception types for different errors
- [ ] User-friendly error messages
- [ ] Proper error recovery

## Phase 5: Testing and Documentation (Week 7-8)

### Step 5.1: Add Unit Tests

#### Action Items:
1. **Create test projects**
   ```
   easyWSL.Tests/
   ├── Services/
   ├── ViewModels/
   └── Validation/
   
   easyWslLib.Tests/
   ├── Services/
   └── Validation/
   ```

2. **Add test packages**
   ```xml
   <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
   <PackageReference Include="MSTest.TestAdapter" Version="3.1.1" />
   <PackageReference Include="MSTest.TestFramework" Version="3.1.1" />
   <PackageReference Include="Moq" Version="4.20.69" />
   ```

3. **Write tests for critical functionality**

#### Test Coverage Goals:
- [ ] 80%+ code coverage for services
- [ ] 70%+ code coverage for ViewModels
- [ ] 90%+ code coverage for validation logic

### Step 5.2: Update Documentation

#### Action Items:
1. **Update README.md with new requirements**
2. **Add API documentation**
3. **Create deployment guide**
4. **Document configuration options**

#### Validation:
- [ ] All public APIs documented
- [ ] Setup instructions updated
- [ ] Migration notes for users

## Post-Migration Validation

### Functional Testing
- [ ] All existing features work correctly
- [ ] New error handling provides better user experience
- [ ] Performance improvements measurable
- [ ] Security vulnerabilities addressed

### Performance Testing
- [ ] Memory usage reduced
- [ ] Download speeds improved
- [ ] UI responsiveness enhanced
- [ ] Startup time not degraded

### Security Testing
- [ ] Input validation prevents injection attacks
- [ ] Credentials properly encrypted
- [ ] Network requests secure
- [ ] File operations safe

## Rollback Plan

### If Critical Issues Found:
1. **Immediate Actions**
   - [ ] Stop deployment
   - [ ] Document issue
   - [ ] Assess impact

2. **Rollback Steps**
   - [ ] Revert to previous version
   - [ ] Restore user data if needed
   - [ ] Communicate with users

3. **Recovery Plan**
   - [ ] Fix identified issues
   - [ ] Additional testing
   - [ ] Gradual re-deployment

## Success Metrics

### Technical Metrics
- [ ] Zero critical security vulnerabilities
- [ ] 50% reduction in memory usage
- [ ] 30% improvement in download performance
- [ ] 80%+ unit test coverage

### Quality Metrics
- [ ] Zero code analysis warnings
- [ ] Consistent coding standards
- [ ] Comprehensive error handling
- [ ] Proper logging throughout

### User Experience Metrics
- [ ] Improved error messages
- [ ] Better progress feedback
- [ ] Accessibility compliance
- [ ] Reduced support requests

## Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| 1 | Week 1-2 | Framework updates, dependency updates |
| 2 | Week 2-3 | Security improvements, input validation |
| 3 | Week 3-5 | DI implementation, service layer, MVVM |
| 4 | Week 5-7 | Async modernization, logging, error handling |
| 5 | Week 7-8 | Testing, documentation |

**Total Duration: 8 weeks**

This migration guide provides a structured approach to modernizing the easyWSL codebase while maintaining stability and minimizing risk. Each phase builds upon the previous one, allowing for incremental validation and rollback if needed.
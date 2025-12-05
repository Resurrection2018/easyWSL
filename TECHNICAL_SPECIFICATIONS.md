# easyWSL Technical Specifications for Modernization

## 1. Dependency Updates and Framework Modernization

### 1.1 Project File Updates

#### easyWSL/easyWSL.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    <EnablePreviewMsixTooling>true</EnablePreviewMsixTooling>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>NU1701</WarningsNotAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- Updated Core Packages -->
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.240923002" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1" />
    
    <!-- Dependency Injection and Configuration -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.1" />
    
    <!-- MVVM Toolkit -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
    
    <!-- HTTP Client -->
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />
    
    <!-- Security -->
    <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
    
    <!-- Updated Existing Packages -->
    <PackageReference Include="Microsoft.Dism" Version="3.1.0" />
    <PackageReference Include="PInvoke.User32" Version="0.7.124" />
    <PackageReference Include="System.Management" Version="8.0.0" />
  </ItemGroup>
</Project>
```

#### easyWslLib/easyWslLib.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
    <PackageReference Include="System.Security.Cryptography.Algorithms" Version="4.3.1" />
  </ItemGroup>
</Project>
```

#### easyWSLcmd/easyWSLcmd.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.1" />
  </ItemGroup>
</Project>
```

## 2. Security Improvements

### 2.1 Secure HTTP Client Implementation

#### Services/IHttpService.cs
```csharp
using System.Net.Http.Headers;

namespace easyWslLib.Services;

public interface IHttpService
{
    Task<string> GetAsync(string url, CancellationToken cancellationToken = default);
    Task<string> GetWithAuthAsync(string url, string token, string acceptType, CancellationToken cancellationToken = default);
    Task<Stream> GetStreamAsync(string url, IEnumerable<KeyValuePair<string, string>> headers, CancellationToken cancellationToken = default);
}

public class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpService> _logger;

    public HttpService(HttpClient httpClient, ILogger<HttpService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configure default headers and timeout
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("easyWSL", "2.0"));
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Making GET request to {Url}", url);
            
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Successfully received {ContentLength} characters from {Url}", content.Length, url);
            
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Url}", url);
            throw new DockerServiceException($"Failed to fetch data from {url}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout for {Url}", url);
            throw new DockerServiceException($"Request timeout for {url}", ex);
        }
    }

    public async Task<string> GetWithAuthAsync(string url, string token, string acceptType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Authenticated HTTP request failed for {Url}", url);
            throw new DockerServiceException($"Failed to fetch authenticated data from {url}", ex);
        }
    }

    public async Task<Stream> GetStreamAsync(string url, IEnumerable<KeyValuePair<string, string>> headers, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Stream request failed for {Url}", url);
            throw new DockerServiceException($"Failed to get stream from {url}", ex);
        }
    }
}
```

### 2.2 Input Validation and Sanitization

#### Validation/InputValidator.cs
```csharp
using System.Text.RegularExpressions;

namespace easyWslLib.Validation;

public static class InputValidator
{
    private static readonly Regex DistroNameRegex = new(@"^[a-zA-Z0-9_-]{1,50}$", RegexOptions.Compiled);
    private static readonly Regex DockerImageRegex = new(@"^[a-z0-9]+(?:[._-][a-z0-9]+)*(?:/[a-z0-9]+(?:[._-][a-z0-9]+)*)*(?::[a-zA-Z0-9_.-]+)?$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(@"^[a-z_][a-z0-9_-]{0,31}$", RegexOptions.Compiled);

    public static ValidationResult ValidateDistroName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Failure("Distro name cannot be empty");

        if (!DistroNameRegex.IsMatch(name))
            return ValidationResult.Failure("Distro name can only contain letters, numbers, underscores, and hyphens (max 50 characters)");

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateDockerImage(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
            return ValidationResult.Failure("Docker image name cannot be empty");

        if (!DockerImageRegex.IsMatch(image))
            return ValidationResult.Failure("Invalid Docker image format");

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return ValidationResult.Failure("Username cannot be empty");

        if (!UsernameRegex.IsMatch(username))
            return ValidationResult.Failure("Username must start with a letter or underscore, followed by letters, numbers, underscores, or hyphens (max 32 characters)");

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ValidationResult.Failure("File path cannot be empty");

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                return ValidationResult.Failure("File does not exist");

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Invalid file path: {ex.Message}");
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
```

### 2.3 Secure Credential Management

#### Security/CredentialManager.cs
```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace easyWslLib.Security;

public interface ICredentialManager
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
    void SecurelyDisposeString(string sensitiveData);
}

public class CredentialManager : ICredentialManager
{
    private readonly ILogger<CredentialManager> _logger;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("easyWSL-v2-entropy");

    public CredentialManager(ILogger<CredentialManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = await Task.Run(() => 
                ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser));
            
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw new SecurityException("Failed to encrypt sensitive data", ex);
        }
        finally
        {
            SecurelyDisposeString(plainText);
        }
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decryptedBytes = await Task.Run(() => 
                ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser));
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new SecurityException("Failed to decrypt sensitive data", ex);
        }
    }

    public void SecurelyDisposeString(string sensitiveData)
    {
        if (string.IsNullOrEmpty(sensitiveData))
            return;

        // Note: In .NET, strings are immutable, so we can't actually clear the memory
        // This is a placeholder for best practices documentation
        // In a real implementation, you'd use SecureString or char arrays
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
```

## 3. Architecture Refactoring

### 3.1 Service Layer Implementation

#### Services/IDockerService.cs
```csharp
namespace easyWslLib.Services;

public interface IDockerService
{
    Task<DownloadResult> DownloadImageAsync(string imageName, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<CombineResult> CombineLayersAsync(string outputPath, IProgress<CombineProgress>? progress = null, CancellationToken cancellationToken = default);
}

public class DockerService : IDockerService
{
    private readonly IHttpService _httpService;
    private readonly IFileService _fileService;
    private readonly ILogger<DockerService> _logger;
    private readonly DockerConfiguration _configuration;

    public DockerService(
        IHttpService httpService,
        IFileService fileService,
        ILogger<DockerService> logger,
        IOptions<DockerConfiguration> configuration)
    {
        _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<DownloadResult> DownloadImageAsync(
        string imageName, 
        IProgress<DownloadProgress>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var validationResult = InputValidator.ValidateDockerImage(imageName);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(validationResult.ErrorMessage, nameof(imageName));
        }

        _logger.LogInformation("Starting download for Docker image: {ImageName}", imageName);

        try
        {
            var imageInfo = ParseImageName(imageName);
            var authToken = await GetAuthTokenAsync(imageInfo, cancellationToken);
            var manifest = await GetManifestAsync(imageInfo, authToken, cancellationToken);
            var layers = ExtractLayers(manifest);

            var downloadTasks = layers.Select(async (layer, index) =>
            {
                var layerProgress = new Progress<long>(bytesReceived =>
                {
                    progress?.Report(new DownloadProgress
                    {
                        LayerIndex = index,
                        TotalLayers = layers.Count,
                        BytesReceived = bytesReceived,
                        TotalBytes = layer.Size
                    });
                });

                return await DownloadLayerAsync(imageInfo, layer, authToken, layerProgress, cancellationToken);
            });

            var layerPaths = await Task.WhenAll(downloadTasks);

            _logger.LogInformation("Successfully downloaded {LayerCount} layers for image {ImageName}", 
                layerPaths.Length, imageName);

            return new DownloadResult
            {
                IsSuccess = true,
                LayerPaths = layerPaths,
                ImageName = imageName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Docker image: {ImageName}", imageName);
            throw new DockerServiceException($"Failed to download image {imageName}", ex);
        }
    }

    private async Task<string> GetAuthTokenAsync(DockerImageInfo imageInfo, CancellationToken cancellationToken)
    {
        var authUrl = $"{_configuration.AuthUrl}?service={_configuration.RegistryUrl}&scope=repository:{imageInfo.Repository}:pull";
        var response = await _httpService.GetAsync(authUrl, cancellationToken);
        
        var authResponse = JsonSerializer.Deserialize<AuthorizationResponse>(response);
        return authResponse?.Token ?? throw new DockerServiceException("Failed to get authentication token");
    }

    // Additional implementation methods...
}
```

### 3.2 MVVM Implementation

#### ViewModels/RegisterDistroViewModel.cs
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace easyWSL.ViewModels;

public partial class RegisterDistroViewModel : ObservableObject
{
    private readonly IDockerService _dockerService;
    private readonly IWslService _wslService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RegisterDistroViewModel> _logger;

    [ObservableProperty]
    private string distroName = string.Empty;

    [ObservableProperty]
    private string selectedDistroSource = "Supported distro list";

    [ObservableProperty]
    private bool isInstalling;

    [ObservableProperty]
    private double installProgress;

    [ObservableProperty]
    private string installStatusText = string.Empty;

    [ObservableProperty]
    private bool canInstall = true;

    public RegisterDistroViewModel(
        IDockerService dockerService,
        IWslService wslService,
        IDialogService dialogService,
        ILogger<RegisterDistroViewModel> logger)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [RelayCommand]
    private async Task InstallDistroAsync()
    {
        var validationResult = InputValidator.ValidateDistroName(DistroName);
        if (!validationResult.IsValid)
        {
            await _dialogService.ShowErrorAsync("Validation Error", validationResult.ErrorMessage!);
            return;
        }

        try
        {
            IsInstalling = true;
            CanInstall = false;
            InstallStatusText = "Preparing installation...";

            var progress = new Progress<DownloadProgress>(p =>
            {
                InstallProgress = (double)p.BytesReceived / p.TotalBytes * 100;
                InstallStatusText = $"Downloading layer {p.LayerIndex + 1} of {p.TotalLayers}...";
            });

            using var cancellationTokenSource = new CancellationTokenSource();
            
            // Download Docker image
            var downloadResult = await _dockerService.DownloadImageAsync(
                GetSelectedImageName(), 
                progress, 
                cancellationTokenSource.Token);

            if (!downloadResult.IsSuccess)
            {
                await _dialogService.ShowErrorAsync("Download Failed", "Failed to download the Docker image.");
                return;
            }

            // Combine layers
            InstallStatusText = "Combining layers...";
            var combineResult = await _dockerService.CombineLayersAsync(
                Path.Combine(Path.GetTempPath(), "install.tar"), 
                null, 
                cancellationTokenSource.Token);

            // Register with WSL
            InstallStatusText = "Registering with WSL...";
            await _wslService.RegisterDistroAsync(DistroName, combineResult.OutputPath, cancellationTokenSource.Token);

            InstallStatusText = "Installation completed successfully!";
            await _dialogService.ShowSuccessAsync("Success", $"Distribution '{DistroName}' has been installed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Installation was cancelled by user");
            InstallStatusText = "Installation cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install distribution {DistroName}", DistroName);
            await _dialogService.ShowErrorAsync("Installation Failed", $"Failed to install distribution: {ex.Message}");
        }
        finally
        {
            IsInstalling = false;
            CanInstall = true;
            InstallProgress = 0;
        }
    }

    [RelayCommand]
    private void CancelInstallation()
    {
        // Implementation for cancellation
    }

    private string GetSelectedImageName()
    {
        // Implementation to get selected image name based on source
        return SelectedDistroSource switch
        {
            "Ubuntu 20.04" => "ubuntu:20.04",
            "Ubuntu 22.04" => "ubuntu:22.04",
            "Debian Stable" => "debian:stable",
            _ => "ubuntu:latest"
        };
    }
}
```

## 4. Error Handling and Logging

### 4.1 Custom Exception Types

#### Exceptions/DockerServiceException.cs
```csharp
namespace easyWslLib.Exceptions;

public class DockerServiceException : Exception
{
    public string? ImageName { get; }
    public DockerErrorCode ErrorCode { get; }

    public DockerServiceException(string message) : base(message)
    {
        ErrorCode = DockerErrorCode.Unknown;
    }

    public DockerServiceException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = DockerErrorCode.Unknown;
    }

    public DockerServiceException(string imageName, DockerErrorCode errorCode, string message) : base(message)
    {
        ImageName = imageName;
        ErrorCode = errorCode;
    }

    public DockerServiceException(string imageName, DockerErrorCode errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ImageName = imageName;
        ErrorCode = errorCode;
    }
}

public enum DockerErrorCode
{
    Unknown,
    ImageNotFound,
    AuthenticationFailed,
    NetworkError,
    InvalidImageFormat,
    DownloadFailed,
    ManifestParsingFailed
}
```

### 4.2 Structured Logging

#### Logging/LoggingExtensions.cs
```csharp
namespace easyWslLib.Logging;

public static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Starting Docker image download for {ImageName}")]
    public static partial void LogDockerDownloadStarted(this ILogger logger, string imageName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Docker image download completed for {ImageName} in {Duration}ms")]
    public static partial void LogDockerDownloadCompleted(this ILogger logger, string imageName, long duration);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Docker image download failed for {ImageName}")]
    public static partial void LogDockerDownloadFailed(this ILogger logger, string imageName, Exception exception);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "WSL distribution registration started for {DistroName}")]
    public static partial void LogWslRegistrationStarted(this ILogger logger, string distroName);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "WSL distribution registration completed for {DistroName}")]
    public static partial void LogWslRegistrationCompleted(this ILogger logger, string distroName);
}
```

## 5. Performance Optimizations

### 5.1 Async File Operations

#### Services/IFileService.cs
```csharp
namespace easyWslLib.Services;

public interface IFileService
{
    Task<bool> ExistsAsync(string path);
    Task CopyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);
    Task<Stream> CreateAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default);
}

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ExistsAsync(string path)
    {
        return await Task.Run(() => File.Exists(path));
    }

    public async Task CopyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        const int bufferSize = 81920; // 80KB buffer

        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

        await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
    }

    public async Task<Stream> CreateAsync(string path, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }, cancellationToken);
    }

    public async Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }, cancellationToken);
    }
}
```

### 5.2 Memory-Efficient Stream Processing

#### Services/StreamProcessor.cs
```csharp
namespace easyWslLib.Services;

public class StreamProcessor
{
    private readonly ILogger<StreamProcessor> _logger;
    private const int DefaultBufferSize = 81920; // 80KB

    public StreamProcessor(ILogger<StreamProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessStreamAsync(
        Stream source, 
        Stream destination, 
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[DefaultBufferSize];
        long totalBytesRead = 0;

        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stream after {BytesProcessed} bytes", totalBytesRead);
            throw;
        }
    }

    public async Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        return await sha256.ComputeHashAsync(stream, cancellationToken);
    }
}
```

This technical specification provides the detailed implementation guidance for modernizing the easyWSL codebase. Each section includes specific code examples and architectural patterns that address the identified issues while following modern .NET development practices.
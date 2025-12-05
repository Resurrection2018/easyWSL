# Phase 3: Modernization & Package Updates - Implementation Plan

> **Timeline**: Weeks 3-4 (10 days)  
> **Effort**: 5-7 days  
> **Priority**: P1 - High Priority for Build Success

---

## 🎯 Objectives

Modernize the codebase by updating packages, migrating deprecated APIs, and resolving build compatibility issues.

---

## 📋 Task Breakdown

### Task 1: Update NuGet Packages
**Files**: All `.csproj` files  
**Effort**: 2 hours  
**Risk**: Medium

**Current Packages**:
```xml
<!-- easyWSL/easyWSL.csproj -->
<PackageReference Include="Microsoft.Dism" Version="2.4.0" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.0.1" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.22000.197" />
<PackageReference Include="PInvoke.User32" Version="0.7.104" />
<PackageReference Include="System.Management" Version="6.0.0" />

<!-- easyWSLcmd/easyWSLcmd.csproj -->
<PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />

<!-- easyWslLib/easyWslLib.csproj -->
<PackageReference Include="System.Text.Json" Version="8.0.5" />
```

**Updated Packages** (as of Dec 2025):
```xml
<!-- easyWSL/easyWSL.csproj -->
<PackageReference Include="Microsoft.Dism" Version="3.1.0" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240311000" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1" />
<PackageReference Include="PInvoke.User32" Version="0.7.124" />
<PackageReference Include="System.Management" Version="8.0.0" />

<!-- easyWSLcmd/easyWSLcmd.csproj -->
<PackageReference Include="System.CommandLine" Version="2.0.0-beta4.24324.3" />
<!-- Note: 2.0.0 stable is available but different API -->

<!-- easyWslLib/easyWslLib.csproj -->
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

**Implementation Steps**:
1. Update packages one project at a time
2. Build after each update to catch breaking changes
3. Test functionality after all updates

---

### Task 2: Migrate WebRequest to HttpClient
**Files**: [`easyWslLib/Helpers.cs`](easyWslLib/Helpers.cs)  
**Effort**: 2-3 hours  
**Risk**: Medium

**Current Code** (deprecated):
```csharp
public string GetRequest(string url)
{
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);  // OBSOLETE
    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
    request.Credentials = CredentialCache.DefaultCredentials;
    Stream receiveStream = response.GetResponseStream();
    StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
    string responseStream = readStream.ReadToEnd();
    response.Close();
    readStream.Close();
    return responseStream;
}
```

**New Code** (modern HttpClient):
```csharp
private static readonly HttpClient _httpClient = new HttpClient();

public async Task<string> GetRequestAsync(string url)
{
    try
    {
        var response = await _httpClient.GetStringAsync(url);
        return response;
    }
    catch (HttpRequestException ex)
    {
        throw new Exception($"HTTP request failed: {ex.Message}", ex);
    }
}

public string GetRequest(string url)
{
    // Synchronous wrapper for backward compatibility
    return GetRequestAsync(url).GetAwaiter().GetResult();
}
```

**Similarly for GetRequestWithHeader**:
```csharp
public async Task<string> GetRequestWithHeaderAsync(string url, string token, string acceptType)
{
    try
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("Authorization", $"Bearer {token}");
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(acceptType));
        
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
    catch (HttpRequestException ex)
    {
        throw new Exception($"HTTP request failed: {ex.Message}", ex);
    }
}

public string GetRequestWithHeader(string url, string token, string type)
{
    return GetRequestWithHeaderAsync(url, token, type).GetAwaiter().GetResult();
}
```

**Changes Required in DockerDownloader**:
- Make `DownloadImage` properly async (it already is)
- Update calls to use async methods
- No breaking changes to public API

---

### Task 3: Replace Windows.Web.Http with System.Net.Http
**Files**: 
- [`easyWSL/PlatformHelpers.cs`](easyWSL/PlatformHelpers.cs)
- [`easyWSL/App.xaml.cs`](easyWSL/App.xaml.cs)

**Effort**: 2 hours  
**Risk**: Low-Medium

**Current Code** (App.xaml.cs):
```csharp
using Windows.Web.Http;  // DEPRECATED

public static readonly HttpClient httpClient = new();  // Windows.Web.Http.HttpClient
```

**New Code**:
```csharp
using System.Net.Http;  // Modern API

public static readonly HttpClient httpClient = new();  // System.Net.Http.HttpClient
```

**Current Code** (PlatformHelpers.cs):
```csharp
using Windows.Web.Http;

public async Task DownloadFileAsync(Uri uri, IEnumerable<KeyValuePair<string,string>> headers, FileInfo destinationPath)
{
    var httpRequestMessage = new HttpRequestMessage  // Windows.Web.Http
    {
        Method = HttpMethod.Get,
        RequestUri = uri,
    };
    // ...
    Progress<HttpProgress> progressCallback = new Progress<HttpProgress>(_httpProgressCallback);
    HttpResponseMessage response = await App.httpClient.SendRequestAsync(httpRequestMessage).AsTask(progressCallback);
}
```

**New Code**:
```csharp
using System.Net.Http;

public async Task DownloadFileAsync(Uri uri, IEnumerable<KeyValuePair<string,string>> headers, FileInfo destinationPath)
{
    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
    
    foreach (var header in headers)
    {
        httpRequestMessage.Headers.Add(header.Key, header.Value);
    }
    
    // For progress tracking, we need to manually track
    using var response = await App.httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();
    
    var totalBytes = response.Content.Headers.ContentLength ?? 0;
    
    using var contentStream = await response.Content.ReadAsStreamAsync();
    using var fileStream = File.Create(destinationPath.FullName);
    
    var buffer = new byte[8192];
    long totalRead = 0;
    int read;
    
    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        await fileStream.WriteAsync(buffer, 0, read);
        totalRead += read;
        
        // Report progress
        _httpProgressCallback(new HttpProgress 
        { 
            BytesReceived = (ulong)totalRead,
            TotalBytesToReceive = (ulong?)totalBytes
        });
    }
}

// Define HttpProgress struct to replace Windows.Web.Http version
public struct HttpProgress
{
    public ulong BytesReceived { get; set; }
    public ulong? TotalBytesToReceive { get; set; }
}
```

**Update RegisterNewDistro_Page.xaml.cs**:
```csharp
public void HttpProgressCallback(HttpProgress progress)  // Use local struct instead
{
    if (progress.TotalBytesToReceive == null) return;
    
    registerDistroProgressBar.Minimum = 0;
    registerDistroProgressBar.Maximum = (double)progress.TotalBytesToReceive;
    registerDistroProgressBar.Value = progress.BytesReceived;
}
```

---

### Task 4: Fix WindowsAppSDK Compatibility
**Files**: [`easyWSL/easyWSL.csproj`](easyWSL/easyWSL.csproj)  
**Effort**: 30 minutes  
**Risk**: Low

**Change**:
```xml
<!-- Before -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.0.1" />

<!-- After -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240311000" />
```

This should resolve the XAML compiler error:
```
error MSB4062: The "Microsoft.UI.Xaml.Markup.Compiler.Tasks.CompileXaml" task could not be loaded
Could not load file or assembly 'System.Security.Permissions, Version=0.0.0.0'
```

---

### Task 5: Consider .NET 8.0 Migration
**Files**: All `.csproj` files  
**Effort**: 1-2 hours  
**Risk**: Medium

**.NET 6.0 Status**:
- End of support: November 12, 2024
- No longer receiving security updates

**.NET 8.0 Benefits**:
- LTS (Long Term Support) until November 2026
- Performance improvements
- Latest C# 12 features
- Better nullability handling

**Changes Required**:

1. Update `TargetFramework` in all projects:
```xml
<!-- easyWslLib/easyWslLib.csproj -->
<TargetFramework>net8.0</TargetFramework>

<!-- easyWSLcmd/easyWSLcmd.csproj -->
<TargetFramework>net8.0</TargetFramework>

<!-- easyWSL/easyWSL.csproj -->
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
```

2. Update SDK version in global.json (if exists):
```json
{
  "sdk": {
    "version": "8.0.404",
    "rollForward": "latestMinor"
  }
}
```

3. Test all functionality after migration

**Recommendation**: Proceed with .NET 8.0 migration as .NET 6.0 is EOL.

---

### Task 6: Fix Typos and Minor Issues
**Files**: Various  
**Effort**: 30 minutes  
**Risk**: Low

1. **Fix typo in DockerDownloader.cs**:
```csharp
// Line 31-40: "autorizationResponse" → "authorizationResponse"
public class authorizationResponse  // Rename class
{
    public string token { get; set; }
    public string access_token { get; set; }
    public int expires_in { get; set; }
    public string issued_at { get; set; }
}
```

2. **Fix property naming (follow C# conventions)**:
```csharp
// Should be PascalCase for properties
public class AuthorizationResponse
{
    public string Token { get; set; }
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string IssuedAt { get; set; }
}
```

3. **Fix other typos**:
- "distrbution" → "distribution" (RegisterNewDistro_Page.xaml.cs:202)
- "Succesfuly" → "Successfully" (ManageSnapshotsPage.xaml.cs:60)

---

## 🧪 Testing Strategy

### After Each Major Change

1. **After Package Updates**:
   ```bash
   dotnet build easyWSL.sln -c Debug
   dotnet build easyWSL.sln -c Release
   ```

2. **After HttpClient Migration**:
   - Test Docker image download
   - Test manifest retrieval
   - Verify error handling

3. **After Windows.Web.Http Migration**:
   - Test file download with progress
   - Verify progress bar updates correctly
   - Test with large files

4. **After .NET 8.0 Migration**:
   - Full regression test
   - Test all features
   - Check for performance improvements

### Final Testing Checklist
- [ ] Solution builds without errors
- [ ] All warnings addressed or documented
- [ ] Docker image download works
- [ ] Progress tracking works
- [ ] File operations work
- [ ] WSL integration works
- [ ] Settings save/load correctly
- [ ] Snapshots work

---

## 📊 Success Criteria

1. ✅ All NuGet packages updated to latest stable
2. ✅ No deprecated API warnings
3. ✅ Solution builds successfully
4. ✅ .NET 8.0 LTS (recommended)
5. ✅ All existing functionality preserved
6. ✅ UI project builds and runs
7. ✅ Performance maintained or improved

---

## 🔄 Implementation Order

### Day 1 (3-4 hours)
1. Update WindowsAppSDK (fixes immediate build issue)
2. Update other NuGet packages in easyWSL project
3. Build and test UI

### Day 2 (3-4 hours)
1. Update packages in easyWslLib
2. Update packages in easyWSLcmd
3. Migrate WebRequest to HttpClient in Helpers.cs
4. Test Docker downloads

### Day 3 (2-3 hours)
1. Replace Windows.Web.Http with System.Net.Http
2. Update progress tracking
3. Test file downloads with progress

### Day 4 (2-3 hours)
1. Migrate to .NET 8.0
2. Fix any compatibility issues
3. Full regression testing

### Day 5 (2 hours)
1. Fix typos and naming conventions
2. Code cleanup
3. Final testing
4. Documentation

---

## ⚠️ Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking changes in packages | High | Update one at a time, test after each |
| HttpClient async changes | Medium | Keep sync wrappers for compatibility |
| .NET 8.0 compatibility | Medium | Test thoroughly, can rollback if needed |
| Progress tracking broken | Low | Implement custom progress tracking |

---

## 📝 Rollback Plan

If issues occur:
1. Git commit after each successful change
2. Can rollback individual changes
3. Keep .NET 6.0 branch if .NET 8.0 has issues
4. Package versions can be reverted individually

---

## 🚀 Expected Outcomes

After Phase 3:
- ✅ Modern, supported framework (.NET 8.0 LTS)
- ✅ Latest package versions with security updates
- ✅ No deprecated API warnings
- ✅ UI project builds successfully
- ✅ Better performance
- ✅ Ready for production deployment

---

## 📄 Documentation Updates Needed

After completion:
- Update README.md with new prerequisites
- Document .NET 8.0 requirement
- Update build instructions
- Add migration notes for contributors

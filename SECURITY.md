# Security Policy

## Supported Versions

| Version | Supported          | Status |
| ------- | ------------------ | ------ |
| 2.0.x   | :white_check_mark: | Active development, security updates |
| < 2.0   | :x:                | End of life, no security updates |

## Security Features in easyWSL 2.0

easyWSL 2.0 has been comprehensively hardened with enterprise-grade security features:

### 🔐 Secure Password Handling

**Problem Solved**: Previous versions exposed passwords in process arguments (visible to all users via Task Manager or Process Explorer).

**Solution**:
- Passwords are written to temporary files with restricted Windows ACL permissions
- Only the current user can read the password file
- Files are securely overwritten with random data before deletion
- Guaranteed cleanup even if errors occur

**Technical Details**:
- Location: `easyWslLib/SecurePasswordHelper.cs`
- Uses Windows Access Control Lists (ACL)
- Multiple-pass secure deletion with cryptographic random data

### 🛡️ Input Validation & Sanitization

**Comprehensive validation prevents**:
- Command injection attacks
- Path traversal attacks (e.g., `../../../Windows/System32`)
- Invalid characters in distribution names
- Reserved system names (CON, PRN, AUX, etc.)
- Invalid Docker image names
- Malicious usernames

**Technical Details**:
- Location: `easyWslLib/InputValidator.cs`
- Shell argument escaping for bash/sh safety
- Regex-based format validation
- Length limits to prevent DoS

### 📊 Security Audit Logging

**Complete audit trail** of security-relevant events:
- Validation failures
- Path traversal attempts
- Distribution registrations
- Command executions
- File operations
- Docker download blocks

**Log Location**: `%LOCALAPPDATA%\easyWSL\security.log`

**Log Format**:
```
[2025-12-04 22:25:00] [FAILURE] PATH_TRAVERSAL_ATTEMPT: Attempted=../../../etc/passwd, Base=C:\Users\...\easyWSL
[2025-12-04 22:25:15] [SUCCESS] DISTRO_REGISTERED: Name=Ubuntu2204, Path=C:\...\easyWSL\Ubuntu2204
[2025-12-04 22:25:30] [FAILURE] DOCKER_DOWNLOAD_BLOCKED: Image too large: 25 GB (max 20 GB)
```

**Features**:
- Thread-safe logging (no race conditions)
- Automatic log directory creation
- Never throws exceptions (graceful failure)
- Sensitive data filtering (no password logging)
- Log injection prevention

### 🚫 Resource Limits (DoS Prevention)

**Protection against resource exhaustion attacks**:
- **Maximum total download**: 20 GB
- **Maximum layer size**: 5 GB per layer
- **Maximum layers**: 100 layers per image
- **Timeout**: 5 minutes for HTTP requests

**Technical Details**:
- Location: `easyWslLib/DockerDownloader.cs`
- Validates before downloading (saves bandwidth)
- All blocks are logged for audit

### 🔒 Path Security

**Prevents unauthorized file system access**:
- Validates all paths stay within allowed directories
- Sanitizes filenames to remove dangerous characters
- Blocks absolute paths and UNC paths
- Prevents directory traversal (`../`, `..\\`)

**Protected Operations**:
- Distribution installation paths
- Snapshot storage paths
- Temporary file locations
- Configuration file paths

### ✅ Docker Image Validation

**Prevents malicious Docker images**:
- Blocks URLs (prevents redirect attacks)
- Validates image name format (RFC compliance)
- Validates tag format
- Length limits (prevents DoS)
- Character set restrictions

**Example Blocked Attacks**:
```
❌ https://evil.com/malicious:latest
❌ [300+ character string]
❌ ubuntu:../../etc/passwd
❌ image:tag:tag:tag
```

---

## Reporting a Vulnerability

### How to Report

If you discover a security vulnerability in easyWSL, please email:

**Email**: security@redcodelabs.io  
**Subject**: easyWSL Security Vulnerability Report

### What to Include

Please provide:
1. **Description**: Clear description of the vulnerability
2. **Steps to Reproduce**: Detailed steps to trigger the issue
3. **Impact Assessment**: Potential security impact (data exposure, privilege escalation, etc.)
4. **Affected Versions**: Which versions are affected
5. **Suggested Fix**: If you have one (optional but appreciated)
6. **Proof of Concept**: Code or screenshots (if applicable)

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Updates**: Every 5 business days
- **Resolution**: Critical issues within 30 days, others within 90 days
- **Disclosure**: Coordinated disclosure after fix is released

### What to Expect

1. We will acknowledge receipt of your report
2. We will investigate and validate the vulnerability
3. We will develop and test a fix
4. We will release a security update
5. We will credit you in the release notes (if desired)

---

## Security Best Practices for Users

When using easyWSL, follow these best practices:

### 1. Trusted Sources Only
- ✅ Only download images from official Docker Hub repositories
- ✅ Verify image names before installation
- ⚠️ Be cautious with community images
- ❌ Never use images from unknown sources

### 2. Regular Updates
- ✅ Keep easyWSL updated to the latest version
- ✅ Update your WSL distributions regularly
- ✅ Apply Windows security updates
- ✅ Monitor the security log periodically

### 3. Strong Passwords
- ✅ Use strong, unique passwords for WSL users
- ✅ Consider enabling Windows Hello integration
- ✅ Don't reuse Windows account passwords
- ⚠️ Change default passwords immediately

### 4. Review Security Logs
- ✅ Check `%LOCALAPPDATA%\easyWSL\security.log` periodically
- ✅ Look for failed validation attempts (may indicate attack)
- ✅ Verify expected distribution registrations
- ⚠️ Investigate unexpected path traversal attempts

### 5. Limit Privileges
- ✅ Don't run easyWSL as Administrator unless necessary
- ✅ Use standard user accounts for WSL distributions
- ✅ Only grant sudo access when needed
- ⚠️ Review sudoers file in critical distributions

---

## Known Security Limitations

### WSL Inherent Limitations
- easyWSL cannot prevent vulnerabilities within WSL distributions themselves
- Users are responsible for securing their WSL environments
- Docker images may contain vulnerabilities - verify before use

### Windows Defender Integration
- easyWSL relies on Windows Defender for malware scanning
- Ensure Windows Defender is enabled and up-to-date
- Real-time protection provides additional security layer

### Network Security
- Docker image downloads use HTTPS but trust Docker Hub's infrastructure
- Consider using a private Docker registry for sensitive environments
- Network-level security (firewall, VPN) is user's responsibility

---

## Security Features by Version

### Version 2.0 (Current)
- ✅ Secure password handling (no command-line exposure)
- ✅ Path traversal prevention
- ✅ Input validation & sanitization
- ✅ Security audit logging
- ✅ Resource limits (DoS prevention)
- ✅ Docker image validation
- ✅ Modern .NET 8.0 LTS (security updates until Nov 2026)
- ✅ Zero deprecated APIs

### Version 1.x (Legacy)
- ⚠️ Basic validation only
- ❌ Passwords visible in process arguments
- ❌ Limited input validation
- ❌ No security logging
- ❌ No resource limits

**Recommendation**: Upgrade to version 2.0 immediately for enhanced security.

---

## Security Certifications & Audits

### Current Status
- **Internal Security Review**: ✅ Completed (December 2025)
- **External Audit**: Pending
- **Penetration Testing**: Not yet performed
- **CVE Database**: No known CVEs

### Future Plans
- Professional security code review
- Penetration testing
- Security compliance certification (if needed for enterprise)
- Regular dependency scanning

---

## Security Contacts

- **General Security**: security@redcodelabs.io
- **Urgent Issues**: Include [URGENT] in subject line
- **GitHub**: Use GitHub Security Advisory for public disclosure

---

## Acknowledgments

We thank the security research community for helping keep easyWSL secure. If you report a valid security vulnerability, we will:

- Credit you in release notes (with your permission)
- Recognize your contribution publicly (if desired)
- Coordinate responsible disclosure

---

## Additional Resources

- [WSL Security Best Practices](https://docs.microsoft.com/en-us/windows/wsl/wsl-security)
- [Docker Security](https://docs.docker.com/engine/security/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)

---

**Last Updated**: December 4, 2025  
**Version**: 2.0  
**Maintainer**: Red Code Labs
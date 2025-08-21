# Steeltoe Security

Authentication and DataProtection libraries which simplify interacting with security services on Tanzu platforms.

For more information on how to use these components see the [Steeltoe documentation](https://steeltoe.io/).

## Security Best Practices

### Certificate Validation

#### Recommended Configuration
- **Always enable certificate validation** in production environments
- **Use certificate revocation checking** when possible by setting `RevocationMode` to `Online`
- **Validate certificate paths** to prevent directory traversal attacks

```json
{
  "Certificates": {
    "Default": {
      "RevocationMode": "Online"
    }
  },
  "Client": {
    "YourService": {
      "ValidateCertificates": true
    }
  }
}
```

#### Development vs Production
- Certificate validation bypass (`ValidateCertificates: false`) should **only** be used in development
- Consider using self-signed certificates with proper trust stores for development instead

### JWT Token Security

The Steeltoe JWT authentication automatically configures secure defaults:
- **Issuer validation**: Enabled
- **Audience validation**: Enabled  
- **Lifetime validation**: Enabled
- **Signature validation**: Enabled
- **Clock skew**: Limited to 30 seconds
- **Signed tokens required**: Yes

#### Configuration Example
```json
{
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "Authority": "https://your-auth-server.com",
        "ClientId": "your-client-id"
      }
    }
  }
}
```

### Input Validation and Sanitization

Use the `SecurityUtilities` class for secure input handling:
- `SanitizeInput()`: HTML-encode and remove line breaks for logging
- `SanitizeForLogging()`: Remove control characters for safe logging
- `IsUrlSafe()`: Validate URLs to allow only HTTP/HTTPS schemes

### Configuration Security

#### URL Security
- **Use HTTPS URLs** in production environments
- **Validate URL schemes** to prevent protocol-based attacks
- **Avoid hardcoded credentials** in configuration files

#### Example Secure Configuration
```json
{
  "YourService": {
    "Url": "https://secure-service.com/api",
    "ValidateCertificates": true
  }
}
```

### Data Protection

When using Redis for data protection:
- **Use TLS connections** to Redis in production
- **Secure Redis instance** with proper authentication
- **Regular key rotation** following your security policies

### Error Handling

- **Avoid exposing sensitive information** in error messages
- **Log security events** appropriately without including sensitive data
- **Use structured logging** with proper sanitization

### Security Monitoring

Enable security validation warnings by using `SecurityConfigurationValidator`:
- Warns about disabled certificate validation
- Alerts on HTTP URLs in configuration
- Validates URL safety across settings

## Sample Applications

See the `Security` directory of the [Samples](https://github.com/SteeltoeOSS/Samples) repository for examples of how to use these packages.

## Security Reporting

If you discover a security vulnerability, please report it following the [security policy](../../SECURITY.md) guidelines.

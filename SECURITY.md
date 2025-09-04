# Security Policy

## Reporting Security Vulnerabilities

The Steeltoe team takes security seriously. If you believe you have found a security vulnerability in Steeltoe, please report it responsibly.

### How to Report

Please **do not** report security vulnerabilities through public GitHub issues.

Instead, please report security vulnerabilities by emailing [steeltoe-security@vmware.com](mailto:steeltoe-security@vmware.com).

### What to Include

When reporting a security vulnerability, please include:

- A clear description of the vulnerability
- Steps to reproduce the issue
- Potential impact of the vulnerability
- Any suggested fixes or mitigations
- Your contact information for follow-up

### Response Timeline

- **Initial Response**: We will acknowledge receipt of your report within 2 business days
- **Investigation**: We will investigate and assess the vulnerability within 5 business days
- **Resolution**: We will work to resolve valid security issues as quickly as possible

### Security Best Practices

For developers using Steeltoe, please follow these security best practices:

1. **Keep Dependencies Updated**: Regularly update to the latest stable versions
2. **Secure Configuration**: Follow the security guidelines in our documentation
3. **Certificate Validation**: Always enable certificate validation in production
4. **HTTPS**: Use HTTPS for all communications in production environments
5. **Input Validation**: Use Steeltoe's built-in sanitization utilities for user inputs

### Supported Versions

We provide security updates for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 3.2.x   | :white_check_mark: |
| 3.1.x   | :white_check_mark: |
| 3.0.x   | :x:                |
| < 3.0   | :x:                |

### Security Features

Steeltoe includes several built-in security features:

- **Secure JWT validation** with strict defaults
- **Certificate validation** with configurable revocation checking
- **Input sanitization** utilities for secure logging
- **Configuration validation** to detect insecure settings
- **Secure JSON encoding** to prevent XSS vulnerabilities

For more information, see the [Security documentation](src/Security/README.md).
# Self signed, valid, client EKU Certificate
$filePath = 'validSelfSignedClientEkuCertificate.cer'
$certificate = New-SelfSignedCertificate `
    -Subject 'CN=Valid Self Signed Client EKU,OU=dev,DC=idunno-dev,DC=org' `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation "cert:CurrentUser\My" `
    -FriendlyName "Valid Self Signed Client EKU" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2")
$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)
Export-Certificate -Cert $certificatePath -FilePath $filePath
Remove-Item $certificatePath

# Self signed, valid, server EKU Certificate
$filePath = 'validSelfSignedServerEkuCertificate.cer'
$certificate = New-SelfSignedCertificate `
    -Subject 'CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org' `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation "cert:CurrentUser\My" `
    -FriendlyName "Valid Self Signed Server EKU" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")
$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)
Export-Certificate -Cert $certificatePath -FilePath $filePath
Remove-Item $certificatePath

# Self signed, valid, No EKU Certificate
$filePath = 'validSelfSignedNoEkuCertificate.cer'
$certificate = New-SelfSignedCertificate `
    -Subject 'CN=Valid Self Signed No EKU,OU=dev,DC=idunno-dev,DC=org' `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation "cert:CurrentUser\My" `
    -FriendlyName "Valid Self Signed No EKU Restrictions" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature
$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)
Export-Certificate -Cert $certificatePath -FilePath $filePath
Remove-Item $certificatePath

# Self signed, valid, No EKU Certificate, expired
$filePath = 'selfSignedNoEkuCertificateExpired.cer'
$certificate = New-SelfSignedCertificate `
    -Subject 'CN=Expired Self Signed,OU=dev,DC=idunno-dev,DC=org' `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date).AddYears(-1) `
    -NotAfter (Get-Date).AddDays(-1) `
    -CertStoreLocation "cert:CurrentUser\My" `
    -FriendlyName "Self Signed No EKU Restrictions Expired" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature
$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)
Export-Certificate -Cert $certificatePath -FilePath $filePath
Remove-Item $certificatePath


# Self signed, valid, No EKU Certificate, not valid yet
$filePath = 'selfSignedNoEkuCertificateNotValidYet.cer'
$certificate = New-SelfSignedCertificate `
    -Subject 'CN=Not Valid Yet Self Signed,OU=dev,DC=idunno-dev,DC=org' `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date).AddYears(2) `
    -NotAfter (Get-Date).AddYears(3) `
    -CertStoreLocation "cert:CurrentUser\My" `
    -FriendlyName "Self Signed No EKU Restrictions, not valid yet" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature
$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)
Export-Certificate -Cert $certificatePath -FilePath $filePath
Remove-Item $certificatePath


# Ensure script runs as Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "This script requires administrative privileges. Relaunching with elevated permissions..."
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

# Define variables
$CurrentPath = (Get-Location).Path
$CertFriendlyName = "IpSpecMetrixCert"
$CertFileName = "IpSpecMetrix.pfx"
$CertPath = Join-Path $CurrentPath $CertFileName
$PasswordString = "!pSpecMetrix8"

# Convert the password to SecureString
$Password = ConvertTo-SecureString -String $PasswordString -Force -AsPlainText

# Remove existing certificate if it exists
$existingCert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.FriendlyName -eq $CertFriendlyName }
if ($existingCert) {
    Remove-Item -Path "Cert:\LocalMachine\My\$($existingCert.Thumbprint)" -Force
    Write-Host "Existing certificate removed."
}

# Generate a self-signed certificate with exportable private key
$cert = New-SelfSignedCertificate -DnsName "localhost" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -FriendlyName $CertFriendlyName `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears(2) `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension "2.5.29.37={text}1.3.6.1.5.5.7.3.1"

Write-Host "Certificate created with Thumbprint: $($cert.Thumbprint)"

# Export the certificate to .pfx
Export-PfxCertificate -Cert $cert -FilePath $CertPath -Password $Password
Write-Host "Certificate exported to $CertPath"

# Grant IIS_IUSRS and NETWORK SERVICE access to the private key
$keyPath = "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys"
$keyContainer = $cert.PrivateKey.CspKeyContainerInfo.UniqueKeyContainerName
$keyFile = Get-ChildItem -Path $keyPath | Where-Object { $_.Name -eq $keyContainer }

if ($keyFile) {
    icacls "$keyFile.FullName" /grant "IIS_IUSRS:R"
    icacls "$keyFile.FullName" /grant "NETWORK SERVICE:R"
    Write-Host "Permissions updated for IIS_IUSRS and NETWORK SERVICE."
} else {
    Write-Host "⚠️ Warning: Private key file not found. Manual intervention may be required."
}

# Bind certificate to IIS (port 443)
$thumbprint = $cert.Thumbprint
$appid = "{00112233-4455-6677-8899-AABBCCDDEEFF}" # Replace with your app GUID

# Remove existing binding (if any)
$sslCert = netsh http show sslcert | Select-String -Pattern $thumbprint
if ($sslCert) {
    netsh http delete sslcert ipport=0.0.0.0:443
    Write-Host "Old SSL certificate removed from IIS."
} else {
    Write-Host "No existing SSL certificate found on port 443."
}

# Add new binding
netsh http add sslcert ipport=0.0.0.0:443 certhash=$thumbprint appid=$appid
Write-Host "Certificate successfully bound to IIS on port 443."

# Restart IIS (Fix for "iisreset not found" error)
if (Get-Command -Name "iisreset" -ErrorAction SilentlyContinue) {
    iisreset
    Write-Host "IIS restarted."
} else {
    Write-Host "⚠️ IIS reset command not found. Please restart IIS manually."
}

Write-Host "✅ Script execution complete!"

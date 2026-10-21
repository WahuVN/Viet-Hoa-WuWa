# Sign a fully staged WAHU Windows distribution with an authentic publicly
# trusted Code Signing certificate. Never create, import or trust a self-signed
# root. Supports provider-issued certificates in the Windows certificate store.
# For Microsoft Azure Artifact Signing: sign the same files through the official
# signing action first, then run Assert-ReleaseSigning.ps1 before packaging.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$PackageRoot,
  [Parameter(Mandatory=$true)][ValidatePattern('^[0-9A-Fa-f]{40}$')]
  [string]$CertificateThumbprint,
  [ValidateSet("CurrentUser","LocalMachine")][string]$StoreLocation = "CurrentUser",
  [string]$TimestampUrl = "http://timestamp.acs.microsoft.com",
  [string]$SignToolPath = "",
  [string[]]$RequiredRelativePaths = @(
    "VHWuWa.exe", "VHWuWa.Updater.exe",
    "content\loader\version.dll", "content\loader\verorg.dll",
    "content\loader\WuWaVH.dll", "editor\WahuPakEditor.exe",
    "tools\font-weight-instance.exe", "tools\repak.exe"
  )
)
$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath($PackageRoot)
if (-not (Test-Path -LiteralPath $root -PathType Container)) {
  throw "Không tìm thấy thư mục cài: $root"
}
$thumb = $CertificateThumbprint.ToUpperInvariant()
$storePath = "Cert:\$StoreLocation\My\$thumb"
$cert = Get-Item -LiteralPath $storePath -ErrorAction SilentlyContinue
if (-not $cert) {
  throw "Chưa có chứng chỉ Code Signing $thumb trong $StoreLocation\My. Không tự ký và không hạ bảo vệ Windows."
}
if (-not $cert.HasPrivateKey) { throw "Chứng chỉ không có khóa riêng để ký mã." }
if ([datetime]::UtcNow -lt $cert.NotBefore.ToUniversalTime() -or
    [datetime]::UtcNow -gt $cert.NotAfter.ToUniversalTime()) {
  throw "Chứng chỉ không còn trong thời hạn hiệu lực."
}
$codeSigningEku = "1.3.6.1.5.5.7.3.3"
$ekuValues = @(
  foreach ($ext in $cert.Extensions) {
    if ($ext -is [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]) {
      foreach ($oid in $ext.EnhancedKeyUsages) { $oid.Value }
    }
  }
)
if ($ekuValues -notcontains $codeSigningEku) {
  throw "Chứng chỉ không có EKU Code Signing."
}
$chain = [Security.Cryptography.X509Certificates.X509Chain]::new()
try {
  $chain.ChainPolicy.RevocationMode = [Security.Cryptography.X509Certificates.X509RevocationMode]::Online
  $chain.ChainPolicy.VerificationFlags = [Security.Cryptography.X509Certificates.X509VerificationFlags]::NoFlag
  if (-not $chain.Build($cert)) {
    throw "Chứng chỉ không xác minh được tới CA tin cậy trên máy build."
  }
  $rootCert = $chain.ChainElements[$chain.ChainElements.Count-1].Certificate
  if ($rootCert.Thumbprint -eq $cert.Thumbprint) {
    throw "Chứng chỉ tự ký không được chấp nhận cho Smart App Control."
  }
} finally { $chain.Dispose() }
if (-not $SignToolPath) {
  $signers = @(Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Filter signtool.exe -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
    Sort-Object FullName -Descending)
  if ($signers.Count -eq 0) { throw "Cần cài Windows SDK SignTool x64 trước khi ký." }
  $SignToolPath = $signers[0].FullName
}
if (-not (Test-Path -LiteralPath $SignToolPath -PathType Leaf)) {
  throw "Không tìm thấy SignTool: $SignToolPath"
}
$files = @(Get-ChildItem -LiteralPath $root -Recurse -File |
  Where-Object { $_.Extension -in @(".exe",".dll") } |
  Sort-Object FullName)
if ($files.Count -eq 0) { throw "Không tìm thấy EXE/DLL trong gói." }
# Preflight ALL binaries BEFORE signing any file. Reject broken existing
# signatures rather than silently replacing evidence of tampering.
$todo = @()
foreach ($file in $files) {
  $signature = Get-AuthenticodeSignature -LiteralPath $file.FullName
  if ($signature.Status -eq [Management.Automation.SignatureStatus]::Valid) {
    Write-Output ("Đã có chữ ký hợp lệ: "+$file.Name)
  } elseif ($signature.Status -eq [Management.Automation.SignatureStatus]::NotSigned) {
    $todo += $file
  } else {
    throw "File có chữ ký sai/hỏng: $($file.FullName) [$($signature.Status)]. Dừng toàn bộ ký."
  }
}
foreach ($file in $todo) {
  $args = @("sign", "/v", "/sha1", $thumb, "/s", "My", "/fd", "SHA256",
      "/tr", $TimestampUrl, "/td", "SHA256")
  if ($StoreLocation -eq "LocalMachine") { $args += "/sm" }
  $args += $file.FullName
  Write-Output ("Ký: "+$file.FullName.Substring($root.TrimEnd('\').Length+1))
  & $SignToolPath @args
  if ($LASTEXITCODE -ne 0) { throw "SignTool lỗi tại $($file.Name): $LASTEXITCODE" }
  $verify = Get-AuthenticodeSignature -LiteralPath $file.FullName
  if ($verify.Status -ne [Management.Automation.SignatureStatus]::Valid -or
      $verify.SignerCertificate.Thumbprint -ne $thumb) {
    throw "Chữ ký sau ký không hợp lệ: $($file.FullName)"
  }
}
& (Join-Path $PSScriptRoot "Assert-ReleaseSigning.ps1") -PackageRoot $root -RequiredRelativePaths $RequiredRelativePaths
if (-not $?) { throw "Sau ký còn thành phần EXE/DLL không hợp lệ." }
Write-Output ("SIGNED_RELEASE_READY: "+$todo.Count+" files signed; cần nghiệm thu trên Windows 11 Smart App Control On.")

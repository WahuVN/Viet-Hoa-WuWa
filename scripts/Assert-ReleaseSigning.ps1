# WAHU release protection: ALL executable PE files must have a valid, trusted
# Authenticode signature. No self-signed development certificate is sufficient.
# Smart App Control can still block unsafe binaries; this is a necessary, not
# sufficient, condition for public Windows releases.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$PackageRoot,
  [string]$ReportPath = "",
  [string[]]$RequiredRelativePaths = @(
    "VHWuWa.exe",
    "VHWuWa.Updater.exe",
    "content\loader\version.dll",
    "content\loader\verorg.dll",
    "content\loader\WuWaVH.dll",
    "editor\WahuPakEditor.exe",
    "tools\font-weight-instance.exe",
    "tools\repak.exe"
  ),
  [switch]$AuditOnly
)
$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath($PackageRoot)
if (-not (Test-Path -LiteralPath $root -PathType Container)) {
  throw "Không tìm thấy thư mục bộ cài: $root"
}
$all = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force |
  Where-Object { $_.Extension -in @(".exe",".dll") } |
  Sort-Object FullName)
if ($all.Count -eq 0) { throw "Không có EXE/DLL để kiểm tra trong $root" }
foreach ($rel in $RequiredRelativePaths) {
  $p = [IO.Path]::GetFullPath((Join-Path $root $rel))
  if (-not $p.StartsWith($root.TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar,
      [StringComparison]::OrdinalIgnoreCase)) {
    throw "Đường dẫn bắt buộc nằm ngoài gói: $rel"
  }
  if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
    throw "Gói thiếu thành phần thực thi bắt buộc: $rel"
  }
}
$rows = @()
foreach ($f in $all) {
  $rel = $f.FullName.Substring($root.TrimEnd([IO.Path]::DirectorySeparatorChar).Length+1)
  $signature = Get-AuthenticodeSignature -LiteralPath $f.FullName
  $good = $signature.Status -eq [System.Management.Automation.SignatureStatus]::Valid -and
    $null -ne $signature.SignerCertificate
  $sha = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
  $rows += [pscustomobject]@{
    path = $rel.Replace("\","/")
    bytes = $f.Length
    authenticode = $signature.Status.ToString()
    subject = if($signature.SignerCertificate){[string]$signature.SignerCertificate.Subject}else{""}
    issuer = if($signature.SignerCertificate){[string]$signature.SignerCertificate.Issuer}else{""}
    sha256 = $sha
    valid_signature = [bool]$good
  }
}
$bad = @($rows | Where-Object { -not $_.valid_signature })
$report = [ordered]@{
  status = if ($bad.Count -eq 0) { "PASS_SIGNED" } else { "FAIL_UNSIGNED_OR_UNTRUSTED" }
  package_root = $root
  executable_files = $rows.Count
  valid_files = $rows.Count - $bad.Count
  missing_or_untrusted = $bad.Count
  files = $rows
  warning = "Authenticode Valid là điều kiện cần; vẫn phải chạy thử trên Windows 11 Smart App Control ở chế độ On."
}
if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
  $dest = [IO.Path]::GetFullPath($ReportPath)
  New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
  [IO.File]::WriteAllText($dest, ($report | ConvertTo-Json -Depth 6),
    [Text.UTF8Encoding]::new($false))
}
foreach ($row in $rows) {
  $label = if ($row.valid_signature) { "SIGNED" } else { "BLOCKER" }
  Write-Output ("[{0}] {1} : {2}" -f $label,$row.path,$row.authenticode)
}
Write-Output ("RELEASE_SIGNING_{0}: valid={1}/{2}" -f $report.status, $report.valid_files, $report.executable_files)
if (-not $AuditOnly -and $bad.Count -ne 0) {
  $names = ($bad | ForEach-Object { $_.path+" ("+$_.authenticode+")" }) -join "; "
  throw "Chặn phát hành: EXE/DLL thiếu chữ ký số hợp lệ: $names. Ký mã từ CA tin cậy rồi kiểm thử Smart App Control; không hướng dẫn người dùng tắt bảo vệ."
}

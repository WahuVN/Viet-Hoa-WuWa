param([switch]$BeforePublish)
$ErrorActionPreference='Stop'
$root='D:\WAHU_WORK\WUWA_MOD_CLEAN_SAFE_20261013'
$staging=Join-Path $root 'dist\release-prep\v3.0.17\RELEASE_PREFLIGHT.json'
if(-not(Test-Path $staging)){throw "Missing verified release preflight; run verify-v317-github-release-assets.py"}
$local=Get-Content $staging -Raw -Encoding UTF8 |ConvertFrom-Json
if($local.status -ne 'READY_TO_UPLOAD_DRAFT_NOT_PUBLIC'){throw "Release preflight status invalid"}
$client=New-Object System.Net.WebClient
$client.Headers.Add('User-Agent','VHWuWa-Updater/3.0.16')
$client.Headers.Add('Accept','application/vnd.github+json')
$client.Headers.Add('X-GitHub-Api-Version','2022-11-28')
try {
 $feed=[Text.Encoding]::UTF8.GetString($client.DownloadData('https://api.github.com/repos/WahuVN/Viet-Hoa-WuWa/releases/latest'))|ConvertFrom-Json
} finally { $client.Dispose() }
if($BeforePublish){
 if($feed.tag_name -ne 'v3.0.16'){throw "Expected published v3.0.16 to stay latest while 3.0.17 is draft, got $($feed.tag_name)"}
 Write-Output "BEFORE_PUBLISH_PASS: latest is still $($feed.tag_name), old users are not notified early"
 exit 0
}
if($feed.tag_name -ne 'v3.0.17'){throw "NOT_LIVE: latest is $($feed.tag_name), expected v3.0.17"}
if($feed.draft -or $feed.prerelease){throw 'Latest release is Draft/Prerelease'}
foreach($asset in $local.assets) {
 $online=@($feed.assets|Where-Object {$_.name -ceq $asset.name})
 if($online.Count -ne 1){throw "Missing public asset: $($asset.name)"}
 if($online[0].size -ne $asset.bytes){throw "Public asset size does not match: $($asset.name)"}
 if($online[0].digest -ne ('sha256:'+$asset.sha256)){throw "Public asset SHA does not match: $($asset.name)"}
 if(-not $online[0].browser_download_url.StartsWith('https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v3.0.17/')){
  throw "Unexpected download URL: $($asset.name)"
 }
 Write-Output "PUBLIC_ASSET_PASS $($asset.name) sha256:$($asset.sha256)"
}
Write-Output 'PUBLIC_GITHUB_RELEASE_CONTRACT_PASS: v3.0.16 will detect new v3.0.17 on next enabled check'

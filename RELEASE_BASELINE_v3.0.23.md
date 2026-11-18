# PC standard release baseline

Public baseline: v3.0.22
Public ZIP SHA-256: 7C7D7808655B0803394A6BE6A36C6D7120A301713731953E046586E3662EBA44
Public main commit: ad146f86540c860eb3e18243739594baeb247bb7

Next prepared candidate: v3.0.23.

The R29 translation PAK, Lexend 660 TTF/PAK, 73-font catalog and video are hash-pinned by scripts/build-standard-pc-release.ps1.

Candidate builds may be unsigned for QA. Production publishing must use -RequireAuthenticode and WAHU_WINDOWS_SIGN_CERT_THUMBPRINT pointing to a valid CurrentUser/My Code Signing certificate with private key.

Do not publish if tests, VerifyOnly, ZIP byte verification, SHA-256, startup-health or required Authenticode checks fail.

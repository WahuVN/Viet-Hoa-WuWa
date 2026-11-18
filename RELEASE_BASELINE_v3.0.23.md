# PC v3.0.23 release baseline

Previous public release: v3.0.22
Previous ZIP SHA-256: 7C7D7808655B0803394A6BE6A36C6D7120A301713731953E046586E3662EBA44

The v3.0.23 PC release retains the exact R29 translation PAK (resource 3.8.11),
Lexend 660 font TTF/PAK, 73-font catalog and original launcher video,
all hash-pinned by scripts/build-standard-pc-release.ps1.

Only PC application/UI, user guidance and safer process-management code change.
Windows Authenticode code signing is optional for this release at the owner's
request; no signature is spoofed or required to run the app. Windows may display
security prompts for an unsigned executable. This is distinct from game PAK SIG.
SHA-256 integrity verification, correct EXE versions, byte-for-byte ZIP
validation, release startup-health and QA tests must still pass.

The real Wuthering Waves game shutdown behavior on WIN3 is not yet E2E-verified:
there is an official launcher but no installed Client-Win64-Shipping.exe.
Do not claim the real game is free of lingering process issues based only
on synthetic process tests.
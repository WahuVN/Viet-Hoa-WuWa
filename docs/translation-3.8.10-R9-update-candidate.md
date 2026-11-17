# WUWA 3.8.10 R9 translation update candidate - NOT PUBLIC

The prepared localization kit is in C:\Users\ADMIN\Downloads: WAHU_WUWA_3810_R9_UPDATE_READY_KIT_WITH_ROLLBACK.zip. SHA256: 907e1d5b67f9b1c121f14c73fbd9907535e7fa3f2c25a14bc87b8c2cfbd47f2b.

Translation: 14890 changed VI strings; 2148 masked redirects fixed; last title placeholder updated from authentic 3.8.10 JA/ZH and original WAHU VI. 4/4 KuroV12 ConfigDB fresh unpack SHA and SQLite QA PASS.

Tests: 9/9 localization install QA; 6/6 real PAK R8 to R9 rollback under fake game; final archive smoke PASS; .NET 149/149; VHWuWa app updater 3.0.18 to 3.0.19 sandbox E2E PASS.

This is NOT a release of the Windows application VHWuWa 3.0.19. That auto-updater requires versioned VHWuWa.exe and VHWuWa.Updater.exe plus independent current-game EN and HanViet PAK variants. WAHU dist has neither 3.8 EN nor HanViet; 3.7 loader/font runtime is unverified for 3.8; WIN3 has launcher only. The 3.8.9 recovered story source is not independently confirmed semantically unchanged in 3.8.10.

No GitHub commit/push, publication, real game file modification, or automatic install was performed. Do a controlled real-game PC load/font/story and rollback test before public release.

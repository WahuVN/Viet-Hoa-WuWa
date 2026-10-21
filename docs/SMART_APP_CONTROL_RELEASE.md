# WAHU Windows / Smart App Control — quy trình phát hành

**Trạng thái:** Bản public v3.0.18 chưa có chữ ký Authenticode hợp lệ trên 7/8 EXE/DLL được đóng gói. Đây là một nguyên nhân có thể khiến Smart App Control chặn launcher/game; để kết luận máy khách bị chặn file nào cần xem event 3077. Không được tuyên bố đã khắc phục hoàn toàn khi chưa có bản ký và kiểm thử trên SAC **On**.

## Các tệp PHẢI được kiểm tra trong bộ cài cuối

```text
VHWuWa.exe
VHWuWa.Updater.exe
editor/WahuPakEditor.exe
tools/repak.exe
tools/font-weight-instance.exe
content/loader/version.dll
content/loader/WuWaVH.dll
content/loader/verorg.dll
```

`verorg.dll` trong v3.0.18 mang chữ ký Microsoft hợp lệ. **Không thay đổi hoặc ký đè lên file hệ thống đã ký.** Các tệp còn lại phải có chữ ký Code Signing từ CA được Windows tin cậy. Nếu thêm EXE/DLL mới, cổng kiểm tra sẽ tự quét thêm.

## Cách thiết lập ký số được Windows tin cậy

Có hai lựa chọn, **không thể dùng chứng chỉ tự ký**:

1. **Microsoft Azure Artifact Signing:** cần tài khoản Azure, xác minh danh tính nhà phát hành, Signing Account, Certificate Profile. Dùng action `azure/artifact-signing-action@v2` trên Windows runner, OIDC `azure/login@v3`. Cấu hình GitHub:
   - Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
   - Variables: `ARTIFACT_SIGNING_ENDPOINT` (phù hợp vùng tài khoản), `ARTIFACT_SIGNING_ACCOUNT`, `ARTIFACT_SIGNING_CERT_PROFILE`.
   - Workflow tag `v*` sẽ **fail closed** nếu chưa có cấu hình hoặc file không được ký.
   - Workflow CI chỉ ký EXE của app/updater do CI dựng. **Bộ cài đầy đủ còn loader/Pak Editor/công cụ phụ trợ nên phải ký toàn bộ thư mục staging bằng cơ chế tương đương trước khi phát hành.**
2. **Chứng chỉ Code Signing từ CA thuộc Microsoft Trusted Root Program** được cài trong kho `Cert:\CurrentUser\My` hoặc `Cert:\LocalMachine\My`, có khóa riêng và EKU Code Signing. Ví dụ trên máy phát hành, sau khi chuẩn bị xong bundle:
   ```powershell
   .\scripts\Sign-ReleaseBinaries.ps1 -PackageRoot 'D:\...\package_stage' -CertificateThumbprint '<THUMBPRINT_CA_TRUSTED>'
   .\scripts\Assert-ReleaseSigning.ps1 -PackageRoot 'D:\...\package_stage'
   ```

`scripts/build-dist.ps1` và `scripts/build-release.ps1` giờ yêu cầu chữ ký hợp lệ **trước bước tạo ZIP**; nếu cung cấp `-CodeSigningThumbprint`, lệnh build sẽ ký trước khi chạy kiểm tra. Sau khi tạo ZIP, `scripts/Assert-ReleaseZipSigning.ps1` **giải nén từng EXE/DLL và kiểm chữ ký trên chính ZIP cuối**, chặn các gói bị ghi đè/trộn lẫn. Dừng nếu bất cứ thành phần nào không hợp lệ. Ký số thay đổi byte/SHA của EXE và DLL, vì vậy **phải đóng gói lại và cập nhật SHA-256 GitHub Release**, tuyệt đối không dùng lại hash của v3.0.18.

## Kiểm thử bắt buộc trước v3.0.19

- Kiểm tra chính xác mọi EXE/DLL sau ký bằng `Get-AuthenticodeSignature`, kiểm tra nhà phát hành, chain, timestamp; chạy kiểm tra `Assert-ReleaseSigning.ps1` trên **chính staging sẽ được ZIP**.
- Giải nén ZIP cuối sang thư mục mới, xác minh chữ ký lần nữa. Test launcher/updater/Pak Editor/công cụ font và đường dẫn loader trong thư mục game thử nghiệm.
- Trên **Windows 11 Smart App Control = On**, thử toàn bộ trình tự mở app, cài dịch, chạy game PC, sửa font, cập nhật và gỡ. Kiểm tra `Event Viewer → Applications and Services Logs → Microsoft → Windows → CodeIntegrity → Operational`, ID 3077 và 3089. Kiểm cả AppLocker/MSI and Script khi phù hợp.
- Bộ loader có thể chịu các cơ chế chống gian lận/chống injection riêng của trò chơi: dù được ký vẫn không được đảm bảo game chạy; test thực tế mới kết luận.
- **Không yêu cầu người dùng tắt Smart App Control/Defender**, không tự thêm ngoại lệ, không can thiệp chính sách bảo mật hoặc giả nhà phát hành.

## Chẩn đoán khi máy khách bị chặn

```powershell
.\scripts\Inspect-SmartAppControlBlocks.ps1 -Hours 48 -ReportPath .\SmartAppControl-WAHU.json
```

Lệnh chỉ đọc nhật ký, không sửa cài đặt Windows. Nếu không có event, điều đó không chứng minh ứng dụng tương thích; cần thông tin cụ thể lỗi và event trên máy bị chặn.

Tài liệu Microsoft:
- https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview
- https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control
- https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/operations/event-id-explanations

namespace VHWuWa.Core.Models;

/// <summary>Biến thể tên nhân vật của bản Việt hóa (theo pak dựng sẵn).</summary>
public enum NameVariant
{
    /// <summary>Tên Hán Việt (Kim Tịch, Trường Ly…).</summary>
    HanViet,
    /// <summary>Tên tiếng Anh (Jinhsi, Changli…).</summary>
    English
}

/// <summary>Nội dung Việt hóa dựng sẵn kèm theo app (thư mục content\).</summary>
public sealed class ViethoaContent
{
    public string ContentDir { get; set; } = "";
    public bool HasHanViet { get; set; }
    public bool HasEnglish { get; set; }
    public bool HasLoader { get; set; }
    /// <summary>Đường dẫn font pak mặc định (nếu có trong content\font\).</summary>
    public string? FontPak { get; set; }
    public bool Ready => (HasHanViet || HasEnglish) && HasLoader;
}

/// <summary>Trạng thái đã cài Việt hóa trong thư mục game.</summary>
public sealed class ViethoaStatus
{
    public bool Installed { get; set; }
    /// <summary>"hanviet" | "en" | "".</summary>
    public string Variant { get; set; } = "";
    public string? FontPak { get; set; }
    public string GamePath { get; set; } = "";

    public string VariantLabel => Variant switch
    {
        "hanviet" => "Tên Hán Việt",
        "en" => "Tên tiếng Anh",
        _ => "-"
    };
}

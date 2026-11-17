namespace VHWuWa.Core.Services;

/// <summary>
/// Never overwrite editor/project.db on application updates. A new portable
/// installation seeds it from project.default.db on the first editor request.
/// Temp-copy then atomic rename avoids exposing a partially copied database.
/// </summary>
public static class EditorProjectDatabaseSeeder
{
    public static bool EnsureSeeded(string editorDirectory)
    {
        var root = Path.GetFullPath(editorDirectory);
        var active = Path.Combine(root, "project.db");
        var seeded = Path.Combine(root, "project.default.db");
        if (File.Exists(active)) return false;
        if (!File.Exists(seeded))
            throw new FileNotFoundException("Thiếu project.default.db để tạo dữ liệu editor mới.", seeded);

        var temporary = Path.Combine(root, ".project-seed-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.Copy(seeded, temporary, overwrite: false);
            try
            {
                File.Move(temporary, active);
                return true;
            }
            catch (IOException) when (File.Exists(active))
            {
                // A concurrent editor request already published a complete DB.
                return false;
            }
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
        }
    }
}

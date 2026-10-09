using System;
using System.IO;

public static class ReplayFile
{
    public const string Extension = ".ipgn";
    public const string DeletedLabel = "Файл удалён";

    public static string PathFromName(string currentPath, string rawName, out string error)
    {
        error = null;
        string name = (rawName ?? string.Empty).Trim();
        if (name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            name = Path.GetFileNameWithoutExtension(name);
        if (name.Length == 0)
        {
            error = "Введите имя файла";
            return null;
        }
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            error = "Некорректное имя файла";
            return null;
        }

        string dest = Path.Combine(Path.GetDirectoryName(currentPath), name + Extension);
        if (File.Exists(dest)
            && !string.Equals(Path.GetFullPath(dest), Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase))
        {
            error = "Файл с таким именем уже существует";
            return null;
        }
        return dest;
    }

    public static bool TryRename(string currentPath, string rawName, out string dest, out string error)
    {
        if (string.IsNullOrEmpty(currentPath) || !File.Exists(currentPath))
        {
            dest = null;
            error = DeletedLabel;
            return false;
        }
        dest = PathFromName(currentPath, rawName, out error);
        if (error != null)
            return false;
        if (!string.Equals(Path.GetFullPath(dest), Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase))
            File.Move(currentPath, dest);
        return true;
    }
}

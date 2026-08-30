using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HistoryMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] GameObject content;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] GameObject emptyLabel;
    [SerializeField] GameObject renamePanel;
    [SerializeField] InputField renameInput;
    [SerializeField] Text renameError;

    private readonly List<GameObject> items = new();
    private string renamePath;

    public void Open()
    {
        panel.SetActive(true);
        renamePanel.SetActive(false);
        Refresh();
    }

    public void Close()
    {
        renamePanel.SetActive(false);
        panel.SetActive(false);
    }

    public void ConfirmRename()
    {
        string dest = ReplayPathFromName(renamePath, renameInput.text, out string error);
        if (error != null)
        {
            renameError.text = error;
            return;
        }
        if (!string.Equals(Path.GetFullPath(dest), Path.GetFullPath(renamePath), StringComparison.OrdinalIgnoreCase))
            File.Move(renamePath, dest);
        renamePanel.SetActive(false);
        Refresh();
    }

    public void CancelRename()
    {
        renamePanel.SetActive(false);
    }

    private void Refresh()
    {
        foreach (GameObject item in items)
            Destroy(item);
        items.Clear();

        string directory = GameRecorder.GamesDirectory;
        string[] files = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.ipgn")
            : Array.Empty<string>();

        var entries = new List<(string path, DateTime time)>(files.Length);
        foreach (string path in files)
            entries.Add((path, GameTime(path)));
        entries.Sort((a, b) => b.time.CompareTo(a.time));

        emptyLabel.SetActive(entries.Count == 0);
        foreach ((string path, DateTime _) in entries)
        {
            GameObject item = Instantiate(itemPrefab);
            item.transform.SetParent(content.transform, false);
            item.SetActive(true);
            item.transform.localScale = Vector3.one;
            string captured = path;
            item.transform.Find("Name").GetComponent<Text>().text = Path.GetFileNameWithoutExtension(path);
            item.GetComponent<Button>().onClick.AddListener(() => OpenReplay(captured));
            item.transform.Find("Rename").GetComponent<Button>().onClick.AddListener(() => BeginRename(captured));
            item.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(() => DeleteReplay(captured));
            items.Add(item);
        }
    }

    private void BeginRename(string path)
    {
        renamePath = path;
        renameInput.text = Path.GetFileNameWithoutExtension(path);
        renameError.text = string.Empty;
        renamePanel.SetActive(true);
        renamePanel.transform.SetAsLastSibling();
    }

    private void DeleteReplay(string path)
    {
        File.Delete(path);
        Refresh();
    }

    private static string ReplayPathFromName(string currentPath, string rawName, out string error)
    {
        error = null;
        string name = (rawName ?? string.Empty).Trim();
        if (name.EndsWith(".ipgn", StringComparison.OrdinalIgnoreCase))
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

        string dest = Path.Combine(Path.GetDirectoryName(currentPath), name + ".ipgn");
        if (File.Exists(dest)
            && !string.Equals(Path.GetFullPath(dest), Path.GetFullPath(currentPath), StringComparison.OrdinalIgnoreCase))
        {
            error = "Файл с таким именем уже существует";
            return null;
        }
        return dest;
    }

    private static DateTime GameTime(string path)
    {
        GameRecord record = IpgnParser.Parse(File.ReadAllText(path));
        if (DateTime.TryParseExact(
                $"{record.Date} {record.UTCTime}",
                "yyyy.MM.dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime time))
            return time;
        return File.GetLastWriteTimeUtc(path);
    }

    private static void OpenReplay(string path)
    {
        Settings.GameMode = GameMode.Replay;
        Settings.ReplayFilePath = path;
        SceneManager.LoadScene(1);
    }
}

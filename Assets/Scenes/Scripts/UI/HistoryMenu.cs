using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HistoryMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject emptyLabel;
    [SerializeField] private GameObject renamePanel;
    [SerializeField] private InputField renameInput;
    [SerializeField] private Text renameError;

    private static readonly Color currentRowColor = new Color(0.62f, 0.46f, 0.14f, 1f);

    private readonly List<GameObject> items = new();
    private string renamePath;

    public bool IsOpen => panel != null && panel.activeSelf;

    public event Action<string> OpenReplayRenamed;
    public event Action OpenReplayRemoved;

    public static HistoryMenu FindInScene()
    {
        HistoryMenu[] found = FindObjectsOfType<HistoryMenu>(true);
        return found.Length > 0 ? found[0] : null;
    }

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
        string previous = renamePath;
        if (!ReplayFile.TryRename(previous, renameInput.text, out string dest, out string error))
        {
            renameError.text = error;
            return;
        }
        if (IsOpenReplay(previous))
        {
            Settings.ReplayFilePath = dest;
            OpenReplayRenamed?.Invoke(dest);
        }
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
            Image row = item.GetComponent<Image>();
            if (row != null && IsOpenReplay(path))
                row.color = currentRowColor;
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
        bool current = IsOpenReplay(path);
        if (renamePath == path)
            renamePanel.SetActive(false);
        File.Delete(path);
        if (current)
        {
            Settings.ReplayFilePath = null;
            OpenReplayRemoved?.Invoke();
        }
        Refresh();
    }

    private static bool IsOpenReplay(string path)
    {
        return Settings.GameMode == GameMode.Replay && SamePath(path, Settings.ReplayFilePath);
    }

    private static bool SamePath(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
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

    private void OpenReplay(string path)
    {
        if (IsOpenReplay(path))
        {
            Close();
            return;
        }
        Settings.GameMode = GameMode.Replay;
        Settings.ReplayFilePath = path;
        Settings.ClearStartPosition();
        SceneManager.LoadScene(1);
    }
}

using System;
using System.Collections;
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
    [SerializeField] private GameObject drawer;
    [SerializeField] private Text drawerLabel;
    [SerializeField] private RectTransform exitButton;
    [SerializeField] private InputField searchInput;
    [SerializeField] private bool markOpenReplay;

    private static readonly Color currentRowColor = new Color(0.62f, 0.46f, 0.14f, 1f);
    private static readonly Color renameIdleColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color renameCheckColor = new Color(0.18f, 0.42f, 0.24f, 1f);

    private const string NoGamesLabel = "Нет партий";
    private const string NoMatchesLabel = "Ничего не найдено";
    private const float ExitMargin = 16f;
    private const float ExitGap = 8f;

    private readonly List<GameObject> items = new();
    private string renamePath;
    private string shownQuery = string.Empty;
    private Coroutine pendingScroll;
    private bool listed;

    public bool IsOpen => panel != null && panel.activeSelf;

    public event Action<string> OpenReplayRenamed;
    public event Action OpenReplayRemoved;

    private void OnEnable()
    {
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);
    }

    private void OnDisable()
    {
        if (searchInput != null)
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        if (pendingScroll != null)
        {
            StopCoroutine(pendingScroll);
            pendingScroll = null;
        }
    }

    public static HistoryMenu FindInScene()
    {
        HistoryMenu[] found = FindObjectsOfType<HistoryMenu>(true);
        return found.Length > 0 ? found[0] : null;
    }

    public void Open()
    {
        if (drawer != null)
            drawer.SetActive(true);
        panel.SetActive(true);
        renamePanel.SetActive(false);
        PlaceChrome();
        SetDrawerArrow();
        if (!listed)
            Refresh();
    }

    public void Close()
    {
        renamePanel.SetActive(false);
        panel.SetActive(false);
        PlaceChrome();
        SetDrawerArrow();
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    private void SetDrawerArrow()
    {
        if (drawerLabel != null)
            drawerLabel.text = IsOpen ? ">" : "<";
    }

    private void PlaceChrome()
    {
        if (exitButton != null)
        {
            float halfWidth = exitButton.sizeDelta.x * 0.5f;
            float halfHeight = exitButton.sizeDelta.y * 0.5f;
            float y = -(ExitMargin + halfHeight);
            float x = -(ExitMargin + halfWidth);
            if (IsOpen)
            {
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                float panelWidth = panelRect.sizeDelta.x * panelRect.localScale.x;
                x = -(panelWidth + ExitGap + halfWidth);
            }
            exitButton.anchoredPosition = new Vector2(x, y);
        }
        if (drawer == null)
            return;
        RectTransform drawerRect = drawer.GetComponent<RectTransform>();
        if (IsOpen)
            drawerRect.anchoredPosition = Vector2.zero;
        else if (exitButton != null)
            drawerRect.anchoredPosition = new Vector2(0f, -(ExitMargin + exitButton.sizeDelta.y + ExitGap));
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

    private void OnSearchChanged(string _)
    {
        if (listed)
            Refresh();
    }

    private void Refresh()
    {
        foreach (GameObject item in items)
            Destroy(item);
        items.Clear();

        List<string> games = ListGames();
        bool folderEmpty = games.Count == 0;
        string query = SearchQuery();
        ApplySearch(games);
        ShowEmpty(games.Count == 0, folderEmpty);
        foreach (string path in games)
        {
            GameObject item = Instantiate(itemPrefab);
            item.transform.SetParent(content.transform, false);
            item.SetActive(true);
            item.transform.localScale = Vector3.one;
            string captured = path;
            Image row = item.GetComponent<Image>();
            if (row != null && IsOpenReplay(path))
                row.color = currentRowColor;
            item.GetComponent<Button>().onClick.AddListener(() => OpenReplay(captured));
            item.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(() => DeleteReplay(captured));
            InputField input = item.transform.Find("Name").GetComponent<InputField>();
            if (input != null)
                BindInlineRename(input, item.transform.Find("Rename").GetComponent<Button>(), captured);
            else
            {
                item.transform.Find("Name").GetComponent<Text>().text = Path.GetFileNameWithoutExtension(path);
                item.transform.Find("Rename").GetComponent<Button>().onClick.AddListener(() => BeginRename(captured));
            }
            items.Add(item);
        }
        listed = true;
        if (!string.Equals(shownQuery, query, StringComparison.Ordinal))
        {
            shownQuery = query;
            QueueScrollToTop();
        }
    }

    public void RefreshListed()
    {
        Refresh();
    }

    private void BindInlineRename(InputField input, Button edit, string path)
    {
        string saved = Path.GetFileNameWithoutExtension(path);
        input.text = saved;
        input.interactable = false;
        Image field = input.GetComponent<Image>();
        if (field != null)
        {
            field.raycastTarget = false;
            field.color = new Color(field.color.r, field.color.g, field.color.b, 0f);
        }
        Transform icon = edit.transform.Find("Icon");
        Transform mark = edit.transform.Find("Check");
        Image buttonImage = edit.GetComponent<Image>();
        if (icon != null)
            icon.gameObject.SetActive(true);
        if (mark != null)
            mark.gameObject.SetActive(false);
        if (buttonImage != null)
            buttonImage.color = renameIdleColor;
        input.onValueChanged.AddListener(_ => ShowRenameError(string.Empty));
        input.onSubmit.AddListener(_ => CommitInlineRename(path, input));
        edit.onClick.AddListener(() =>
        {
            if (input.interactable)
            {
                CommitInlineRename(path, input);
                return;
            }
            input.interactable = true;
            if (field != null)
            {
                field.raycastTarget = true;
                field.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            }
            if (icon != null)
                icon.gameObject.SetActive(false);
            if (mark != null)
                mark.gameObject.SetActive(true);
            if (buttonImage != null)
                buttonImage.color = renameCheckColor;
            input.ActivateInputField();
        });
    }

    private void CommitInlineRename(string path, InputField input)
    {
        string typed = (input.text ?? string.Empty).Trim();
        if (string.Equals(typed, Path.GetFileNameWithoutExtension(path), StringComparison.Ordinal))
        {
            Refresh();
            return;
        }
        if (!ReplayFile.TryRename(path, input.text, out string dest, out string error))
        {
            ShowRenameError(error);
            return;
        }
        ShowRenameError(string.Empty);
        if (IsOpenReplay(path))
        {
            Settings.ReplayFilePath = dest;
            OpenReplayRenamed?.Invoke(dest);
        }
        Refresh();
    }

    private void ShowRenameError(string error)
    {
        if (renameError == null)
            return;
        renameError.text = error ?? string.Empty;
        renameError.gameObject.SetActive(!string.IsNullOrEmpty(error));
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
        // Следующая в видимом списке, а если удалённая была последней — предыдущая.
        string next = current ? NextReplay(path) : null;
        File.Delete(path);
        if (current && next != null)
        {
            ShowReplay(next);
            return;
        }
        if (current)
        {
            Settings.ReplayFilePath = null;
            OpenReplayRemoved?.Invoke();
        }
        Refresh();
    }

    private string NextReplay(string path)
    {
        List<string> games = ListGames();
        ApplySearch(games);
        int index = games.FindIndex(game => SamePath(game, path));
        if (index < 0)
            return games.Count > 0 ? games[0] : null;
        if (index + 1 < games.Count)
            return games[index + 1];
        if (index > 0)
            return games[index - 1];
        return null;
    }

    public static string NewestPath()
    {
        List<string> games = ListGames();
        return games.Count == 0 ? null : games[0];
    }

    private void QueueScrollToTop()
    {
        if (!isActiveAndEnabled || content == null)
            return;
        if (pendingScroll != null)
            StopCoroutine(pendingScroll);
        pendingScroll = StartCoroutine(ScrollToTop());
    }

    private IEnumerator ScrollToTop()
    {
        yield return null;
        pendingScroll = null;
        if (content == null)
            yield break;
        ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    private void ShowEmpty(bool show, bool folderEmpty)
    {
        if (emptyLabel == null)
            return;
        emptyLabel.SetActive(show);
        if (!show)
            return;
        Text label = emptyLabel.GetComponent<Text>();
        if (label == null)
            return;
        label.text = folderEmpty ? NoGamesLabel : NoMatchesLabel;
    }

    private void ApplySearch(List<string> games)
    {
        string query = SearchQuery();
        if (query.Length == 0)
            return;
        games.RemoveAll(path => !NameMatches(path, query));
    }

    private string SearchQuery()
    {
        if (searchInput == null)
            return string.Empty;
        return (searchInput.text ?? string.Empty).Trim();
    }

    private static bool NameMatches(string path, string query)
    {
        return Path.GetFileNameWithoutExtension(path)
            .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static List<string> ListGames()
    {
        string directory = GameRecorder.GamesDirectory;
        string[] files = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.ipgn")
            : Array.Empty<string>();
        var entries = new List<(string path, DateTime time)>(files.Length);
        foreach (string file in files)
            entries.Add((file, GameTime(file)));
        entries.Sort((a, b) => b.time.CompareTo(a.time));
        var games = new List<string>(entries.Count);
        foreach ((string file, DateTime _) in entries)
            games.Add(file);
        return games;
    }

    private bool IsOpenReplay(string path)
    {
        if (!markOpenReplay)
            return false;
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
            return;
        ShowReplay(path);
    }

    private void ShowReplay(string path)
    {
        Settings.GameMode = GameMode.Replay;
        Settings.ReplayFilePath = path;
        Settings.ClearStartPosition();
        ReplayController[] replays = FindObjectsOfType<ReplayController>(true);
        if (replays.Length > 0 && replays[0].enabled)
        {
            renamePanel.SetActive(false);
            replays[0].ShowCurrent();
            Refresh();
            return;
        }
        SceneManager.LoadScene(1);
    }
}

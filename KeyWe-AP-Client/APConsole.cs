using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeyWe_AP_Client;

public class APConsole : MonoBehaviour
{
    // GAME SPECIFIC STUFF
    private static readonly Dictionary<string, string> KeywordColors = new()
    {
        { "summer", "#00ff00" },
        { "fall", "#ff6600" },
        { "winter", "#3399ff" },
        { "warning", "#ff0000" },
        { "movement+", "#ffff00" },
        { "respawn+", "#ffff00" },
        { "dash+", "#ffff00" },
        { "jump+", "#ffff00" },
        { "swim+", "#ffff00" },
        { "chirp+", "#ffff00" },
        { "peck+", "#ffff00" },
        { "random facewear", "#ff00ff" },
        { "random hat", "#ff00ff" },
        { "random skin", "#ff00ff" },
        { "random footwear", "#ff00ff" },
        { "random backwear", "#ff00ff" },
        { "random hairstyle", "#ff00ff" },
        { "random arms", "#ff00ff" }
    };

    private static string
        _fontName = "BlackHanSans-Regular SDF"; // Run the game to print a list of fonts to console then select one

    private static string _gameName = "KeyWe";
    private static KeyCode _logToggleKey = KeyCode.F7; // Reassign in Create if configurable
    private static KeyCode _historyToggleKey = KeyCode.F8; // Reassign in Create if configurable
    private static CursorLockMode _defaultCursorMode = CursorLockMode.None;
    private static bool _defaultCursorVisible = true;

    // CONSOLE PARAMS
    private const float MessageHeight = 28f;
    private const float ConsoleHeight = 280f;

    private const float SlideInTime = 0.25f;
    private const float HoldTime = 3.0f;
    private const float FadeOutTime = 0.5f;

    private const float SlideInOffset = -50f;
    private const float FadeUpOffset = 20f;

    private const float PaddingX = 25f;
    private const float PaddingY = 25f;

    private const float MessageSpacing = 6f;

    // COLLECTIONS
    private static TMP_FontAsset _font;

    private readonly Queue<Image> _backgroundPool = new();

    private readonly Queue<LogEntry> _cachedEntries = new();

    private readonly Queue<TextMeshProUGUI> _textPool = new();
    private readonly List<LogEntry> _visibleEntries = [];
    private readonly List<LogEntry> _historyEntries = [];
    private GameObject _historyPanel;
    private RectTransform _historyContent;
    private bool _showHistory = false;
    private ScrollRect _historyScrollRect;
    private RectTransform _historyViewport;


    private Transform _messageParent;

    private bool _showConsole = true;

    public static APConsole Instance { get; private set; }

    public static void Create()
    {
        if (Instance != null)
            return;
        Resources.FindObjectsOfTypeAll<TMP_FontAsset>().ForEachItem(x => Debug.Log(x.name));
        _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name == _fontName);
        var consoleObject = new GameObject("ArchipelagoConsoleUI");
        DontDestroyOnLoad(consoleObject);
        Instance = consoleObject.AddComponent<APConsole>();
        Instance.BuildUI();
        Instance.Log($"Welcome to {_gameName} Archipelago!");
        Instance.Log(
            $"Press {_logToggleKey.ToString()} to Toggle log and {_historyToggleKey.ToString()} to toggle history");
        // foreach (var word in KeywordColors.Keys)
        // {
        //     Instance.Log(word);
        // }
    }

    private void Update()
    {
        UpdateMessages(Time.deltaTime);
        TryAddNewMessages();
        if (Input.GetKeyDown(_logToggleKey))
            ToggleConsole();
        if (Input.GetKeyDown(_historyToggleKey))
            ToggleHistory();

        if (_showHistory)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UpdateMessages(float delta)
    {
        for (var i = _visibleEntries.Count - 1; i >= 0; i--)
        {
            var e = _visibleEntries[i];
            var done = AnimateEntry(e, delta);

            if (done)
            {
                RecycleEntry(e);
                _visibleEntries.RemoveAt(i);
                RecalculateBaseY();
            }
            else
            {
                UpdateEntryVisual(e);
            }
        }
    }

    private void RecalculateBaseY()
    {
        var y = 0f;
        for (var i = _visibleEntries.Count - 1; i >= 0; i--)
        {
            var e = _visibleEntries[i];
            e.baseY = y;
            y += e.height + MessageSpacing;
        }
    }

    private bool AnimateEntry(LogEntry entry, float delta)
    {
        entry.stateTimer += delta;

        switch (entry.state)
        {
            case LogEntry.State.SlideIn:
            {
                var t = Mathf.Clamp01(entry.stateTimer / SlideInTime);
                entry.offsetY = Mathf.Lerp(SlideInOffset, 0f, EaseOutQuad(t));

                if (t >= 1f)
                {
                    entry.state = LogEntry.State.Hold;
                    entry.stateTimer = 0f;
                }
            }
                break;

            case LogEntry.State.Hold:
            {
                entry.offsetY = 0f;
                if (entry.stateTimer >= HoldTime)
                {
                    entry.state = LogEntry.State.FadeOut;
                    entry.stateTimer = 0f;
                }
            }
                break;

            case LogEntry.State.FadeOut:
            {
                var t = Mathf.Clamp01(entry.stateTimer / FadeOutTime);
                entry.offsetY = Mathf.Lerp(0f, FadeUpOffset, t);
                var alpha = 1f - t;
                entry.text.color = new Color(1f, 1f, 1f, alpha);
                entry.background.color = new Color(0f, 0f, 0f, 0.8f * alpha);

                if (t >= 1f)
                    return true;
            }
                break;
        }

        return false;
    }

    private static float EaseOutQuad(float x)
    {
        return 1f - (1f - x) * (1f - x);
    }

    private void TryAddNewMessages()
    {
        if (_showHistory)
            return;

        if (!_cachedEntries.Any())
            return;

        var maxMessages = Mathf.FloorToInt(ConsoleHeight / MessageHeight);
        if (_visibleEntries.Count >= maxMessages)
            return;

        var entry = _cachedEntries.Dequeue();
        entry.state = LogEntry.State.SlideIn;
        entry.stateTimer = 0f;

        entry.offsetY = SlideInOffset;
        entry.animatedY = entry.baseY + entry.offsetY;

        CreateEntryVisual(entry);

        _visibleEntries.Add(entry);
        RecalculateBaseY();
        entry.animatedY = entry.baseY + entry.offsetY;
    }

    private void AddHistoryEntryVisual(LogEntry entry)
    {
        var bg = GetBackground();
        bg.transform.SetParent(_historyContent, false);

        var bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.pivot = new Vector2(0.5f, 1);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(600f, MessageHeight);

        var text = GetText();
        var tRect = text.rectTransform;
        tRect.SetParent(bg.transform, false);

        tRect.anchorMin = new Vector2(0, 0);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.pivot = new Vector2(0, 0.5f);
        tRect.offsetMin = new Vector2(8f, 4f);
        tRect.offsetMax = new Vector2(-8f, -4f);

        entry.text = text;
        entry.background = bg;

        text.color = Color.white;
        bg.color = new Color(0, 0, 0, 0.8f);

        text.text = Colorize(entry.message);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
        var height = Mathf.Max(MessageHeight, text.preferredHeight + 8f);
        var layoutElement = bg.GetComponent<LayoutElement>();
        if (!layoutElement)
            layoutElement = bg.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredHeight = height;
    }

    private void CreateEntryVisual(LogEntry entry)
    {
        var bg = GetBackground();
        bg.transform.SetParent(_messageParent, false);

        var bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(0, 0);
        bgRect.pivot = new Vector2(0, 0);
        bgRect.sizeDelta = new Vector2(600f, MessageHeight);

        var text = GetText();
        var tRect = text.rectTransform;
        tRect.SetParent(bg.transform, false);

        tRect.anchorMin = new Vector2(0, 0);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.pivot = new Vector2(0, 0.5f);
        tRect.offsetMin = new Vector2(8f, 4f);
        tRect.offsetMax = new Vector2(-8f, -4f);

        entry.text = text;
        entry.background = bg;
        text.color = new Color(1, 1, 1, 1);
        bg.color = new Color(0, 0, 0, 0.8f);

        UpdateEntryVisual(entry);
    }

    private void UpdateEntryVisual(LogEntry entry)
    {
        entry.text.text = Colorize(entry.message);

        var bgRect = entry.background.rectTransform;
        var textHeight = entry.text.preferredHeight;
        entry.height = Mathf.Max(MessageHeight, textHeight + 8f);
        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, entry.height);

        var targetY = entry.baseY + entry.offsetY;
        entry.animatedY = Mathf.Lerp(entry.animatedY, targetY, Time.deltaTime * 12f);

        entry.background.rectTransform.anchoredPosition =
            new Vector2(0f, entry.animatedY);
    }

    private TextMeshProUGUI GetText()
    {
        if (_textPool.Count > 0)
        {
            var t = _textPool.Dequeue();
            t.gameObject.SetActive(true);
            return t;
        }

        var go = new GameObject("LogText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var t2 = go.GetComponent<TextMeshProUGUI>();
        t2.fontSize = 19;
        t2.color = Color.white;
        t2.font = _font;
        t2.wordSpacing = 20f;
        t2.alignment = TextAlignmentOptions.MidlineLeft;
        return t2;
    }

    private Image GetBackground()
    {
        if (_backgroundPool.Count > 0)
        {
            var img = _backgroundPool.Dequeue();
            img.gameObject.SetActive(true);
            return img;
        }

        var go = new GameObject("LogBG");
        var imgNew = go.AddComponent<Image>();
        imgNew.color = new Color(0, 0, 0, 0.8f);
        imgNew.type = Image.Type.Sliced;

        return imgNew;
    }

    private void RecycleEntry(LogEntry entry)
    {
        entry.text.gameObject.SetActive(false);
        entry.background.gameObject.SetActive(false);

        _textPool.Enqueue(entry.text);
        _backgroundPool.Enqueue(entry.background);
    }

    private string Colorize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        List<string> tokens = Tokenize(input);
        ApplyMultiWordColoring(tokens);
        ApplySingleWordColoring(tokens);
        return string.Concat(tokens);
    }

    private List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();

        if (string.IsNullOrEmpty(input))
            return tokens;

        bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '\'';
        var lastWasWord = IsWordChar(input[0]);

        foreach (var c in input)
        {
            var isWordChar = IsWordChar(c);
            if (isWordChar != lastWasWord)
            {
                tokens.Add(sb.ToString());
                sb.Clear();
            }

            sb.Append(c);
            lastWasWord = isWordChar;
        }

        if (sb.Length > 0)
            tokens.Add(sb.ToString());
        return tokens;
    }

    private void ApplySingleWordColoring(List<string> tokens)
    {
        var singleKeys = KeywordColors
            .Where(kvp => !kvp.Key.Contains(" "))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (!IsWord(token))
                continue;

            var lower = token.ToLowerInvariant();

            if (singleKeys.TryGetValue(lower, out var color))
            {
                tokens[i] = $"<color={color}>{token}</color>";
            }
        }
    }

    private void ApplyMultiWordColoring(List<string> tokens)
    {
        var multiKeys = KeywordColors
            .Where(kvp => kvp.Key.Contains(" "))
            .Select(kvp => new
            {
                Words = kvp.Key.Split([' '], StringSplitOptions.None),
                Color = kvp.Value
            })
            .OrderByDescending(kvp => kvp.Words.Length)
            .ToList();

        if (multiKeys.Count == 0 || tokens.Count == 0)
            return;

        var wordIndices = new List<int>();
        for (var i = 0; i < tokens.Count; i++)
            if (IsWord(tokens[i]))
                wordIndices.Add(i);

        var wi = 0;
        while (wi < wordIndices.Count)
        {
            var matchedAny = false;

            foreach (var key in multiKeys)
            {
                var wordCount = key.Words.Length;
                if (wi + wordCount > wordIndices.Count)
                    continue;

                var match = true;
                for (var k = 0; k < wordCount; k++)
                {
                    var token = tokens[wordIndices[wi + k]];
                    if (token.Equals(key.Words[k], StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    match = false;
                    break;
                }

                if (!match)
                    continue;

                var startTokenIndex = wordIndices[wi];
                var endTokenIndex = wordIndices[wi + wordCount - 1];
                var phraseText = string.Concat(tokens.Skip(startTokenIndex)
                    .Take(endTokenIndex - startTokenIndex + 1));
                tokens[startTokenIndex] = $"<color={key.Color}>{phraseText}</color>";
                for (var t = startTokenIndex + 1; t <= endTokenIndex; t++)
                    tokens[t] = string.Empty;
                wi += wordCount;
                matchedAny = true;
                break;
            }

            if (!matchedAny)
                wi++;
        }
    }

    private bool IsWord(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;
        var hasLetterOrDigit = false;
        for (var i = 0; i < token.Length; i++)
        {
            var c = token[i];
            if (char.IsLetterOrDigit(c))
            {
                hasLetterOrDigit = true;
                continue;
            }

            if (c != '\'' || i <= 0 || i >= token.Length - 1)
                return false;
            if (char.IsLetterOrDigit(token[i - 1]) && char.IsLetterOrDigit(token[i + 1]))
                continue;
            return false;
        }

        return hasLetterOrDigit;
    }


    public void Log(string text)
    {
        var entry = new LogEntry(text);

        _cachedEntries.Enqueue(entry);

        _historyEntries.Add(entry);

        if (_historyPanel != null && _historyPanel.activeSelf)
            AddHistoryEntryVisual(entry);
    }

    private void ToggleHistory()
    {
        _showHistory = !_showHistory;

        _messageParent.gameObject.SetActive(!_showHistory);

        _historyPanel.SetActive(_showHistory);

        if (_showHistory)
        {
            foreach (var e in _visibleEntries)
            {
                if (e.text != null) e.text.gameObject.SetActive(false);
                if (e.background != null) e.background.gameObject.SetActive(false);

                _textPool.Enqueue(e.text);
                _backgroundPool.Enqueue(e.background);
            }

            _visibleEntries.Clear();
            _cachedEntries.Clear();

            RebuildHistory();
        }
        else
        {
            Cursor.lockState = _defaultCursorMode;
            Cursor.visible = _defaultCursorVisible;
            _messageParent.gameObject.SetActive(_showConsole);
        }
    }

    private void ToggleConsole()
    {
        _showConsole = !_showConsole;

        foreach (var e in _visibleEntries)
        {
            if (e.background != null)
                e.background.gameObject.SetActive(_showConsole);
            if (e.text != null)
                e.text.gameObject.SetActive(_showConsole);
        }

        _messageParent.gameObject.SetActive(_showConsole);

        if (_showConsole)
            return;
        _showHistory = false;
        _historyPanel.SetActive(false);
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("APConsoleCanvas", typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var container = new GameObject("Messages", typeof(RectTransform));
        var rect = container.GetComponent<RectTransform>();
        rect.SetParent(canvasObject.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(PaddingX, PaddingY);
        _messageParent = container.transform;

        _historyPanel = new GameObject("HistoryPanel", typeof(RectTransform));
        var historyRect = _historyPanel.GetComponent<RectTransform>();
        historyRect.SetParent(canvasObject.transform, false);

        historyRect.anchorMin = new Vector2(0f, 0f);
        historyRect.anchorMax = new Vector2(0f, 0f);
        historyRect.pivot = new Vector2(0f, 0f);
        historyRect.anchoredPosition = new Vector2(PaddingX, PaddingY);
        historyRect.sizeDelta = new Vector2(600f, ConsoleHeight);

        _historyPanel.SetActive(false);

        _historyScrollRect = _historyPanel.AddComponent<ScrollRect>();
        _historyScrollRect.horizontal = false;
        _historyScrollRect.vertical = true;
        _historyScrollRect.scrollSensitivity = 10f;
        _historyScrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask));
        _historyViewport = viewport.GetComponent<RectTransform>();
        viewport.transform.SetParent(_historyPanel.transform, false);

        _historyViewport.anchorMin = Vector2.zero;
        _historyViewport.anchorMax = Vector2.one;
        _historyViewport.offsetMin = Vector2.zero;
        _historyViewport.offsetMax = Vector2.zero;

        var vpImage = viewport.GetComponent<Image>();
        vpImage.color = Color.white;
        vpImage.type = Image.Type.Simple;
        vpImage.raycastTarget = true;

        var mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        _historyScrollRect.viewport = _historyViewport;
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewport.transform, false);

        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _historyScrollRect.content = contentRect;
        _historyContent = contentRect;
    }

    private void RebuildHistory()
    {
        if (_historyContent == null)
            return;
        for (var i = _historyContent.childCount - 1; i >= 0; i--)
            Destroy(_historyContent.GetChild(i).gameObject);
        foreach (var entry in _historyEntries)
            AddHistoryEntryVisual(entry);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_historyContent);
        Canvas.ForceUpdateCanvases();
        _historyScrollRect.verticalNormalizedPosition = 0f;
    }

    [Serializable]
    public class LogEntry
    {
        public enum State
        {
            SlideIn,
            Hold,
            FadeOut
        }

        public State state = State.SlideIn;

        public float stateTimer;
        public float offsetY;
        public float baseY;
        public float animatedY;

        public TextMeshProUGUI text;
        public Image background;

        public string message;
        public float height = MessageHeight;

        public LogEntry(string msg)
        {
            message = msg;
        }
    }
}
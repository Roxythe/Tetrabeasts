using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class HelpMenuUI : MonoBehaviour
{
    [Header("Data")]
    public List<HelpTopicSO> topics = new List<HelpTopicSO>();

    [Header("Left Sidebar")]
    public Transform contentRoot;                 // ScrollView/Viewport/Content
    public HelpCategoryHeaderUI categoryHeaderPrefab;
    public HelpTopicButtonUI topicButtonPrefab;

    [Header("Right Content")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Image infoImage;

    [Header("Optional Video")]
    public GameObject videoRoot;                  // Parent object to hide/show
    public RawImage videoRawImage;
    public VideoPlayer videoPlayer;
    VideoClip _pendingClip;

    readonly Dictionary<string, HelpCategoryHeaderUI> _headers = new();
    readonly Dictionary<string, List<HelpTopicButtonUI>> _topicButtonsByCategory = new();

    string _openCategory = null;

    void OnEnable()
    {
        HookVideoEvents();
        RebuildSidebar();
        ShowDefault();
    }

    void OnDisable()
    {
        UnhookVideoEvents();
    }

    public void RebuildSidebar()
    {
        // Clear old UI
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        _headers.Clear();
        _topicButtonsByCategory.Clear();

        // Group topics by category
        var groups = topics
            .Where(t => t != null)
            .GroupBy(t => string.IsNullOrWhiteSpace(t.category) ? "Other" : t.category.Trim())
            .OrderBy(g => g.Key);

        foreach (var g in groups)
        {
            // Create category header
            var header = Instantiate(categoryHeaderPrefab, contentRoot);
            header.SetLabel(g.Key);

            _headers[g.Key] = header;
            _topicButtonsByCategory[g.Key] = new List<HelpTopicButtonUI>();

            // Create all topic buttons Starts hidden
            foreach (var topic in g.OrderBy(t => t.title))
            {
                var btn = Instantiate(topicButtonPrefab, contentRoot);
                btn.SetTopic(topic, () => ShowTopic(topic));
                btn.gameObject.SetActive(false); // Collapsed by default
                _topicButtonsByCategory[g.Key].Add(btn);
            }

            // Hook expand/collapse
            header.SetOnToggle(() =>
            {
                bool expandedNow = header.IsExpanded;
                SetCategoryExpandedExclusive(g.Key, expandedNow);
            });
        }
    }

    void SetCategoryExpanded(string category, bool expanded)
    {
        if (!_topicButtonsByCategory.TryGetValue(category, out var buttons)) return;
        foreach (var b in buttons)
            b.gameObject.SetActive(expanded);
    }

    void SetCategoryExpandedExclusive(string category, bool expanded)
    {
        if (!expanded)
        {
            // Just collapse this category
            SetCategoryExpanded(category, false);

            if (_openCategory == category)
                _openCategory = null;

            if (_headers.TryGetValue(category, out var h))
                h.SetExpanded(false);

            return;
        }

        // Collapse all other categories first
        foreach (var kvp in _topicButtonsByCategory)
        {
            string cat = kvp.Key;
            bool isTarget = (cat == category);

            SetCategoryExpanded(cat, isTarget);

            if (_headers.TryGetValue(cat, out var header))
                header.SetExpanded(isTarget);
        }

        _openCategory = category;
    }

    public void ShowTopic(HelpTopicSO topic)
    {
        if (!topic) return;

        if (titleText) titleText.text = topic.title;
        if (descriptionText) descriptionText.text = topic.description ?? "";

        if (infoImage)
        {
            infoImage.sprite = topic.image;
            infoImage.enabled = (topic.image != null);
        }

        // Video
        bool hasVideo = topic.videoClip != null && videoPlayer != null && videoRawImage != null;

        if (!hasVideo)
        {
            StopAndHideVideo(clearTexture: true);
        }
        else
        {
            StartVideo(topic.videoClip);
        }
    }

    void ShowDefault()
    {
        if (titleText) titleText.text = "Help Menu";
        if (descriptionText) descriptionText.text = "";

        if (infoImage)
        {
            infoImage.sprite = null;
            infoImage.enabled = false;
        }

        StopAndHideVideo(clearTexture: true);
    }

    void HookVideoEvents()
    {
        if (!videoPlayer) return;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    void UnhookVideoEvents()
    {
        if (!videoPlayer) return;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (_pendingClip != null && vp.clip != _pendingClip)
            return;

        vp.isLooping = true;
        vp.Play();
    }

    void StopAndHideVideo(bool clearTexture)
    {
        if (videoPlayer)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        _pendingClip = null;

        if (videoRawImage)
        {
            videoRawImage.enabled = false;

            if (videoPlayer && videoPlayer.targetTexture != null)
                videoRawImage.texture = videoPlayer.targetTexture;
        }

        if (clearTexture && videoPlayer && videoPlayer.targetTexture != null)
        {
            var rt = videoPlayer.targetTexture;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        if (videoRoot) videoRoot.SetActive(false);
    }

    void StartVideo(VideoClip clip)
    {
        if (!videoPlayer || !videoRawImage || clip == null)
        {
            StopAndHideVideo(clearTexture: false);
            return;
        }

        // Keep the UI visible
        if (videoRoot) videoRoot.SetActive(true);
        videoRawImage.enabled = true;

        if (videoPlayer.targetTexture != null)
            videoRawImage.texture = videoPlayer.targetTexture;

        bool comingFromNoVideo = (videoPlayer.clip == null);
        if (comingFromNoVideo && videoPlayer.targetTexture != null)
        {
            var rt = videoPlayer.targetTexture;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        // If already showing the same clip, do nothing
        if (videoPlayer.clip == clip && (videoPlayer.isPlaying || videoPlayer.isPrepared))
            return;

        _pendingClip = clip;

        videoPlayer.Stop();
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.clip = clip;

        videoPlayer.Prepare();
    }
}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImagePingPongAnimator : MonoBehaviour
{
    enum PlaybackMode
    {
        PingPong,
        Loop
    }

    [SerializeField] Image targetImage;
    [SerializeField] Sprite[] frames;
    [SerializeField, Min(1f)] float framesPerSecond = 12f;
    [SerializeField] PlaybackMode playbackMode = PlaybackMode.PingPong;
    [SerializeField, Min(0)] int firstFrame;
    [SerializeField, Tooltip("Use 0 to include the final frame in the array.")] int lastFrame;
    [SerializeField] bool playOnEnable = true;

    int _frameIndex;
    int _direction = 1;
    float _timer;
    bool _isPlaying;

    void Awake()
    {
        EnsureTargetImage();
        ApplyFrame();
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    void Update()
    {
        if (!_isPlaying || !EnsureTargetImage() || frames == null || frames.Length == 0)
            return;

        if (GetActiveFrameCount() <= 1)
        {
            ApplyFrame();
            return;
        }

        _timer += Time.unscaledDeltaTime;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);

        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            StepFrame();
        }
    }

    public void Play()
    {
        _isPlaying = true;
        ApplyFrame();
    }

    public void Stop()
    {
        _isPlaying = false;
    }

    public void ResetAnimation()
    {
        _timer = 0f;
        _frameIndex = GetNearestValidFrameIndex(GetFirstFrameIndex(), 1);
        _direction = 1;
        ApplyFrame();
    }

    public void Configure(Image image, Sprite[] animationFrames, float newFramesPerSecond, bool shouldPlay = true)
    {
        targetImage = image ? image : targetImage;
        frames = animationFrames;
        framesPerSecond = Mathf.Max(1f, newFramesPerSecond);
        ResetAnimation();

        if (shouldPlay)
            Play();
        else
            Stop();
    }

    void StepFrame()
    {
        int first = GetFirstFrameIndex();
        int last = GetLastFrameIndex(first);
        _frameIndex = Mathf.Clamp(_frameIndex, first, last);

        int nextFrameIndex = _frameIndex + _direction;

        if (playbackMode == PlaybackMode.Loop)
        {
            _direction = 1;
            if (nextFrameIndex > last)
                nextFrameIndex = first;

            _frameIndex = GetNearestValidFrameIndex(nextFrameIndex, 1);
            ApplyFrame();
            return;
        }

        if (nextFrameIndex > last)
        {
            nextFrameIndex = Mathf.Max(first, last - 1);
            _direction = -1;
        }
        else if (nextFrameIndex < first)
        {
            nextFrameIndex = Mathf.Min(last, first + 1);
            _direction = 1;
        }

        _frameIndex = GetNearestValidFrameIndex(nextFrameIndex, _direction);
        ApplyFrame();
    }

    void ApplyFrame()
    {
        if (!EnsureTargetImage() || frames == null || frames.Length == 0)
            return;

        _frameIndex = GetNearestValidFrameIndex(_frameIndex, _direction);
        Sprite frame = frames[_frameIndex];
        if (frame)
            targetImage.sprite = frame;
    }

    bool EnsureTargetImage()
    {
        if (!targetImage)
            targetImage = GetComponent<Image>();

        return targetImage != null;
    }

    int GetNearestValidFrameIndex(int desiredIndex, int searchDirection)
    {
        if (frames == null || frames.Length == 0)
            return 0;

        int first = GetFirstFrameIndex();
        int last = GetLastFrameIndex(first);
        desiredIndex = Mathf.Clamp(desiredIndex, first, last);
        if (frames[desiredIndex])
            return desiredIndex;

        int direction = searchDirection >= 0 ? 1 : -1;
        int activeFrameCount = Mathf.Max(1, last - first + 1);
        for (int offset = 1; offset < activeFrameCount; offset++)
        {
            int candidateIndex = desiredIndex + (offset * direction);
            if (candidateIndex >= first && candidateIndex <= last && frames[candidateIndex])
                return candidateIndex;

            candidateIndex = desiredIndex - (offset * direction);
            if (candidateIndex >= first && candidateIndex <= last && frames[candidateIndex])
                return candidateIndex;
        }

        return desiredIndex;
    }

    int GetActiveFrameCount()
    {
        if (frames == null || frames.Length == 0)
            return 0;

        int first = GetFirstFrameIndex();
        int last = GetLastFrameIndex(first);
        return Mathf.Max(1, last - first + 1);
    }

    int GetFirstFrameIndex()
    {
        if (frames == null || frames.Length == 0)
            return 0;

        return Mathf.Clamp(firstFrame, 0, frames.Length - 1);
    }

    int GetLastFrameIndex(int first)
    {
        if (frames == null || frames.Length == 0)
            return 0;

        int resolvedLastFrame = lastFrame <= 0 ? frames.Length - 1 : lastFrame;
        return Mathf.Clamp(resolvedLastFrame, first, frames.Length - 1);
    }
}

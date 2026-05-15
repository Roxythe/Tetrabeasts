using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImagePingPongAnimator : MonoBehaviour
{
    [SerializeField] Image targetImage;
    [SerializeField] Sprite[] frames;
    [SerializeField, Min(1f)] float framesPerSecond = 12f;
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

        if (frames.Length == 1)
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
        _frameIndex = GetNearestValidFrameIndex(0, 1);
        _direction = 1;
        ApplyFrame();
    }

    void StepFrame()
    {
        int nextFrameIndex = _frameIndex + _direction;

        if (nextFrameIndex >= frames.Length)
        {
            nextFrameIndex = Mathf.Max(0, frames.Length - 2);
            _direction = -1;
        }
        else if (nextFrameIndex < 0)
        {
            nextFrameIndex = Mathf.Min(frames.Length - 1, 1);
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

        desiredIndex = Mathf.Clamp(desiredIndex, 0, frames.Length - 1);
        if (frames[desiredIndex])
            return desiredIndex;

        int direction = searchDirection >= 0 ? 1 : -1;
        for (int offset = 1; offset < frames.Length; offset++)
        {
            int candidateIndex = desiredIndex + (offset * direction);
            if (candidateIndex >= 0 && candidateIndex < frames.Length && frames[candidateIndex])
                return candidateIndex;

            candidateIndex = desiredIndex - (offset * direction);
            if (candidateIndex >= 0 && candidateIndex < frames.Length && frames[candidateIndex])
                return candidateIndex;
        }

        return desiredIndex;
    }
}

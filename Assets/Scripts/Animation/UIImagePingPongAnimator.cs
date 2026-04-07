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
        if (!targetImage)
            targetImage = GetComponent<Image>();

        ApplyFrame();
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    void Update()
    {
        if (!_isPlaying || targetImage == null || frames == null || frames.Length == 0)
            return;

        if (frames.Length == 1)
        {
            targetImage.sprite = frames[0];
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
        _frameIndex = 0;
        _direction = 1;
        ApplyFrame();
    }

    void StepFrame()
    {
        _frameIndex += _direction;

        if (_frameIndex >= frames.Length)
        {
            _frameIndex = Mathf.Max(0, frames.Length - 2);
            _direction = -1;
        }
        else if (_frameIndex < 0)
        {
            _frameIndex = Mathf.Min(frames.Length - 1, 1);
            _direction = 1;
        }

        ApplyFrame();
    }

    void ApplyFrame()
    {
        if (!targetImage || frames == null || frames.Length == 0)
            return;

        _frameIndex = Mathf.Clamp(_frameIndex, 0, frames.Length - 1);
        targetImage.sprite = frames[_frameIndex];
    }
}
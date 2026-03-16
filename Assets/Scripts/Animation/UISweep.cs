using UnityEngine;

public sealed class UISweep : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] RectTransform target;

    [Header("Flip")]
    [SerializeField] RectTransform flipTarget;
    [SerializeField] bool invertFacing = false;

    [Header("Horizontal Walk")]
    [SerializeField] float horizontalSpeed = 70f;
    [SerializeField] float firstLegDistance = 70f;
    [SerializeField] float fullLegDistance = 140f;

    [Header("Vertical Bob")]
    [SerializeField] float bobAmplitude = 3f;
    [SerializeField] float bobCyclesPerSecond = 3.0f;

    [SerializeField] float bobPhase = 0f;
    [SerializeField] bool useUnscaledTime = true;

    [Header("Runtime Control")]
    [SerializeField] bool startPaused = false;
    [SerializeField] float bobStopEpsilon = 0.05f;
    [SerializeField] bool pauseAfterBobStep = true;

    [Header("Critical Hobble")]
    [SerializeField] bool criticalHobbleEnabled = true;

    [Range(0.05f, 1f)]
    [SerializeField] float criticalSpeedMultiplier = 0.50f;

    [Min(0.01f)]
    [SerializeField] float criticalBurstDistance = 12f;

    [Min(0f)]
    [SerializeField] float criticalBurstPauseSeconds = 0.08f;
    [SerializeField] bool criticalDisableBob = true;

    public bool IsPaused => _paused;
    public bool IsPauseRequested => _pauseRequested;
    public bool IsCriticalHobble => _criticalHobble;

    Vector2 _basePos;
    float _offsetX;
    float _remaining;
    int _dir = +1;

    Vector3 _flipBaseScale;

    float _timeAccum;
    bool _paused;
    bool _pauseRequested;

    float _speedMultiplier = 1f;

    // Critical hobble runtime
    bool _criticalHobble;
    float _hobbleBurstRemaining;
    float _hobblePauseRemaining;

    void Awake()
    {
        if (!target) target = (RectTransform)transform;
        if (!flipTarget) flipTarget = target;

        _basePos = target.anchoredPosition;
        _flipBaseScale = flipTarget.localScale;

        ResetWalkState();
        _paused = startPaused;
        _pauseRequested = false;

        ApplyFacingForDirection();
        ApplyPose();
    }

    void OnEnable()
    {
        if (!target) return;
        if (!flipTarget) flipTarget = target;

        _basePos = target.anchoredPosition;
        _flipBaseScale = flipTarget.localScale;

        ResetWalkState();
        _paused = startPaused;
        _pauseRequested = false;

        ApplyFacingForDirection();
        ApplyPose();
    }

    void OnDisable()
    {
        if (!target) return;

        target.anchoredPosition = _basePos;

        if (flipTarget)
            flipTarget.localScale = _flipBaseScale;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Clamp(multiplier, 0.01f, 10f);
    }

    public void SetCriticalHobble(bool enabled)
    {
        bool next = enabled && criticalHobbleEnabled;
        if (_criticalHobble == next) return;

        _criticalHobble = next;

        // Reset burst cycle so it starts with a small step immediately
        _hobblePauseRemaining = 0f;
        _hobbleBurstRemaining = Mathf.Max(0.01f, criticalBurstDistance);

        // Remove vertical bob immediately if configured
        if (_criticalHobble && criticalDisableBob)
            ApplyPose(forceBobZero: true);
    }

    public void SetPaused(bool paused)
    {
        if (!paused)
        {
            _paused = false;
            _pauseRequested = false;
            return;
        }

        // While critical hobble is active, pause immediately
        if (_criticalHobble)
        {
            _paused = true;
            _pauseRequested = false;
            ApplyPose(forceBobZero: true);
            return;
        }

        if (!pauseAfterBobStep || bobAmplitude <= 0f || bobCyclesPerSecond <= 0f)
        {
            _paused = true;
            _pauseRequested = false;
            ApplyPose(forceBobZero: true);
            return;
        }

        _pauseRequested = true;
    }

    void ResetWalkState()
    {
        _offsetX = 0f;
        _dir = +1;
        _remaining = Mathf.Max(0f, firstLegDistance);

        _timeAccum = 0f;

        _hobblePauseRemaining = 0f;
        _hobbleBurstRemaining = Mathf.Max(0.01f, criticalBurstDistance);
    }

    void Update()
    {
        if (!target) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        if (_paused)
            return;

        // Effective speed multiplier
        float effMult = _speedMultiplier * (_criticalHobble ? criticalSpeedMultiplier : 1f);
        float effHorizSpeed = Mathf.Max(0f, horizontalSpeed) * effMult;

        // If a pause is requested, freeze horizontal movement immediately but finish bob
        if (_pauseRequested)
        {
            float effBobCps = Mathf.Max(0f, bobCyclesPerSecond) * effMult;
            AdvanceBob(dt, effBobCps);
            ApplyPose();

            if (IsBobNearGround(effBobCps))
            {
                _paused = true;
                _pauseRequested = false;
                ApplyPose(forceBobZero: true);
            }
            return;
        }

        if (_criticalHobble)
        {
            TickCriticalHobble(dt, effHorizSpeed);           
            ApplyPose(forceBobZero: criticalDisableBob); // Critical disables bob entirely
            return;
        }

        // Normal continuous walk
        float step = effHorizSpeed * dt;
        float move = Mathf.Min(step, _remaining);
        _remaining -= move;
        _offsetX += move * _dir;

        if (_remaining <= 0.00001f)
        {
            _dir *= -1;
            ApplyFacingForDirection();
            _remaining = Mathf.Max(0f, fullLegDistance);
        }

        float effBob = Mathf.Max(0f, bobCyclesPerSecond) * effMult;
        AdvanceBob(dt, effBob);
        ApplyPose();
    }

    void TickCriticalHobble(float dt, float effHorizSpeed)
    {
        // Pause phase
        if (_hobblePauseRemaining > 0f)
        {
            _hobblePauseRemaining -= dt;
            return;
        }

        // Move phase (burst)
        if (_hobbleBurstRemaining <= 0.00001f)
            _hobbleBurstRemaining = Mathf.Max(0.01f, criticalBurstDistance);

        float step = effHorizSpeed * dt;

        // Constrain by leg remainder and burst remainder
        float move = Mathf.Min(step, _remaining, _hobbleBurstRemaining);

        _remaining -= move;
        _hobbleBurstRemaining -= move;
        _offsetX += move * _dir;

        // If leg ends, flip direction and start next leg
        if (_remaining <= 0.00001f)
        {
            _dir *= -1;
            ApplyFacingForDirection();
            _remaining = Mathf.Max(0f, fullLegDistance);

            // Start a fresh burst after turning
            _hobbleBurstRemaining = Mathf.Max(0.01f, criticalBurstDistance);
            _hobblePauseRemaining = Mathf.Max(0f, criticalBurstPauseSeconds);
            return;
        }

        // If burst ends, pause briefly
        if (_hobbleBurstRemaining <= 0.00001f)
        {
            _hobblePauseRemaining = Mathf.Max(0f, criticalBurstPauseSeconds);
            _hobbleBurstRemaining = Mathf.Max(0.01f, criticalBurstDistance);
        }
    }

    void AdvanceBob(float dt, float effBobCps)
    {
        if (bobAmplitude <= 0f || effBobCps <= 0f)
            return;

        _timeAccum += dt;
    }

    bool IsBobNearGround(float effBobCps)
    {
        if (bobAmplitude <= 0f || effBobCps <= 0f)
            return true;

        float omega = Mathf.PI * 2f * effBobCps;
        float phase = (_timeAccum * omega) + bobPhase;
        float y = Mathf.Sin(phase) * bobAmplitude;

        return Mathf.Abs(y) <= bobStopEpsilon;
    }

    void ApplyPose(bool forceBobZero = false)
    {
        float bob = 0f;

        if (!forceBobZero && bobAmplitude > 0f && bobCyclesPerSecond > 0f)
        {
            float effBobCps = Mathf.Max(0f, bobCyclesPerSecond) * _speedMultiplier;
            if (effBobCps > 0f)
            {
                float omega = Mathf.PI * 2f * effBobCps;
                bob = Mathf.Sin((_timeAccum * omega) + bobPhase) * bobAmplitude;
            }
        }

        target.anchoredPosition = _basePos + new Vector2(_offsetX, bob);
    }

    void ApplyFacingForDirection()
    {
        if (!flipTarget) return;

        float absX = Mathf.Abs(_flipBaseScale.x);
        float dirSign = (_dir >= 0) ? 1f : -1f;
        float invert = invertFacing ? -1f : 1f;

        var s = flipTarget.localScale;
        s.x = absX * dirSign * invert;
        s.y = _flipBaseScale.y;
        s.z = _flipBaseScale.z;
        flipTarget.localScale = s;
    }
}
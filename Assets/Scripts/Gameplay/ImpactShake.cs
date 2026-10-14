using UnityEngine;

// Applies only an additive offset, restoring it before layout/gameplay can move the target.
[DefaultExecutionOrder(-1000)]
public sealed class ImpactShake : MonoBehaviour
{
    Vector3 offset;
    float remaining, duration, strength;
    float lastTriggerTime = float.NegativeInfinity;
    public static void Play(Transform target, float seconds, float pixels)
    {
        if (!target) return;
        var shake = target.GetComponent<ImpactShake>() ?? target.gameObject.AddComponent<ImpactShake>();
        float carried = shake.remaining > 0f && shake.duration > 0f ? shake.strength * (shake.remaining / shake.duration) : 0f;
        // Keep a small impact memory even just after a short shake has finished.
        carried = Mathf.Max(carried, shake.strength * .35f * Mathf.Clamp01(1f - (Time.time - shake.lastTriggerTime) / .4f));
        shake.strength = Mathf.Min(Mathf.Max(pixels * 1.8f, 12f), pixels + carried * 0.65f);
        shake.duration = Mathf.Max(seconds, shake.remaining);
        shake.remaining = shake.duration;
        shake.lastTriggerTime = Time.time;
    }
    void Update() { transform.localPosition -= offset; offset = Vector3.zero; }
    public static void Stop(Transform target)
    {
        var shake = target ? target.GetComponent<ImpactShake>() : null;
        if (shake) shake.OnDisable();
    }
    void LateUpdate()
    {
        if (remaining <= 0f || Time.timeScale <= 0f) return;
        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        offset = (Vector3)(Random.insideUnitCircle * strength * (remaining / duration));
        transform.localPosition += offset;
    }
    void OnDisable() { transform.localPosition -= offset; offset = Vector3.zero; remaining = 0f; lastTriggerTime = float.NegativeInfinity; }
}

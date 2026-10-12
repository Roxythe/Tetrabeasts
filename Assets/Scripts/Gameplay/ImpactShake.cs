using UnityEngine;

// Applies only an additive offset, restoring it before layout/gameplay can move the target.
[DefaultExecutionOrder(-1000)]
public sealed class ImpactShake : MonoBehaviour
{
    Vector3 offset;
    float remaining, duration, strength;
    public static void Play(Transform target, float seconds, float pixels)
    {
        if (!target) return;
        var shake = target.GetComponent<ImpactShake>() ?? target.gameObject.AddComponent<ImpactShake>();
        shake.duration = seconds; shake.remaining = seconds; shake.strength = pixels;
    }
    void Update() { transform.localPosition -= offset; offset = Vector3.zero; }
    void LateUpdate()
    {
        if (remaining <= 0f || Time.timeScale <= 0f) return;
        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        offset = (Vector3)(Random.insideUnitCircle * strength * (remaining / duration));
        transform.localPosition += offset;
    }
    void OnDisable() { transform.localPosition -= offset; offset = Vector3.zero; remaining = 0f; }
}

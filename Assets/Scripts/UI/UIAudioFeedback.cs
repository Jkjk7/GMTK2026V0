using UnityEngine;

/// <summary>
/// Shared UI sounds. Assigned clips take priority over quiet, cached transaction tones.
/// </summary>
public sealed class UIAudioFeedback : MonoBehaviour
{
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip clickClip;
    [SerializeField] AudioClip purchaseClip;
    [SerializeField] AudioClip breachClip;
    [SerializeField] AudioClip rejectClip;

    static UIAudioFeedback _shared;
    static AudioClip _receiptTone;
    static AudioClip _rejectTone;
    float _lastReceipt = float.NegativeInfinity;
    float _lastReject = float.NegativeInfinity;

    void OnEnable()
    {
        if (_shared == null) _shared = this;
    }

    void OnDisable()
    {
        if (_shared == this) _shared = null;
    }

    public static void Receipt()
    {
        if (_shared != null) _shared.PlayPurchase();
    }

    public static void Rejected()
    {
        if (_shared != null) _shared.PlayRejection();
    }

    public void EnsureSource()
    {
        if (source == null)
        {
            source = gameObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
        }
    }

    public void PlayClick() => Play(clickClip);
    public void PlayBreach() => Play(breachClip);

    public void PlayPurchase()
    {
        float now = Time.realtimeSinceStartup;
        if (now - _lastReceipt < 0.09f) return;
        _lastReceipt = now;
        Play(purchaseClip != null ? purchaseClip : GetTone(true));
    }

    public void PlayRejection()
    {
        float now = Time.realtimeSinceStartup;
        if (now - _lastReject < 0.12f) return;
        _lastReject = now;
        Play(rejectClip != null ? rejectClip : GetTone(false));
    }

    static AudioClip GetTone(bool receipt)
    {
        AudioClip cached = receipt ? _receiptTone : _rejectTone;
        if (cached != null) return cached;
        const int sampleRate = 22050;
        float duration = receipt ? 0.24f : 0.13f;
        var samples = new float[Mathf.CeilToInt(sampleRate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)sampleRate;
            float attack = Mathf.Min(1f, t / 0.004f);
            float tail = Mathf.Clamp01((duration - t) / 0.025f);
            float envelope = attack * tail * Mathf.Exp(-t * (receipt ? 17f : 24f));
            float note;
            if (receipt)
            {
                // A small glass bell; deterministic partials never touch gameplay randomness.
                note = Mathf.Sin(2f * Mathf.PI * 1568f * t)
                    + 0.28f * Mathf.Sin(2f * Mathf.PI * 3136f * t) * Mathf.Exp(-t * 12f);
            }
            else
            {
                float phase = 2f * Mathf.PI * (280f * t - 390f * t * t);
                note = Mathf.Sin(phase) + 0.15f * Mathf.Sin(phase * 2f);
            }
            samples[i] = note * envelope * (receipt ? 0.11f : 0.08f);
        }
        cached = AudioClip.Create(receipt ? "UI receipt bell" : "UI rejected tap",
            samples.Length, 1, sampleRate, false);
        cached.hideFlags = HideFlags.HideAndDontSave;
        cached.SetData(samples, 0);
        if (receipt) _receiptTone = cached;
        else _rejectTone = cached;
        return cached;
    }

    void Play(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSource();
        source.PlayOneShot(clip);
    }
}

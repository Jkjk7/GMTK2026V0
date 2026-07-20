using UnityEngine;

/// <summary>
/// UI 音效反馈占位。暂无 AudioClip 时静默，后续可在 GameSkin/Inspector 挂音效。
/// </summary>
public sealed class UIAudioFeedback : MonoBehaviour
{
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip clickClip;
    [SerializeField] AudioClip purchaseClip;
    [SerializeField] AudioClip breachClip;

    public void EnsureSource()
    {
        if (source == null)
        {
            source = gameObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
        }
    }

    public void PlayClick() => Play(clickClip);
    public void PlayPurchase() => Play(purchaseClip);
    public void PlayBreach() => Play(breachClip);

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

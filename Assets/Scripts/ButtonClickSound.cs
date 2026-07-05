using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioSource audioSource;

    public void Configure(AudioClip clip, AudioSource source)
    {
        clickClip = clip;
        audioSource = source;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable)
        {
            return;
        }

        if (clickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickClip);
        }
    }
}

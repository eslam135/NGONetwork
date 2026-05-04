using System.Collections;
using TMPro;
using UnityEngine;

public class KillFeedEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float _visibleDuration = 2.5f;
    [SerializeField] private float _fadeDuration = 1f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (_messageText == null)
        {
            _messageText = GetComponent<TMP_Text>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void Initialize(string message)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(_visibleDuration);

        float timer = 0f;

        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
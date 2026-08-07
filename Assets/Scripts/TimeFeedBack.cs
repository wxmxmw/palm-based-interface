using System.Collections;
using UnityEngine;

public class TimedFeedback : MonoBehaviour
{
    [SerializeField] private GameObject feedbackObject;
    [SerializeField] private float displaySeconds = 0.5f;

    private Coroutine hideCoroutine;

    public void ShowFeedback()
    {
        feedbackObject.SetActive(true);

        // restart
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displaySeconds);

        feedbackObject.SetActive(false);
        hideCoroutine = null;
    }

    private void OnDisable()
    {
        if (feedbackObject != null)
        {
            feedbackObject.SetActive(false);
        }

        hideCoroutine = null;
    }
}
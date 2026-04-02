using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade UI")]
    [SerializeField] private Image fadeImage;

    [Header("Timing")]
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.45f;

    [Header("Startup")]
    [SerializeField] private bool fadeInOnGameStart = true;

    private bool isFading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureFadeImage();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        EnsureFadeImage();

        if (fadeInOnGameStart)
        {
            SetAlphaImmediate(1f);
            StartCoroutine(Fade(1f, 0f, fadeInDuration));
        }
        else
        {
            SetAlphaImmediate(0f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void FadeToScene(string sceneName)
    {
        if (isFading)
            return;

        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        isFading = true;

        EnsureFadeImage();

        // Smooth fade to black
        yield return Fade(fadeImage.color.a, 1f, fadeOutDuration);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (!op.isDone)
            yield return null;

        // sceneLoaded callback will handle fade-in
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureFadeImage();

        // Keep screen black on first frame of new scene
        SetAlphaImmediate(1f);

        StartCoroutine(FadeInAfterSceneLoad());
    }

    private IEnumerator FadeInAfterSceneLoad()
    {
        // Let scene initialize one frame first
        yield return null;

        EnsureFadeImage();
        yield return Fade(1f, 0f, fadeInDuration);

        isFading = false;
    }

    private void EnsureFadeImage()
    {
        if (fadeImage != null)
            return;

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.name == "FadeImage")
            {
                fadeImage = img;
                return;
            }
        }

        GameObject go = GameObject.Find("FadeImage");
        if (go != null)
            fadeImage = go.GetComponent<Image>();
    }

    private void SetAlphaImmediate(float alpha)
    {
        if (fadeImage == null)
            return;

        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null)
            yield break;

        float t = 0f;
        Color c = fadeImage.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            c.a = Mathf.Lerp(from, to, progress);
            fadeImage.color = c;

            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }
}
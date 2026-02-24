using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade UI")]
    public Image fadeImage; // Assign in Inspector (Main scene)

    [Header("Timing")]
    public float fadeOutDuration = 0.3f;
    public float fadeInDuration = 0.3f;

    [Header("Debug Test")]
    public bool enableTestKey = true;
    public string testSceneName = "Warehouse";

    private bool isFading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Ensure fully transparent at start
        EnsureFadeImage();
        SetAlphaImmediate(0f);
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    public void FadeToScene(string sceneName)
    {
        if (isFading) return;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        isFading = true;

        EnsureFadeImage();
        yield return Fade(0f, 1f, fadeOutDuration);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        // OnSceneLoaded will rebind fadeImage if needed
        EnsureFadeImage();
        yield return Fade(1f, 0f, fadeInDuration);

        isFading = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ✅ After scene changes, re-find FadeImage if the old one was destroyed
        EnsureFadeImage();
        SetAlphaImmediate(0f);
    }

    private void EnsureFadeImage()
    {
        if (fadeImage != null) return;

        // Looks for an Image named "FadeImage" anywhere in the scene hierarchy
        var go = GameObject.Find("FadeImage");
        if (go != null)
            fadeImage = go.GetComponent<Image>();
    }

    private void SetAlphaImmediate(float a)
    {
        if (fadeImage == null) return;
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null) yield break;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = duration > 0 ? t / duration : 1f;
            float alpha = Mathf.Lerp(from, to, progress);

            var c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        var final = fadeImage.color;
        final.a = to;
        fadeImage.color = final;
    }
}
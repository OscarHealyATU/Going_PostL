#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class ScreenshotCapture : MonoBehaviour
{
    [MenuItem("Tools/Capture Screenshot")]
    public static void CaptureScreenshot()
    {
        string folderPath = Path.Combine(Application.dataPath, "..", "Screenshots");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filename = Path.Combine(folderPath, "screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");
        ScreenCapture.CaptureScreenshot(filename);
        Debug.Log("Screenshot saved: " + filename);
    }
}
#endif
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameButton : MonoBehaviour
{
    [Header("Scene To Start New Game In")]
    [SerializeField] private string firstSceneName = "Main";

    [Header("Optional")]
    [SerializeField] private bool clearPlayerPrefs = true;

    private bool isBusy;

    public void StartNewGame()
    {
        if (isBusy) return;
        StartCoroutine(StartNewGameRoutine());
    }

    private IEnumerator StartNewGameRoutine()
    {
        isBusy = true;

        // Close and remove the live DB connection if it exists
        if (DbBoot.Instance != null)
        {
            if (DbBoot.Instance.GameDb != null)
            {
                DbBoot.Instance.GameDb.Dispose();
            }

            Destroy(DbBoot.Instance.gameObject);
            yield return null; // allow destroy to complete
        }

        string dbPath = GameDb.DbPath;

        // Delete main DB file
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
            Debug.Log("[NewGameButton] Deleted DB: " + dbPath);
        }

        // Delete possible SQLite sidecar files too
        string walPath = dbPath + "-wal";
        string shmPath = dbPath + "-shm";
        string journalPath = dbPath + "-journal";

        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);
        if (File.Exists(journalPath)) File.Delete(journalPath);

        if (clearPlayerPrefs)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[NewGameButton] Cleared PlayerPrefs.");
        }

        // Load the first gameplay scene.
        // DbBoot should recreate the DB there with default values.
        SceneManager.LoadScene(firstSceneName);
    }
}
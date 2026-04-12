using System.Collections.Generic;
using UnityEngine;

public class GameplayUILock : MonoBehaviour
{
    [Header("Disable While UI Is Open")]
    [SerializeField] private MonoBehaviour[] behavioursToDisable;

    private readonly List<MonoBehaviour> disabledByLock = new List<MonoBehaviour>();
    private bool isLocked;

    public bool IsLocked => isLocked;

    public void Lock()
    {
        if (isLocked)
            return;

        disabledByLock.Clear();

        if (behavioursToDisable != null)
        {
            foreach (var behaviour in behavioursToDisable)
            {
                if (behaviour == null)
                    continue;

                if (behaviour.enabled)
                {
                    behaviour.enabled = false;
                    disabledByLock.Add(behaviour);
                }
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isLocked = true;
    }

    public void Unlock()
    {
        if (!isLocked)
            return;

        foreach (var behaviour in disabledByLock)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        disabledByLock.Clear();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isLocked = false;
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class ClockToggle : MonoBehaviour
{
    [SerializeField] private GameObject clockRoot;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (clockRoot != null)
                clockRoot.SetActive(!clockRoot.activeSelf);
        }
    }
}
using UnityEngine;

public class ResumeButtonVisibility : MonoBehaviour
{
    [SerializeField] private GameObject target;

    private void Start()
    {
        if (target == null)
            target = gameObject;

        target.SetActive(PlayerService.HasResumePoint());
    }
}
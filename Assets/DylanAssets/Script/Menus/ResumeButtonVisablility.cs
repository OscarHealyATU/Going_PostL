using UnityEngine;
using UnityEngine.UI;

public class ResumeButtonVisibility : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private float disableAlpha = 0.5f;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        bool hasResume = PlayerService.HasResumePoint();
        button.interactable = hasResume;

        var colors = button.colors; 
        colors.disabledColor = new Color(1f, 1f,1f, disableAlpha);
        button.colors = colors;
        
    }
}
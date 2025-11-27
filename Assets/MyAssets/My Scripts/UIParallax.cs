using UnityEngine;
using UnityEngine.InputSystem;
public class UIParallax : MonoBehaviour
{
    public float paraStrengthX = 20;
    public float paraStrengthY = 20;
    public float smoothSpeed = 5;
    public RectTransform rectTransform;
    private Vector2 startPosition;
    private Quaternion startRotation;

    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
        startRotation = rectTransform.rotation;
        
    }
    void Update()
    {
        // mouse position relative to screen centre
        Vector2 mousePos = Mouse.current.position.ReadValue();

        float normalisedX = (mousePos.x / Screen.width - 0.5f) * 2f;
        float normalisedY = (mousePos.y / Screen.width - 0.5f) * 2f;
        float targetRotationZ = (-normalisedX * normalisedY) * 10f;
        
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f,targetRotationZ,0f);

        Vector2 targetPos = startPosition + new Vector2(
            normalisedX * paraStrengthX, normalisedY * paraStrengthY
        );

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, targetPos,
            Time.deltaTime * smoothSpeed);

        rectTransform.rotation = Quaternion.Lerp(
            rectTransform.rotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );
    }
}

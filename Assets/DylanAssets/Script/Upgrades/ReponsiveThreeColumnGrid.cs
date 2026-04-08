using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveThreeColumnGrid : MonoBehaviour
{
    [SerializeField] private int columns = 3;
    [SerializeField] private float spacingX = 20f;
    [SerializeField] private float spacingY = 20f;
    [SerializeField] private float paddingLeft = 20f;
    [SerializeField] private float paddingRight = 20f;
    [SerializeField] private float paddingTop = 20f;
    [SerializeField] private float paddingBottom = 20f;
    [SerializeField] private float cardHeight = 400f;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;
    private float lastWidth = -1f;

    private void Awake()
    {
        Cache();
        Apply();
    }

    private void OnEnable()
    {
        Cache();
        Apply();
    }

    private void OnValidate()
    {
        Cache();
        Apply();
    }

    private void Update()
    {
        if (rectTransform == null)
            Cache();

        if (rectTransform == null)
            return;

        float width = rectTransform.rect.width;
        if (!Mathf.Approximately(width, lastWidth))
            Apply();
    }

    private void Cache()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (rectTransform == null)
            rectTransform = transform as RectTransform;
    }

    private void Apply()
    {
        if (grid == null || rectTransform == null)
            return;

        columns = Mathf.Max(1, columns);

        grid.padding.left = Mathf.RoundToInt(paddingLeft);
        grid.padding.right = Mathf.RoundToInt(paddingRight);
        grid.padding.top = Mathf.RoundToInt(paddingTop);
        grid.padding.bottom = Mathf.RoundToInt(paddingBottom);

        grid.spacing = new Vector2(spacingX, spacingY);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        float totalWidth = rectTransform.rect.width;
        float usableWidth = totalWidth - paddingLeft - paddingRight - ((columns - 1) * spacingX);
        float cardWidth = usableWidth / columns;

        if (cardWidth < 1f)
            cardWidth = 1f;

        grid.cellSize = new Vector2(cardWidth, cardHeight);
        lastWidth = totalWidth;
    }

    public void SetCardHeight(float newHeight)
    {
        cardHeight = Mathf.Max(1f, newHeight);
        Apply();
    }
}
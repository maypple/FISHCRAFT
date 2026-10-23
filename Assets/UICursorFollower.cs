using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/UICursor Follower")]
public class UICursorFollower : MonoBehaviour
{
    public enum FollowSpace { CanvasSpace } // future-proof if you ever add world-space
    [Header("References")]
    [Tooltip("Canvas the cursor graphic is on.")]
    public Canvas canvas;

    [Tooltip("RectTransform of the cursor graphic (hand). If null, uses this object.")]
    public RectTransform cursorRect;

    [Header("Behavior")]
    [Tooltip("Hide the system cursor while this follower is enabled.")]
    public bool hideSystemCursor = true;

    [Tooltip("Seconds to smooth movement. 0 = instant.")]
    [Min(0f)] public float smoothTime = 0.08f;

    [Tooltip("Use unscaled time (ignore Time.timeScale).")]
    public bool useUnscaledTime = true;

    [Tooltip("Offset from the graphic's pivot to the interaction point (e.g., fingertip), in LOCAL UI units (anchoredPosition space).")]
    public Vector2 hotspotOffset = Vector2.zero;

    [Tooltip("Keep this graphic from blocking UI clicks.")]
    public bool ignoreRaycasts = true;

    [Header("Sorting")]
    [Tooltip("Optional: raise to ensure the hand is above all UI.")]
    public int sortingOrder = 10000;

    Vector2 _vel; // for SmoothDamp
    Camera _cam;
    RectTransform _canvasRect;

    void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
        cursorRect = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (cursorRect == null) cursorRect = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("[UICursorFollower] No Canvas found. Please assign one.", this);

        _canvasRect = canvas ? canvas.transform as RectTransform : null;
        _cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        // Make sure this graphic doesn't eat clicks
        if (ignoreRaycasts)
        {
            var cg = cursorRect.GetComponent<CanvasGroup>();
            if (!cg) cg = cursorRect.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // Also disable raycast target on common graphics
            var g = cursorRect.GetComponent<Graphic>();
            if (g) g.raycastTarget = false;
        }

        // Ensure it's drawn on top if this canvas is ScreenSpaceCamera or WorldSpace
        var c = cursorRect.GetComponent<Canvas>();
        if (!c)
        {
            c = cursorRect.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = sortingOrder;
        }
        else
        {
            c.overrideSorting = true;
            c.sortingOrder = sortingOrder;
        }
    }

    void OnEnable()
    {
        if (hideSystemCursor) Cursor.visible = false;
    }

    void OnDisable()
    {
        if (hideSystemCursor) Cursor.visible = true;
    }

    void Update()
    {
        if (canvas == null || cursorRect == null || _canvasRect == null) return;

        Vector2 screenPos = Input.mousePosition;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _cam, out localPoint);

        // Target = mouse in canvas space + hotspot offset
        Vector2 target = localPoint + hotspotOffset;

        if (smoothTime <= 0f)
        {
            cursorRect.anchoredPosition = target;
        }
        else
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            cursorRect.anchoredPosition = Vector2.SmoothDamp(
                cursorRect.anchoredPosition, target, ref _vel, smoothTime, Mathf.Infinity, dt);
        }
    }
}

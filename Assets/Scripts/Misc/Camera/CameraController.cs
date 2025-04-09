using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float panSpeed = 0.1f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;

    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 5f);

    private Vector3 lastMousePosition;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandlePanning();
        HandleZooming();
    }

    void HandlePanning()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            PanHorizontal(-delta.x * panSpeed);
            PanVertical(-delta.y * panSpeed);
            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Zoom(-scroll * zoomSpeed);
        }
    }

    public void PanHorizontal(float amount)
    {
        transform.position += new Vector3(amount, 0f, 0f);
        ClampPosition();
    }

    public void PanVertical(float amount)
    {
        transform.position += new Vector3(0f, amount, 0f);
        ClampPosition();
    }

    public void Zoom(float amount)
    {
        if (cam == null || !cam.orthographic) return;

        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + amount, minZoom, maxZoom);
    }

    void ClampPosition()
    {   
        //Aca se calcula con que choca con los bordes puestos, ahora esta con la camara en su totalidad
        //Si se quiere que se calcule con el centro ignoramos los extent y usamos min/maxbounds
        if (cam == null || !cam.orthographic) return;

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float minX = minBounds.x + horzExtent;
        float maxX = maxBounds.x - horzExtent;
        float minY = minBounds.y + vertExtent;
        float maxY = maxBounds.y - vertExtent;

        Vector3 clamped = transform.position;
        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
        transform.position = clamped;
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 bottomLeft = new Vector3(minBounds.x, minBounds.y, transform.position.z);
        Vector3 topRight = new Vector3(maxBounds.x, maxBounds.y, transform.position.z);
        Vector3 size = topRight - bottomLeft;

        Gizmos.DrawWireCube(bottomLeft + size / 2f, size);
    }
}

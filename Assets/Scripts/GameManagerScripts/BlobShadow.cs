using UnityEngine;

// Sombra redonda bajo un personaje, se pone con BlobShadow.Attach
public sealed class BlobShadow : MonoBehaviour
{
    [SerializeField] private float diameter = 1.4f;
    [SerializeField, Range(0f, 1f)] private float opacity = 0.55f;
    [SerializeField] private float groundOffset = 0.02f;

    private static Texture2D s_texture;
    private static Material s_material;

    private Transform _shadow;
    private Collider _collider;

    public static BlobShadow Attach(GameObject target, float diameter)
    {
        BlobShadow shadow = target.GetComponent<BlobShadow>();
        if (shadow == null)
            shadow = target.AddComponent<BlobShadow>();

        shadow.diameter = diameter;
        return shadow;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        CreateShadow();
    }

    private void OnEnable()
    {
        if (_shadow != null)
            _shadow.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_shadow != null)
            _shadow.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_shadow != null)
            Destroy(_shadow.gameObject);
    }

    private void LateUpdate()
    {
        if (_shadow == null) return;

        // La sombra va pegada al suelo, justo bajo los pies
        float groundY = _collider != null && _collider.enabled
            ? _collider.bounds.min.y
            : transform.position.y;

        _shadow.position = new Vector3(transform.position.x, groundY + groundOffset, transform.position.z);
        _shadow.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void CreateShadow()
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"{name}_BlobShadow";
        Destroy(quad.GetComponent<Collider>());

        _shadow = quad.transform;
        _shadow.rotation = Quaternion.Euler(90f, 0f, 0f);

        MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = GetMaterial(opacity);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private static Material GetMaterial(float opacity)
    {
        if (s_material != null) return s_material;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        s_material = new Material(shader) { mainTexture = GetTexture(), color = new Color(0f, 0f, 0f, opacity) };
        return s_material;
    }

    // Crea por codigo un circulo que se difumina hacia los bordes
    private static Texture2D GetTexture()
    {
        if (s_texture != null) return s_texture;

        const int size = 64;
        s_texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                s_texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, alpha * 1.6f)));
            }
        }

        s_texture.Apply();
        return s_texture;
    }
}

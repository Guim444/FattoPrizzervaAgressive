using UnityEngine;


[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteMaterialColorOverride : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    [Header("Animated Color")]
    [SerializeField] private Color tintColor = Color.white;
    public Color TintColor
    {
        get => tintColor;
        set
        {
            tintColor = value;
            ApplyColor();
        }
    }
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propBlock;
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
        ApplyColor();
    }
    private void OnValidate()
    {
        ApplyColor();
    }

    //This Unity callback is executed when the animator or the Animation Window
    //updates a frame property
    private void OnDidApplyAnimationProperties()
    {
        ApplyColor();
    }
    public void ApplyColor()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();
        _spriteRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColorId, tintColor);
        _spriteRenderer.SetPropertyBlock(_propBlock);
    }
}

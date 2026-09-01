using HighlightPlus;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SkyActivator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private HighlightEffect _skyHightLightEffect;
    private bool _active = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_active || _skyHightLightEffect == null) return;

        _skyHightLightEffect.enabled = true;
        _active = true;
    }
}

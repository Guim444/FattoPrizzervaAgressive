using UnityEngine;

/// <summary>
/// Permite abrir una URL en el navegador al hacer clic en un botón.
/// Puedes asignar la URL en el Inspector o pasarla directamente en el OnClick().
/// </summary>
public class OpenURLButton : MonoBehaviour
{
    [Tooltip("URL que se abrirá en el navegador.")]
    [SerializeField] private string url;

    /// <summary>
    /// Abre la URL asignada en el campo del Inspector.
    /// </summary>
    public void Open()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
    }

    /// <summary>
    /// Abre la URL especificada como parámetro en el evento OnClick.
    /// </summary>
    public void Open(string targetUrl)
    {
        if (!string.IsNullOrEmpty(targetUrl))
        {
            Application.OpenURL(targetUrl);
        }
    }
}

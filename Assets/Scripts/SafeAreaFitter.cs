using UnityEngine;

/// <summary>
/// Ajusta este RectTransform para respetar el área segura del dispositivo
/// (notch de Android, Dynamic Island de iPhone, etc.)
///
/// SETUP:
///   Agrega este script al RectTransform RAÍZ de cada panel principal.
///   El panel raíz debe tener anchorMin=(0,0), anchorMax=(1,1), stretch completo.
///   El script ajusta automáticamente los anchors al safe area del dispositivo.
/// </summary>
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect          ultimoSafeArea = Rect.zero;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        AplicarAreaSegura();
    }

    void Update()
    {
        // Solo recalcular si cambió (rotación de pantalla, modo multitarea, etc.)
        if (Screen.safeArea != ultimoSafeArea)
            AplicarAreaSegura();
    }

    void AplicarAreaSegura()
    {
        ultimoSafeArea = Screen.safeArea;

        if (rectTransform == null) return;

        Vector2 anchorMin = new Vector2(
            Screen.safeArea.x / Screen.width,
            Screen.safeArea.y / Screen.height);

        Vector2 anchorMax = new Vector2(
            (Screen.safeArea.x + Screen.safeArea.width)  / Screen.width,
            (Screen.safeArea.y + Screen.safeArea.height) / Screen.height);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}

using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Toast de notificación en pantalla.
/// Llama: MensajeFlotante.Mostrar("¡Snap Perfecto!", Color.cyan)
///
/// SETUP en Unity Editor:
///   1. En el Canvas del HUD, crea un TextMeshProUGUI centrado (tercio superior).
///      Tamaño: 36-42pt, negrita, Outline para legibilidad sobre AR.
///   2. Agrega este script al mismo GameObject que el TMP.
///   3. Deja el texto vacío en el Inspector (" " o "").
///   4. No conectes nada más — Instance se asigna solo.
///
/// POR QUÉ NO USAMOS SetActive(false):
///   El script y el TMP comparten el mismo GameObject. SetActive(false) mata las
///   Coroutines del MonoBehaviour, rompiendo la animación. En su lugar usamos
///   tmp.enabled = false para ocultar solo el componente, no el objeto.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class MensajeFlotante : MonoBehaviour
{
    public static MensajeFlotante Instance;

    [Header("Animación")]
    [Tooltip("Desplazamiento vertical en píxeles durante la animación")]
    public float desplazamientoY = 80f;
    [Tooltip("Duración total del mensaje (fade in + hold + fade out)")]
    public float duracion        = 2.2f;
    [Tooltip("Tiempo de fade in")]
    public float tiempoFadeIn    = 0.25f;
    [Tooltip("Tiempo de fade out")]
    public float tiempoFadeOut   = 0.55f;

    private TextMeshProUGUI tmp;
    private RectTransform   rect;
    private Vector2         posInicial;
    private Coroutine       animacion;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        tmp  = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
        posInicial = rect.anchoredPosition;

        // Ocultar solo el componente TMP — NO el GameObject (eso mataría las Coroutines)
        tmp.enabled = false;
        tmp.text    = "";
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Muestra un mensaje flotante. Puede llamarse desde cualquier script.</summary>
    public static void Mostrar(string texto, Color color, float duracionOverride = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[MensajeFlotante] No hay instancia en la escena. " +
                             "Adjunta este script a un TextMeshProUGUI en el Canvas del HUD.");
            return;
        }
        Instance.MostrarInterno(texto, color,
            duracionOverride > 0 ? duracionOverride : Instance.duracion);
    }

    // ── Shortcuts ─────────────────────────────────────────────────────────────
    public static void SnapPerfecto()  => Mostrar("** Snap Perfect!",     new Color(0.30f, 0.80f, 1.00f));
    public static void MetaAlcanzada() => Mostrar("GOAL REACHED!",         new Color(0.15f, 0.90f, 0.25f));
    // El motivo va en el mensaje: antes decía siempre "New Record!", también
    // cuando la gema era por completar un nivel, y no se entendía de dónde salía
    public static void GemasGanadas(int cantidad, string motivo = "")
        => Mostrar(string.IsNullOrEmpty(motivo) ? $"+{cantidad} gems" : $"+{cantidad} gems  ·  {motivo}",
                   new Color(0.45f, 0.95f, 1.00f));
    public static void MonedasGanadas(int cantidad) => Mostrar($"+{cantidad} coins",             new Color(0.95f, 0.80f, 0.10f));
    public static void CuboPlomoActivo()            => Mostrar("Lead Block",                     new Color(0.70f, 0.70f, 0.80f));

    // ── Implementación ────────────────────────────────────────────────────────

    void MostrarInterno(string texto, Color color, float dur)
    {
        if (animacion != null) StopCoroutine(animacion);
        animacion = StartCoroutine(AnimarMensaje(texto, color, dur));
    }

    IEnumerator AnimarMensaje(string texto, Color color, float dur)
    {
        tmp.text    = texto;
        tmp.enabled = true;
        rect.anchoredPosition = posInicial;

        float holdTime = Mathf.Max(0f, dur - tiempoFadeIn - tiempoFadeOut);

        // ── Fade IN ───────────────────────────────────────────────────────────
        float t = 0f;
        while (t < tiempoFadeIn)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / tiempoFadeIn);
            tmp.color = new Color(color.r, color.g, color.b, alpha);
            rect.anchoredPosition = posInicial + Vector2.up * (desplazamientoY * 0.3f * alpha);
            yield return null;
        }

        // ── HOLD ──────────────────────────────────────────────────────────────
        tmp.color = new Color(color.r, color.g, color.b, 1f);
        t = 0f;
        while (t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            rect.anchoredPosition = posInicial + Vector2.up * (
                desplazamientoY * 0.3f + desplazamientoY * 0.7f * Mathf.Clamp01(t / holdTime));
            yield return null;
        }

        // ── Fade OUT ──────────────────────────────────────────────────────────
        t = 0f;
        while (t < tiempoFadeOut)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(1f - t / tiempoFadeOut);
            tmp.color = new Color(color.r, color.g, color.b, alpha);
            rect.anchoredPosition = posInicial + Vector2.up * desplazamientoY;
            yield return null;
        }

        // Ocultar solo el componente — el GameObject sigue activo para las Coroutines
        tmp.enabled = false;
        tmp.text    = "";
        animacion   = null;
    }
}

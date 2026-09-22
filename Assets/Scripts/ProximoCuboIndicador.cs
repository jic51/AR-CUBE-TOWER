using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD que muestra el tipo del PRÓXIMO cubo que aparecerá tras soltar el actual.
/// Esto permite al jugador anticipar y estrategizar (¿guardo el Plomo para más tarde?).
///
/// SETUP en Unity Editor:
///   1. Crea un Panel pequeño en el Canvas del HUD (ej. esquina superior izquierda,
///      fuera del área de los comodines).
///   2. Dentro: un Image (cuadrado de color, 48x48px) + un TextMeshProUGUI con el nombre.
///      Opcionalmente un TextMeshProUGUI de etiqueta fija "NEXT".
///   3. Adjunta este script al Panel.
///   4. Asigna los campos en el Inspector.
///   5. Asigna el GruaController en el Inspector.
/// </summary>
public class ProximoCuboIndicador : MonoBehaviour
{
    [Header("Referencias")]
    public GruaController grua;

    [Header("UI")]
    [Tooltip("Image que cambia de color según el tipo. Puede ser un cuadrado simple.")]
    public Image           imagenColor;

    [Tooltip("TextMeshPro que muestra el nombre del tipo (NORMAL, PLOMO, etc.)")]
    public TextMeshProUGUI textoNombreTipo;

    [Tooltip("Etiqueta fija 'SIGUIENTE' — opcional, para referencia visual.")]
    public TextMeshProUGUI textoEtiqueta;   // puede quedar vacío

    [Header("Animación")]
    [Tooltip("El panel rebota levemente cuando cambia el tipo")]
    public bool animarAlCambiar = true;

    // ── Estado interno ────────────────────────────────────────────────────────
    private TipoCubo ultimoTipo  = (TipoCubo)(-1); // fuerza actualización en el primer frame
    private RectTransform rect;
    private Vector3 escalaBase;
    private Coroutine coroutineBounce;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        escalaBase = rect != null ? rect.localScale : Vector3.one;
        if (textoEtiqueta != null) textoEtiqueta.text = "NEXT";
    }

    void LateUpdate()
    {
        if (grua == null) return;

        TipoCubo proximo = grua.tipoProximo;
        if (proximo == ultimoTipo) return;  // sin cambios, salir
        ultimoTipo = proximo;

        // ── Obtener configuración del tipo ────────────────────────────────────
        ConfigTipoCubo cfg = grua.ObtenerConfig(proximo);
        if (cfg == null) return;

        // ── Actualizar color ──────────────────────────────────────────────────
        if (imagenColor != null)
        {
            Color c = cfg.colorEnVuelo;
            c.a = 1f;
            imagenColor.color = c;
        }

        // ── Actualizar texto ──────────────────────────────────────────────────
        if (textoNombreTipo != null)
            textoNombreTipo.text = cfg.nombreMostrar.ToUpper();

        // ── Bounce al cambiar ─────────────────────────────────────────────────
        if (animarAlCambiar && rect != null)
        {
            if (coroutineBounce != null) StopCoroutine(coroutineBounce);
            coroutineBounce = StartCoroutine(AnimarBounce());
        }
    }

    System.Collections.IEnumerator AnimarBounce()
    {
        // Escalar rápido a 1.2x → volver a 1x en 0.25s
        float dur = 0.25f;
        float t   = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float factor = 1f + 0.2f * Mathf.Sin(Mathf.PI * (t / dur));
            rect.localScale = escalaBase * factor;
            yield return null;
        }
        rect.localScale = escalaBase;
        coroutineBounce = null;
    }
}

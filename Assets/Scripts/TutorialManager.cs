using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gestiona el tutorial del primer nivel (nivel 0).
/// Solo se activa cuando PlayerData.tutorialVisto == false.
///
/// Flujo de pasos:
///   Paso 0 — Setup:      "Apunta al suelo para detectar la superficie"
///   Paso 1 — Plataforma: "2 dedos para escalar · Toca ANCLAR cuando estés listo"
///   Paso 2 — Jugando:    "Mira hacia la plataforma para alinear el cubo"
///   Paso 3 — Cubo listo: "Toca la pantalla para soltarlo"
///   Paso 4 — Aterrizó:   "¡Bien! Sigue apilando para alcanzar la meta"
///
/// UI esperada en Canvas:
///   PanelTutorial
///   ├── FondoTutorial         (Image semitransparente, opcional)
///   ├── TextoTutorial         (TextMeshProUGUI — el mensaje)
///   └── ImagenFlecha          (Image — flecha animada, opcional)
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    // ── UI ────────────────────────────────────────────────────────────────────
    [Header("Panel Tutorial")]
    public GameObject panelTutorial;
    public TextMeshProUGUI textoTutorial;

    [Tooltip("Imagen de flecha (opcional). Pulsa entre 0.85 y 1.15 escala en loop.")]
    public Image imagenFlecha;

    // ── Configuración ─────────────────────────────────────────────────────────
    [Header("Configuración")]
    [Tooltip("Segundos máximos que espera cada paso antes de avanzar solo.")]
    public float tiempoMaxPorPaso = 6f;

    // ── Estado ────────────────────────────────────────────────────────────────
    private int  pasoActual  = -1;   // -1 = tutorial no iniciado
    private bool activo      = false;
    private Coroutine coroutinePaso;
    private Coroutine coroutineFlecha;

    // ── Textos de cada paso ───────────────────────────────────────────────────
    private static readonly string[] textos =
    {
        "Apunta al suelo y mueve\nel teléfono para detectar la superficie",   // 0 — Setup
        "Con 2 dedos puedes escalar la plataforma\nToca ANCLAR cuando estés listo", // 1 — Plataforma lista
        "Mira hacia la plataforma\npara alinear el cubo",                      // 2 — Jugando
        "Toca la pantalla\npara soltar el cubo",                               // 3 — Cubo listo
        "¡Bien! Sigue apilando para\nalcanzar la meta de altura",              // 4 — Primer aterrizaje
    };

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (panelTutorial) panelTutorial.SetActive(false);
    }

    // ── API pública (llamada desde GameManager y otros scripts) ───────────────

    /// <summary>
    /// Verifica si el tutorial debe mostrarse y lo inicia si corresponde.
    /// Llamar desde GameManager al entrar en estado Setup, solo para nivel 0.
    /// </summary>
    public void IntentarIniciar()
    {
        // Solo si es el primer nivel y no se ha visto el tutorial
        bool esNivel0    = LevelManager.NivelSeleccionado == 0;
        bool noVisto     = GameManager.Instance != null &&
                          !GameManager.Instance.GetDatos().tutorialVisto;

        if (!esNivel0 || !noVisto)
        {
            activo = false;
            return;
        }

        activo = true;
        MostrarPaso(0);
    }

    /// <summary>Avanzar al siguiente paso del tutorial.</summary>
    public void AvanzarPaso()
    {
        if (!activo) return;
        MostrarPaso(pasoActual + 1);
    }

    /// <summary>
    /// Avanzar al paso concreto (sin saltarse pasos).
    /// Útil para disparos de eventos específicos (plataforma lista, cubo soltado, etc.)
    /// </summary>
    public void IrAPaso(int paso)
    {
        if (!activo || paso <= pasoActual) return;
        MostrarPaso(paso);
    }

    /// <summary>
    /// Terminar el tutorial y marcarlo como visto.
    /// </summary>
    public void Completar()
    {
        if (!activo) return;
        activo = false;

        // Guardar que ya se vio
        var datos = GameManager.Instance?.GetDatos();
        if (datos != null)
        {
            datos.tutorialVisto = true;
            SaveSystem.Guardar(datos);
        }

        // Ocultar UI
        if (coroutinePaso    != null) StopCoroutine(coroutinePaso);
        if (coroutineFlecha  != null) StopCoroutine(coroutineFlecha);

        StartCoroutine(OcultarConFadeOut());
    }

    // ── Internos ──────────────────────────────────────────────────────────────

    void MostrarPaso(int paso)
    {
        if (paso >= textos.Length)
        {
            Completar();
            return;
        }

        pasoActual = paso;

        if (panelTutorial) panelTutorial.SetActive(true);
        if (textoTutorial) textoTutorial.text = textos[paso];

        // Animar flecha
        if (imagenFlecha != null)
        {
            imagenFlecha.gameObject.SetActive(true);
            if (coroutineFlecha != null) StopCoroutine(coroutineFlecha);
            coroutineFlecha = StartCoroutine(AnimarFlecha());
        }

        // Timer de avance automático
        if (coroutinePaso != null) StopCoroutine(coroutinePaso);
        coroutinePaso = StartCoroutine(AvancarAutoTras(tiempoMaxPorPaso));
    }

    IEnumerator AvancarAutoTras(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (activo) MostrarPaso(pasoActual + 1);
    }

    IEnumerator AnimarFlecha()
    {
        if (imagenFlecha == null) yield break;

        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 2f;
            float s = Mathf.Lerp(0.85f, 1.15f, (Mathf.Sin(t) + 1f) * 0.5f);
            imagenFlecha.transform.localScale = Vector3.one * s;
            yield return null;
        }
    }

    IEnumerator OcultarConFadeOut()
    {
        // Fade out suave del panel tutorial (0.4 segundos)
        if (textoTutorial != null)
        {
            float t = 0f;
            Color colorOriginal = textoTutorial.color;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                textoTutorial.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, alpha);
                yield return null;
            }
            textoTutorial.color = colorOriginal; // restaurar para la próxima vez
        }

        if (panelTutorial) panelTutorial.SetActive(false);
    }
}

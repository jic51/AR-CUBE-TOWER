using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Barra de progreso 3D flotante al lado de la plataforma.
///
/// COMPORTAMIENTO:
///   - OCULTA hasta que el primer cubo aterriza (≥ 0.04 m).
///   - Posición FIJA al costado de la plataforma (nunca salta entre puntos).
///   - Colores: rojo → naranja → amarillo → verde según progreso.
///   - Línea conectora del top de la torre a la marca en la barra (con fade).
///   - Texto dinámico en la marca actual que muestra "0.45 m".
///
/// POR QUÉ EL COLOR ERA NEGRO:
///   El material se creaba con mat.color = colorRojo, luego se cambiaba startColor/endColor
///   a verde. Unity multiplica vertex_color × material_color → verde × rojo ≈ negro.
///   FIX: el material SIEMPRE tiene color blanco; solo se usan startColor/endColor.
/// </summary>
public class BarraProgresoAR : MonoBehaviour
{
    [Tooltip("Material Unlit opcional. Si está vacío se crea uno automáticamente (Sprites/Default).")]
    public Material materialOverride;

    // ── Elementos visuales ─────────────────────────────────────────────────────
    private LineRenderer lineaFondo;       // barra de fondo (gris, altura total = meta)
    private LineRenderer lineaRelleno;     // barra de progreso (coloreada, crece)
    private LineRenderer lineaMeta;        // tick horizontal en la meta (blanco)
    private LineRenderer lineaMarcaActual; // tick horizontal en altura actual (colored)
    private LineRenderer lineaConector;    // línea diagonal cubo→barra (fade negro)
    private TextMeshPro  textoAltura;      // "0.45 m" al lado de la marca actual

    // ── Estado ─────────────────────────────────────────────────────────────────
    private Transform camaraRef;
    private Transform plataformaRef;
    private float     metaAltura;
    private float     alturaActual;
    private float     escala;
    private bool      estaVisible = false;

    // Material compartido (blanco) — se instancia una vez y se reutiliza
    private Material materialBlanco;

    // ── Colores (VERTEX-COLOR ONLY — material siempre blanco) ──────────────────
    private static readonly Color colRojo     = new Color(0.95f, 0.20f, 0.20f, 1f);
    private static readonly Color colNaranja  = new Color(0.95f, 0.55f, 0.05f, 1f);
    private static readonly Color colAmarillo = new Color(0.95f, 0.90f, 0.05f, 1f);
    private static readonly Color colVerde    = new Color(0.15f, 0.90f, 0.25f, 1f);
    private static readonly Color colFondo    = new Color(0.30f, 0.30f, 0.30f, 0.50f);
    private static readonly Color colMeta     = new Color(1.00f, 1.00f, 1.00f, 0.90f);

    // ─────────────────────────────────────────────────────────────────────────
    // Inicialización
    // ─────────────────────────────────────────────────────────────────────────

    public void Inicializar(Transform plataforma, float meta, Transform camara)
    {
        plataformaRef = plataforma;
        camaraRef     = camara;
        metaAltura    = meta;
        escala        = SetupFase.EscalaSeleccionada;

        // Posición inicial: costado derecho desde la perspectiva de la cámara
        // (LateUpdate la irá ajustando dinámicamente cada frame)
        Vector3 derechaInicial = camara.right;
        derechaInicial.y = 0f;
        if (derechaInicial.sqrMagnitude < 0.001f) derechaInicial = Vector3.right;
        derechaInicial.Normalize();
        transform.position = plataforma.position + derechaInicial * (0.55f * escala);

        // Material blanco base (CRÍTICO — evita multiplicación de colores)
        materialBlanco = CrearMaterialBlanco();

        float ancho = 0.006f * escala;

        // Barra fondo (altura completa = meta)
        lineaFondo   = CrearLineaLocal("Fondo",   colFondo,  ancho * 0.5f);
        SetLocalY(lineaFondo, 0f, meta);

        // Barra relleno (empieza en 0, crece)
        lineaRelleno = CrearLineaLocal("Relleno", colRojo,   ancho);
        SetLocalY(lineaRelleno, 0f, 0.001f);

        // Tick en la meta (horizontal, blanco)
        lineaMeta = CrearTickHorizontal("Meta", colMeta, ancho * 0.4f, meta);

        // Tick en la posición actual (se mueve)
        lineaMarcaActual = CrearTickHorizontal("MarcaActual", colRojo, ancho * 0.7f, 0f);

        // Conector cubo→barra (world space, gradiente negro)
        lineaConector = CrearConector("Conector");

        // Texto de altura actual
        textoAltura = CrearTexto("TextoAltura");

        SetVisible(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Actualización (llamada desde GameManager cada frame)
    // ─────────────────────────────────────────────────────────────────────────

    public void ActualizarProgreso(float altura)
    {
        alturaActual = Mathf.Max(0f, altura);

        // Ocultar hasta que aterrice el primer cubo
        if (!estaVisible)
        {
            if (alturaActual < 0.04f) return;
            SetVisible(true);
        }

        float ratio = metaAltura > 0f ? Mathf.Clamp01(alturaActual / metaAltura) : 0f;
        Color colorActual = ColorPorRatio(ratio);

        // ── Relleno ──────────────────────────────────────────────────────────
        SetLocalY(lineaRelleno, 0f, Mathf.Max(alturaActual, 0.002f));
        lineaRelleno.startColor = colorActual;
        lineaRelleno.endColor   = colorActual;

        // ── Marca actual (tick horizontal en la altura de la torre) ───────────
        ActualizarTick(lineaMarcaActual, colorActual, alturaActual);

        // ── Texto en el CENTRO de la barra (mitad de la altura actual) ───────
        if (textoAltura != null)
        {
            float mitad = Mathf.Max(0.04f, alturaActual * 0.5f);
            textoAltura.transform.localPosition = new Vector3(
                0.018f * Mathf.Max(0.5f, escala),
                mitad,
                0f);

            textoAltura.text  = ratio >= 1f
                ? "Goal!\n" + metaAltura.ToString("F1") + " m"
                : alturaActual.ToString("F2") + " m\n/ " + metaAltura.ToString("F1") + " m";
            textoAltura.color = colorActual;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LateUpdate — Billboard + conector
    // ─────────────────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (camaraRef == null || plataformaRef == null) return;

        // Billboard: la barra siempre mira a la cámara (solo eje Y)
        Vector3 dirCam = camaraRef.position - transform.position;
        dirCam.y = 0f;
        if (dirCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dirCam);

        // ── Posición: costado DERECHO desde la perspectiva de la cámara ─────────
        // Usamos camaraRef.right proyectado en horizontal → la barra queda siempre
        // a la derecha de la vista, sin importar hacia dónde apunta el jugador.
        Vector3 derecha = camaraRef.right;
        derecha.y = 0f;
        if (derecha.sqrMagnitude < 0.001f) derecha = Vector3.right;
        derecha.Normalize();

        float esc = SetupFase.EscalaSeleccionada;
        float offset = 0.55f * esc;

        // Posición fija al costado derecho de la plataforma — no se mueve por los cubos
        Vector3 targetPos = plataformaRef.position + derecha * offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        // ── Conector diagonal cubo→barra ─────────────────────────────────────
        if (lineaConector != null && estaVisible && alturaActual > 0.04f)
        {
            Vector3 topCubo = ObtenerTopTorre();
            Vector3 puntoEnBarra = transform.position + Vector3.up * alturaActual;

            lineaConector.SetPosition(0, puntoEnBarra);  // barra (más opaco)
            lineaConector.SetPosition(1, topCubo);        // cubo  (más transparente)

            // Fade: opaco en la barra, transparente cerca del cubo
            lineaConector.startColor = new Color(0.8f, 0.8f, 0.8f, 0.55f);
            lineaConector.endColor   = new Color(0.1f, 0.1f, 0.1f, 0.05f);

            lineaConector.enabled = true;
        }
        else if (lineaConector != null)
        {
            lineaConector.enabled = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers — construcción visual
    // ─────────────────────────────────────────────────────────────────────────

    Color ColorPorRatio(float r)
    {
        if (r < 0.33f) return Color.Lerp(colRojo,    colNaranja,  r / 0.33f);
        if (r < 0.66f) return Color.Lerp(colNaranja, colAmarillo, (r - 0.33f) / 0.33f);
        return             Color.Lerp(colAmarillo, colVerde,    (r - 0.66f) / 0.34f);
    }

    Vector3 ObtenerTopTorre()
    {
        float maxY = float.MinValue;
        Vector3 mejor = transform.position + Vector3.up * alturaActual;

        foreach (var cubo in CuboInteligente.cubosActivos)
        {
            if (cubo == null || !cubo.HaAterrizado) continue;
            float topY = cubo.transform.position.y + cubo.transform.localScale.y * 0.5f;
            if (topY > maxY)
            {
                maxY = topY;
                mejor = cubo.transform.position;
                mejor.y = topY;
            }
        }
        return mejor;
    }

    // ── Activar/desactivar todo ───────────────────────────────────────────────

    void SetVisible(bool visible)
    {
        estaVisible = visible;
        foreach (var lr in GetComponentsInChildren<LineRenderer>())
            lr.enabled = visible;
        if (textoAltura != null)
            textoAltura.gameObject.SetActive(visible);
        // El conector empieza siempre desactivado (se activa en LateUpdate cuando hay cubo)
        if (lineaConector != null) lineaConector.enabled = false;
    }

    // ── LineRenderer local (posiciones en espacio local del padre) ─────────────

    LineRenderer CrearLineaLocal(string nombre, Color color, float ancho)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial          = materialBlanco;
        lr.startColor              = color;
        lr.endColor                = color;
        lr.startWidth              = ancho;
        lr.endWidth                = ancho;
        lr.positionCount           = 2;
        lr.useWorldSpace           = false;
        lr.shadowCastingMode       = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows          = false;
        lr.generateLightingData    = false;
        return lr;
    }

    LineRenderer CrearTickHorizontal(string nombre, Color color, float ancho, float posY)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        float margen = 0.020f * Mathf.Max(0.5f, escala);

        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial       = materialBlanco;
        lr.startColor           = color;
        lr.endColor             = color;
        lr.startWidth           = ancho;
        lr.endWidth             = ancho;
        lr.positionCount        = 2;
        lr.useWorldSpace        = false;
        lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.SetPosition(0, new Vector3(-margen, posY, 0f));
        lr.SetPosition(1, new Vector3( margen, posY, 0f));
        return lr;
    }

    // Actualiza solo la posición Y de un tick horizontal manteniendo X/Z
    void ActualizarTick(LineRenderer lr, Color color, float posY)
    {
        if (lr == null) return;
        float margen = 0.020f * Mathf.Max(0.5f, escala);
        lr.SetPosition(0, new Vector3(-margen, posY, 0f));
        lr.SetPosition(1, new Vector3( margen, posY, 0f));
        lr.startColor = color;
        lr.endColor   = color;
    }

    // ── Conector (world space) ────────────────────────────────────────────────

    LineRenderer CrearConector(string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var lr = go.AddComponent<LineRenderer>();
        lr.material             = CrearMaterialTransparente(); // necesita alpha
        lr.startWidth           = 0.003f * Mathf.Max(0.5f, escala);
        lr.endWidth             = 0.001f * Mathf.Max(0.5f, escala);
        lr.positionCount        = 2;
        lr.useWorldSpace        = true;   // ← world space para conectar barra y cubo
        lr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows       = false;
        lr.generateLightingData = false;
        lr.enabled              = false;
        return lr;
    }

    // ── Texto ─────────────────────────────────────────────────────────────────

    TextMeshPro CrearTexto(string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0.018f * escala, 0.05f, 0f);
        go.transform.localScale    = Vector3.one * (0.0018f * Mathf.Max(0.5f, escala));

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font      = TMP_Settings.defaultFontAsset;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.fontSize  = 12f;
        tmp.color     = Color.white;
        tmp.text      = "";
        return tmp;
    }

    // ── Posición de un LineRenderer local (solo eje Y) ────────────────────────

    void SetLocalY(LineRenderer lr, float y0, float y1)
    {
        lr.SetPosition(0, new Vector3(0f, y0, 0f));
        lr.SetPosition(1, new Vector3(0f, y1, 0f));
    }

    // ── Materiales ────────────────────────────────────────────────────────────
    //
    // IMPORTANTE: el material de LineRenderer DEBE ser blanco.
    // Unity multiplica vertex_color × material_color.
    // Si material_color = rojo y vertex_color = verde → resultado ≈ negro.
    // Al dejar el material blanco, vertex_color es el color final real.

    Material CrearMaterialBlanco()
    {
        if (materialOverride != null)
        {
            var m = new Material(materialOverride);
            m.color = Color.white;
            return m;
        }

        // Sprites/Default: soporta vertex colors y NO requiere URP
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");

        var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        mat.color = Color.white;
        return mat;
    }

    Material CrearMaterialTransparente()
    {
        // Para el conector necesitamos transparencia real (alpha < 1)
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Transparent");

        if (shader != null)
        {
            var mat = new Material(shader);
            mat.color = Color.white;
            return mat;
        }

        // Fallback URP con modo transparente
        var matUrp = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
        matUrp.SetFloat("_Surface", 1);   // Transparent
        matUrp.color = Color.white;
        return matUrp;
    }
}

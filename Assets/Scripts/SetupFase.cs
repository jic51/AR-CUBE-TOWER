using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections;
using System.Collections.Generic;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SetupFase : MonoBehaviour
{
    [Header("Referencias")]
    public ARRaycastManager raycastManager;
    public GameObject plataformaPrefab;
    public GameObject cuboFantasmaPrefab;
    public GameObject botonAnclar;
    public TMPro.TextMeshProUGUI textoGuiaAR;

    [Header("Configuración Escala")]
    public float escalaMin           = 0.5f;
    public float escalaMax           = 4.0f;
    // (sensibilidadZoom se eliminó: el pinch ahora es proporcional y no necesita ajuste)
    public float sensibilidadRotacion = 2.0f;

    [Header("Tiempos del flujo AR (segundos)")]
    public float tiempoTextoInicial = 2.5f;  // texto de instrucción antes de escanear
    public float tiempoMostrarPlano = 2.5f;  // ver el plano antes de mostrar plataforma

    [Header("Efectos (opcional)")]
    [Tooltip("Prefab de partículas de polvo — mismo que usas en CuboInteligente. Puede quedar vacío.")]
    public GameObject prefabDust;

    [Header("Indicador de Escala (opcional)")]
    [Tooltip("TextMeshPro que muestra 'Escala: 1.0x' al hacer pinch. Puede quedar vacío.")]
    public TMPro.TextMeshProUGUI textoEscalaIndicador;

    public static float EscalaSeleccionada = 1.0f;

    // ── Objetos de escena ────────────────────────────────────────────────────
    private GameObject plataformaActual;
    private GameObject cuboFantasmaActual;   // YA NO es hijo de la plataforma
    private bool estaAnclado = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // ── Control del flujo escalonado ─────────────────────────────────────────
    private bool escaneoActivo        = false;
    private bool plataformaPermitida  = false;
    private bool primerPlanoDetectado = false;

    private Coroutine coroutineEscalaIndicador;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        EscalaSeleccionada = 1.0f;
    }

    void OnEnable()
    {
        // EnhancedTouch es más fiable que Input.GetTouch con ARFoundation + New Input System
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        if (botonAnclar) botonAnclar.SetActive(false);

        // El texto empieza siempre oculto.
        // La corrutina la lanza GameManager cuando cambia al estado Setup
        // (no aquí, para evitar que aparezca texto antes de que el jugador haya
        // seleccionado nivel o mientras el menú principal está visible).
        if (textoGuiaAR) textoGuiaAR.gameObject.SetActive(false);
    }

    /// <summary>
    /// Llamado por GameManager.CambiarEstado(Setup).
    /// Reinicia el estado del Setup y arranca la secuencia AR guiada.
    /// </summary>
    public void IniciarSetup()
    {
        // Reiniciar estado por si se reutiliza (retry / siguiente nivel)
        escaneoActivo        = false;
        plataformaPermitida  = false;
        primerPlanoDetectado = false;
        estaAnclado          = false;

        if (botonAnclar) botonAnclar.SetActive(false);
        if (textoGuiaAR) textoGuiaAR.gameObject.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(SecuenciaInicioAR());
    }

    // ── Flujo escalonado ──────────────────────────────────────────────────────

    IEnumerator SecuenciaInicioAR()
    {
        MostrarTextoGuia("Move your phone slowly\nover a flat surface");
        yield return new WaitForSeconds(tiempoTextoInicial);

        escaneoActivo = true;
        MostrarTextoGuia("Scanning for a flat surface...");

        yield return new WaitUntil(() => primerPlanoDetectado);
        MostrarTextoGuia("Surface found! Hold steady...");

        yield return new WaitForSeconds(tiempoMostrarPlano);
        plataformaPermitida = true;
    }

    IEnumerator GuiaTresPasos()
    {
        MostrarTextoGuia("Move your phone to position the platform");
        yield return new WaitForSeconds(4f);
        if (estaAnclado) yield break;
        MostrarTextoGuia("Pinch with 2 fingers to resize  ·  Twist to rotate");
        yield return new WaitForSeconds(4f);
        if (estaAnclado) yield break;
        MostrarTextoGuia("Tap ANCHOR when you're ready!");
        // Texto persiste hasta que el jugador ancle — se oculta en AnclarPlataforma()
    }

    void MostrarTextoGuia(string texto)
    {
        if (textoGuiaAR == null) return;
        textoGuiaAR.gameObject.SetActive(true);
        textoGuiaAR.text = texto;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (estaAnclado) return;

        var touches = ETouch.activeTouches;

        // ── Pinch con 2 dedos: SIEMPRE activo en Setup (EnhancedTouch) ───────
        if (touches.Count >= 2)
        {
            EscalarYRotar(touches[0], touches[1]);
            ActualizarCuboFantasma();
            return;
        }
        _pinchActivo = false;   // al levantar un dedo, el próximo pinch empieza de cero

        // El resto del input requiere que el escaneo esté activo
        if (!escaneoActivo) return;

        if (touches.Count == 0)
        {
            SeguirMirada();
        }
        else if (touches.Count == 1)
        {
            // Ignorar el toque si cayó sobre un elemento UI (botón ANCHOR, etc.)
            // Evita que al presionar el botón la plataforma salte a los pies del jugador
            var pe = new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current)
                { position = touches[0].screenPosition };
            var hits2D = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current?.RaycastAll(pe, hits2D);
            if (hits2D.Count == 0)
                ArrastrarPlataforma(touches[0].screenPosition);
        }

        ActualizarCuboFantasma();
    }

    // ── Movimiento de la plataforma ───────────────────────────────────────────

    void SeguirMirada()
    {
        Vector2 centroPantalla = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycastManager.Raycast(centroPantalla, hits, TrackableType.PlaneWithinPolygon))
        {
            primerPlanoDetectado = true;
            if (plataformaPermitida)
                ColocarOActualizar(hits[0].pose);
        }
    }

    void ArrastrarPlataforma(Vector2 posicion)
    {
        if (plataformaActual == null) return;
        if (raycastManager.Raycast(posicion, hits, TrackableType.PlaneWithinPolygon))
            ColocarOActualizar(hits[0].pose);
    }

    void ColocarOActualizar(Pose pose)
    {
        // La plataforma es delgada (Y=0.05), el offset la sube al ras del suelo AR
        float offsetY        = 0.025f; // mitad del Y scale (0.05 / 2)
        Vector3 posCorregida = pose.position + Vector3.up * offsetY;

        if (plataformaActual == null)
        {
            // ── Crear plataforma con animación de materialización ─────────────
            plataformaActual = Instantiate(plataformaPrefab, posCorregida, pose.rotation);
            plataformaActual.tag = "Plataforma";
            // Empieza casi invisible en Y → crece con bounce
            plataformaActual.transform.localScale = new Vector3(EscalaSeleccionada, 0.001f, EscalaSeleccionada);
            StartCoroutine(AnimarEntradaPlataforma());

            // ── Crear cubo fantasma INDEPENDIENTE (no hijo de la plataforma) ─
            cuboFantasmaActual = Instantiate(cuboFantasmaPrefab);
            // La escala del cubo fantasma es independiente — siempre es un cubo, nunca se aplana
            float tamCubo = 0.15f * EscalaSeleccionada;
            cuboFantasmaActual.transform.localScale = Vector3.one * tamCubo;

            // Desactivar física/colisión del cubo fantasma
            var col = cuboFantasmaActual.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var rb = cuboFantasmaActual.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            botonAnclar.SetActive(true);
            if (LevelManager.NivelSeleccionado == 0)
                StartCoroutine(GuiaTresPasos());

            // Tutorial paso 1: plataforma creada por primera vez
            TutorialManager.Instance?.IrAPaso(1);
        }
        else
        {
            // ── Mover suavemente ──────────────────────────────────────────────
            plataformaActual.transform.position = Vector3.Lerp(
                plataformaActual.transform.position, posCorregida, Time.deltaTime * 20f);

            // Rotar en dirección contraria al cubo fantasma (30°/s opuesto)
            if (ETouch.activeTouches.Count < 2)
                plataformaActual.transform.Rotate(Vector3.up, -30f * Time.deltaTime, Space.World);
        }
    }

    // ── Cubo fantasma flota sobre la plataforma ───────────────────────────────

    void ActualizarCuboFantasma()
    {
        if (cuboFantasmaActual == null || plataformaActual == null) return;

        // Plataforma Y=0.05, techo = posY + 0.025 (mitad de 0.05)
        float techoPlatforma = plataformaActual.transform.position.y + 0.025f;
        float mitadCubo      = cuboFantasmaActual.transform.localScale.y * 0.5f;
        float flotacion      = 0.02f; // pequeño gap visual

        Vector3 pos = plataformaActual.transform.position;
        pos.y = techoPlatforma + mitadCubo + flotacion;
        cuboFantasmaActual.transform.position = pos;

        // El cubo fantasma gira lentamente (efecto visual bonito)
        cuboFantasmaActual.transform.Rotate(Vector3.up, 30f * Time.deltaTime, Space.World);
    }

    // ── Escalar y Rotar (EnhancedTouch) ──────────────────────────────────────

    // Estado del pinch en curso. La escala se calcula PROPORCIONAL a la distancia
    // entre los dedos respecto al inicio del gesto (como al hacer zoom a una foto).
    // Antes se sumaban deltas por fotograma, con un umbral de 6 px por fotograma y
    // una sensibilidad de 0.000025: un pinch normal se mueve menos de 6 px por
    // fotograma y se ignoraba entero, y uno rápido cambiaba la escala en ~0.06.
    // Por eso el resize nunca funcionó.
    private bool  _pinchActivo;
    private float _pinchDistInicial;
    private float _pinchEscalaInicial;

    // Distancia mínima entre dedos para empezar a escalar (evita divisiones
    // inestables si los dos dedos caen casi en el mismo punto)
    private const float PinchDistMinima = 40f;

    void EscalarYRotar(ETouch t1, ETouch t2)
    {
        Vector2 pos1Actual = t1.screenPosition;
        Vector2 pos2Actual = t2.screenPosition;
        Vector2 pos1Prev   = pos1Actual - t1.delta;
        Vector2 pos2Prev   = pos2Actual - t2.delta;

        // 1. ESCALAR (pinch proporcional)
        float distActual = Vector2.Distance(pos1Actual, pos2Actual);

        if (!_pinchActivo)
        {
            if (distActual < PinchDistMinima) return;
            _pinchActivo        = true;
            _pinchDistInicial   = distActual;
            _pinchEscalaInicial = EscalaSeleccionada;
            return;   // el primer fotograma solo fija la referencia
        }

        float nuevaEscala = Mathf.Clamp(
            _pinchEscalaInicial * (distActual / _pinchDistInicial), escalaMin, escalaMax);

        if (!Mathf.Approximately(nuevaEscala, EscalaSeleccionada))
        {
            EscalaSeleccionada = nuevaEscala;

            if (plataformaActual != null)
                plataformaActual.transform.localScale =
                    new Vector3(EscalaSeleccionada, 0.05f, EscalaSeleccionada);

            if (cuboFantasmaActual != null)
                cuboFantasmaActual.transform.localScale =
                    Vector3.one * (0.15f * EscalaSeleccionada);

            MostrarIndicadorEscala();
        }

        // 2. ROTAR (twist) — solo si la plataforma existe
        if (plataformaActual != null)
        {
            Vector2 vPrev   = pos2Prev   - pos1Prev;
            Vector2 vActual = pos2Actual - pos1Actual;
            float angulo    = Vector2.SignedAngle(vPrev, vActual);

            if (Mathf.Abs(angulo) > 0.1f)
                plataformaActual.transform.Rotate(Vector3.up, -angulo * sensibilidadRotacion);
        }
    }

    // ── Indicador de escala ───────────────────────────────────────────────────

    void MostrarIndicadorEscala()
    {
        if (textoEscalaIndicador == null) return;

        textoEscalaIndicador.gameObject.SetActive(true);
        textoEscalaIndicador.text = "Scale: " + EscalaSeleccionada.ToString("F1") + "x";

        // Reiniciar el timer de ocultado
        if (coroutineEscalaIndicador != null) StopCoroutine(coroutineEscalaIndicador);
        coroutineEscalaIndicador = StartCoroutine(OcultarIndicadorEscala());
    }

    IEnumerator OcultarIndicadorEscala()
    {
        yield return new WaitForSeconds(1.5f);
        if (textoEscalaIndicador != null) textoEscalaIndicador.gameObject.SetActive(false);
        coroutineEscalaIndicador = null;
    }

    // ── Anclar ────────────────────────────────────────────────────────────────

    IEnumerator AnimarEntradaPlataforma()
    {
        if (plataformaActual == null) yield break;

        float dur = 0.35f, t = 0f;
        float escalaFinalY = 0.05f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            // Overshoot al 115% en los primeros 70% del tiempo, luego settle al 100%
            float yFactor = p < 0.7f
                ? Mathf.Lerp(0f, 1.15f, p / 0.7f)
                : Mathf.Lerp(1.15f, 1.0f, (p - 0.7f) / 0.3f);

            if (plataformaActual != null)
                plataformaActual.transform.localScale =
                    new Vector3(EscalaSeleccionada, escalaFinalY * yFactor, EscalaSeleccionada);
            yield return null;
        }

        if (plataformaActual != null)
            plataformaActual.transform.localScale =
                new Vector3(EscalaSeleccionada, escalaFinalY, EscalaSeleccionada);

        // Polvo al materializarse
        if (prefabDust != null && plataformaActual != null)
        {
            var dust = Instantiate(prefabDust, plataformaActual.transform.position,
                                   Quaternion.Euler(180f, 0f, 0f));
            Destroy(dust, 1.5f);
        }
    }

    public void AnclarPlataforma()
    {
        if (plataformaActual == null) return;

        // ── Snap: plataforma queda con UN LADO de frente al usuario (nunca una arista) ──
        if (Camera.main != null)
        {
            Vector3 dir = Camera.main.transform.forward;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                plataformaActual.transform.rotation =
                    Quaternion.LookRotation(-dir.normalized);
        }

        estaAnclado = true;
        botonAnclar.SetActive(false);

        // Parar todas las coroutines del setup (incluye GuiaTresPasos)
        StopAllCoroutines();

        // Ocultar el cubo fantasma
        if (cuboFantasmaActual) Destroy(cuboFantasmaActual);

        // Ocultar texto guía
        if (textoGuiaAR) textoGuiaAR.gameObject.SetActive(false);

        // Limpiar los planos AR detectados
        var planeManager = GetComponent<ARPlaneManager>();
        if (planeManager)
        {
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
            planeManager.enabled = false;
        }

        // Animación "slam": la plataforma cae, rebota, y luego arranca la partida
        StartCoroutine(AnimarAnclaje());
    }

    IEnumerator AnimarAnclaje()
    {
        if (plataformaActual == null) yield break;

        Vector3 posInicial = plataformaActual.transform.position;
        // La plataforma cae 2 cm, rebota 1 cm y settle
        Vector3 posAbajo  = posInicial + Vector3.down * 0.02f;
        Vector3 posRebote = posInicial + Vector3.up   * 0.005f;

        // Caída rápida (0.12 s)
        float t = 0f, dur = 0.12f;
        while (t < dur)
        {
            t += Time.deltaTime;
            plataformaActual.transform.position =
                Vector3.Lerp(posInicial, posAbajo, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }

        // Rebote suave (0.10 s)
        t = 0f; dur = 0.10f;
        while (t < dur)
        {
            t += Time.deltaTime;
            plataformaActual.transform.position =
                Vector3.Lerp(posAbajo, posRebote, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }

        // Settle (0.08 s)
        t = 0f; dur = 0.08f;
        while (t < dur)
        {
            t += Time.deltaTime;
            plataformaActual.transform.position =
                Vector3.Lerp(posRebote, posInicial, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }

        plataformaActual.transform.position = posInicial;

        // Ancla AR: evita que la torre "salte" cuando ARCore corrige el tracking.
        // Los managers de trackables viven en el XR Origin, igual que el raycast.
        if (raycastManager != null)
            AnclaTorre.Crear(plataformaActual.transform, raycastManager.gameObject);

        if (GameManager.Instance != null)
            GameManager.Instance.IniciarPartida(plataformaActual.transform);
        else
            Debug.LogError("No se encontró GameManager en la escena.");
    }
}

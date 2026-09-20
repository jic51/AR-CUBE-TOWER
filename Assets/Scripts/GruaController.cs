using UnityEngine;
using UnityEngine.InputSystem;

public class GruaController : MonoBehaviour
{
    public static GruaController Instance;

    [Header("Configuración")]
    public float alturaBaseDeCaida   = 0.5f;
    public float inerciaCubo         = 3.0f;
    [HideInInspector]
    public float intervaloEntregaCubo = 1.0f; // lo setea GameManager.IniciarPartida()
    
    [Header("Referencias")]
    public GameObject cuboPrefab;
    public GameObject reticuloPrefab;
    public Transform camaraAR; 

    private GameObject cuboActual;
    private GameObject reticuloActual;
    private bool juegoIniciado = false;
    private Vector3 objetivoCubo;
    private float alturaSpawnActual;
    public LayerMask capasParaReticulo;

    private Renderer rendererCuboActual;
    private Color colorCuboVolando = new Color(0.75f, 0.70f, 0.63f, 1f);
    private const float distanciaParaListo = 0.05f;

    public bool  ReticuloVisible   => reticuloActual != null && reticuloActual.activeSelf;
    /// <summary>Altura Y del punto donde impacta el retículo (para que GameManager detecte si está en la cima).</summary>
    public float ReticuloAlturaHit { get; private set; } = -999f;

    // Referencia al Renderer del retículo (prefab plano — color cambia al llegar a zona de drop)
    private Renderer reticuloRenderer;

    // ── Materiales por tipo (arrastrar desde Assets/Materials en el Inspector) ──
    [Header("Materiales Visuales de Cubos (arrastra desde Assets/Materials)")]
    [Tooltip("Si se asigna, se usa este material en lugar del procedural")]
    public Material matNormal;
    public Material matPlomo;
    public Material matPlumas;
    public Material matHielo;
    public Material matFuego;
    public Material matGelatina;
    public Material matLava;
    public Material matHierba;
    public Material matAgua;
    public Material matNube;
    public Material matPiedra;

    // ── Sistema de tipos de cubo ──────────────────────────────────────────────
    [Header("Tipos de Cubo (vacío = defaults automáticos)")]
    public ConfigTipoCubo[] configuracionesTipo;

    /// <summary>Tipo del cubo actualmente en vuelo (le dice al CuboInteligente qué física aplicar).</summary>
    [HideInInspector] public TipoCubo tipoActual  = TipoCubo.Normal;
    /// <summary>Tipo del PRÓXIMO cubo — lo lee ProximoCuboIndicador para mostrar la preview.</summary>
    [HideInInspector] public TipoCubo tipoProximo = TipoCubo.Normal;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InicializarTipos();

        reticuloActual = Instantiate(reticuloPrefab);
        reticuloActual.SetActive(false);

        // Obtener el Renderer del prefab (usa la malla original — cámbiala a Quad en el Editor)
        reticuloRenderer = reticuloActual.GetComponentInChildren<Renderer>();
        if (reticuloRenderer != null)
            reticuloRenderer.material = new Material(reticuloRenderer.sharedMaterial);

        // El cubo en tránsito (CuboVolador) no debe colisionar con la torre (Plataforma)
        // mientras se mueve hacia su objetivo. Al soltarlo cambia de layer y recupera colisiones.
        int layerVol  = LayerMask.NameToLayer("CuboVolador");
        int layerPlat = LayerMask.NameToLayer("Plataforma");
        if (layerVol >= 0 && layerPlat >= 0)
            Physics.IgnoreLayerCollision(layerVol, layerPlat, true);
    }

    void Update()
    {
        if (!juegoIniciado) return;
        MoverSistemasSeparados();
        DetectarInput();
    }

    public void ActivarGrúa()
    {
        juegoIniciado = true;
        // Pre-generar el primer "próximo tipo" para que el indicador UI ya lo muestre
        tipoActual  = TipoCubo.Normal;
        tipoProximo = ElegirTipoAleatorio();
        // Quad se extiende en X e Y → escala X/Y controlan el diámetro, Z casi cero
        reticuloActual.transform.localScale = new Vector3(
            SetupFase.EscalaSeleccionada * 0.15f,
            SetupFase.EscalaSeleccionada * 0.15f,
            0.01f);
        GenerarNuevoCubo();
    }

    public void DesactivarGrúa()
    {
        juegoIniciado = false;
        CancelInvoke(nameof(GenerarNuevoCubo));

        if (cuboActual != null)
        {
            Destroy(cuboActual);
            cuboActual = null;
        }
        if (reticuloActual != null) reticuloActual.SetActive(false);
    }

    void CalcularAlturaSpawn()
    {
        if (reticuloActual.activeSelf)
        {
            RaycastHit hit;
            Vector3 origen = reticuloActual.transform.position + Vector3.up * 10.0f;
            
            if (Physics.Raycast(origen, Vector3.down, out hit, 20.0f))
            {
                alturaSpawnActual = hit.point.y - reticuloActual.transform.position.y + alturaBaseDeCaida;
                if (alturaSpawnActual < alturaBaseDeCaida) alturaSpawnActual = alturaBaseDeCaida;
            }
        }
        else
        {
            alturaSpawnActual = alturaBaseDeCaida;
        }
    }

    // Método unificado para calcular la altura y nacer fuera de cámara
    void GenerarNuevoCubo()
    {
        CalcularAlturaSpawn(); // calcula 'alturaSpawnActual' según la torre actual

        // ── Elegir tipo: el cubo actual toma el "próximo" pre-generado ────────
        tipoActual  = tipoProximo;
        tipoProximo = ElegirTipoAleatorio();   // ya visible en el indicador UI
        ConfigTipoCubo cfg = ObtenerConfig(tipoActual);
        colorCuboVolando = cfg?.colorEnVuelo ?? new Color(0.6f, 0.6f, 0.6f, 0.85f);

        // Toast para tipos especiales
        if (tipoActual != TipoCubo.Normal) MostrarToastTipo(tipoActual);

        // 1. Punto de inicio (fuera de cámara, derecha + arriba)
        Camera cam = camaraAR.GetComponent<Camera>();
        Vector3 puntoDerecha = cam.ViewportToWorldPoint(new Vector3(1.2f, 0.8f, 1.5f));

        // 2. Instanciar
        cuboActual = Instantiate(cuboPrefab, puntoDerecha, Quaternion.identity);
        cuboActual.layer = LayerMask.NameToLayer("CuboVolador");
        cuboActual.transform.localScale = Vector3.one * (0.15f * SetupFase.EscalaSeleccionada);

        // 3. Pasar tipo al CuboInteligente para que Aterrizar() aplique la física correcta
        var ci = cuboActual.GetComponent<CuboInteligente>();
        if (ci != null) ci.tipo = tipoActual;

        Rigidbody rb = cuboActual.GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;

        rendererCuboActual = cuboActual.GetComponent<Renderer>();
        if (rendererCuboActual != null)
            rendererCuboActual.material = CrearMaterialParaTipo(tipoActual, colorCuboVolando);
    }

    Material CrearMaterialParaTipo(TipoCubo tipo, Color colorBase)
    {
        // ── Usar material del Inspector si está asignado (recomendado) ──────────
        Material asignado = ObtenerMatAsignado(tipo);
        if (asignado != null)
            return new Material(asignado); // instancia para no modificar el original

        // ── Fallback: crear material proceduralmente ─────────────────────────────
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        switch (tipo)
        {
            case TipoCubo.Plomo:
                mat.color = colorBase;
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.85f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.75f);
                break;
            case TipoCubo.Hielo:
                Color hielo = colorBase; hielo.a = 0.72f;
                mat.color = hielo;
                if (mat.HasProperty("_Surface"))    mat.SetFloat("_Surface",    1f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
                mat.renderQueue = 3000;
                break;
            case TipoCubo.Fuego:
                mat.color = colorBase;
                if (mat.HasProperty("_Smoothness"))    mat.SetFloat("_Smoothness",    0.2f);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", colorBase * 0.5f);
                mat.EnableKeyword("_EMISSION");
                break;
            case TipoCubo.Plumas:
                mat.color = colorBase;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
                break;
            default:
                mat.color = colorBase;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.05f);
                break;
        }
        return mat;
    }

    Material ObtenerMatAsignado(TipoCubo tipo)
    {
        switch (tipo)
        {
            case TipoCubo.Normal:    return matNormal;
            case TipoCubo.Plomo:     return matPlomo;
            case TipoCubo.Plumas:    return matPlumas;
            case TipoCubo.Hielo:     return matHielo;
            case TipoCubo.Fuego:     return matFuego;
            case TipoCubo.Gelatina:  return matGelatina;
            case TipoCubo.Lava:      return matLava;
            case TipoCubo.Hierba:    return matHierba;
            case TipoCubo.Agua:      return matAgua;
            case TipoCubo.Nube:      return matNube;
            case TipoCubo.Piedra:    return matPiedra;
            default:                 return null;
        }
    }

    void MoverSistemasSeparados()
    {
        Ray rayoCamara = new Ray(camaraAR.position, camaraAR.forward);
        RaycastHit golpe;
        
        // Raycast que SOLO detecta la capa "Plataforma" (muy importante para la precisión)
        if (Physics.Raycast(rayoCamara, out golpe, Mathf.Infinity, capasParaReticulo))
        {
            ReticuloAlturaHit = golpe.point.y;
            reticuloActual.SetActive(true);

            // Posición: pegada a la superficie
            reticuloActual.transform.position = golpe.point + golpe.normal * 0.003f;

            // Rotación: la cara del Quad sigue la normal de la superficie (se inclina en cubos laterales)
            // PERO un lado (no esquina) siempre mira al usuario — usando la derecha de la cámara
            // proyectada sobre el plano de la superficie como eje "up" del Quad.
            Vector3 normal   = golpe.normal.normalized;
            Vector3 camRight = camaraAR.right;
            Vector3 edgeDir  = camRight - Vector3.Dot(camRight, normal) * normal;
            if (edgeDir.sqrMagnitude < 0.0001f)
                edgeDir = Vector3.Cross(normal, camaraAR.forward);
            reticuloActual.transform.rotation = Quaternion.LookRotation(normal, edgeDir.normalized);

            objetivoCubo = golpe.point + (Vector3.up * alturaSpawnActual);
        }
        else
        {
            ReticuloAlturaHit = -999f;
            reticuloActual.SetActive(false);
        }

        if (cuboActual != null)
        {
            cuboActual.transform.position = Vector3.Lerp(cuboActual.transform.position, objetivoCubo, Time.deltaTime * inerciaCubo);

            Vector3 dir = (cuboActual.transform.position - camaraAR.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                cuboActual.transform.rotation = Quaternion.Lerp(cuboActual.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

            bool listo = rendererCuboActual != null &&
                         Vector3.Distance(cuboActual.transform.position, objetivoCubo) <= distanciaParaListo;

            // Retículo: ROJO cuando lejos, VERDE cuando el cubo está listo para soltarse
            if (reticuloRenderer != null && reticuloActual.activeSelf)
            {
                reticuloRenderer.material.color = listo
                    ? new Color(0.2f, 0.95f, 0.35f, 0.90f)   // verde = listo
                    : new Color(0.85f, 0.25f, 0.25f, 0.90f);  // rojo = en vuelo
            }

            if (listo) TutorialManager.Instance?.IrAPaso(3);
        }
    }

    void DetectarInput()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Pointer.current.deviceId)) 
                return;

            SoltarCubo();
        }
    }

    void SoltarCubo()
    {
        if (cuboActual == null) return;

        float distanciaAlObjetivo = Vector3.Distance(cuboActual.transform.position, objetivoCubo);
        if (distanciaAlObjetivo > 0.08f) return; // Cubo aún en camino

        // ── Snap assist ──────────────────────────────────────────────────────
        if (GameManager.Instance != null)
        {
            bool snapForzado = ComodinesManager.Instance != null &&
                               ComodinesManager.Instance.ConsumeSnapPerfecto();

            if (snapForzado)
            {
                // Snap PERFECTO: cae exactamente donde apunta el retículo.
                // NO va al centro de la plataforma (que puede diferir de la torre
                // si el jugador apiló cubos descentrados → causaba el bug de "se va al lado").
                if (reticuloActual.activeSelf)
                {
                    cuboActual.transform.position = new Vector3(
                        reticuloActual.transform.position.x,
                        cuboActual.transform.position.y,
                        reticuloActual.transform.position.z);
                }
                MensajeFlotante.SnapPerfecto();  // ← feedback visual
            }
            else
            {
                // Snap ASSIST normal: pequeña corrección si el cubo ya está muy cerca del centro
                Vector3 centro    = GameManager.Instance.ObtenerCentroPlataforma();
                float   tolerancia = LevelManager.ObtenerToleranciaSnap();

                float distXZ = Vector2.Distance(
                    new Vector2(cuboActual.transform.position.x, cuboActual.transform.position.z),
                    new Vector2(centro.x, centro.z));

                if (distXZ < tolerancia)
                {
                    cuboActual.transform.position = new Vector3(
                        centro.x,
                        cuboActual.transform.position.y,
                        centro.z);
                }
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        cuboActual.layer = LayerMask.NameToLayer("Plataforma");

        Rigidbody rb = cuboActual.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity  = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // ── Física del tipo de cubo (se aplica ANTES del comodín para que el comodín pueda override) ──
        AplicarFisicaTipoEnVuelo(rb, tipoActual);

        // ── Comodín Cubo Plomo (override: aplica encima del tipo) ─────────────
        if (ComodinesManager.Instance != null && ComodinesManager.Instance.ConsumeCuboPlomo())
        {
            rb.mass           = 80f;
            rb.linearDamping  = 2f;
            rb.angularDamping = 4f;
        }
        // ─────────────────────────────────────────────────────────────────────

        rb.AddForce(Vector3.down * (0.2f * SetupFase.EscalaSeleccionada), ForceMode.Impulse);

        cuboActual         = null;
        rendererCuboActual = null;

        GameManager.Instance.RegistrarCuboLanzado();

        // Generar siguiente cubo solo si el nivel permite más
        if (GameManager.Instance.PuedeLanzarCubo())
            Invoke(nameof(GenerarNuevoCubo), intervaloEntregaCubo);
    }

    // ── Sistema de Tipos ──────────────────────────────────────────────────────

    /// <summary>
    /// Si el Inspector no tiene configuracionesTipo asignado, usa valores por defecto.
    /// Así funciona el juego sin ninguna configuración extra en el Editor.
    /// </summary>
    void InicializarTipos()
    {
        if (configuracionesTipo != null && configuracionesTipo.Length > 0) return;
        configuracionesTipo = new ConfigTipoCubo[]
        {
            // ── V1 ─────────────────────────────────────────────────────────────
            new ConfigTipoCubo { tipo = TipoCubo.Normal,   nombreMostrar = "Normal",
                colorEnVuelo = new Color(0.75f, 0.70f, 0.63f, 1f), probabilidad = 0.45f,
                descripcionJugador = "Solid and stable." },
            new ConfigTipoCubo { tipo = TipoCubo.Plomo,    nombreMostrar = "Lead",
                colorEnVuelo = new Color(0.33f, 0.34f, 0.38f, 1f), probabilidad = 0.14f,
                descripcionJugador = "Heavy impact. Crushes Feather, shatters Ice." },
            new ConfigTipoCubo { tipo = TipoCubo.Plumas,   nombreMostrar = "Feather",
                colorEnVuelo = new Color(0.95f, 0.92f, 0.78f, 1f), probabilidad = 0.14f,
                descripcionJugador = "Ultra-light. Burns on contact with Fire." },
            new ConfigTipoCubo { tipo = TipoCubo.Hielo,    nombreMostrar = "Ice",
                colorEnVuelo = new Color(0.55f, 0.82f, 0.96f, 1f), probabilidad = 0.09f,
                descripcionJugador = "Slippery surface. Melts under Fire or Lead." },
            new ConfigTipoCubo { tipo = TipoCubo.Fuego,    nombreMostrar = "Fire",
                colorEnVuelo = new Color(0.92f, 0.28f, 0.06f, 1f), probabilidad = 0.09f,
                descripcionJugador = "Radial blast on impact. Burns everything nearby." },
            // ── V2 ─────────────────────────────────────────────────────────────
            new ConfigTipoCubo { tipo = TipoCubo.Gelatina, nombreMostrar = "Jelly",
                colorEnVuelo = new Color(0.30f, 0.90f, 0.45f, 0.75f), probabilidad = 0.03f,
                descripcionJugador = "Bouncy! Absorbs impact without breaking." },
            new ConfigTipoCubo { tipo = TipoCubo.Lava,     nombreMostrar = "Lava",
                colorEnVuelo = new Color(0.85f, 0.20f, 0.02f, 1f), probabilidad = 0.02f,
                descripcionJugador = "Molten rock. Melts Ice and chars Normal blocks." },
            new ConfigTipoCubo { tipo = TipoCubo.Hierba,   nombreMostrar = "Grass",
                colorEnVuelo = new Color(0.25f, 0.70f, 0.20f, 1f), probabilidad = 0.02f,
                descripcionJugador = "Natural adhesion. Very hard to topple." },
            new ConfigTipoCubo { tipo = TipoCubo.Agua,     nombreMostrar = "Water",
                colorEnVuelo = new Color(0.20f, 0.55f, 0.90f, 0.70f), probabilidad = 0.01f,
                descripcionJugador = "Fluid and slippery. Extinguishes Fire and Lava." },
            new ConfigTipoCubo { tipo = TipoCubo.Nube,     nombreMostrar = "Cloud",
                colorEnVuelo = new Color(0.95f, 0.97f, 1.00f, 0.65f), probabilidad = 0.01f,
                descripcionJugador = "Barely there. Floats and can be displaced by blasts." },
            new ConfigTipoCubo { tipo = TipoCubo.Piedra,  nombreMostrar = "Stone",
                colorEnVuelo = new Color(0.40f, 0.40f, 0.42f, 1f),    probabilidad = 0.04f,
                descripcionJugador = "Dense and immovable. Nothing shifts a Stone block." },
        };
    }

    /// <summary>Devuelve el tipo con probabilidades ponderadas.</summary>
    TipoCubo ElegirTipoAleatorio()
    {
        float total = 0f;
        foreach (var c in configuracionesTipo) total += c.probabilidad;
        float r = Random.Range(0f, total);
        float acum = 0f;
        foreach (var c in configuracionesTipo)
        {
            acum += c.probabilidad;
            if (r <= acum) return c.tipo;
        }
        return TipoCubo.Normal;
    }

    /// <summary>Devuelve la configuración de un tipo. Público para que ProximoCuboIndicador lo lea.</summary>
    public ConfigTipoCubo ObtenerConfig(TipoCubo t)
    {
        if (configuracionesTipo == null) return null;
        foreach (var c in configuracionesTipo)
            if (c.tipo == t) return c;
        return null;
    }

    /// <summary>Aplica física durante la caída según el tipo (antes del comodín).</summary>
    void AplicarFisicaTipoEnVuelo(Rigidbody rb, TipoCubo t)
    {
        switch (t)
        {
            case TipoCubo.Plomo:
                rb.mass = 40f; rb.linearDamping = 0.8f; rb.angularDamping = 2.0f; break;
            case TipoCubo.Plumas:
                rb.mass = 0.8f; rb.linearDamping = 7.0f; rb.angularDamping = 3.0f; break;
            case TipoCubo.Hielo:
                rb.mass = 5f; rb.linearDamping = 0.2f; break;
            case TipoCubo.Fuego:
                rb.mass = 10f; rb.linearDamping = 0.3f; break;
            case TipoCubo.Gelatina:
                rb.mass = 3f; rb.linearDamping = 1.5f; rb.angularDamping = 1.0f; break;
            case TipoCubo.Lava:
                rb.mass = 35f; rb.linearDamping = 0.5f; rb.angularDamping = 1.5f; break;
            case TipoCubo.Hierba:
                rb.mass = 8f; rb.linearDamping = 0.8f; break;
            case TipoCubo.Agua:
                rb.mass = 2f; rb.linearDamping = 4.0f; rb.angularDamping = 2.0f; break;
            case TipoCubo.Nube:
                rb.mass = 0.2f; rb.linearDamping = 12f; rb.angularDamping = 5.0f; break;
            case TipoCubo.Piedra:
                rb.mass = 18f; rb.linearDamping = 0.4f; rb.angularDamping = 1.2f; break;
        }
    }

    /// <summary>Muestra un toast cuando aparece un cubo especial.</summary>
    void MostrarToastTipo(TipoCubo t)
    {
        ConfigTipoCubo cfg = ObtenerConfig(t);
        if (cfg == null) return;
        MensajeFlotante.Mostrar(cfg.nombreMostrar + " Block", cfg.colorEnVuelo, 1.8f);
    }
}
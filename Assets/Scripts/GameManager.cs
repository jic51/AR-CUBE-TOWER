using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Layered.ARAdSystem;

public enum EstadoJuego { Menu, Setup, Jugando, Pausa, GameOver }

public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance;
    public static bool saltarDirectoASetup = false;

    // ── Perfil ───────────────────────────────────────────────────────────────
    [Header("Perfil de Usuario")]
    public TMP_InputField              inputNombreMenu;
    public TextMeshProUGUI             textoNombreHUD;

    [Tooltip("Avatar que aparece en el panel_menu_principal (dentro de image_avatar_container)")]
    public UnityEngine.UI.Image        imagenAvatarMenu;

    [Tooltip("Avatar pequeño en el HUD durante el juego (profilepic, nieto de image_profilecontainer)")]
    public UnityEngine.UI.Image        imagenAvatarHUD;

    public Sprite[]                    catalogoAvatares;

    // ── Referencias clave ────────────────────────────────────────────────────
    [Header("Referencias Clave")]
    public GruaController              miGrua;
    public SetupFase                   miSetup;
    public TutorialManager             miTutorial;   // opcional — puede quedar vacío
    public UnityEngine.XR.ARFoundation.ARSession sesionAR;

    // ── Paneles UI ───────────────────────────────────────────────────────────
    [Header("UI — Paneles")]
    public GameObject panelMenuPrincipal;
    public GameObject panelJuegoHUD;
    public GameObject panelPausa;
    public GameObject panelGameOver;
    public GameObject panelRescate;       // "¿Te rindes?" — aparece ANTES del Game Over

    // ── HUD durante juego ────────────────────────────────────────────────────
    [Header("UI — HUD Juego")]
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoAltura;
    public TextMeshProUGUI textoCubos;
    public TextMeshProUGUI textoMonedasHUD;   // monedas actuales (esquina)
    public TextMeshProUGUI textoVidasHUD;     // corazones / número de vidas

    // ── Panel GameOver ────────────────────────────────────────────────────────
    [Header("UI — GameOver")]
    public TextMeshProUGUI textoEstadoFinal;
    public TextMeshProUGUI textoStatsFinales;
    public TextMeshProUGUI textoAlturaFinal;
    public TextMeshProUGUI textoCubosFinal;
    public TextMeshProUGUI textoRecordFinal;
    public TextMeshProUGUI textoEstrellas;        // texto fallback si no hay imágenes
    public TextMeshProUGUI textoMonedasGanadas;  // "+50 monedas" al ganar
    public GameObject      botonSiguienteNivel;

    [Header("UI — Estrellas (iconos)")]
    [Tooltip("Arrastra 3 objetos Image aquí (uno por estrella). Se activan según el rating.")]
    public UnityEngine.UI.Image[] imagenesEstrellas;  // array de 3 Image — el sprite se asigna en Editor

    // ── Panel Rescate ─────────────────────────────────────────────────────────
    [Header("UI — Panel Rescate (dentro de panelRescate)")]
    public TextMeshProUGUI textoCountdownRescate;
    public TextMeshProUGUI textoPrecioRescate;
    public TextMeshProUGUI textoMonedasEnRescate;
    public UnityEngine.UI.Button botonComprarRescate;
    [Tooltip("Botón 'Ver anuncio (gratis)' — solo visible si LayeredAds está listo")]
    public UnityEngine.UI.Button botonAdRescate;

    // ── Ads AR (LAYERED) ──────────────────────────────────────────────────────
    [Header("Ads AR — LAYERED")]
    [Tooltip("API Key de la plataforma LAYERED (asignada por el equipo)")]
    public string layeredApiKey = "LAYERED_API_KEY_AQUI";
    [Tooltip("ScriptableObject creado con Assets → Create → LAYERED → AR Ad Config")]
    public ARAdConfig adConfigDefault;
    [Tooltip("Mostrar un AR Break cada N derrotas (0 = nunca automático)")]
    public int adCadaNDerrotas = 2;

    // Derrotas desde el último anuncio. ESTÁTICO a propósito: "Retry" y
    // "siguiente nivel" recargan la escena y recrean este GameManager; como
    // campo normal volvía a 0 en cada partida y "cada N derrotas" nunca se
    // cumplía para N > 1. Vive lo que dura la sesión de la app.
    private static int s_derrotasSinAd = 0;

    // True si el panel de rescate ofreció el anuncio en esta partida
    private bool _adOfrecidoEnRescate = false;

    // Watchdog del AR Break: garantiza que el juego se recupere aunque el SDK
    // no devuelva el callback. _adResuelto evita que callback y watchdog
    // actúen los dos sobre el mismo break.
    private const float SegundosMaxAd = 45f;
    private bool _adResuelto = true;

    // True si ya se mostró un anuncio en esta partida. Evita encadenar dos
    // anuncios seguidos cuando el jugador salta el del rescate y cae al GameOver.
    private bool _adMostradoEstaPartida = false;

    // True mientras está abierto el panel "Don't give up!" (antes del GameOver)
    private bool _enRescate = false;

    // ── Configuración ─────────────────────────────────────────────────────────
    [Header("Configuración Juego")]
    public float tiempoLimite     = 60.0f;
    public float metaAlturaNivel  = 1.0f;
    public int   cubosMaximos     = 0;      // 0 = ilimitado
    public float intervaloEntrega = 1.0f;   // segundos entre cubo y cubo
    public bool  snapDesactivado  = false;  // desactiva snap assist en niveles de precisión

    // HUD extras para niveles especiales
    [Header("UI — HUD extras")]
    public TextMeshProUGUI textoCubosRestantes; // "Cubos: 8/12" cuando hay límite

    // ── Estado privado ────────────────────────────────────────────────────────
    private float       tiempoRestante;
    private int         cubosUsados   = 0;
    private float       alturaMaxima  = 0;
    private bool        yaUsoRescate  = false;   // solo 1 rescate por partida
    private Transform   basePlataforma;
    private PlayerData  datosJugador;
    private BarraProgresoAR barraProgreso;
    private Coroutine   coroutineRescate;

    // ── Pozo ──────────────────────────────────────────────────────────────────
    private float alturaInicialPlataforma = 0f;
    private float pozoTargetY             = 0f;
    private float pozoUltimoDescenso      = -999f;
    private float tiempoSinVerCima        = 0f;
    private float tiempoDesdeCaida        = float.MaxValue;

    [Header("Pozo — configuración")]
    [Tooltip("Segundos seguidos con la cima por encima de pozoUmbralViewport antes de bajar la plataforma")]
    public float pozSegundosSinCima     = 2f;
    [Tooltip("Gracia en segundos tras un cubo que cae — no baja durante este tiempo")]
    public float pozSegundosGraciaCaida = 8f;
    [Tooltip("Cooldown mínimo entre descensos consecutivos")]
    public float pozoCooldown           = 3f;
    [Tooltip("Altura en pantalla (0 = abajo, 1 = arriba) a partir de la cual la cima se considera " +
             "demasiado alta y la plataforma empieza a bajar. 0.85 deja margen para ver dónde cae el cubo.")]
    [Range(0.5f, 1f)]
    public float pozoUmbralViewport     = 0.85f;

    private Camera _camara;

    // Tiempo que se deja al último cubo de un nivel con cubos limitados para
    // caer y asentarse antes de evaluar la partida
    private const float SegundosGraciaUltimoCubo = 4f;

    // Valor centinela del reloj en niveles sin límite de tiempo (EFFICIENCY)
    private const float SinReloj = 999f;

    public EstadoJuego estadoActual;

    // ── Awake / Start ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        datosJugador = SaveSystem.Cargar();

        // Deshabilitar oclusión AR (si AROcclusionManager quedó en la escena por error)
        var occ = FindFirstObjectByType<UnityEngine.XR.ARFoundation.AROcclusionManager>();
        if (occ != null)
            occ.requestedEnvironmentDepthMode =
                UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Disabled;

        // Inicializar SDK de AR Ads
        if (!string.IsNullOrEmpty(layeredApiKey) && layeredApiKey != "LAYERED_API_KEY_AQUI")
        {
            LayeredAds.Inicializar(layeredApiKey);
            if (adConfigDefault != null) LayeredAds.SetAdConfig(adConfigDefault);
        }
    }

    void Start()
    {
        // Calcular vidas regeneradas offline
        EconomiaManager.Instance?.InicializarAlArrancar();

        // Bonus diario: primer arranque del día → +30 monedas
        if (datosJugador != null)
        {
            string hoy = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (datosJugador.ultimoDiaJugado != hoy)
            {
                // Se entrega dentro de la corrutina, con el menú ya visible y el
                // contador en pantalla. Antes se sumaba aquí, en el arranque, en
                // un menú sin contador de monedas: el jugador nunca lo veía.
                StartCoroutine(EntregarBonusDiario(hoy, 0.8f));
            }
        }

        // Cargar perfil en UI
        if (datosJugador != null)
        {
            if (inputNombreMenu != null) inputNombreMenu.text = datosJugador.nombreUsuario;
            if (textoNombreHUD  != null) textoNombreHUD.text  = datosJugador.nombreUsuario;

            if (catalogoAvatares != null && catalogoAvatares.Length > 0)
            {
                int id = Mathf.Clamp(datosJugador.avatarId, 0, catalogoAvatares.Length - 1);
                if (imagenAvatarMenu != null) imagenAvatarMenu.sprite = catalogoAvatares[id];
                if (imagenAvatarHUD  != null) imagenAvatarHUD.sprite  = catalogoAvatares[id];
            }
        }

        if (saltarDirectoASetup)
        {
            saltarDirectoASetup = false;
            // Reintentar y "siguiente nivel" recargan la escena y entran por aquí:
            // también tienen que pasar por el control de vidas.
            if (HayVidasParaJugar()) CambiarEstado(EstadoJuego.Setup);
            else                     CambiarEstado(EstadoJuego.Menu);
        }
        else
        {
            CambiarEstado(EstadoJuego.Menu);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (estadoActual != EstadoJuego.Jugando) return;

        // Durante el panel de rescate la partida está detenida: el estado sigue
        // siendo Jugando (para poder reanudar), pero el pozo no debe seguir
        // bajando la plataforma ni la altura recalculándose.
        if (_enRescate) return;

        if (tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0)
            {
                tiempoRestante = 0;
                FinalizarJuego();
            }
        }

        CalcularAlturaTorre();
        ActualizarPozo();
        ActualizarUI();
    }

    // ── Acceso a PlayerData (para EconomiaManager) ────────────────────────────

    /// <summary>Fuente única de PlayerData. EconomiaManager lee y escribe a través de aquí.</summary>
    public PlayerData GetDatos() => datosJugador;

    // ── Perfil ────────────────────────────────────────────────────────────────

    public void ActualizarNombreEnTiempoReal()
    {
        datosJugador.nombreUsuario = inputNombreMenu.text;
        if (textoNombreHUD != null) textoNombreHUD.text = datosJugador.nombreUsuario;
        SaveSystem.Guardar(datosJugador);
    }

    public void SeleccionarAvatar(int id)
    {
        datosJugador.avatarId = id;
        if (catalogoAvatares != null && id < catalogoAvatares.Length)
        {
            if (imagenAvatarMenu != null) imagenAvatarMenu.sprite = catalogoAvatares[id];
            if (imagenAvatarHUD  != null) imagenAvatarHUD.sprite  = catalogoAvatares[id];
        }
        SaveSystem.Guardar(datosJugador);
    }

    // ── Cambio de Estado ──────────────────────────────────────────────────────

    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        estadoActual = nuevoEstado;
        _enRescate   = false;

        // Pantalla siempre encendida mientras la sesión AR está activa. El juego
        // tiene esperas sin tocar la pantalla (el pozo, apuntar) y el teléfono se
        // apagaba; tocarla para evitarlo soltaba un cubo. En los menús vuelve el
        // ajuste del sistema para no gastar batería.
        bool sesionActiva = nuevoEstado == EstadoJuego.Setup
                         || nuevoEstado == EstadoJuego.Jugando
                         || nuevoEstado == EstadoJuego.Pausa;
        Screen.sleepTimeout = sesionActiva ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

        if (panelMenuPrincipal) panelMenuPrincipal.SetActive(false);
        if (panelJuegoHUD)      panelJuegoHUD.SetActive(false);
        if (panelPausa)         panelPausa.SetActive(false);
        if (panelGameOver)      panelGameOver.SetActive(false);
        if (panelRescate)       panelRescate.SetActive(false);

        // El panel de comodines solo es visible mientras se está jugando activamente
        ComodinesManager.Instance?.MostrarPanel(nuevoEstado == EstadoJuego.Jugando);

        // El texto guía AR solo aparece durante el Setup — ocultarlo en cualquier otro estado
        if (miSetup != null && miSetup.textoGuiaAR != null && nuevoEstado != EstadoJuego.Setup)
            miSetup.textoGuiaAR.gameObject.SetActive(false);

        switch (nuevoEstado)
        {
            case EstadoJuego.Menu:
                panelMenuPrincipal.SetActive(true);
                if (sesionAR) sesionAR.enabled = false;
                Time.timeScale = 1;
                // Header oculto en el menú principal — el menú ya muestra avatar + nombre
                PlayerHeaderUI.Mostrar(false);
                break;

            case EstadoJuego.Setup:
                if (sesionAR) { sesionAR.Reset(); sesionAR.enabled = true; }
                if (miSetup)
                {
                    miSetup.enabled = true;
                    miSetup.IniciarSetup();
                }
                miTutorial?.IntentarIniciar();
                // Header oculto durante el setup AR (no distraer al jugador)
                PlayerHeaderUI.Mostrar(false);
                break;

            case EstadoJuego.Jugando:
                panelJuegoHUD.SetActive(true);
                Time.timeScale = 1;
                // Header visible durante el juego: monedas/gemas/vidas siempre a la vista
                PlayerHeaderUI.Mostrar(true);
                break;

            case EstadoJuego.Pausa:
                panelPausa.SetActive(true);
                Time.timeScale = 0;
                // Header visible en pausa: el jugador puede ver sus recursos
                PlayerHeaderUI.Mostrar(true);
                break;

            case EstadoJuego.GameOver:
                panelGameOver.SetActive(true);
                Time.timeScale = 1;
                // Header visible en game over: monedas ganadas visibles
                PlayerHeaderUI.Mostrar(true);
                break;
        }
    }

    // ── Inicio de partida ─────────────────────────────────────────────────────

    public void IniciarPartida(Transform plataforma)
    {
        // La vida se cobra aquí: la partida arranca de verdad en este punto.
        // El control de disponibilidad ya se hizo en BotonIrASetup().
        EconomiaManager.Instance?.GastarVida();

        AplicarDatosNivel();

        basePlataforma          = plataforma;
        alturaInicialPlataforma = plataforma.position.y;
        pozoTargetY         = plataforma.position.y;
        pozoUltimoDescenso  = Time.time;
        tiempoSinVerCima    = 0f;
        tiempoDesdeCaida    = float.MaxValue; // empieza sin "cubo caído reciente"
        tiempoRestante          = tiempoLimite > 0 ? tiempoLimite : SinReloj;
        cubosUsados              = 0;
        alturaMaxima             = 0;
        yaUsoRescate             = false;
        _adMostradoEstaPartida   = false;
        _adOfrecidoEnRescate     = false;
        metaAlcanzadaMostrada    = false;
        CuboInteligente.comboConsecutivo = 0;

        CambiarEstado(EstadoJuego.Jugando);

        // Tutorial paso 2: primer cubo en vuelo
        miTutorial?.IrAPaso(2);

        // Aplicar intervalo de entrega al controlador de grúa
        if (miGrua != null)
        {
            miGrua.intervaloEntregaCubo = intervaloEntrega;
            miGrua.ActivarGrúa();
        }

        // Mostrar/ocultar HUD de cubos restantes
        if (textoCubosRestantes != null)
            textoCubosRestantes.gameObject.SetActive(cubosMaximos > 0);

        // Barra de progreso AR (no mostrar en niveles sin meta de altura)
        if (metaAlturaNivel > 0)
        {
            var barraGO = new GameObject("BarraProgresoAR");
            barraProgreso = barraGO.AddComponent<BarraProgresoAR>();
            barraProgreso.Inicializar(plataforma, metaAlturaNivel, Camera.main.transform);

            // La barra reemplaza el texto de altura en el HUD (menos ruido visual)
            if (textoAltura != null) textoAltura.gameObject.SetActive(false);
        }

        // Panel de comodines (desactivar snap si el nivel lo requiere)
        ComodinesManager.Instance?.IniciarPartida();
    }

    /// <summary>
    /// Carga la configuración del nivel actual justo al empezar la partida.
    /// Es la única fuente de estos valores: antes se copiaban al elegir el nivel
    /// en el menú, y "Retry" (que recarga la escena) arrancaba con los valores
    /// por defecto de la escena —60 s y 1 m— en lugar de los del nivel.
    /// La meta se multiplica por la escala de la plataforma porque los cubos
    /// crecen con ella: sin esto, agrandar la plataforma hacía los niveles triviales.
    /// </summary>
    void AplicarDatosNivel()
    {
        if (LevelManager.Instance == null) return;
        DatosNivel d = LevelManager.Instance.ObtenerNivelActual();

        metaAlturaNivel  = d.metaAltura * SetupFase.EscalaSeleccionada;
        tiempoLimite     = d.tiempoLimite > 0 ? d.tiempoLimite : 0f;   // 0 = sin reloj
        cubosMaximos     = d.cubosMaximos;
        intervaloEntrega = d.intervaloEntrega > 0 ? d.intervaloEntrega : 1f;
        snapDesactivado  = d.snapDesactivado;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void RegistrarCuboLanzado()
    {
        cubosUsados++;
        // Sin cubos: la partida acaba, pero no en este fotograma. El último cubo
        // acaba de SOLTARSE y tiene que caer y asentarse para contar; antes el
        // reloj se ponía a 0 aquí y ese cubo nunca sumaba altura.
        if (cubosMaximos > 0 && cubosUsados >= cubosMaximos)
            tiempoRestante = Mathf.Min(tiempoRestante, SegundosGraciaUltimoCubo);
    }

    public bool PuedeLanzarCubo()
    {
        // False si se agotaron los cubos del nivel
        return cubosMaximos == 0 || cubosUsados < cubosMaximos;
    }

    /// <summary>
    /// Llamado por AnclaTorre cuando ARCore corrige el tracking y la torre se ha
    /// desplazado junto con su ancla. El pozo guarda alturas absolutas: sin este
    /// ajuste, tras una corrección vertical arrastraría la plataforma de vuelta
    /// a la altura vieja y la separaría de los cubos.
    /// </summary>
    public void NotificarCorreccionAncla(float deltaY)
    {
        if (Mathf.Approximately(deltaY, 0f)) return;
        pozoTargetY             += deltaY;
        alturaInicialPlataforma += deltaY;
        foreach (var c in CuboInteligente.cubosActivos) c?.AjustarAlturaReferencia(deltaY);
    }

    /// <summary>Llamado por CuboInteligente cuando un cubo cae físicamente de la torre.</summary>
    public void NotificarCuboCaido() => tiempoDesdeCaida = 0f;

    public float  ObtenerAlturaTorreMundo()  => basePlataforma != null ? basePlataforma.position.y + alturaMaxima : -100f;
    public Vector3 ObtenerCentroPlataforma() => basePlataforma != null ? basePlataforma.position : Vector3.zero;

    /// <summary>
    /// Altura de la base de la plataforma (el suelo del juego AR).
    /// Los cubos se destruyen si caen más de margenCaidaFuera por debajo de este punto.
    /// </summary>
    public float ObtenerAlturaSuelo() => basePlataforma != null ? basePlataforma.position.y : 0f;

    /// <summary>Añade segundos al timer. Llamado por ComodinesManager (+30s) y flujo de rescate.</summary>
    public void AgregarTiempo(float segundos)
    {
        tiempoRestante += segundos;
    }

    // ── Efecto Pozo ───────────────────────────────────────────────────────────
    // La plataforma baja SOLO cuando SE CUMPLEN LAS DOS condiciones:
    //   1. El retículo lleva N segundos sin estar en la CIMA de la torre
    //      (el usuario no puede ver/apuntar bien al cubo más alto)
    //   2. NO cayó un cubo recientemente (gracia de 8s tras una caída)
    // Si un cubo cayó: el jugador necesita tiempo para recuperarse → no bajar.
    // Si el retículo SÍ está en la cima → el usuario está jugando bien → no bajar.
    // ─────────────────────────────────────────────────────────────────────────

    void ActualizarPozo()
    {
        if (basePlataforma == null || miGrua == null || alturaMaxima < 0.1f) return;

        tiempoDesdeCaida += Time.deltaTime;

        // ¿Se está saliendo la cima por arriba de la pantalla?
        // Antes se miraba si el retículo apuntaba a la cima, pero el retículo
        // indica dónde caerá el próximo cubo, no lo que el jugador ve: con una
        // torre de 3 cubos entera en pantalla, apuntar al lado bastaba para que
        // la plataforma bajara. Bajarla solo sirve si la cima queda alta en el
        // encuadre; si el jugador mira a otra parte, bajarla no le ayuda.
        float cubeSize   = 0.15f * SetupFase.EscalaSeleccionada;
        float alturaCima = basePlataforma.position.y + alturaMaxima;

        if (_camara == null && Camera.main != null) _camara = Camera.main;

        bool cimaMuyAlta = false;
        if (_camara != null)
        {
            Vector3 basePos = basePlataforma.position;
            Vector3 vpCima  = _camara.WorldToViewportPoint(new Vector3(basePos.x, alturaCima, basePos.z));

            // Solo cuenta si el jugador está MIRANDO LA TORRE: algún tramo de la
            // columna entre la base y la cima tiene que estar dentro del encuadre.
            // Sin esto, mirar al suelo fuera de la plataforma dejaba la cima "por
            // encima" del encuadre y la plataforma bajaba sin motivo. No se exige
            // ver la base: con una torre alta se mira el tramo medio, y es
            // justo ahí cuando más hace falta bajarla.
            bool mirandoTorre = false;
            for (int i = 0; i <= 4 && !mirandoTorre; i++)
            {
                float   y  = Mathf.Lerp(basePos.y, alturaCima, i / 4f);
                Vector3 vp = _camara.WorldToViewportPoint(new Vector3(basePos.x, y, basePos.z));
                mirandoTorre = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            }

            cimaMuyAlta = mirandoTorre && vpCima.z > 0f && vpCima.y > pozoUmbralViewport;
        }

        if (cimaMuyAlta) tiempoSinVerCima += Time.deltaTime;
        else             tiempoSinVerCima  = 0f;

        // Condición de descenso: lleva N segundos sin ver la cima Y no cayó cubo recientemente
        bool debesBajar = tiempoSinVerCima     >= pozSegundosSinCima
                       && tiempoDesdeCaida     >= pozSegundosGraciaCaida
                       && Time.time - pozoUltimoDescenso > pozoCooldown;

        if (debesBajar)
        {
            pozoTargetY       -= cubeSize;
            pozoUltimoDescenso = Time.time;
            tiempoSinVerCima   = 0f;
            MensajeFlotante.Mostrar("Platform descending...", new Color(1f, 0.7f, 0.2f), 1.5f);
        }

        // Lerp suave hacia el target
        float prevY = basePlataforma.position.y;
        Vector3 pos = basePlataforma.position;
        pos.y = Mathf.Lerp(pos.y, pozoTargetY, Time.deltaTime * 1.5f);
        basePlataforma.position = pos;

        float delta = basePlataforma.position.y - prevY;
        if (delta < -0.001f)
            foreach (var c in CuboInteligente.cubosActivos) c?.AjustarAlturaReferencia(delta);
    }

    // ── Cálculo de altura ─────────────────────────────────────────────────────

    void CalcularAlturaTorre()
    {
        if (basePlataforma == null) return;

        // Usamos la lista estática de CuboInteligente en lugar de un raycast central.
        // El raycast solo medía el cubo justo encima del centro de la plataforma —
        // si la torre estaba inclinada o el cubo más alto era lateral, devolvía menos altura
        // de la real y el jugador perdía injustamente.
        float alturaBase = basePlataforma.position.y;
        float altMax     = alturaMaxima; // no resetear, solo crecer (por si el cubo cae)

        foreach (var cubo in CuboInteligente.cubosActivos)
        {
            if (cubo == null || !cubo.HaAterrizado) continue;

            // Techo del cubo = centro Y + mitad de su escala en Y
            float techo = cubo.transform.position.y + cubo.transform.localScale.y * 0.5f;
            float h     = techo - alturaBase;
            if (h > altMax) altMax = h;
        }

        if (altMax > 0.05f)
        {
            // ── Feedback: meta alcanzada por primera vez ──────────────────────
            if (!metaAlcanzadaMostrada && altMax >= metaAlturaNivel)
            {
                metaAlcanzadaMostrada = true;
                MensajeFlotante.MetaAlcanzada();
            }

            alturaMaxima = altMax;
        }
    }

    private bool metaAlcanzadaMostrada = false;

    private const int MonedasBonusDiario = 30;

    /// <summary>
    /// Entrega el bono diario de forma visible. El menú principal no muestra el
    /// header (decisión de diseño), así que se revela solo mientras dura la
    /// animación de monedas y se vuelve a ocultar al terminar.
    /// </summary>
    System.Collections.IEnumerator EntregarBonusDiario(string hoy, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        bool enMenu = estadoActual == EstadoJuego.Menu;
        if (enMenu) PlayerHeaderUI.Mostrar(true);
        yield return null;   // un fotograma para que el header calcule su layout

        // Racha: sigue si el último día jugado fue ayer; si no, vuelve a 1
        string ayer = System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        datosJugador.rachaDias = datosJugador.ultimoDiaJugado == ayer ? datosJugador.rachaDias + 1 : 1;
        bool premioRacha = datosJugador.rachaDias % EconomiaManager.DIAS_RACHA == 0;

        // El día se marca en el mismo guardado que suma las monedas: si la app
        // se cierra antes de este punto, el bono se vuelve a ofrecer, no se pierde
        datosJugador.ultimoDiaJugado = hoy;
        AnimadorMonedas.AnimarDesdeCentro(MonedasBonusDiario);
        EconomiaManager.Instance?.GanarMonedas(MonedasBonusDiario);
        MensajeFlotante.Mostrar($"Daily bonus  +{MonedasBonusDiario} coins  ·  Day {datosJugador.rachaDias}",
                                new Color(0.95f, 0.80f, 0.10f), 2.5f);

        // Cada 7 días seguidos: gemas, con su animación después de las monedas
        if (premioRacha)
        {
            AnimadorMonedas.AnimarGemasDesdeCentro(EconomiaManager.GEMAS_RACHA, RetrasoGemas);
            EconomiaManager.Instance?.GanarGemas(EconomiaManager.GEMAS_RACHA);
            StartCoroutine(MensajeGemasTras(RetrasoGemas, EconomiaManager.GEMAS_RACHA,
                                            $"{EconomiaManager.DIAS_RACHA}-day streak"));
            yield return new WaitForSecondsRealtime(RetrasoGemas);   // que el header siga visible
        }

        // Duración aproximada de la animación completa + un momento para verlo
        yield return new WaitForSecondsRealtime(2.8f);

        // Solo se oculta si el jugador sigue en el menú (si ya entró a jugar,
        // el header lo gobierna CambiarEstado)
        if (enMenu && estadoActual == EstadoJuego.Menu) PlayerHeaderUI.Mostrar(false);
    }

    // Umbrales de estrellas sobre la meta (2026-09-22). La partida no termina
    // al alcanzar la meta sino al acabarse el tiempo, así que las estrellas
    // premian seguir construyendo por encima. Antes bastaba ratio ≥ 1 para 3
    // estrellas: toda victoria daba 3, y 1 o 2 solo salían al perder.
    private const float Ratio2Estrellas = 1.25f;
    private const float Ratio3Estrellas = 1.50f;

    // Las gemas salen después de las monedas, para que cada animación se lea
    private const float RetrasoGemas = 1.2f;

    System.Collections.IEnumerator MensajeGemasTras(float segundos, int cantidad, string motivo)
    {
        yield return new WaitForSecondsRealtime(segundos);
        MensajeFlotante.GemasGanadas(cantidad, motivo);
    }

    int CalcularEstrellas()
    {
        float ratio = metaAlturaNivel > 0 ? alturaMaxima / metaAlturaNivel : 0f;
        return ratio >= Ratio3Estrellas ? 3 : ratio >= Ratio2Estrellas ? 2 : ratio >= 1f ? 1 : 0;
    }

    /// <summary>
    /// Gemas del final de partida, con el motivo de cada una para enseñarlo.
    /// Reglas aprobadas el 2026-09-22.
    /// </summary>
    int CalcularGemas(bool gano, bool primerVez, int estrellas, bool nuevoRecord,
                      System.Collections.Generic.List<string> motivos)
    {
        int gemas  = 0;
        int nivel  = LevelManager.NivelSeleccionado;

        if (gano && primerVez)
        {
            gemas += EconomiaManager.GEMAS_PRIMERA_VEZ;
            motivos.Add("First clear");

            // Último nivel de un bloque de 5 (niveles 5, 10, 15…)
            if ((nivel + 1) % 5 == 0)
            {
                gemas += EconomiaManager.GEMAS_BLOQUE;
                motivos.Add("Block complete");
            }
        }

        // 3 estrellas: solo la primera vez que se consiguen en ese nivel
        if (gano && estrellas == 3 && MejoresEstrellas(nivel) < 3)
        {
            gemas += EconomiaManager.GEMAS_TRES_ESTRELLAS;
            motivos.Add("3 stars");
        }

        if (nuevoRecord)
        {
            gemas += EconomiaManager.GEMAS_POR_RECORD;
            motivos.Add("New record");
        }
        return gemas;
    }

    int MejoresEstrellas(int nivel) =>
        datosJugador.estrellasNivel != null && nivel >= 0 && nivel < datosJugador.estrellasNivel.Length
            ? datosJugador.estrellasNivel[nivel] : 0;

    void GuardarEstrellas(int nivel, int estrellas)
    {
        if (nivel < 0) return;
        var arr = datosJugador.estrellasNivel ?? new int[0];
        if (arr.Length <= nivel) System.Array.Resize(ref arr, nivel + 1);
        arr[nivel] = Mathf.Max(arr[nivel], estrellas);
        datosJugador.estrellasNivel = arr;
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void ActualizarUI()
    {
        // Timer
        if (textoTiempo)
        {
            // Niveles sin reloj: "--" hasta que se acaben los cubos (entonces
            // aparece la cuenta de gracia del último cubo). No "∞": la fuente
            // LiberationSans SDF tiene atlas estático sin ese glifo.
            bool sinReloj = tiempoLimite <= 0f && tiempoRestante > SegundosGraciaUltimoCubo;
            int s = Mathf.CeilToInt(tiempoRestante);
            textoTiempo.text  = sinReloj ? "--" : s + "s";
            textoTiempo.color = !sinReloj && tiempoRestante <= 10f ? Color.red : Color.white;
        }

        if (textoAltura) textoAltura.text = alturaMaxima.ToString("F2") + " m";

        // Cubos: si hay límite mostramos "Usados: 5/12", si no "Cubos: 5"
        if (textoCubos)
        {
            textoCubos.text = cubosMaximos > 0
                ? "Cubes: " + cubosUsados + "/" + cubosMaximos
                : "Cubes: " + cubosUsados;
        }
        if (textoCubosRestantes && cubosMaximos > 0)
        {
            int restantes = Mathf.Max(0, cubosMaximos - cubosUsados);
            textoCubosRestantes.text  = "Left: " + restantes;
            textoCubosRestantes.color = restantes <= 3 ? Color.red : Color.white;
        }

        // Monedas y vidas en HUD (actualizar cada frame es barato para textos simples)
        if (textoMonedasHUD && EconomiaManager.Instance != null)
            textoMonedasHUD.text = EconomiaManager.Instance.Monedas.ToString();

        if (textoVidasHUD && EconomiaManager.Instance != null)
            textoVidasHUD.text = EconomiaManager.Instance.Vidas + "/" + EconomiaManager.MAX_VIDAS;

        if (barraProgreso != null) barraProgreso.ActualizarProgreso(alturaMaxima);
    }

    // ── Finalizar Juego ───────────────────────────────────────────────────────

    void FinalizarJuego()
    {
        bool gano = alturaMaxima >= metaAlturaNivel;

        // ── Rescate: siempre al perder (primera vez) ─────────────────────────
        // El panel siempre aparece; los botones se habilitan/deshabilitan según recursos
        if (!gano && !yaUsoRescate)
        {
            coroutineRescate = StartCoroutine(FlujoRescate());
            return;
        }

        MostrarGameOver(gano);
    }

    // ── Flujo de Rescate (countdown 5s antes del GameOver) ───────────────────

    IEnumerator FlujoRescate()
    {
        _enRescate = true;   // se libera en CambiarEstado() o MostrarGameOver()
        if (miGrua != null) miGrua.DesactivarGrúa();

        // Mostrar panel de rescate
        if (panelRescate) panelRescate.SetActive(true);

        // Configurar textos del panel
        if (textoPrecioRescate)
            textoPrecioRescate.text = EconomiaManager.PRECIO_RESCATE_MONEDAS + " coins";

        if (textoMonedasEnRescate && EconomiaManager.Instance != null)
            textoMonedasEnRescate.text = "You have: " + EconomiaManager.Instance.Monedas;

        if (botonComprarRescate && EconomiaManager.Instance != null)
            botonComprarRescate.interactable =
                EconomiaManager.Instance.TieneMonedas(EconomiaManager.PRECIO_RESCATE_MONEDAS);

        // Botón "Ver anuncio gratis" — solo visible si el SDK AR está listo
        if (botonAdRescate != null)
        {
            bool ofrecer = LayeredAds.EstaListo();
            botonAdRescate.gameObject.SetActive(ofrecer);
            if (ofrecer) _adOfrecidoEnRescate = true;
        }

        // Countdown
        float tiempo = 5f;
        while (tiempo > 0f)
        {
            tiempo -= Time.deltaTime;
            if (textoCountdownRescate)
                textoCountdownRescate.text = Mathf.CeilToInt(tiempo) + "s";
            yield return null;
        }

        // Tiempo agotado → GameOver normal
        if (panelRescate) panelRescate.SetActive(false);
        MostrarGameOver(alturaMaxima >= metaAlturaNivel);
    }

    /// <summary>Botón "Sí, continuar" en el panel de rescate.</summary>
    public void BotonRescatarConMonedas()
    {
        if (EconomiaManager.Instance == null) return;
        if (!EconomiaManager.Instance.ComprarTiempoExtra()) return; // Verifica y descuenta

        // Cancelar el countdown
        if (coroutineRescate != null) { StopCoroutine(coroutineRescate); coroutineRescate = null; }
        if (panelRescate) panelRescate.SetActive(false);

        // Reanudar la partida
        yaUsoRescate   = true;
        tiempoRestante = 30f;
        if (miGrua != null) miGrua.ActivarGrúa();
        CambiarEstado(EstadoJuego.Jugando);
    }

    /// <summary>Botón "No, gracias" en el panel de rescate.</summary>
    public void BotonRechazarRescate()
    {
        if (coroutineRescate != null) { StopCoroutine(coroutineRescate); coroutineRescate = null; }
        if (panelRescate) panelRescate.SetActive(false);
        MostrarGameOver(alturaMaxima >= metaAlturaNivel);
    }

    /// <summary>
    /// Botón "Ver anuncio AR (gratis)" en el panel de rescate.
    /// El jugador apunta el teléfono en la dirección indicada y ve el AR Break.
    /// Si lo completa → continúa con +30s sin coste de monedas.
    /// Si hace skip → Game Over normal.
    /// </summary>
    public void BotonRescatarConAd()
    {
        if (!LayeredAds.EstaListo()) return;

        if (coroutineRescate != null) { StopCoroutine(coroutineRescate); coroutineRescate = null; }
        if (panelRescate) panelRescate.SetActive(false);

        // Pantalla limpia: ocultar todo para que el ad sea el único foco
        PlayerHeaderUI.Mostrar(false);
        ComodinesManager.Instance?.MostrarPanel(false);
        Time.timeScale = 0; // pausa física y timer — el ad usa unscaledTime, no se afecta

        _adResuelto            = false;
        _adMostradoEstaPartida = true;
        s_derrotasSinAd        = 0;   // un anuncio visto reinicia la cuenta
        StartCoroutine(WatchdogRescate(SegundosMaxAd));

        LayeredAds.MostrarBreak(resultado =>
        {
            if (_adResuelto) return;   // el watchdog ya resolvió este break
            _adResuelto = true;

            Time.timeScale = 1; // restaurar tiempo antes de cualquier otra acción

            if (resultado.vioCompleto || resultado.hizoClic)
            {
                yaUsoRescate   = true;
                tiempoRestante = 30f;
                if (miGrua != null) miGrua.ActivarGrúa();
                CambiarEstado(EstadoJuego.Jugando); // restaura header + comodines

                int monedas = EconomiaManager.Instance?.CalcularRecompensaAd(resultado.monedasRecomendadas) ?? 0;
                if (monedas > 0)
                {
                    AnimadorMonedas.AnimarDesdeCentro(monedas);
                    EconomiaManager.Instance?.GanarMonedas(monedas);
                }
            }
            else
            {
                MostrarGameOver(alturaMaxima >= metaAlturaNivel);
            }
        });
    }

    /// <summary>
    /// Watchdog del anuncio de rescate. Si el break no responde, concedemos el
    /// rescate al jugador: el fallo es nuestro, no suyo, y dejarle en GameOver
    /// tras pedirle que viera un anuncio sería castigarle por un bug.
    /// </summary>
    IEnumerator WatchdogRescate(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);

        if (_adResuelto) yield break;
        _adResuelto = true;

        Debug.LogWarning("[GameManager] El AR Break de rescate no respondió. Concediendo el rescate.");

        Time.timeScale = 1;
        yaUsoRescate   = true;
        tiempoRestante = 30f;
        if (miGrua != null) miGrua.ActivarGrúa();
        CambiarEstado(EstadoJuego.Jugando);
    }

    // ── Mostrar resultado final ───────────────────────────────────────────────

    void MostrarGameOver(bool gano)
    {
        estadoActual = EstadoJuego.GameOver;
        _enRescate   = false;

        if (miGrua != null) miGrua.DesactivarGrúa();
        ComodinesManager.Instance?.TerminarPartida();

        if (barraProgreso != null)
        {
            Destroy(barraProgreso.gameObject);
            barraProgreso = null;
        }

        if (panelJuegoHUD) panelJuegoHUD.SetActive(false);

        // ── AR Ad Break automático (cada N derrotas) ──────────────────────────
        // Decidimos aquí si toca anuncio, pero NO retornamos: la economía y el
        // guardado de la partida tienen que ejecutarse siempre. Solo se difiere
        // la aparición del panel de GameOver hasta que el anuncio termine.
        bool mostrarAd = false;

        if (!gano && adCadaNDerrotas > 0)
        {
            s_derrotasSinAd++;

            // Nunca tras un rescate: si en esta partida ya se le ofreció (o vio)
            // el anuncio de rescate, el jugador ya tomó su decisión sobre anuncios.
            // Mostrarle otro a los dos segundos de decir "no" es lo que más irrita.
            bool elegible = !_adMostradoEstaPartida && !_adOfrecidoEnRescate;

            if (elegible && s_derrotasSinAd >= adCadaNDerrotas && LayeredAds.EstaListo())
            {
                s_derrotasSinAd = 0;
                mostrarAd = true;
            }
        }
        if (gano) s_derrotasSinAd = 0;

        // Si toca anuncio el panel aparece después, al terminar el break
        if (!mostrarAd && panelGameOver) panelGameOver.SetActive(true);

        // Estado final
        if (textoEstadoFinal)
        {
            textoEstadoFinal.text  = gano ? "LEVEL COMPLETE!" : "TIME'S UP!";
            textoEstadoFinal.color = gano ? Color.green : Color.red;
        }

        // Récord personal, medido en CUBOS: en metros dependía del tamaño de la
        // plataforma (con cubos más grandes cualquier torre batía el récord)
        float alturaCubos = alturaMaxima / (LevelManager.CUBO * Mathf.Max(0.01f, SetupFase.EscalaSeleccionada));
        bool nuevoRecord  = alturaCubos > datosJugador.mejorAlturaCubos + 0.01f && alturaCubos >= 1f;
        if (nuevoRecord)
        {
            datosJugador.mejorAlturaCubos = alturaCubos;
            datosJugador.mejorAltura      = alturaMaxima;
        }

        if (textoRecordFinal)
        {
            textoRecordFinal.text  = nuevoRecord ? "NEW RECORD!"
                                    : "Best: " + Mathf.FloorToInt(datosJugador.mejorAlturaCubos) + " cubes";
            textoRecordFinal.color = nuevoRecord ? new Color(1f, 0.84f, 0f) : Color.white;
        }

        // Rating — imágenes de estrellas + texto de respaldo
        int estrellas = CalcularEstrellas();
        {

            // Imágenes (si el usuario las asignó en el Inspector)
            if (imagenesEstrellas != null)
            {
                Color colorOro    = new Color(1f, 0.84f, 0f);
                Color colorPlata  = new Color(0.75f, 0.75f, 0.75f);
                Color colorBronce = new Color(0.80f, 0.50f, 0.20f);
                Color colorVacio  = new Color(0.25f, 0.25f, 0.25f, 0.5f);

                for (int i = 0; i < imagenesEstrellas.Length; i++)
                {
                    if (imagenesEstrellas[i] == null) continue;
                    bool activa = i < estrellas;
                    imagenesEstrellas[i].gameObject.SetActive(true); // siempre visible, cambia color
                    imagenesEstrellas[i].color = activa
                        ? (estrellas == 3 ? colorOro : estrellas == 2 ? colorPlata : colorBronce)
                        : colorVacio;
                }
            }

            // Texto de respaldo (útil si no hay imágenes asignadas)
            if (textoEstrellas)
            {
                string[] ratings = { "No rating", "Bronze", "Silver", "GOLD!" };
                textoEstrellas.text  = ratings[estrellas];
                textoEstrellas.color = estrellas == 3 ? new Color(1f, 0.84f, 0f)
                                     : estrellas == 2 ? new Color(0.75f, 0.75f, 0.75f)
                                     : estrellas == 1 ? new Color(0.80f, 0.50f, 0.20f)
                                     :                  Color.gray;
            }
        }

        if (textoAlturaFinal)  textoAlturaFinal.text  = alturaMaxima.ToString("F2") + " m";
        if (textoCubosFinal)   textoCubosFinal.text    = cubosUsados.ToString();
        if (textoStatsFinales) textoStatsFinales.text  = "Goal: " + metaAlturaNivel + "m";

        // ── Economía ─────────────────────────────────────────────────────────
        int monedasGanadas = 0;

        // ¿Es la primera vez que el jugador completa este nivel?
        bool primerVez = LevelManager.NivelSeleccionado >= datosJugador.nivelMaximoDesbloqueado;

        // Gemas: se calculan antes de guardar las estrellas (la regla de 3
        // estrellas compara con el mejor resultado anterior del nivel)
        var motivosGemas = new System.Collections.Generic.List<string>();
        int gemasGanadas = CalcularGemas(gano, primerVez, estrellas, nuevoRecord, motivosGemas);
        if (gano) GuardarEstrellas(LevelManager.NivelSeleccionado, estrellas);

        if (gano)
        {
            // Avanzar el nivel máximo (sin retroceder si repite un nivel anterior)
            datosJugador.nivelMaximoDesbloqueado = Mathf.Max(
                datosJugador.nivelMaximoDesbloqueado,
                LevelManager.NivelSeleccionado + 1);

            monedasGanadas = EconomiaManager.MONEDAS_POR_GANAR;
            if (nuevoRecord) monedasGanadas += EconomiaManager.MONEDAS_BONUS_RECORD;

            // Animar ANTES de sumar: la animación retiene las monedas y el
            // contador sube con cada una que llega, en vez de saltar al total
            AnimadorMonedas.AnimarDesdeCentro(monedasGanadas);
            EconomiaManager.Instance?.GanarMonedas(monedasGanadas);
            MensajeFlotante.MonedasGanadas(monedasGanadas);
        }

        // Las gemas se suman ya (nada se pierde si se cierra la app), pero su
        // animación y el mensaje con el motivo llegan después de las monedas
        if (gemasGanadas > 0)
        {
            AnimadorMonedas.AnimarGemasDesdeCentro(gemasGanadas, RetrasoGemas);
            EconomiaManager.Instance?.GanarGemas(gemasGanadas);
            StartCoroutine(MensajeGemasTras(RetrasoGemas, gemasGanadas, string.Join(" · ", motivosGemas)));
        }

        if (!gano)
        {
            // La vida ya se cobró al iniciar la partida (IniciarPartida).
            // El escudo no evita el cobro: lo compensa devolviendo la vida.
            bool tieneEscudo = ComodinesManager.Instance?.ConsumeEscudo() ?? false;
            if (tieneEscudo)
            {
                EconomiaManager.Instance?.GanarVida(1);
                MensajeFlotante.Mostrar("Shield saved your life!", new Color(0.30f, 0.80f, 1f), 2f);
            }
        }

        if (textoMonedasGanadas)
        {
            textoMonedasGanadas.gameObject.SetActive(gano && monedasGanadas > 0);
            textoMonedasGanadas.text = "+" + monedasGanadas + " coins";
        }

        // ── Progreso ──────────────────────────────────────────────────────────
        datosJugador.totalCubosUsadosHistorico += cubosUsados;
        SaveSystem.Guardar(datosJugador);

        // Verificación de guardado (visible en Logcat en Android)
        Debug.Log($"[GameManager] Guardado → nivel max desbloqueado: {datosJugador.nivelMaximoDesbloqueado} | Ganó: {gano}");


        // Botón Siguiente Nivel
        if (botonSiguienteNivel != null)
        {
            bool haySiguiente = LevelManager.Instance != null &&
                                LevelManager.NivelSeleccionado + 1 < LevelManager.Instance.niveles.Length;
            botonSiguienteNivel.SetActive(gano && haySiguiente);
        }

        // ── Anuncio diferido ──────────────────────────────────────────────────
        // La partida ya está contabilizada, guardada y con el panel preparado.
        // El anuncio se muestra ahora sobre pantalla limpia y, al terminar,
        // revela el panel de GameOver.
        if (mostrarAd) MostrarAdTrasGameOver();
    }

    // ── Anuncio posterior al GameOver ─────────────────────────────────────────

    /// <summary>
    /// Muestra el AR Break sobre pantalla limpia y revela el GameOver al terminar.
    /// Incluye un watchdog: si el SDK no devuelve el callback (error interno,
    /// tracking perdido), el juego se recupera solo en lugar de quedarse congelado.
    /// </summary>
    void MostrarAdTrasGameOver()
    {
        PlayerHeaderUI.Mostrar(false);
        if (panelJuegoHUD)  panelJuegoHUD.SetActive(false);
        if (panelGameOver)  panelGameOver.SetActive(false);
        Time.timeScale = 0;

        _adResuelto = false;
        StartCoroutine(WatchdogAd(SegundosMaxAd));

        LayeredAds.MostrarBreak(resultado =>
        {
            if (_adResuelto) return;   // el watchdog ya cerró el break
            _adResuelto = true;

            int monedas = EconomiaManager.Instance?.CalcularRecompensaAd(resultado.monedasRecomendadas) ?? 0;
            CerrarAdYMostrarGameOver();   // primero el header visible, luego la animación

            if (monedas > 0)
            {
                AnimadorMonedas.AnimarDesdeCentro(monedas);
                EconomiaManager.Instance?.GanarMonedas(monedas);
            }
        });
    }

    /// <summary>
    /// Red de seguridad: si pasados N segundos el break no ha devuelto callback,
    /// restauramos el juego por nuestra cuenta. Sin esto, un fallo del SDK deja
    /// Time.timeScale en 0 y la partida injugable.
    /// </summary>
    IEnumerator WatchdogAd(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);

        if (_adResuelto) yield break;
        _adResuelto = true;

        Debug.LogWarning("[GameManager] El AR Break no respondió a tiempo. Recuperando el juego.");
        CerrarAdYMostrarGameOver();
    }

    void CerrarAdYMostrarGameOver()
    {
        Time.timeScale = 1;
        PlayerHeaderUI.Mostrar(true);
        if (panelGameOver) panelGameOver.SetActive(true);
    }

    // ── Botones ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Punto de entrada único a una partida. Comprueba que el jugador tenga vidas
    /// antes de dejarle entrar al setup AR.
    /// La vida NO se descuenta aquí sino en IniciarPartida(), cuando la partida
    /// arranca de verdad: si el jugador abandona durante el setup no pierde nada.
    /// </summary>
    public void BotonIrASetup()
    {
        if (!HayVidasParaJugar()) return;
        CambiarEstado(EstadoJuego.Setup);
    }

    /// <summary>
    /// True si el jugador puede empezar una partida. Si no le quedan vidas, avisa
    /// y le abre la tienda en la pestaña de vidas.
    /// </summary>
    bool HayVidasParaJugar()
    {
        var eco = EconomiaManager.Instance;
        if (eco == null) return true;   // sin economía cargada no bloqueamos el juego
        if (eco.TieneVidas()) return true;

        MensajeFlotante.Mostrar("No lives left!", new Color(0.91f, 0.27f, 0.27f), 2f);
        TiendaManager.Instance?.AbrirTiendaVidas();
        return false;
    }
    public void BotonPausar()       => CambiarEstado(EstadoJuego.Pausa);
    public void BotonReanudar()     => CambiarEstado(EstadoJuego.Jugando);

    public void BotonReintentar()
    {
        // Sin esto, LevelManager.Awake() vuelve al nivel 1 al recargar la escena
        LevelManager.preservarNivel = true;
        saltarDirectoASetup = true;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BotonMenu()
    {
        saltarDirectoASetup = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Tienda ────────────────────────────────────────────────────────────────

    /// <summary>Botón "Tienda" desde menú principal o pausa.</summary>
    public void BotonAbrirTienda()
    {
        TiendaManager.Instance?.AbrirTienda();
    }

    /// <summary>Botón "Comprar Vidas" desde GameOver cuando el jugador no tiene vidas.</summary>
    public void BotonAbrirTiendaVidas()
    {
        TiendaManager.Instance?.AbrirTiendaVidas();
    }

    public void BotonSalir()
    {
        saltarDirectoASetup = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BotonSiguienteNivel()
    {
        if (LevelManager.Instance == null) return;
        int siguiente = LevelManager.NivelSeleccionado + 1;
        if (siguiente >= LevelManager.Instance.niveles.Length) return;

        LevelManager.preservarNivel    = true;
        LevelManager.NivelSeleccionado = siguiente;

        DatosNivel datos = LevelManager.Instance.niveles[siguiente];
        metaAlturaNivel  = datos.metaAltura;
        tiempoLimite     = datos.tiempoLimite;

        saltarDirectoASetup = true;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── DEBUG — solo para probar en el Editor ────────────────────────────────
    // Click derecho en el componente GameManager en el Inspector → aparece el menú

    [ContextMenu("DEBUG: Probar AR Ad ahora")]
    void DEBUG_ProbarAd()
    {
        // Inicializa con key de prueba para poder testear el flujo completo
        LayeredAds.Inicializar("DEBUG_TEST_KEY");
        if (adConfigDefault != null)
            LayeredAds.SetAdConfig(adConfigDefault);
        else
        {
            Debug.LogWarning("[DEBUG Ad] Asigna un ARAdConfig al campo 'Ad Config Default' en el Inspector.");
            return;
        }
        LayeredAds.MostrarBreak(resultado => {
            Debug.Log($"[DEBUG Ad] Resultado → {resultado}");
            if (resultado.monedasRecomendadas > 0)
                EconomiaManager.Instance?.GanarMonedas(resultado.monedasRecomendadas);
        });
    }

    [ContextMenu("DEBUG: Dar recursos para probar")]
    void DEBUG_DarRecursos()
    {
        if (datosJugador == null) datosJugador = SaveSystem.Cargar();

        datosJugador.monedas = 999;
        datosJugador.gemas   = 50;
        datosJugador.vidas   = 5;

        // 5 de cada comodín: [0]=Snap [1]=Tiempo [2]=Plomo [3]=Escudo
        if (datosJugador.comodinesInventario == null || datosJugador.comodinesInventario.Length < 4)
            datosJugador.comodinesInventario = new int[4];

        for (int i = 0; i < 4; i++)
            datosJugador.comodinesInventario[i] = 5;

        SaveSystem.Guardar(datosJugador);
        Debug.Log("✅ DEBUG: 999 monedas, 50 gemas, 5 vidas, 5 de cada comodín. Recarga la escena para ver cambios.");
    }

    [ContextMenu("DEBUG: Borrar guardado (reset completo)")]
    void DEBUG_BorrarGuardado()
    {
        string path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "jugador.json");
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log("✅ DEBUG: Guardado borrado. Recarga la escena para empezar de cero.");
        }
        else
        {
            Debug.Log("ℹ️ DEBUG: No había archivo de guardado.");
        }
    }

    [ContextMenu("DEBUG: Mostrar ruta del guardado")]
    void DEBUG_MostrarRuta()
    {
        Debug.Log("📁 Ruta del archivo: " + System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "jugador.json"));
    }
}

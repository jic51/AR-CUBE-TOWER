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

    private int _derrotasConsecutivas = 0;

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
    [Tooltip("Segundos sin ver la cima antes de bajar la plataforma")]
    public float pozSegundosSinCima     = 2f;
    [Tooltip("Gracia en segundos tras un cubo que cae — no baja durante este tiempo")]
    public float pozSegundosGraciaCaida = 8f;
    [Tooltip("Cooldown mínimo entre descensos consecutivos")]
    public float pozoCooldown           = 3f;

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
                datosJugador.ultimoDiaJugado = hoy;
                EconomiaManager.Instance?.GanarMonedas(30);
                SaveSystem.Guardar(datosJugador);
                // El mensaje se muestra cuando el menú ya está visible (0.5s delay)
                StartCoroutine(MostrarBonusDiarioTras(0.8f));
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
            CambiarEstado(EstadoJuego.Setup);
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
        basePlataforma          = plataforma;
        alturaInicialPlataforma = plataforma.position.y;
        pozoTargetY         = plataforma.position.y;
        pozoUltimoDescenso  = Time.time;
        tiempoSinVerCima    = 0f;
        tiempoDesdeCaida    = float.MaxValue; // empieza sin "cubo caído reciente"
        tiempoRestante          = tiempoLimite > 0 ? tiempoLimite : 999f;
        cubosUsados              = 0;
        alturaMaxima             = 0;
        yaUsoRescate             = false;
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void RegistrarCuboLanzado()
    {
        cubosUsados++;
        if (cubosMaximos > 0 && cubosUsados >= cubosMaximos)
            tiempoRestante = 0;
    }

    public bool PuedeLanzarCubo()
    {
        // False si se agotaron los cubos del nivel
        return cubosMaximos == 0 || cubosUsados < cubosMaximos;
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

        // ¿Está el retículo sobre la cima de la torre?
        float cubeSize   = 0.15f * SetupFase.EscalaSeleccionada;
        float alturaCima = basePlataforma.position.y + alturaMaxima;
        bool reticuloEnCima = miGrua.ReticuloVisible &&
                              miGrua.ReticuloAlturaHit >= alturaCima - cubeSize * 1.8f;

        if (reticuloEnCima)
        {
            tiempoSinVerCima = 0f;  // el usuario ve bien la cima → reset
        }
        else
        {
            tiempoSinVerCima += Time.deltaTime;
        }

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

    System.Collections.IEnumerator MostrarBonusDiarioTras(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        MensajeFlotante.Mostrar("Daily bonus  +30 coins!", new Color(0.95f, 0.80f, 0.10f), 2.5f);
    }

    System.Collections.IEnumerator MensajeGemasTras(float segundos, int cantidad)
    {
        yield return new WaitForSecondsRealtime(segundos);
        MensajeFlotante.GemasGanadas(cantidad);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void ActualizarUI()
    {
        // Timer
        if (textoTiempo)
        {
            int s = Mathf.CeilToInt(tiempoRestante);
            textoTiempo.text  = s + "s";
            textoTiempo.color = tiempoRestante <= 10f ? Color.red : Color.white;
        }

        if (textoAltura) textoAltura.text = alturaMaxima.ToString("F2") + " m";

        // Cubos: si hay límite mostramos "Usados: 5/12", si no "Cubos: 5"
        if (textoCubos)
        {
            textoCubos.text = cubosMaximos > 0
                ? "Cubos: " + cubosUsados + "/" + cubosMaximos
                : "Cubos: " + cubosUsados;
        }
        if (textoCubosRestantes && cubosMaximos > 0)
        {
            int restantes = Mathf.Max(0, cubosMaximos - cubosUsados);
            textoCubosRestantes.text  = "Restantes: " + restantes;
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
        if (miGrua != null) miGrua.DesactivarGrúa();

        // Mostrar panel de rescate
        if (panelRescate) panelRescate.SetActive(true);

        // Configurar textos del panel
        if (textoPrecioRescate)
            textoPrecioRescate.text = EconomiaManager.PRECIO_RESCATE_MONEDAS + " monedas";

        if (textoMonedasEnRescate && EconomiaManager.Instance != null)
            textoMonedasEnRescate.text = "Tienes: " + EconomiaManager.Instance.Monedas;

        if (botonComprarRescate && EconomiaManager.Instance != null)
            botonComprarRescate.interactable =
                EconomiaManager.Instance.TieneMonedas(EconomiaManager.PRECIO_RESCATE_MONEDAS);

        // Botón "Ver anuncio gratis" — solo visible si el SDK AR está listo
        if (botonAdRescate != null)
            botonAdRescate.gameObject.SetActive(LayeredAds.EstaListo());

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

        LayeredAds.MostrarBreak(resultado =>
        {
            Time.timeScale = 1; // restaurar tiempo antes de cualquier otra acción

            if (resultado.vioCompleto || resultado.hizoClic)
            {
                yaUsoRescate   = true;
                tiempoRestante = 30f;
                if (miGrua != null) miGrua.ActivarGrúa();
                CambiarEstado(EstadoJuego.Jugando); // restaura header + comodines

                if (resultado.monedasRecomendadas > 0)
                    EconomiaManager.Instance?.GanarMonedas(resultado.monedasRecomendadas);
            }
            else
            {
                MostrarGameOver(alturaMaxima >= metaAlturaNivel);
            }
        });
    }

    // ── Mostrar resultado final ───────────────────────────────────────────────

    void MostrarGameOver(bool gano)
    {
        estadoActual = EstadoJuego.GameOver;

        if (miGrua != null) miGrua.DesactivarGrúa();
        ComodinesManager.Instance?.TerminarPartida();

        if (barraProgreso != null)
        {
            Destroy(barraProgreso.gameObject);
            barraProgreso = null;
        }

        if (panelJuegoHUD) panelJuegoHUD.SetActive(false);

        // ── AR Ad Break automático (cada N derrotas) ──────────────────────────
        if (!gano && adCadaNDerrotas > 0)
        {
            _derrotasConsecutivas++;
            if (_derrotasConsecutivas >= adCadaNDerrotas && LayeredAds.EstaListo())
            {
                _derrotasConsecutivas = 0;

                // Pantalla limpia para el ad: sin paneles ni header superpuestos
                PlayerHeaderUI.Mostrar(false);
                if (panelJuegoHUD) panelJuegoHUD.SetActive(false);
                Time.timeScale = 0; // pausa física durante el ad

                LayeredAds.MostrarBreak(resultado =>
                {
                    Time.timeScale = 1;
                    if (resultado.monedasRecomendadas > 0)
                        EconomiaManager.Instance?.GanarMonedas(resultado.monedasRecomendadas);
                    // Ahora sí mostrar GameOver con header visible
                    PlayerHeaderUI.Mostrar(true);
                    if (panelGameOver) panelGameOver.SetActive(true);
                });
                return;
            }
        }
        if (gano) _derrotasConsecutivas = 0;

        if (panelGameOver) panelGameOver.SetActive(true);

        // Estado final
        if (textoEstadoFinal)
        {
            textoEstadoFinal.text  = gano ? "¡NIVEL COMPLETADO!" : "¡TIEMPO AGOTADO!";
            textoEstadoFinal.color = gano ? Color.green : Color.red;
        }

        // Récord personal
        bool nuevoRecord = alturaMaxima > datosJugador.mejorAltura;
        if (nuevoRecord) datosJugador.mejorAltura = alturaMaxima;

        if (textoRecordFinal)
        {
            textoRecordFinal.text  = nuevoRecord ? "¡NUEVO RÉCORD!"
                                    : "Récord: " + datosJugador.mejorAltura.ToString("F2") + " m";
            textoRecordFinal.color = nuevoRecord ? new Color(1f, 0.84f, 0f) : Color.white;
        }

        // Rating — imágenes de estrellas + texto de respaldo
        {
            float ratio    = metaAlturaNivel > 0 ? alturaMaxima / metaAlturaNivel : 0;
            int estrellas  = ratio >= 1f ? 3 : ratio >= 0.66f ? 2 : ratio >= 0.33f ? 1 : 0;

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
                string[] ratings = { "Sin rating", "Bronce", "Plata", "ORO!" };
                textoEstrellas.text  = ratings[estrellas];
                textoEstrellas.color = estrellas == 3 ? new Color(1f, 0.84f, 0f)
                                     : estrellas == 2 ? new Color(0.75f, 0.75f, 0.75f)
                                     : estrellas == 1 ? new Color(0.80f, 0.50f, 0.20f)
                                     :                  Color.gray;
            }
        }

        if (textoAlturaFinal)  textoAlturaFinal.text  = alturaMaxima.ToString("F2") + " m";
        if (textoCubosFinal)   textoCubosFinal.text    = cubosUsados.ToString();
        if (textoStatsFinales) textoStatsFinales.text  = "Meta: " + metaAlturaNivel + "m";

        // ── Economía ─────────────────────────────────────────────────────────
        int monedasGanadas = 0;
        int gemasGanadas   = 0;

        if (gano)
        {
            // ¿Es la primera vez que el jugador completa este nivel?
            bool primerVez = LevelManager.NivelSeleccionado >= datosJugador.nivelMaximoDesbloqueado;
            // Avanzar el nivel máximo (sin retroceder si repite un nivel anterior)
            datosJugador.nivelMaximoDesbloqueado = Mathf.Max(
                datosJugador.nivelMaximoDesbloqueado,
                LevelManager.NivelSeleccionado + 1);

            monedasGanadas = EconomiaManager.MONEDAS_POR_GANAR;
            if (nuevoRecord) monedasGanadas += EconomiaManager.MONEDAS_BONUS_RECORD;

            // GEMAS: primera vez este nivel = +2, nuevo récord de altura = +1
            if (primerVez)   gemasGanadas += 2;
            if (nuevoRecord) gemasGanadas += EconomiaManager.GEMAS_POR_RECORD;

            EconomiaManager.Instance?.GanarMonedas(monedasGanadas);
            MensajeFlotante.MonedasGanadas(monedasGanadas);
            AnimadorMonedas.AnimarDesdeCentro(monedasGanadas);

            if (gemasGanadas > 0)
            {
                EconomiaManager.Instance?.GanarGemas(gemasGanadas);
                StartCoroutine(MensajeGemasTras(1.2f, gemasGanadas));
            }
        }
        else
        {
            bool tieneEscudo = ComodinesManager.Instance?.ConsumeEscudo() ?? false;
            if (!tieneEscudo) EconomiaManager.Instance?.GastarVida();
        }

        if (textoMonedasGanadas)
        {
            textoMonedasGanadas.gameObject.SetActive(gano && monedasGanadas > 0);
            textoMonedasGanadas.text = "+" + monedasGanadas + " monedas";
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
    }

    // ── Botones ───────────────────────────────────────────────────────────────

    public void BotonIrASetup()     => CambiarEstado(EstadoJuego.Setup);
    public void BotonPausar()       => CambiarEstado(EstadoJuego.Pausa);
    public void BotonReanudar()     => CambiarEstado(EstadoJuego.Jugando);

    public void BotonReintentar()
    {
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

using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Gestiona el panel de la tienda: tabs, compras, feedback visual.
/// Accesible desde: menú principal, pausa y pantalla de GameOver (solo la tab de vidas).
///
/// Estructura UI esperada:
///   PanelTienda
///   ├── Header
///   │   ├── TextoMonedas       → saldo actual
///   │   ├── TextoGemas         → saldo actual
///   │   ├── BotonTabVidas      → muestra ContenidoVidas
///   │   └── BotonTabComodines  → muestra ContenidoComodines
///   ├── ContenidoVidas         → (panel que se activa/desactiva según tab)
///   │   ├── FilaVida1          → 1 vida, 50 monedas
///   │   └── FilaVidaPack       → 3 vidas, 120 monedas
///   ├── ContenidoComodines     → (panel que se activa/desactiva según tab)
///   │   ├── FilaSnap           → Snap Perfecto, 50 monedas
///   │   ├── FilaTiempo         → +30s, 75 monedas
///   │   ├── FilaPlomo          → Cubo Plomo, 60 monedas
///   │   └── FilaEscudo         → Escudo, 80 monedas
///   ├── TextoFeedback          → "¡Compra exitosa!" / "Not enough coins"
///   └── BotonCerrar
/// </summary>
public class TiendaManager : MonoBehaviour
{
    public static TiendaManager Instance;

    // ── Panel principal ──────────────────────────────────────────────────────
    [Header("Panel")]
    public GameObject panelTienda;

    // ── Header — saldo ───────────────────────────────────────────────────────
    [Header("Header — Saldo")]
    public TextMeshProUGUI textoMonedas;
    public TextMeshProUGUI textoGemas;
    public TextMeshProUGUI textoVidasDisponibles;

    // ── Tabs ──────────────────────────────────────────────────────────────────
    [Header("Tabs")]
    public GameObject contenidoVidas;
    public GameObject contenidoComodines;
    public Image imagenTabVidas;       // para cambiar color al seleccionar
    public Image imagenTabComodines;

    // ── Botones de compra — Vidas ────────────────────────────────────────────
    [Header("Botones — Vidas")]
    public Button botonComprarVida1;       // 1 vida, 50 monedas
    public Button botonComprarVidaPack;    // 3 vidas, 120 monedas
    public TextMeshProUGUI textoVidasActuales; // muestra "2 / 5"

    [Tooltip("Texto del precio de 1 vida. Se rellena desde código para que nunca difiera de lo que se cobra.")]
    public TextMeshProUGUI textoPrecioVida1;
    [Tooltip("Texto del precio del pack de vidas. Se rellena desde código.")]
    public TextMeshProUGUI textoPrecioVidaPack;

    // ── Botones de compra — Comodines ─────────────────────────────────────────
    [Header("Botones — Comodines")]
    public Button botonComprarSnap;    // 50 monedas
    public Button botonComprarTiempo;  // 75 monedas
    public Button botonComprarPlomo;   // 60 monedas
    public Button botonComprarEscudo;  // 80 monedas

    // Badges de inventario en la tienda (cuántos tiene ya)
    [Header("Stock actual (badges en botones de tienda)")]
    public TextMeshProUGUI textoStockSnap;
    public TextMeshProUGUI textoStockTiempo;
    public TextMeshProUGUI textoStockPlomo;
    public TextMeshProUGUI textoStockEscudo;

    // ── Feedback ──────────────────────────────────────────────────────────────
    [Header("Feedback")]
    public TextMeshProUGUI textoFeedback;
    [Tooltip("Segundos que se muestra el mensaje de feedback")]
    public float duracionFeedback = 2f;

    // ── Colores tab activo/inactivo ───────────────────────────────────────────
    private readonly Color colorTabActivo   = new Color(0.91f, 0.27f, 0.38f, 1f); // coral
    private readonly Color colorTabInactivo = new Color(0.25f, 0.25f, 0.35f, 1f); // gris oscuro

    private Coroutine coroutineFeedback;

    // Delegates para poder desuscribirse correctamente
    private System.Action<int> _onMonedas;
    private System.Action<int> _onGemas;
    private System.Action<int> _onVidas;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (panelTienda) panelTienda.SetActive(false);

        // Esclavizar los textos de saldo a EconomiaManager:
        // cada vez que cambia una moneda/gema/vida, la tienda se actualiza sola.
        _onMonedas = _ => { if (panelTienda != null && panelTienda.activeSelf) ActualizarUI(); };
        _onGemas   = _ => { if (panelTienda != null && panelTienda.activeSelf) ActualizarUI(); };
        _onVidas   = _ => { if (panelTienda != null && panelTienda.activeSelf) ActualizarUI(); };

        if (EconomiaManager.Instance != null)
        {
            EconomiaManager.Instance.OnMonedasCambiadas += _onMonedas;
            EconomiaManager.Instance.OnGemasCambiadas   += _onGemas;
            EconomiaManager.Instance.OnVidasCambiadas   += _onVidas;
        }
    }

    void OnDestroy()
    {
        if (EconomiaManager.Instance != null)
        {
            EconomiaManager.Instance.OnMonedasCambiadas -= _onMonedas;
            EconomiaManager.Instance.OnGemasCambiadas   -= _onGemas;
            EconomiaManager.Instance.OnVidasCambiadas   -= _onVidas;
        }
    }

    // ── Abrir / Cerrar ────────────────────────────────────────────────────────

    /// <summary>Abre la tienda mostrando la tab de comodines (caso general).</summary>
    public void AbrirTienda()
    {
        AbrirTiendaEnTab(TabTienda.Comodines);
    }

    /// <summary>Abre la tienda directamente en la tab de Vidas (desde GameOver sin vidas).</summary>
    public void AbrirTiendaVidas()
    {
        AbrirTiendaEnTab(TabTienda.Vidas);
    }

    public void CerrarTienda()
    {
        if (panelTienda) panelTienda.SetActive(false);
        // Restaurar el botón del header a modo pausa
        PlayerHeaderUI.ModoBotonCerrar(false);
    }

    private enum TabTienda { Vidas, Comodines }

    private void AbrirTiendaEnTab(TabTienda tab)
    {
        if (panelTienda) panelTienda.SetActive(true);

        // El botón de pausa del header se convierte en botón "Cerrar tienda"
        PlayerHeaderUI.ModoBotonCerrar(true);

        ActualizarUI();
        MostrarTab(tab);
    }

    // ── Tabs ──────────────────────────────────────────────────────────────────

    public void SeleccionarTabVidas()     => MostrarTab(TabTienda.Vidas);
    public void SeleccionarTabComodines() => MostrarTab(TabTienda.Comodines);

    private void MostrarTab(TabTienda tab)
    {
        bool esVidas = tab == TabTienda.Vidas;

        if (contenidoVidas)      contenidoVidas.SetActive(esVidas);
        if (contenidoComodines)  contenidoComodines.SetActive(!esVidas);

        if (imagenTabVidas)      imagenTabVidas.color      = esVidas ? colorTabActivo : colorTabInactivo;
        if (imagenTabComodines)  imagenTabComodines.color  = esVidas ? colorTabInactivo : colorTabActivo;
    }

    // ── Actualización de UI ───────────────────────────────────────────────────

    void ActualizarUI()
    {
        var eco = EconomiaManager.Instance;
        if (eco == null) return;

        // Header — saldo
        if (textoMonedas)         textoMonedas.text         = eco.Monedas.ToString();
        if (textoGemas)           textoGemas.text            = eco.Gemas.ToString();
        if (textoVidasDisponibles) textoVidasDisponibles.text = eco.Vidas + " / " + EconomiaManager.MAX_VIDAS;

        // Tab Vidas
        if (textoVidasActuales)   textoVidasActuales.text   = eco.Vidas + " / " + EconomiaManager.MAX_VIDAS;

        if (textoPrecioVida1)    textoPrecioVida1.text    = EconomiaManager.PRECIO_VIDA_MONEDAS.ToString();
        if (textoPrecioVidaPack) textoPrecioVidaPack.text = EconomiaManager.PRECIO_PACK_VIDAS_MONEDAS.ToString();

        int hueco = EconomiaManager.MAX_VIDAS - eco.Vidas;
        if (botonComprarVida1)
            botonComprarVida1.interactable    = hueco >= 1 && eco.TieneMonedas(EconomiaManager.PRECIO_VIDA_MONEDAS);
        // El pack solo se ofrece si caben las 3 vidas: si no, se pagaría de más
        if (botonComprarVidaPack)
            botonComprarVidaPack.interactable = hueco >= EconomiaManager.VIDAS_POR_PACK
                                             && eco.TieneMonedas(EconomiaManager.PRECIO_PACK_VIDAS_MONEDAS);

        // Tab Comodines — stock en inventario
        if (textoStockSnap)   textoStockSnap.text   = "x" + eco.ObtenerComodin(0);
        if (textoStockTiempo) textoStockTiempo.text  = "x" + eco.ObtenerComodin(1);
        if (textoStockPlomo)  textoStockPlomo.text   = "x" + eco.ObtenerComodin(2);
        if (textoStockEscudo) textoStockEscudo.text  = "x" + eco.ObtenerComodin(3);

        // Botones de comodines: grayed-out si no hay monedas
        if (botonComprarSnap)   botonComprarSnap.interactable   = eco.TieneMonedas(EconomiaManager.PRECIO_SNAP_MONEDAS);
        if (botonComprarTiempo) botonComprarTiempo.interactable  = eco.TieneMonedas(EconomiaManager.PRECIO_TIEMPO_MONEDAS);
        if (botonComprarPlomo)  botonComprarPlomo.interactable   = eco.TieneMonedas(EconomiaManager.PRECIO_PLOMO_MONEDAS);
        if (botonComprarEscudo) botonComprarEscudo.interactable  = eco.TieneMonedas(EconomiaManager.PRECIO_ESCUDO_MONEDAS);
    }

    // ── Compras — Vidas ──────────────────────────────────────────────────────

    public void ComprarVida1()
    {
        if (EconomiaManager.Instance == null) return;

        if (EconomiaManager.Instance.Vidas >= EconomiaManager.MAX_VIDAS)
        { MostrarFeedback("Lives already full", false); return; }

        if (!EconomiaManager.Instance.GastarMonedas(EconomiaManager.PRECIO_VIDA_MONEDAS))
        { MostrarFeedback("Not enough coins", false); return; }

        EconomiaManager.Instance.GanarVida(1);
        MostrarFeedback("+1 life!", true);
        ActualizarUI();
    }

    public void ComprarVidaPack()
    {
        if (EconomiaManager.Instance == null) return;

        // Validar ANTES de cobrar: con 4 vidas el pack cobraba 120 y entregaba 1
        int hueco = EconomiaManager.MAX_VIDAS - EconomiaManager.Instance.Vidas;
        if (hueco < EconomiaManager.VIDAS_POR_PACK)
        { MostrarFeedback(hueco <= 0 ? "Lives already full" : $"Only room for {hueco} more", false); return; }

        if (!EconomiaManager.Instance.GastarMonedas(EconomiaManager.PRECIO_PACK_VIDAS_MONEDAS))
        { MostrarFeedback("Not enough coins", false); return; }

        EconomiaManager.Instance.GanarVida(EconomiaManager.VIDAS_POR_PACK);
        MostrarFeedback("+3 lives!", true);
        ActualizarUI();
    }

    // ── Compras — Comodines ───────────────────────────────────────────────────

    public void ComprarSnap()
    {
        if (!EconomiaManager.Instance.GastarMonedas(EconomiaManager.PRECIO_SNAP_MONEDAS))
        { MostrarFeedback("Not enough coins", false); return; }

        EconomiaManager.Instance.GanarComodin(0);
        MostrarFeedback("Perfect Snap purchased!", true);
        ActualizarUI();
    }

    public void ComprarTiempoExtra()
    {
        if (!EconomiaManager.Instance.GastarMonedas(EconomiaManager.PRECIO_TIEMPO_MONEDAS))
        { MostrarFeedback("Not enough coins", false); return; }

        EconomiaManager.Instance.GanarComodin(1);
        MostrarFeedback("+30s purchased!", true);
        ActualizarUI();
    }

    public void ComprarCuboPlomo()
    {
        if (!EconomiaManager.Instance.GastarMonedas(EconomiaManager.PRECIO_PLOMO_MONEDAS))
        { MostrarFeedback("Not enough coins", false); return; }

        EconomiaManager.Instance.GanarComodin(2);
        MostrarFeedback("Heavy Cube purchased!", true);
        ActualizarUI();
    }

    public void ComprarEscudo()
    {
        if (!EconomiaManager.Instance.GastarMonedas(EconomiaManager.PRECIO_ESCUDO_MONEDAS))
        { MostrarFeedback("Not enough coins", false); return; }

        EconomiaManager.Instance.GanarComodin(3);
        MostrarFeedback("Shield purchased!", true);
        ActualizarUI();
    }

    // ── Feedback visual ───────────────────────────────────────────────────────

    void MostrarFeedback(string mensaje, bool exito)
    {
        if (textoFeedback == null) return;

        if (coroutineFeedback != null) StopCoroutine(coroutineFeedback);
        coroutineFeedback = StartCoroutine(AnimarFeedback(mensaje, exito));
    }

    System.Collections.IEnumerator AnimarFeedback(string mensaje, bool exito)
    {
        textoFeedback.text  = mensaje;
        textoFeedback.color = exito ? new Color(0.22f, 0.90f, 0.32f) : new Color(0.91f, 0.27f, 0.27f);
        textoFeedback.gameObject.SetActive(true);

        yield return new UnityEngine.WaitForSecondsRealtime(duracionFeedback);

        textoFeedback.gameObject.SetActive(false);
        coroutineFeedback = null;
    }
}

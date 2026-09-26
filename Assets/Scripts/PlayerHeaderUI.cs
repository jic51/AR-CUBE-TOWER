using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Header persistente que muestra monedas, gemas y vidas en cualquier panel.
/// Se actualiza automáticamente via eventos de EconomiaManager.
///
/// SETUP en Unity Editor:
///   1. Crea un Panel pequeño horizontal en el CANVAS RAÍZ (no dentro de ningún subpanel).
///      Posición: esquina superior derecha. Siempre activo.
///   2. Dentro: 3 grupos icon+texto: monedas, gemas, vidas.
///   3. Agrega este script al panel raíz.
///   4. Asigna los campos en el Inspector.
///   5. En GameManager, añade referencia a este panel y ocúltalo solo en el menú inicial
///      si prefieres que no aparezca ahí.
/// </summary>
public class PlayerHeaderUI : MonoBehaviour
{
    public static PlayerHeaderUI Instance;

    [Header("Monedas")]
    public TextMeshProUGUI textoMonedas;
    public Image           iconoMonedas;

    [Header("Gemas")]
    public TextMeshProUGUI textoGemas;
    public Image           iconoGemas;

    [Header("Vidas")]
    public TextMeshProUGUI textoVidas;
    public Image           iconoVidas;

    [Header("Nombre de usuario (opcional)")]
    public TextMeshProUGUI textoNombre;
    public Image           imagenAvatar;

    [Header("Botón de acción contextual")]
    [Tooltip("El botón de pausa / cerrar — el mismo botón, dos comportamientos.")]
    public UnityEngine.UI.Button botonAccion;
    [Tooltip("Ícono de pausa — visible durante el juego.")]
    public GameObject iconoPausa;
    [Tooltip("Ícono de cerrar (✕ o ←) — visible cuando hay un panel encima (tienda, etc.).")]
    public GameObject iconoCerrar;

    // Contexto actual del botón
    private enum ContextoBoton { Pausa, CerrarPanel }
    private ContextoBoton _contextoBoton = ContextoBoton.Pausa;

    // Monedas ya sumadas en EconomiaManager pero que la animación todavía no ha
    // "entregado". El contador muestra (total real − en vuelo), así sube moneda a
    // moneda mientras vuelan en lugar de saltar al total de golpe. La economía
    // real no se retrasa: se guarda al instante, esto es solo lo que se ve.
    private static int s_monedasEnVuelo;

    /// <summary>Monedas que el contador muestra en este momento.</summary>
    public static int MonedasMostradas =>
        Mathf.Max(0, (EconomiaManager.Instance?.Monedas ?? 0) - s_monedasEnVuelo);

    /// <summary>Llamar ANTES de sumar monedas que se van a animar.</summary>
    public static void RetenerMonedas(int cantidad)
    {
        if (cantidad <= 0) return;
        s_monedasEnVuelo += cantidad;
        Instance?.Refrescar();
    }

    /// <summary>Llamar cuando llega cada moneda animada con su valor.</summary>
    public static void LiberarMonedas(int cantidad)
    {
        s_monedasEnVuelo = Mathf.Max(0, s_monedasEnVuelo - cantidad);
        Instance?.Refrescar();
    }

    // Mismo mecanismo para las gemas
    private static int s_gemasEnVuelo;

    public static int GemasMostradas =>
        Mathf.Max(0, (EconomiaManager.Instance?.Gemas ?? 0) - s_gemasEnVuelo);

    public static void RetenerGemas(int cantidad)
    {
        if (cantidad <= 0) return;
        s_gemasEnVuelo += cantidad;
        Instance?.Refrescar();
    }

    public static void LiberarGemas(int cantidad)
    {
        s_gemasEnVuelo = Mathf.Max(0, s_gemasEnVuelo - cantidad);
        Instance?.Refrescar();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Una recarga de escena corta cualquier animación a medias
        s_monedasEnVuelo = 0;
        s_gemasEnVuelo   = 0;
    }

    // Delegates guardados como campos para poder desuscribirse correctamente
    private System.Action<int> _onMonedas;
    private System.Action<int> _onGemas;
    private System.Action<int> _onVidas;

    void Start()
    {
        _onMonedas = _ => Refrescar();
        _onGemas   = _ => Refrescar();
        _onVidas   = _ => Refrescar();

        if (EconomiaManager.Instance != null)
        {
            EconomiaManager.Instance.OnMonedasCambiadas += _onMonedas;
            EconomiaManager.Instance.OnGemasCambiadas   += _onGemas;
            EconomiaManager.Instance.OnVidasCambiadas   += _onVidas;
        }
        Refrescar();
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

    /// <summary>Fuerza una actualización visual inmediata.</summary>
    public void Refrescar()
    {
        if (EconomiaManager.Instance == null) return;

        if (textoMonedas) textoMonedas.text = MonedasMostradas.ToString();
        if (textoGemas)   textoGemas.text   = GemasMostradas.ToString();
        if (textoVidas)
        {
            textoVidas.text  = EconomiaManager.Instance.Vidas + "/" + EconomiaManager.MAX_VIDAS;
            textoVidas.color = EconomiaManager.Instance.Vidas <= 1 ? Color.red : Color.white;
        }

        // Nombre e icono desde PlayerData
        var datos = GameManager.Instance?.GetDatos();
        if (datos != null)
        {
            if (textoNombre) textoNombre.text = datos.nombreUsuario;
        }
    }

    /// <summary>
    /// Muestra u oculta el header. Llamar desde GameManager.CambiarEstado().
    /// </summary>
    public static void Mostrar(bool visible)
    {
        if (Instance != null) Instance.gameObject.SetActive(visible);
    }

    // ── Botón contextual ──────────────────────────────────────────────────────

    /// <summary>
    /// Cambia el botón entre modo Pausa (⏸) y modo Cerrar (✕ / ←).
    /// Llamar desde TiendaManager al abrir/cerrar la tienda.
    /// </summary>
    public static void ModoBotonCerrar(bool activar)
    {
        if (Instance == null) return;
        Instance._contextoBoton = activar ? ContextoBoton.CerrarPanel : ContextoBoton.Pausa;
        Instance.RefrescarIconoBoton();
    }

    void RefrescarIconoBoton()
    {
        bool esCerrar = _contextoBoton == ContextoBoton.CerrarPanel;
        if (iconoPausa  != null) iconoPausa.SetActive(!esCerrar);
        if (iconoCerrar != null) iconoCerrar.SetActive(esCerrar);
    }

    /// <summary>
    /// Conectar este método al onClick del botón en el Inspector.
    /// Reemplaza la conexión directa a GameManager.BotonPausar().
    /// </summary>
    public void OnBotonAccionClick()
    {
        switch (_contextoBoton)
        {
            case ContextoBoton.Pausa:
                GameManager.Instance?.BotonPausar();
                break;

            case ContextoBoton.CerrarPanel:
                TiendaManager.Instance?.CerrarTienda();
                break;
        }
    }
}

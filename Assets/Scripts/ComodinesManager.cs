using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Gestiona el estado activo de los comodines durante una partida
/// y su representación en el HUD (panel izquierdo con botones circulares).
///
/// Slots:
///   0 = Snap Perfecto  — el próximo cubo que se suelte va exacto al centro
///   1 = Tiempo Extra   — añade 30s al timer (efecto inmediato)
///   2 = Cubo Plomo     — el próximo cubo pesa 80 kg, aplana la torre
///   3 = Escudo         — la próxima derrota no cuesta una vida
/// </summary>
public class ComodinesManager : MonoBehaviour
{
    public static ComodinesManager Instance;

    [Header("Panel HUD (izquierdo)")]
    public GameObject panelComodines;

    [Header("Botones de comodín (4 elementos)")]
    public Button[]             botonesComodin;   // 4 botones circulares
    public TextMeshProUGUI[]    textoCantidad;     // Badge con cantidad disponible
    public Image[]              imagenBoton;       // Para cambiar el color al activarse

    [Header("Indicadores de estado activo")]
    public GameObject indicadorSnapActivo;    // Icono/borde que brilla cuando snap está activo
    public GameObject indicadorEscudoActivo;  // Icono/borde para escudo
    public GameObject indicadorPlomoPendiente;// Icono/borde para cubo plomo

    // ── Estado activo durante la partida ────────────────────────────────────
    private bool snapPerfectoActivo   = false;
    private bool escudoActivo         = false;
    private bool cuboPlomoPendiente   = false;

    // Propiedades públicas de solo lectura para otros scripts
    public bool SnapPerfectoActivo    => snapPerfectoActivo;
    public bool EscudoActivo          => escudoActivo;
    public bool CuboPlomoPendiente    => cuboPlomoPendiente;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>Llamar desde GameManager.IniciarPartida() para preparar el panel.</summary>
    public void IniciarPartida()
    {
        ResetearEstadosActivos();
        ActualizarBadges();
        MostrarPanel(true);
    }

    /// <summary>Llamar desde GameManager.FinalizarJuego() para ocultar el panel.</summary>
    public void TerminarPartida()
    {
        DevolverNoUsados();
        MostrarPanel(false);
        ResetearEstadosActivos();
    }

    /// <summary>
    /// Devuelve al inventario los comodines que se activaron pero nunca llegaron
    /// a usarse. El jugador los pagó: quedarse con un escudo sin gastar porque
    /// ganó la partida sería cobrarle por nada.
    /// </summary>
    void DevolverNoUsados()
    {
        var eco = EconomiaManager.Instance;
        if (eco == null) return;

        if (snapPerfectoActivo) eco.GanarComodin(0);
        if (cuboPlomoPendiente) eco.GanarComodin(2);
        if (escudoActivo)       eco.GanarComodin(3);
    }

    public void MostrarPanel(bool mostrar)
    {
        if (panelComodines) panelComodines.SetActive(mostrar);
    }

    // ── Actualización de la UI ───────────────────────────────────────────────

    void ActualizarBadges()
    {
        if (EconomiaManager.Instance == null) return;

        for (int i = 0; i < 4; i++)
        {
            int cantidad = EconomiaManager.Instance.ObtenerComodin(i);

            if (textoCantidad != null && i < textoCantidad.Length && textoCantidad[i] != null)
                textoCantidad[i].text = cantidad.ToString();

            // Atenuar el botón si no hay unidades disponibles
            if (botonesComodin != null && i < botonesComodin.Length && botonesComodin[i] != null)
                botonesComodin[i].interactable = cantidad > 0;
        }

        // Indicadores de estado activo (objetos opcionales del Editor)
        if (indicadorSnapActivo)    indicadorSnapActivo.SetActive(snapPerfectoActivo);
        if (indicadorEscudoActivo)  indicadorEscudoActivo.SetActive(escudoActivo);
        if (indicadorPlomoPendiente) indicadorPlomoPendiente.SetActive(cuboPlomoPendiente);

        // Respaldo que no depende del Editor: los tres indicadores de arriba
        // están SIN ASIGNAR en la escena, así que hasta ahora no había ninguna
        // señal de que un comodín estuviera armado. El propio botón se ilumina.
        PintarBoton(0, snapPerfectoActivo, ColorSnap);
        PintarBoton(2, cuboPlomoPendiente, ColorPlomo);
        PintarBoton(3, escudoActivo,       ColorEscudo);
    }

    // Color base de cada botón, guardado una sola vez (si se leyera cada vez,
    // acabaría guardando el color de "armado" como si fuera el normal)
    private readonly System.Collections.Generic.Dictionary<int, Color> _colorBase = new();

    void PintarBoton(int slot, bool armado, Color color)
    {
        if (imagenBoton == null || slot >= imagenBoton.Length || imagenBoton[slot] == null) return;

        if (!_colorBase.ContainsKey(slot)) _colorBase[slot] = imagenBoton[slot].color;
        imagenBoton[slot].color = armado ? color : _colorBase[slot];

        if (armado) IniciarLatido(slot);
    }

    private readonly System.Collections.Generic.Dictionary<int, Coroutine> _latidos = new();

    void IniciarLatido(int slot)
    {
        if (_latidos.TryGetValue(slot, out var c) && c != null) return;   // ya late
        _latidos[slot] = StartCoroutine(Latir(slot));
    }

    /// <summary>Latido suave del botón mientras su comodín siga armado.</summary>
    System.Collections.IEnumerator Latir(int slot)
    {
        var img = imagenBoton[slot];
        Transform t = img.transform;
        Vector3 escalaBase = t.localScale;

        while (SlotArmado(slot) && img != null)
        {
            float p = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 5f);
            t.localScale = escalaBase * p;
            yield return null;
        }
        if (t != null) t.localScale = escalaBase;
        _latidos[slot] = null;
    }

    bool SlotArmado(int slot) => slot switch
    {
        0 => snapPerfectoActivo,
        2 => cuboPlomoPendiente,
        3 => escudoActivo,
        _ => false,
    };

    // ── Activación de comodines (llamados por los botones del HUD) ───────────

    // Colores de aviso al activar cada comodín
    private static readonly Color ColorSnap   = new Color(0.25f, 0.85f, 1.00f);
    private static readonly Color ColorTiempo = new Color(0.40f, 0.90f, 0.50f);
    private static readonly Color ColorPlomo  = new Color(0.75f, 0.78f, 0.85f);
    private static readonly Color ColorEscudo = new Color(0.45f, 0.70f, 1.00f);

    /// <summary>Slot 0: Snap Perfecto — el próximo drop va exacto al centro.</summary>
    public void ActivarSnapPerfecto()
    {
        if (EconomiaManager.Instance == null) return;
        // Ya armado: no gastar otro. Antes se consumía y se perdía.
        if (snapPerfectoActivo) { NotificacionesUI.Comodin("Perfect Snap already armed"); return; }
        if (!EconomiaManager.Instance.UsarComodin(0)) return;

        snapPerfectoActivo = true;
        ActualizarBadges();
        NotificacionesUI.Comodin("Perfect Snap armed", "next drop");
    }

    /// <summary>Slot 1: +30 segundos — efecto inmediato en el timer.</summary>
    public void ActivarTiempoExtra()
    {
        if (EconomiaManager.Instance == null) return;
        // En niveles sin reloj no hay tiempo que añadir: no dejar gastarlo
        if (GameManager.Instance != null && !GameManager.Instance.TieneReloj)
        { NotificacionesUI.Aviso("This level has no timer"); return; }
        if (!EconomiaManager.Instance.UsarComodin(1)) return;

        GameManager.Instance?.AgregarTiempo(30f);
        ActualizarBadges();
        NotificacionesUI.Tiempo(30);
    }

    /// <summary>Slot 2: Cubo Plomo — el próximo cubo es de plomo y pesa 80.</summary>
    public void ActivarCuboPlomo()
    {
        if (EconomiaManager.Instance == null) return;
        if (cuboPlomoPendiente) { NotificacionesUI.Comodin("Heavy Cube already queued"); return; }
        if (!EconomiaManager.Instance.UsarComodin(2)) return;

        cuboPlomoPendiente = true;
        ActualizarBadges();
        NotificacionesUI.Comodin("Heavy Cube", "next block");
    }

    /// <summary>Slot 3: Escudo — la próxima derrota no cuesta vida.</summary>
    public void ActivarEscudo()
    {
        if (EconomiaManager.Instance == null) return;
        if (escudoActivo) { NotificacionesUI.Comodin("Shield already active"); return; }
        if (!EconomiaManager.Instance.UsarComodin(3)) return;

        escudoActivo = true;
        ActualizarBadges();
        NotificacionesUI.Comodin("Shield active", "keeps your life");
    }

    // ── Consumo de estados (llamados por GruaController/GameManager) ─────────

    /// <summary>Consume el snap perfecto. Llamar antes de SoltarCubo() en GruaController.</summary>
    public bool ConsumeSnapPerfecto()
    {
        if (!snapPerfectoActivo) return false;
        snapPerfectoActivo = false;
        ActualizarBadges();
        return true;
    }

    /// <summary>Consume el cubo plomo pendiente. GruaController lo llama al generar nuevo cubo.</summary>
    public bool ConsumeCuboPlomo()
    {
        if (!cuboPlomoPendiente) return false;
        cuboPlomoPendiente = false;
        ActualizarBadges();
        return true;
    }

    /// <summary>Consume el escudo. GameManager lo llama antes de descontar vida al perder.</summary>
    public bool ConsumeEscudo()
    {
        if (!escudoActivo) return false;
        escudoActivo = false;
        ActualizarBadges();
        return true;
    }

    private void ResetearEstadosActivos()
    {
        snapPerfectoActivo  = false;
        escudoActivo        = false;
        cuboPlomoPendiente  = false;

        // Devolver los botones a su color y tamaño: si el panel se desactiva con
        // un latido en marcha, la corrutina se corta y el botón se quedaría
        // agrandado y coloreado la próxima partida
        foreach (var kv in _colorBase)
            if (imagenBoton != null && kv.Key < imagenBoton.Length && imagenBoton[kv.Key] != null)
            {
                imagenBoton[kv.Key].color = kv.Value;
                imagenBoton[kv.Key].transform.localScale = Vector3.one;
            }
        _latidos.Clear();
    }
}

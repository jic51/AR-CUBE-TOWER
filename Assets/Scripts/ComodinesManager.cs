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
        MostrarPanel(false);
        ResetearEstadosActivos();
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

        // Indicadores de estado activo
        if (indicadorSnapActivo)    indicadorSnapActivo.SetActive(snapPerfectoActivo);
        if (indicadorEscudoActivo)  indicadorEscudoActivo.SetActive(escudoActivo);
        if (indicadorPlomoPendiente) indicadorPlomoPendiente.SetActive(cuboPlomoPendiente);
    }

    // ── Activación de comodines (llamados por los botones del HUD) ───────────

    /// <summary>Slot 0: Snap Perfecto — el próximo drop va exacto al centro.</summary>
    public void ActivarSnapPerfecto()
    {
        if (EconomiaManager.Instance == null) return;
        if (!EconomiaManager.Instance.UsarComodin(0)) return;

        snapPerfectoActivo = true;
        ActualizarBadges();
        Debug.Log("[Comodin] Snap Perfecto activado");
    }

    /// <summary>Slot 1: +30 segundos — efecto inmediato en el timer.</summary>
    public void ActivarTiempoExtra()
    {
        if (EconomiaManager.Instance == null) return;
        if (!EconomiaManager.Instance.UsarComodin(1)) return;

        GameManager.Instance?.AgregarTiempo(30f);
        ActualizarBadges();
        Debug.Log("[Comodin] +30s activado");
    }

    /// <summary>Slot 2: Cubo Plomo — el próximo cubo tiene masa 80 y cae rápido.</summary>
    public void ActivarCuboPlomo()
    {
        if (EconomiaManager.Instance == null) return;
        if (!EconomiaManager.Instance.UsarComodin(2)) return;

        cuboPlomoPendiente = true;
        ActualizarBadges();
        Debug.Log("[Comodin] Cubo Plomo pendiente");
    }

    /// <summary>Slot 3: Escudo — la próxima derrota no cuesta vida.</summary>
    public void ActivarEscudo()
    {
        if (EconomiaManager.Instance == null) return;
        if (!EconomiaManager.Instance.UsarComodin(3)) return;

        escudoActivo = true;
        ActualizarBadges();
        Debug.Log("[Comodin] Escudo activado");
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
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de configuración / perfil accesible desde el botón de settings del menú principal.
///
/// SETUP en Unity Editor:
///   1. Crea un Panel sobre el Canvas raíz (z-order encima de panelMenuPrincipal).
///   2. Agrega este script al panel raíz de configuración.
///   3. Asigna los campos en el Inspector.
///   4. El botón de settings del menú → onClick → PanelConfiguracion.AbrirConfiguracion()
///   5. Un botón "Cerrar" o "←" dentro del panel → onClick → PanelConfiguracion.CerrarConfiguracion()
///
/// CAMPOS QUE MUESTRA:
///   - Nombre del jugador (TMP_InputField editable, guarda al salir del campo)
///   - Avatar actual (imagen, abrir AvatarPickerUI al tocar)
///   - Sonido on/off (toggle)
///   - Vibración on/off (toggle)
///   - Vidas actuales (solo lectura)
///   - Monedas y gemas (solo lectura)
///   - Botón borrar datos (con confirmación)
/// </summary>
public class PanelConfiguracion : MonoBehaviour
{
    public static PanelConfiguracion Instance;

    [Header("Panel raíz")]
    public GameObject panelConfiguracion;

    [Header("Perfil")]
    public TMP_InputField inputNombre;        // nombre del jugador
    public Image          imagenAvatarConfig; // imagen del avatar (toca para cambiar)
    public TextMeshProUGUI textoVidasConfig;  // "Vidas: 3 / 5"
    public TextMeshProUGUI textoMonedasConfig;
    public TextMeshProUGUI textoGemasConfig;

    [Header("Opciones")]
    public Toggle toggleSonido;
    public Toggle toggleVibracion;

    // ── Claves PlayerPrefs para opciones ────────────────────────────────────────
    private const string KEY_SONIDO     = "cfg_sonido";
    private const string KEY_VIBRACION  = "cfg_vibracion";

    // ─────────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);

        // Cargar preferencias guardadas
        if (toggleSonido    != null) toggleSonido.isOn    = PlayerPrefs.GetInt(KEY_SONIDO,    1) == 1;
        if (toggleVibracion != null) toggleVibracion.isOn = PlayerPrefs.GetInt(KEY_VIBRACION, 1) == 1;

        // Listeners de toggles (guardan al cambiar)
        toggleSonido?.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt(KEY_SONIDO, v ? 1 : 0);
            AudioListener.pause = !v;
        });
        toggleVibracion?.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt(KEY_VIBRACION, v ? 1 : 0);
        });

        // Aplica estado guardado al arrancar
        AudioListener.pause = PlayerPrefs.GetInt(KEY_SONIDO, 1) == 0;

        // Guardar nombre cuando el jugador termina de escribir
        if (inputNombre != null)
            inputNombre.onEndEdit.AddListener(_ => GuardarNombre());
    }

    // ── Abrir / Cerrar ────────────────────────────────────────────────────────────

    public void AbrirConfiguracion()
    {
        if (panelConfiguracion == null) return;
        RellenarDatos();
        panelConfiguracion.SetActive(true);
    }

    public void CerrarConfiguracion()
    {
        GuardarNombre();  // guardar si el jugador estaba escribiendo
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
    }

    // ── Datos del perfil ──────────────────────────────────────────────────────────

    void RellenarDatos()
    {
        var datos = GameManager.Instance?.GetDatos();
        if (datos == null) return;

        if (inputNombre != null)
            inputNombre.text = datos.nombreUsuario;

        // Avatar
        var catalogo = GameManager.Instance?.catalogoAvatares;
        if (imagenAvatarConfig != null && catalogo != null && catalogo.Length > 0)
        {
            int id = Mathf.Clamp(datos.avatarId, 0, catalogo.Length - 1);
            imagenAvatarConfig.sprite = catalogo[id];
        }

        var eco = EconomiaManager.Instance;
        if (textoVidasConfig  != null) textoVidasConfig.text   = $"Vidas: {(eco?.Vidas ?? 0)} / {EconomiaManager.MAX_VIDAS}";
        if (textoMonedasConfig != null) textoMonedasConfig.text = $"Monedas: {eco?.Monedas ?? 0}";
        if (textoGemasConfig   != null) textoGemasConfig.text   = $"Gemas: {eco?.Gemas ?? 0}";
    }

    void GuardarNombre()
    {
        if (inputNombre == null || GameManager.Instance == null) return;
        GameManager.Instance.ActualizarNombreEnTiempoReal();
        // ActualizarNombreEnTiempoReal() lee del inputNombreMenu del GameManager,
        // pero en este panel tenemos nuestro propio input. Guardamos directamente:
        var datos = GameManager.Instance.GetDatos();
        if (datos != null && inputNombre.text.Length > 0)
        {
            datos.nombreUsuario = inputNombre.text;
            SaveSystem.Guardar(datos);
            // Actualizar HUD y menú también
            if (GameManager.Instance.textoNombreHUD != null)
                GameManager.Instance.textoNombreHUD.text = datos.nombreUsuario;
            if (GameManager.Instance.inputNombreMenu != null)
                GameManager.Instance.inputNombreMenu.text = datos.nombreUsuario;
        }
    }

    // ── Botón cambiar avatar ──────────────────────────────────────────────────────

    /// <summary>Toca la imagen del avatar → abre el picker.</summary>
    public void TocarAvatar()
    {
        AvatarPickerUI.Instance?.AbrirPicker();
    }

    // ── Botón borrar datos ────────────────────────────────────────────────────────

    [Header("Confirmación borrar datos")]
    public GameObject panelConfirmacionBorrar;  // panel con "¿Estás seguro?" y botones Sí/No

    public void BotonBorrarDatos()
    {
        if (panelConfirmacionBorrar != null) panelConfirmacionBorrar.SetActive(true);
    }

    public void ConfirmarBorrarDatos()
    {
        string path = System.IO.Path.Combine(
            UnityEngine.Application.persistentDataPath, "jugador.json");
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        PlayerPrefs.DeleteAll();

        // Recargar la escena para empezar desde cero
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void CancelarBorrarDatos()
    {
        if (panelConfirmacionBorrar != null) panelConfirmacionBorrar.SetActive(false);
    }
}

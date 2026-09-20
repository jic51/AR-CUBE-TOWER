using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel selector de avatar. El lápiz de panel_menu_principal llama AbrirPicker().
///
/// ESTRUCTURA UI esperada para el panel del picker:
///
///   PanelAvatarPicker  ← este GameObject (con este script)
///   └── ContenedorBotones  ← GridLayoutGroup con N botones de avatar
///       ├── BotonAvatar_0  ← Button + Image (sprite del avatar 0)
///       ├── BotonAvatar_1
///       └── ...
///
/// SETUP:
///   1. Crea un panel sobre el panel_menu_principal (puede ser hijo del Canvas raíz).
///   2. Agrega este script al root del panel.
///   3. Arrastra el panel al campo "panelPicker" del componente.
///   4. Arrastra el GridLayout al campo "contenedorBotones".
///   5. Arrastra el prefab de botón de avatar (un Button con Image) a "prefabBotonAvatar".
///   6. En el Inspector del lápiz: Button → onClick → AvatarPickerUI.AbrirPicker()
///   7. Asegúrate de que GameManager.catalogoAvatares tiene los sprites.
/// </summary>
public class AvatarPickerUI : MonoBehaviour
{
    public static AvatarPickerUI Instance;

    [Header("Panel del picker")]
    public GameObject panelPicker;

    [Header("Contenedor de botones (GridLayoutGroup)")]
    public Transform  contenedorBotones;

    [Header("Prefab de botón de avatar (Button + Image hijo)")]
    public GameObject prefabBotonAvatar;

    // Colores de selección
    private static readonly Color colorSeleccionado = new Color(0.91f, 0.27f, 0.38f, 1f); // coral
    private static readonly Color colorNormal       = new Color(1f, 1f, 1f, 1f);

    private int avatarSeleccionadoActual = -1;

    // ────────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (panelPicker != null) panelPicker.SetActive(false);
    }

    // ── API pública ──────────────────────────────────────────────────────────────

    /// <summary>Botón lápiz → abre el selector.</summary>
    public void AbrirPicker()
    {
        if (panelPicker == null) return;

        // Leer id actual del jugador
        avatarSeleccionadoActual = GameManager.Instance?.GetDatos()?.avatarId ?? 0;

        GenerarBotones();
        panelPicker.SetActive(true);
    }

    /// <summary>Botón cerrar / fondo oscuro del picker.</summary>
    public void CerrarPicker()
    {
        if (panelPicker != null) panelPicker.SetActive(false);
    }

    // ── Generación de botones ────────────────────────────────────────────────────

    void GenerarBotones()
    {
        if (contenedorBotones == null || prefabBotonAvatar == null) return;

        var catalogo = GameManager.Instance?.catalogoAvatares;
        if (catalogo == null || catalogo.Length == 0) return;

        // Limpiar botones anteriores
        foreach (Transform hijo in contenedorBotones)
            Destroy(hijo.gameObject);

        for (int i = 0; i < catalogo.Length; i++)
        {
            int indice = i;  // capturar para el closure
            GameObject botonGO = Instantiate(prefabBotonAvatar, contenedorBotones);

            // El sprite del avatar va en la Image del botón (o en un hijo Image)
            var imagen = botonGO.GetComponent<Image>()
                      ?? botonGO.GetComponentInChildren<Image>();
            if (imagen != null)
            {
                imagen.sprite = catalogo[indice];
                imagen.color  = (indice == avatarSeleccionadoActual) ? colorSeleccionado : colorNormal;
            }

            // Al hacer clic: seleccionar avatar y cerrar el picker
            var btn = botonGO.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    GameManager.Instance?.SeleccionarAvatar(indice);
                    CerrarPicker();
                });
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Panel compartido de UI Toolkit. Lo usan todas las piezas de la interfaz
/// nueva (notificaciones, botón de bajar, HUD) para no crear un panel por cada
/// una: varios paneles superpuestos se pelean por los toques y cuestan memoria.
///
/// Se monta solo la primera vez que alguien pide la raíz. No hay que crear nada
/// en la escena ni asignar campos en el Inspector.
///
/// Las capas se apilan en el orden en que se piden; cada una decide si acepta
/// toques. Por defecto NADA los acepta, para no robárselos al juego: una capa
/// con botones tiene que pedirlo explícitamente.
/// </summary>
public class CapaUI : MonoBehaviour
{
    // Resolución de diseño: la misma con la que están hechas las maquetas
    private static readonly Vector2Int ResolucionDiseno = new Vector2Int(1080, 1920);

    private static CapaUI _instancia;
    private VisualElement _raiz;

    /// <summary>Raíz del panel compartido. Crea el panel si aún no existe.</summary>
    public static VisualElement Raiz => Instancia()._raiz;

    /// <summary>
    /// Crea una capa hija a pantalla completa.
    /// aceptaToques=false (lo normal) deja pasar los toques al juego.
    /// </summary>
    public static VisualElement NuevaCapa(string nombre, bool aceptaToques = false)
    {
        var capa = new VisualElement
        {
            name = nombre,
            pickingMode = aceptaToques ? PickingMode.Position : PickingMode.Ignore
        };
        // Clase, no estilo en línea: los estilos en línea mandan sobre el USS y
        // una capa no podría reposicionarse desde el tema
        capa.AddToClassList("capa-pantalla");
        Raiz.Add(capa);
        return capa;
    }

    static CapaUI Instancia()
    {
        if (_instancia != null) return _instancia;

        var go = new GameObject("CapaUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<CapaUI>();
        _instancia.Construir();
        return _instancia;
    }

    void Construir()
    {
        var ajustes = ScriptableObject.CreateInstance<PanelSettings>();
        ajustes.themeStyleSheet     = Resources.Load<ThemeStyleSheet>("UI/TemaRuntime");
        ajustes.scaleMode           = PanelScaleMode.ScaleWithScreenSize;
        ajustes.referenceResolution = ResolucionDiseno;
        ajustes.screenMatchMode     = PanelScreenMatchMode.MatchWidthOrHeight;
        ajustes.match               = 0.5f;
        ajustes.sortingOrder        = 100;   // por encima del Canvas actual

        if (ajustes.themeStyleSheet == null)
            Debug.LogWarning("[CapaUI] Falta Assets/Resources/UI/TemaRuntime.tss: " +
                             "la interfaz se verá sin estilo base.");

        var doc = gameObject.AddComponent<UIDocument>();
        doc.panelSettings = ajustes;

        _raiz = doc.rootVisualElement;
        _raiz.pickingMode = PickingMode.Ignore;

        var hoja = Resources.Load<StyleSheet>("UI/Tema");
        if (hoja != null) _raiz.styleSheets.Add(hoja);
        else Debug.LogWarning("[CapaUI] No se encontró Assets/Resources/UI/Tema.uss");
    }
}

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
    ///
    /// La capa NUNCA captura toques, y tampoco deben hacerlo los contenedores
    /// que se le cuelguen: solo los controles concretos (Button). Una capa a
    /// pantalla completa que captura se traga el toque de soltar el cubo — pasó
    /// el 2026-09-27 y dejó el juego injugable.
    /// </summary>
    public static VisualElement NuevaCapa(string nombre)
    {
        var capa = new VisualElement
        {
            name = nombre,
            pickingMode = PickingMode.Ignore
        };
        // Clase, no estilo en línea: los estilos en línea mandan sobre el USS y
        // una capa no podría reposicionarse desde el tema
        capa.AddToClassList("capa-pantalla");
        Raiz.Add(capa);
        return capa;
    }

    /// <summary>
    /// Posición de un elemento en coordenadas de PANTALLA, que es lo que
    /// entiende el animador de monedas (uGUI). Devuelve null si el elemento
    /// no está visible o el panel aún no tiene medidas.
    /// </summary>
    public static Vector2? PuntoPantalla(VisualElement el)
    {
        if (el?.panel == null) return null;
        if (el.resolvedStyle.display == DisplayStyle.None) return null;

        VisualElement raiz = el.panel.visualTree;
        float ancho = raiz.resolvedStyle.width, alto = raiz.resolvedStyle.height;
        if (ancho < 1f || alto < 1f) return null;

        Vector2 centro = el.worldBound.center;
        return new Vector2(centro.x * (Screen.width / ancho),
                           Screen.height - centro.y * (Screen.height / alto));
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

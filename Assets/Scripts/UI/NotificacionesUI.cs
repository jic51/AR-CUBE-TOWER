using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Notificaciones de recompensa, en UI Toolkit.
///
/// Una sola franja bajo el header: la notificación entra deslizándose desde la
/// derecha, se queda unos segundos y sale por donde vino. Monedas, gemas,
/// comodines, tiempo extra y avisos usan SIEMPRE el mismo sitio, así el jugador
/// no tiene que buscar dónde apareció el mensaje.
///
/// Se monta sola: no hay que crear nada en la escena ni asignar campos en el
/// Inspector. El primer aviso crea el panel. El estilo vive en
/// Assets/Resources/UI/Tema.uss — cambiarlo ahí lo cambia en todo el juego.
///
/// Uso:
///   NotificacionesUI.Monedas(50, "Level cleared");
///   NotificacionesUI.Gemas(2, "First clear");
///   NotificacionesUI.Aviso("Platform descending");
/// </summary>
public class NotificacionesUI : MonoBehaviour
{
    public enum Tipo { Monedas, Gemas, Vidas, Comodin, Tiempo, Aviso }

    // Cuánto se queda en pantalla, sin contar las transiciones de entrada y
    // salida (0.25 s cada una, definidas en el USS)
    private const float SegundosVisible = 4f;
    private const float SegundosTransicion = 0.25f;

    // Máximo en pantalla a la vez: más que esto tapa el juego
    private const int MaxSimultaneas = 3;

    private static NotificacionesUI _instancia;
    private VisualElement _capa;
    private readonly Queue<(Tipo tipo, string texto, string valor)> _cola = new();
    private int _visibles;
    private bool _procesando;

    // ── API ───────────────────────────────────────────────────────────────

    public static void Monedas(int cantidad, string motivo) =>
        Mostrar(Tipo.Monedas, motivo, "+" + cantidad);

    public static void Gemas(int cantidad, string motivo) =>
        Mostrar(Tipo.Gemas, motivo, "+" + cantidad);

    public static void Vidas(int cantidad, string motivo) =>
        Mostrar(Tipo.Vidas, motivo, (cantidad >= 0 ? "+" : "") + cantidad);

    public static void Comodin(string texto, string detalle = "") =>
        Mostrar(Tipo.Comodin, texto, detalle);

    public static void Tiempo(int segundos) =>
        Mostrar(Tipo.Tiempo, "Extra time", "+" + segundos + "s");

    public static void Aviso(string texto, string detalle = "") =>
        Mostrar(Tipo.Aviso, texto, detalle);

    public static void Mostrar(Tipo tipo, string texto, string valor = "")
    {
        if (string.IsNullOrEmpty(texto)) return;
        Instancia()._cola.Enqueue((tipo, texto, valor));
        Instancia().Procesar();
    }

    // ── Montaje ───────────────────────────────────────────────────────────

    static NotificacionesUI Instancia()
    {
        if (_instancia != null) return _instancia;

        var go = new GameObject("NotificacionesUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<NotificacionesUI>();
        _instancia.Construir();
        return _instancia;
    }

    void Construir()
    {
        // El panel se crea en código para no depender de assets montados a mano
        var ajustes = ScriptableObject.CreateInstance<PanelSettings>();
        ajustes.themeStyleSheet   = Resources.Load<ThemeStyleSheet>("UI/TemaRuntime");
        ajustes.scaleMode         = PanelScaleMode.ScaleWithScreenSize;
        ajustes.referenceResolution = new Vector2Int(1080, 1920);
        ajustes.screenMatchMode   = PanelScreenMatchMode.MatchWidthOrHeight;
        ajustes.match             = 0.5f;
        ajustes.sortingOrder      = 100;   // por encima del HUD actual

        if (ajustes.themeStyleSheet == null)
            Debug.LogWarning("[NotificacionesUI] Falta Assets/Resources/UI/TemaRuntime.tss: " +
                             "la interfaz se verá sin estilo base.");

        var doc = gameObject.AddComponent<UIDocument>();
        doc.panelSettings = ajustes;

        var raiz = doc.rootVisualElement;
        raiz.pickingMode = PickingMode.Ignore;   // no robar toques al juego

        var hoja = Resources.Load<StyleSheet>("UI/Tema");
        if (hoja != null) raiz.styleSheets.Add(hoja);
        else Debug.LogWarning("[NotificacionesUI] No se encontró Assets/Resources/UI/Tema.uss");

        _capa = new VisualElement { name = "capa-notificaciones", pickingMode = PickingMode.Ignore };
        _capa.AddToClassList("capa-notificaciones");
        raiz.Add(_capa);
    }

    // ── Cola ──────────────────────────────────────────────────────────────

    void Procesar()
    {
        if (_procesando) return;
        StartCoroutine(ProcesarCola());
    }

    IEnumerator ProcesarCola()
    {
        _procesando = true;

        while (_cola.Count > 0)
        {
            // Esperar hueco: encoladas de una en una, como pediste
            while (_visibles >= MaxSimultaneas) yield return null;

            var (tipo, texto, valor) = _cola.Dequeue();
            StartCoroutine(MostrarUna(tipo, texto, valor));

            // Un respiro entre notificaciones para que se lean por separado
            yield return new WaitForSecondsRealtime(0.35f);
        }

        _procesando = false;
    }

    IEnumerator MostrarUna(Tipo tipo, string texto, string valor)
    {
        _visibles++;

        var fila = new VisualElement { pickingMode = PickingMode.Ignore };
        fila.AddToClassList("notificacion");
        fila.AddToClassList(ClaseDe(tipo));

        var icono = new VisualElement();
        icono.AddToClassList("notificacion__icono");
        fila.Add(icono);

        var etiqueta = new Label(texto);
        etiqueta.AddToClassList("notificacion__texto");
        fila.Add(etiqueta);

        if (!string.IsNullOrEmpty(valor))
        {
            var val = new Label(valor);
            val.AddToClassList("notificacion__valor");
            fila.Add(val);
        }

        _capa.Add(fila);

        // Un fotograma con el estilo de reposo aplicado para que la transición
        // tenga desde dónde animar: sin esto aparece de golpe, ya colocada
        yield return null;
        fila.AddToClassList("notificacion--visible");

        // Realtime: el juego puede estar en pausa (timeScale 0) tras un anuncio
        yield return new WaitForSecondsRealtime(SegundosVisible);

        fila.RemoveFromClassList("notificacion--visible");
        yield return new WaitForSecondsRealtime(SegundosTransicion);

        fila.RemoveFromHierarchy();
        _visibles--;
    }

    static string ClaseDe(Tipo tipo) => tipo switch
    {
        Tipo.Monedas => "notificacion--monedas",
        Tipo.Gemas   => "notificacion--gemas",
        Tipo.Vidas   => "notificacion--vidas",
        Tipo.Comodin => "notificacion--comodin",
        Tipo.Tiempo  => "notificacion--tiempo",
        _            => "notificacion--aviso",
    };
}

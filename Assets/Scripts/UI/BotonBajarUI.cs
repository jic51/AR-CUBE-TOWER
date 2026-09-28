using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Botón "bajar la plataforma ahora", con anillo de enfriamiento.
///
/// Por qué existe: a partir de cierta altura el jugador tenía que quedarse
/// mirando la torre 3 o 4 segundos esperando a que la plataforma bajara sola,
/// y ese tiempo salía del reloj de la partida.
///
/// Por qué es GRATIS y no cuesta un recurso (decisión del 2026-09-27): si
/// bajar la plataforma costara algo, el jugador sentiría que le cobran por ver
/// su propia torre. El límite es el enfriamiento, no el dinero.
///
/// Se monta solo: lo crea GameManager al empezar la partida.
/// </summary>
public class BotonBajarUI : MonoBehaviour
{
    // Segundos entre usos. El anillo se llena durante este tiempo.
    public const float SegundosEnfriamiento = 8f;

    private static BotonBajarUI _instancia;

    private VisualElement _capa;
    private Button        _boton;
    private VisualElement _anillo;
    private float         _listoEn;     // Time.unscaledTime en que vuelve a estar listo
    private bool          _visible;

    public static void Mostrar(bool visible)
    {
        if (visible) Instancia()._Mostrar(true);
        else if (_instancia != null) _instancia._Mostrar(false);
    }

    /// <summary>Reinicia el enfriamiento al empezar una partida nueva.</summary>
    public static void Reiniciar()
    {
        if (_instancia != null) _instancia._listoEn = 0f;
    }

    static BotonBajarUI Instancia()
    {
        if (_instancia != null) return _instancia;

        var go = new GameObject("BotonBajarUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<BotonBajarUI>();
        _instancia.Construir();
        return _instancia;
    }

    void Construir()
    {
        // Esta capa SÍ acepta toques: tiene un botón
        _capa = CapaUI.NuevaCapa("capa-bajar", aceptaToques: true);
        _capa.AddToClassList("capa-bajar");

        _anillo = new VisualElement();
        _anillo.AddToClassList("bajar__anillo");
        _anillo.generateVisualContent += DibujarAnillo;

        _boton = new Button(Pulsar) { text = "" };
        _boton.AddToClassList("bajar__boton");
        _boton.tooltip = "Lower the platform";

        // Flecha hacia abajo dibujada con dos barras giradas: sin sprites, así
        // el botón no depende de ninguna imagen con licencia
        var flecha = new VisualElement();
        flecha.AddToClassList("bajar__flecha");
        _boton.Add(flecha);

        var contenedor = new VisualElement();
        contenedor.AddToClassList("bajar__contenedor");
        contenedor.Add(_anillo);
        contenedor.Add(_boton);
        _capa.Add(contenedor);

        _capa.style.display = DisplayStyle.None;
    }

    void _Mostrar(bool visible)
    {
        _visible = visible;
        if (_capa != null) _capa.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void Update()
    {
        if (!_visible || _boton == null) return;

        bool listo = Time.unscaledTime >= _listoEn;
        _boton.SetEnabled(listo);
        _boton.EnableInClassList("bajar__boton--listo", listo);

        // Redibujar el anillo mientras carga
        if (!listo) _anillo.MarkDirtyRepaint();
    }

    void Pulsar()
    {
        if (Time.unscaledTime < _listoEn) return;
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.BajarPlataformaAhora()) return;

        _listoEn = Time.unscaledTime + SegundosEnfriamiento;
        _anillo.MarkDirtyRepaint();
    }

    /// <summary>Anillo que se vacía mientras el botón está enfriando.</summary>
    void DibujarAnillo(MeshGenerationContext ctx)
    {
        float progreso = Time.unscaledTime >= _listoEn
            ? 1f
            : 1f - (_listoEn - Time.unscaledTime) / SegundosEnfriamiento;

        Rect r = ctx.visualElement.contentRect;
        if (r.width < 4f || r.height < 4f) return;

        var pintor = ctx.painter2D;
        float radio = Mathf.Min(r.width, r.height) * 0.5f - 3f;
        var centro = new Vector2(r.width * 0.5f, r.height * 0.5f);

        // Fondo del anillo
        pintor.strokeColor = new Color(1f, 1f, 1f, 0.16f);
        pintor.lineWidth = 5f;
        pintor.BeginPath();
        pintor.Arc(centro, radio, 0f, 360f);
        pintor.Stroke();

        if (progreso <= 0f) return;

        // Progreso, desde arriba y en sentido horario
        pintor.strokeColor = progreso >= 1f
            ? new Color(0.50f, 0.83f, 1f, 1f)      // listo: cian
            : new Color(0.50f, 0.83f, 1f, 0.55f);  // cargando: cian apagado
        pintor.lineWidth = 5f;
        pintor.BeginPath();
        pintor.Arc(centro, radio, -90f, -90f + 360f * progreso);
        pintor.Stroke();
    }
}

using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD de partida en UI Toolkit.
///
/// Lo que arregla del HUD viejo:
///   · Los paneles azules opacos tapaban la cámara. En AR eso mata el efecto:
///     aquí todo es vidrio translúcido y la mesa del jugador siempre se ve.
///   · No había forma de saber cuánto faltaba para la meta. Ahora hay una barra
///     de progreso y el conteo en CUBOS, que es como el jugador ve la torre.
///   · El próximo cubo era un icono sin nombre. Ahora dice qué es y qué hace.
///
/// Lee el estado del GameManager cada fotograma; no guarda nada propio. Se
/// monta solo, sin tocar la escena ni el Inspector.
/// </summary>
public class HudUI : MonoBehaviour
{
    private static HudUI _instancia;

    private VisualElement _capa;
    private Label _tiempo, _cubos, _monedas, _gemas, _vidas, _proxNombre, _proxDetalle;
    private VisualElement _barra, _proxColor;
    private bool _visible;

    public static void Mostrar(bool visible)
    {
        if (visible) Instancia()._Mostrar(true);
        else if (_instancia != null) _instancia._Mostrar(false);
    }

    static HudUI Instancia()
    {
        if (_instancia != null) return _instancia;
        var go = new GameObject("HudUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<HudUI>();
        _instancia.Construir();
        return _instancia;
    }

    void Construir()
    {
        _capa = CapaUI.NuevaCapa("capa-hud");
        _capa.AddToClassList("capa-hud");

        // ── Barra superior ────────────────────────────────────────────────
        var barraSup = new VisualElement { pickingMode = PickingMode.Ignore };
        barraSup.AddToClassList("hud__barra");

        var pausa = new Button(() => GameManager.Instance?.BotonPausar());
        pausa.AddToClassList("hud__pausa");
        pausa.tooltip = "Pause";
        var icoPausa = new VisualElement { pickingMode = PickingMode.Ignore };
        icoPausa.AddToClassList("hud__pausa-icono");
        pausa.Add(icoPausa);
        barraSup.Add(pausa);

        var centro = new VisualElement { pickingMode = PickingMode.Ignore };
        centro.AddToClassList("hud__centro");

        var fila = new VisualElement { pickingMode = PickingMode.Ignore };
        fila.AddToClassList("hud__fila");

        _cubos = new Label("0 of 0 cubes");
        _cubos.AddToClassList("hud__cubos");
        fila.Add(_cubos);

        _tiempo = new Label("0:00");
        _tiempo.AddToClassList("hud__tiempo");
        fila.Add(_tiempo);
        centro.Add(fila);

        var pista = new VisualElement { pickingMode = PickingMode.Ignore };
        pista.AddToClassList("hud__pista");
        _barra = new VisualElement { pickingMode = PickingMode.Ignore };
        _barra.AddToClassList("hud__progreso");
        pista.Add(_barra);
        centro.Add(pista);

        barraSup.Add(centro);
        _capa.Add(barraSup);

        // ── Recursos ──────────────────────────────────────────────────────
        var recursos = new VisualElement { pickingMode = PickingMode.Ignore };
        recursos.AddToClassList("hud__recursos");
        _monedas = ChipRecurso(recursos, "chip--monedas", "0");
        _gemas   = ChipRecurso(recursos, "chip--gemas", "0");
        _vidas   = ChipRecurso(recursos, "chip--vidas", "0");
        _capa.Add(recursos);

        // Las monedas y gemas animadas vuelan hasta estos contadores. El header
        // viejo se oculta en partida, así que sin esto volarían a un icono
        // invisible y el efecto se perdería.
        AnimadorMonedas.ObjetivoMonedasPantalla = () => PuntoPantalla(_monedas);
        AnimadorMonedas.ObjetivoGemasPantalla   = () => PuntoPantalla(_gemas);

        // ── Próximo cubo ──────────────────────────────────────────────────
        var prox = new VisualElement { pickingMode = PickingMode.Ignore };
        prox.AddToClassList("hud__proximo");

        _proxColor = new VisualElement { pickingMode = PickingMode.Ignore };
        _proxColor.AddToClassList("hud__proximo-color");
        _proxColor.generateVisualContent += DibujarCubo;
        prox.Add(_proxColor);

        var textos = new VisualElement { pickingMode = PickingMode.Ignore };
        textos.AddToClassList("hud__proximo-textos");
        var etiqueta = new Label("NEXT");
        etiqueta.AddToClassList("hud__proximo-etiqueta");
        _proxNombre = new Label("Normal");
        _proxNombre.AddToClassList("hud__proximo-nombre");
        _proxDetalle = new Label("");
        _proxDetalle.AddToClassList("hud__proximo-detalle");
        textos.Add(etiqueta); textos.Add(_proxNombre); textos.Add(_proxDetalle);
        prox.Add(textos);

        _capa.Add(prox);
        _capa.style.display = DisplayStyle.None;
    }

    static Label ChipRecurso(VisualElement padre, string claseColor, string valor)
    {
        var chip = new VisualElement { pickingMode = PickingMode.Ignore };
        chip.AddToClassList("chip");
        chip.AddToClassList(claseColor);

        var punto = new VisualElement { pickingMode = PickingMode.Ignore };
        punto.AddToClassList("chip__punto");
        chip.Add(punto);

        var etiqueta = new Label(valor) { pickingMode = PickingMode.Ignore };
        etiqueta.AddToClassList("chip__valor");
        chip.Add(etiqueta);

        padre.Add(chip);
        return etiqueta;
    }

    void _Mostrar(bool visible)
    {
        _visible = visible;
        if (_capa != null) _capa.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void LateUpdate()
    {
        if (!_visible) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Reloj: "--" en los niveles sin límite de tiempo
        if (!gm.TieneReloj && gm.TiempoRestante > 5f)
        {
            _tiempo.text = "--";
            _tiempo.RemoveFromClassList("hud__tiempo--urgente");
        }
        else
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(gm.TiempoRestante));
            _tiempo.text = (s / 60) + ":" + (s % 60).ToString("00");
            _tiempo.EnableInClassList("hud__tiempo--urgente", s <= 10);
        }

        // Cubos: en niveles con límite manda el límite; si no, la meta
        _cubos.text = gm.CubosMaximos > 0
            ? gm.CubosUsados + " of " + gm.CubosMaximos + " cubes"
            : gm.AlturaEnCubos + " of " + gm.MetaEnCubos + " cubes";

        _barra.style.width = Length.Percent(gm.ProgresoMeta * 100f);
        _barra.EnableInClassList("hud__progreso--meta", gm.ProgresoMeta >= 1f);

        var eco = EconomiaManager.Instance;
        if (eco != null)
        {
            _monedas.text = PlayerHeaderUI.MonedasMostradas.ToString();
            _gemas.text   = PlayerHeaderUI.GemasMostradas.ToString();
            _vidas.text   = eco.Vidas.ToString();
        }

        ActualizarProximo();
    }

    private Color _colorProximo = Color.clear;

    /// <summary>
    /// Dibuja el próximo cubo como un cubo isométrico de tres caras, el mismo
    /// de la landing. Un cuadrado de color plano no se leía como un cubo: con
    /// los tipos claros (Normal, Plumas) parecía un recuadro blanco vacío.
    /// </summary>
    void DibujarCubo(MeshGenerationContext ctx)
    {
        Rect r = ctx.visualElement.contentRect;
        if (r.width < 8f || r.height < 8f) return;

        Color baseCol = _colorProximo == Color.clear ? new Color(0.7f, 0.7f, 0.75f) : _colorProximo;
        baseCol.a = 1f;
        Color cara   = baseCol;
        Color arriba = new Color(Mathf.Min(1f, baseCol.r * 1.30f), Mathf.Min(1f, baseCol.g * 1.30f),
                                 Mathf.Min(1f, baseCol.b * 1.30f));
        Color derecha = new Color(baseCol.r * 0.62f, baseCol.g * 0.62f, baseCol.b * 0.62f);

        // Geometría del cubo isométrico de la landing (80 × 86), encajada aquí
        float k  = Mathf.Min(r.width / 80f, r.height / 86f);
        float cx = r.width * 0.5f;
        float cy = (r.height - 86f * k) * 0.5f;
        Vector2 P(float x, float y) => new Vector2(cx + x * k, cy + y * k);

        var p = ctx.painter2D;

        void Cara(Color c, params Vector2[] puntos)
        {
            p.fillColor = c;
            p.BeginPath();
            p.MoveTo(puntos[0]);
            for (int i = 1; i < puntos.Length; i++) p.LineTo(puntos[i]);
            p.ClosePath();
            p.Fill();
        }

        Cara(arriba,  P(0, 0),   P(40, 20), P(0, 40),  P(-40, 20));   // tapa
        Cara(cara,    P(-40, 20), P(0, 40), P(0, 86),  P(-40, 66));   // izquierda
        Cara(derecha, P(0, 40),  P(40, 20), P(40, 66), P(0, 86));     // derecha
    }

    /// <summary>
    /// Convierte la posición de un elemento de UI Toolkit a coordenadas de
    /// pantalla, que es lo que entiende el animador de monedas (uGUI).
    /// </summary>
    Vector2? PuntoPantalla(VisualElement el)
    {
        if (!_visible || el?.panel == null) return null;

        VisualElement raiz = el.panel.visualTree;
        float ancho = raiz.resolvedStyle.width, alto = raiz.resolvedStyle.height;
        if (ancho < 1f || alto < 1f) return null;

        Vector2 centro = el.worldBound.center;
        return new Vector2(centro.x * (Screen.width / ancho),
                           Screen.height - centro.y * (Screen.height / alto));
    }

    void ActualizarProximo()
    {
        var grua = GruaController.Instance;
        if (grua == null) return;

        ConfigTipoCubo cfg = grua.ObtenerConfig(grua.tipoProximo);
        if (cfg == null) return;

        if (_colorProximo != cfg.colorEnVuelo)
        {
            _colorProximo = cfg.colorEnVuelo;
            _proxColor.MarkDirtyRepaint();
        }
        _proxNombre.text  = cfg.nombreMostrar + " Block";
        _proxDetalle.text = cfg.descripcionJugador;
    }
}

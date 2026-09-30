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
    private Label _tiempo, _cubos, _monedas, _gemas, _vidas, _proxNombre, _proxDetalle, _aviso;
    private float _avisoHasta;
    private VisualElement _barra, _proxColor;
    private VisualElement[] _comodines;
    private Label[] _comodinCuenta;
    private bool _visible;
    private float _giroCubo;

    public static void Mostrar(bool visible)
    {
        if (visible) Instancia()._Mostrar(true);
        else if (_instancia != null) _instancia._Mostrar(false);
    }

    /// <summary>
    /// Un aviso corto en la cabecera: combos, meta alcanzada, cubo anclado.
    ///
    /// Es el único sitio donde se escriben estas cosas. Fuera de partida el
    /// HUD no existe, así que ahí caen en las notificaciones de la derecha en
    /// vez de perderse.
    /// </summary>
    public static void Aviso(string texto, Color color, float duracion = 1.6f)
    {
        if (string.IsNullOrEmpty(texto)) return;

        if (_instancia == null || !_instancia._visible)
        {
            NotificacionesUI.Aviso(texto);
            return;
        }

        _instancia._aviso.text        = texto;
        _instancia._aviso.style.color = color;
        _instancia._avisoHasta        = Time.unscaledTime + duracion;
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

        // Los avisos de la partida ("Aligned x3", "Goal reached") viven AQUÍ,
        // dentro de la cabecera. Antes eran texto suelto en medio de la
        // pantalla, encima de la torre: tapaban justo lo que el jugador
        // necesitaba mirar y no se leían sobre una mesa clara.
        _aviso = new Label("") { pickingMode = PickingMode.Ignore };
        _aviso.AddToClassList("hud__aviso");
        centro.Add(_aviso);

        barraSup.Add(centro);
        _capa.Add(barraSup);

        // ── Recursos ──────────────────────────────────────────────────────
        var recursos = new VisualElement { pickingMode = PickingMode.Ignore };
        recursos.AddToClassList("hud__recursos");
        _monedas = ChipRecurso(recursos, "chip--monedas", Iconos.Icono.Moneda, ColorOro);
        _gemas   = ChipRecurso(recursos, "chip--gemas",   Iconos.Icono.Gema,   ColorGema);
        _vidas   = ChipRecurso(recursos, "chip--vidas",   Iconos.Icono.Corazon, ColorVida);
        _capa.Add(recursos);

        // Las monedas y gemas animadas vuelan hasta estos contadores. El header
        // viejo se oculta en partida, así que sin esto volarían a un icono
        // invisible y el efecto se perdería.
        AnimadorMonedas.ObjetivoMonedasPantalla = () => PuntoPantalla(_monedas);
        AnimadorMonedas.ObjetivoGemasPantalla   = () => PuntoPantalla(_gemas);

        // ── Comodines ─────────────────────────────────────────────────────
        // El inventario solo se veía abriendo la tienda. Puesto bajo los
        // recursos, el jugador ve al comprar cómo sube el número y en partida
        // sabe con qué cuenta sin salir del juego.
        var riel = new VisualElement { pickingMode = PickingMode.Ignore };
        riel.AddToClassList("hud__comodines");
        _comodines     = new VisualElement[4];
        _comodinCuenta = new Label[4];
        _comodines[0] = CasillaComodin(riel, 0, Iconos.Icono.Mira,   ColorCielo, "Perfect Snap");
        _comodines[1] = CasillaComodin(riel, 1, Iconos.Icono.Reloj,  ColorExito, "+30 seconds");
        _comodines[2] = CasillaComodin(riel, 2, Iconos.Icono.Pesa,   ColorPlomo, "Heavy Cube");
        _comodines[3] = CasillaComodin(riel, 3, Iconos.Icono.Escudo, ColorGema,  "Shield");
        _capa.Add(riel);

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

    // Los mismos colores del tema, aquí en código porque Painter2D no lee USS
    public static readonly Color ColorOro   = new Color(1f, 0.788f, 0.302f);
    public static readonly Color ColorGema  = new Color(0.925f, 0.455f, 0.733f);
    public static readonly Color ColorVida  = new Color(1f, 0.541f, 0.420f);
    public static readonly Color ColorCielo = new Color(0.498f, 0.831f, 1f);
    public static readonly Color ColorExito = new Color(0.561f, 0.878f, 0.384f);
    public static readonly Color ColorPlomo = new Color(0.788f, 0.816f, 0.847f);

    static Label ChipRecurso(VisualElement padre, string claseColor, Iconos.Icono icono, Color color)
    {
        var chip = new VisualElement { pickingMode = PickingMode.Ignore };
        chip.AddToClassList("chip");
        chip.AddToClassList(claseColor);

        // Un punto de color no decía nada: había que recordar que el rosa eran
        // gemas. Con la figura dibujada se lee sin aprenderse el código de color.
        var figura = Iconos.Crear(icono, color, 22f);
        figura.AddToClassList("chip__icono");
        chip.Add(figura);

        var etiqueta = new Label("0") { pickingMode = PickingMode.Ignore };
        etiqueta.AddToClassList("chip__valor");
        chip.Add(etiqueta);

        padre.Add(chip);
        return etiqueta;
    }

    /// <summary>
    /// Una casilla del riel de comodines: icono, contador y, sobre todo, un
    /// botón que los USA.
    ///
    /// El panel viejo de uGUI quedaba debajo del HUD nuevo y tocarlo soltaba el
    /// cubo en vez de activar el comodín: el jugador perdía la jugada y el
    /// comodín no se armaba. Aquí el riel es el panel, y al ser un Button de
    /// UI Toolkit el toque no llega al juego.
    ///
    /// Nunca se desactiva, ni con cero unidades: un botón deshabilitado deja
    /// pasar el toque al juego y volveríamos al mismo problema. Con el
    /// inventario vacío se avisa y no se gasta nada.
    /// </summary>
    VisualElement CasillaComodin(VisualElement padre, int slot, Iconos.Icono icono, Color color, string nombre)
    {
        var casilla = new Button(() => UsarComodin(slot, nombre)) { text = "" };
        casilla.AddToClassList("comodin");
        casilla.tooltip = nombre;

        casilla.Add(Iconos.Crear(icono, color, 34f));

        var cuenta = new Label("0") { pickingMode = PickingMode.Ignore };
        cuenta.AddToClassList("comodin__cuenta");
        casilla.Add(cuenta);
        _comodinCuenta[slot] = cuenta;

        padre.Add(casilla);
        return casilla;
    }

    static void UsarComodin(int slot, string nombre)
    {
        var eco = EconomiaManager.Instance;
        if (eco != null && eco.ObtenerComodin(slot) <= 0)
        {
            // No abrimos la tienda: el reloj de la partida sigue corriendo y
            // un panel modal encima sería peor que el aviso
            NotificacionesUI.Aviso("No " + nombre + " left", "buy more in the store");
            return;
        }

        var c = ComodinesManager.Instance;
        if (c == null) return;

        switch (slot)
        {
            case 0: c.ActivarSnapPerfecto(); break;
            case 1: c.ActivarTiempoExtra();  break;
            case 2: c.ActivarCuboPlomo();    break;
            case 3: c.ActivarEscudo();       break;
        }
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

        // Cubos: en niveles con límite manda el límite; si no, la meta.
        //
        // Pasada la meta ya no se cuenta contra ella: el nivel no termina al
        // alcanzarla, sigue hasta que se acaba el tiempo, y leer "20 of 16
        // cubes" con la barra llena parecía un error del juego en vez de un
        // logro. Ahora dice que la meta está hecha y cuántos van.
        if (gm.CubosMaximos > 0)
            _cubos.text = gm.CubosUsados + " of " + gm.CubosMaximos + " cubes";
        else if (gm.AlturaEnCubos >= gm.MetaEnCubos && gm.MetaEnCubos > 0)
            _cubos.text = "Goal! " + gm.AlturaEnCubos + " cubes";
        else
            _cubos.text = gm.AlturaEnCubos + " of " + gm.MetaEnCubos + " cubes";

        _barra.style.width = Length.Percent(gm.ProgresoMeta * 100f);
        _barra.EnableInClassList("hud__progreso--meta", gm.ProgresoMeta >= 1f);

        var eco = EconomiaManager.Instance;
        if (eco != null)
        {
            _monedas.text = PlayerHeaderUI.MonedasMostradas.ToString();
            _gemas.text   = PlayerHeaderUI.GemasMostradas.ToString();
            _vidas.text   = eco.Vidas.ToString();

            var com = ComodinesManager.Instance;
            for (int i = 0; i < _comodines.Length; i++)
            {
                int n = eco.ObtenerComodin(i);
                _comodinCuenta[i].text = n.ToString();
                _comodines[i].EnableInClassList("comodin--vacio", n <= 0);

                // Armado: el jugador tiene que ver que su comodín SIGUE ahí
                // esperando. Sin esto pulsaba dos veces creyendo que no había
                // funcionado — y la segunda le decía "already armed".
                bool armado = com != null && i switch
                {
                    0 => com.SnapPerfectoActivo,
                    2 => com.CuboPlomoPendiente,
                    3 => com.EscudoActivo,
                    _ => false,
                };
                _comodines[i].EnableInClassList("comodin--activo", armado);
            }
        }

        if (_avisoHasta > 0f && Time.unscaledTime >= _avisoHasta)
        {
            _aviso.text  = "";
            _avisoHasta  = 0f;
        }

        // El giro usa tiempo sin escalar: en pausa el juego se congela, pero el
        // HUD no se ve ahí, y así no se queda trabado si algo toca timeScale
        _giroCubo = (_giroCubo + 38f * Time.unscaledDeltaTime) % 360f;
        _proxColor.MarkDirtyRepaint();

        ActualizarProximo();
    }

    private Color _colorProximo = Color.clear;

    /// <summary>
    /// El próximo cubo, girando en 3D como en la app anterior. Un cuadro de
    /// color plano no se leía como un cubo: con los tipos claros (Normal,
    /// Plumas) parecía un recuadro blanco vacío.
    /// </summary>
    void DibujarCubo(MeshGenerationContext ctx)
    {
        Color c = _colorProximo == Color.clear ? new Color(0.7f, 0.7f, 0.75f) : _colorProximo;
        CuboGiratorio.Dibujar(ctx, c, _giroCubo);
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

using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// La pantalla de fin de nivel, en UI Toolkit.
///
/// Es la que más se ve —sale en cada nivel— y era la última grande que seguía
/// en la interfaz vieja: fondo azul plano, botones de colores sueltos y una
/// tipografía que no se parecía a nada del resto del juego ni de la landing.
///
/// Lo que arregla, además del aspecto:
///   · Las estrellas mentían. Se pintaban las tres siempre y solo cambiaban de
///     color, así que un "Bronze" enseñaba tres estrellas, dos doradas y una
///     de bronce. Aquí se dibujan con Painter2D: las ganadas rellenas, las
///     que faltan solo con su contorno. Se cuentan de un vistazo.
///   · No se veía qué habías ganado. Las monedas volaban por encima del botón
///     RETRY y tapaban el texto. Ahora la recompensa tiene su propia fila.
///   · El botón principal era RETRY, en naranja, encima de todo. Tras ganar,
///     lo que quiere el jugador es el siguiente nivel: ese es el botón grande
///     y los demás quedan secundarios.
/// </summary>
public class ResultadoUI : MonoBehaviour
{
    private static ResultadoUI _instancia;

    private VisualElement _capa, _tarjeta, _estrellas, _filaPremio;
    private Label _titulo, _rating, _record;
    private Label _altura, _alturaMeta, _cubos;
    private Label _premioMonedas, _premioGemas;
    private Button _siguiente, _reintentar, _tienda, _menu;

    private int _numEstrellas;

    /// <summary>Datos que la pantalla necesita para pintarse.</summary>
    public struct Datos
    {
        public bool  gano;
        public int   estrellas;      // 0..3
        public float alturaMetros;
        public float metaMetros;
        public int   cubos;
        public bool  nuevoRecord;
        public int   mejorCubos;
        public int   monedas;
        public int   gemas;
        public bool  haySiguienteNivel;
    }

    public static void Mostrar(Datos d) => Instancia()._Mostrar(d);

    /// <summary>
    /// Adónde deben volar las monedas y gemas ganadas mientras esta pantalla
    /// esté abierta: a su propia fila de recompensa. Antes volaban al contador
    /// del header, que en el resultado está oculto, así que cruzaban por
    /// encima del botón RETRY y le tapaban el texto.
    /// </summary>
    public static Vector2? PuntoMonedas() => Punto(_instancia?._premioMonedas);
    public static Vector2? PuntoGemas()   => Punto(_instancia?._premioGemas);

    static Vector2? Punto(VisualElement el)
    {
        if (_instancia == null || _instancia._capa.style.display == DisplayStyle.None) return null;
        return CapaUI.PuntoPantalla(el);
    }

    public static void Ocultar()
    {
        if (_instancia != null) _instancia._capa.style.display = DisplayStyle.None;
    }

    static ResultadoUI Instancia()
    {
        if (_instancia != null) return _instancia;
        var go = new GameObject("ResultadoUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<ResultadoUI>();
        _instancia.Construir();
        return _instancia;
    }

    // ── Construcción ──────────────────────────────────────────────────────

    void Construir()
    {
        _capa = CapaUI.NuevaCapa("capa-resultado");
        _capa.AddToClassList("capa-resultado");

        // Captura: es modal, no debe poder tocarse el juego de detrás
        var fondo = new VisualElement { pickingMode = PickingMode.Position };
        fondo.AddToClassList("resultado__fondo");
        _capa.Add(fondo);

        _tarjeta = new VisualElement { pickingMode = PickingMode.Ignore };
        _tarjeta.AddToClassList("resultado");
        fondo.Add(_tarjeta);

        // Estrellas dibujadas: las ganadas rellenas, las que faltan huecas
        _estrellas = new VisualElement { pickingMode = PickingMode.Ignore };
        _estrellas.AddToClassList("resultado__estrellas");
        for (int i = 0; i < 3; i++)
        {
            int indice = i;
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("resultado__estrella");
            e.generateVisualContent += ctx => DibujarEstrella(ctx, indice);
            _estrellas.Add(e);
        }
        _tarjeta.Add(_estrellas);

        _rating = Texto(_tarjeta, "resultado__rating", "");
        _titulo = Texto(_tarjeta, "resultado__titulo", "");
        _record = Texto(_tarjeta, "resultado__record", "");

        // Cifras
        var cifras = new VisualElement { pickingMode = PickingMode.Ignore };
        cifras.AddToClassList("resultado__cifras");
        (_altura, _alturaMeta) = Cifra(cifras, "HEIGHT");
        (_cubos,  _)           = Cifra(cifras, "CUBES");
        _tarjeta.Add(cifras);

        // Recompensa: antes las monedas volaban sobre los botones y no se leía
        // cuánto habías ganado
        _filaPremio = new VisualElement { pickingMode = PickingMode.Ignore };
        _filaPremio.AddToClassList("resultado__premio");
        _premioMonedas = PremioChip(_filaPremio, Iconos.Icono.Moneda, HudUI.ColorOro);
        _premioGemas   = PremioChip(_filaPremio, Iconos.Icono.Gema,   HudUI.ColorGema);
        _tarjeta.Add(_filaPremio);

        // Botones: el principal es el que el jugador quiere pulsar ahora
        _siguiente  = Boton(_tarjeta, "NEXT LEVEL", "resultado__boton--principal",
                            () => { Ocultar(); GameManager.Instance?.BotonSiguienteNivel(); });
        _reintentar = Boton(_tarjeta, "RETRY", "resultado__boton--secundario",
                            () => { Ocultar(); GameManager.Instance?.BotonReintentar(); });

        var fila = new VisualElement { pickingMode = PickingMode.Ignore };
        fila.AddToClassList("resultado__fila-botones");
        _tienda = Boton(fila, "Store", "resultado__boton--plano", () => TiendaUI.Abrir());
        _menu   = Boton(fila, "Menu",  "resultado__boton--plano",
                        () => { Ocultar(); GameManager.Instance?.BotonMenu(); });
        _tarjeta.Add(fila);

        _capa.style.display = DisplayStyle.None;
    }

    static Label Texto(VisualElement padre, string clase, string texto)
    {
        var l = new Label(texto) { pickingMode = PickingMode.Ignore };
        l.AddToClassList(clase);
        padre.Add(l);
        return l;
    }

    static (Label, Label) Cifra(VisualElement padre, string etiqueta)
    {
        var caja = new VisualElement { pickingMode = PickingMode.Ignore };
        caja.AddToClassList("cifra");

        var valor = new Label("0") { pickingMode = PickingMode.Ignore };
        valor.AddToClassList("cifra__valor");
        caja.Add(valor);

        var nombre = new Label(etiqueta) { pickingMode = PickingMode.Ignore };
        nombre.AddToClassList("cifra__etiqueta");
        caja.Add(nombre);

        var pie = new Label("") { pickingMode = PickingMode.Ignore };
        pie.AddToClassList("cifra__pie");
        caja.Add(pie);

        padre.Add(caja);
        return (valor, pie);
    }

    static Label PremioChip(VisualElement padre, Iconos.Icono icono, Color color)
    {
        var chip = new VisualElement { pickingMode = PickingMode.Ignore };
        chip.AddToClassList("premio");
        chip.Add(Iconos.Crear(icono, color, 30f));

        var valor = new Label("+0") { pickingMode = PickingMode.Ignore };
        valor.AddToClassList("premio__valor");
        chip.Add(valor);

        padre.Add(chip);
        return valor;
    }

    static Button Boton(VisualElement padre, string texto, string clase, System.Action accion)
    {
        var b = new Button(accion) { text = texto };
        b.AddToClassList("resultado__boton");
        b.AddToClassList(clase);
        padre.Add(b);
        return b;
    }

    // ── Pintado ───────────────────────────────────────────────────────────

    void _Mostrar(Datos d)
    {
        _numEstrellas = Mathf.Clamp(d.estrellas, 0, 3);
        foreach (var e in _estrellas.Children()) e.MarkDirtyRepaint();

        _titulo.text = d.gano ? "Level complete" : "Time's up";
        _titulo.EnableInClassList("resultado__titulo--perdio", !d.gano);

        string[] nombres = { "No rating", "Bronze", "Silver", "Gold" };
        _rating.text = d.gano ? nombres[_numEstrellas] : "";
        _rating.style.color = ColorRating(_numEstrellas);
        _rating.style.display = d.gano ? DisplayStyle.Flex : DisplayStyle.None;

        if (d.nuevoRecord)
        {
            _record.text = "NEW RECORD";
            _record.AddToClassList("resultado__record--nuevo");
        }
        else
        {
            _record.text = "Best: " + d.mejorCubos + " cubes";
            _record.RemoveFromClassList("resultado__record--nuevo");
        }

        _altura.text     = d.alturaMetros.ToString("F2") + " m";
        _alturaMeta.text = "goal " + d.metaMetros.ToString("F1") + " m";
        _cubos.text      = d.cubos.ToString();

        // Solo se enseña lo que de verdad se ganó
        _premioMonedas.text = "+" + d.monedas;
        _premioGemas.text   = "+" + d.gemas;
        _premioMonedas.parent.style.display = d.monedas > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        _premioGemas.parent.style.display   = d.gemas   > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        _filaPremio.style.display = (d.monedas > 0 || d.gemas > 0)
            ? DisplayStyle.Flex : DisplayStyle.None;

        // Tras ganar, el botón grande es seguir; tras perder, reintentar
        bool siguiente = d.gano && d.haySiguienteNivel;
        _siguiente.style.display = siguiente ? DisplayStyle.Flex : DisplayStyle.None;
        _reintentar.EnableInClassList("resultado__boton--principal", !siguiente);
        _reintentar.EnableInClassList("resultado__boton--secundario", siguiente);

        _capa.style.display = DisplayStyle.Flex;
    }

    static Color ColorRating(int estrellas) => estrellas switch
    {
        3 => new Color(1f,    0.84f, 0.20f),
        2 => new Color(0.80f, 0.84f, 0.88f),
        1 => new Color(0.85f, 0.58f, 0.32f),
        _ => new Color(0.55f, 0.58f, 0.68f),
    };

    /// <summary>
    /// Una estrella de cinco puntas. La ganada va rellena, la que falta solo
    /// con el contorno: así el número se cuenta de un vistazo, que es lo que
    /// no se podía hacer cuando las tres se pintaban siempre iguales.
    /// </summary>
    void DibujarEstrella(MeshGenerationContext ctx, int indice)
    {
        Rect r = ctx.visualElement.contentRect;
        if (r.width < 8f || r.height < 8f) return;

        bool ganada = indice < _numEstrellas;
        Color c = ganada ? ColorRating(_numEstrellas) : new Color(1f, 1f, 1f, 0.22f);

        float radio  = Mathf.Min(r.width, r.height) * 0.5f - 2f;
        var centro   = new Vector2(r.width * 0.5f, r.height * 0.5f);

        var p = ctx.painter2D;
        p.BeginPath();
        for (int i = 0; i < 10; i++)
        {
            // Alterna punta y valle: diez vértices, empezando arriba
            float rad = (i % 2 == 0) ? radio : radio * 0.42f;
            float ang = -90f + i * 36f;
            var v = centro + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad) * rad,
                                         Mathf.Sin(ang * Mathf.Deg2Rad) * rad);
            if (i == 0) p.MoveTo(v); else p.LineTo(v);
        }
        p.ClosePath();

        if (ganada)
        {
            p.fillColor = c;
            p.Fill();
        }
        else
        {
            p.strokeColor = c;
            p.lineWidth   = 2.5f;
            p.Stroke();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tienda en UI Toolkit.
///
/// Lo que arregla de la tienda vieja:
///   · No se veía cuánto dinero, gemas o vidas tenía el jugador: los botones
///     se apagaban sin decir por qué. Ahora el saldo está siempre arriba y el
///     botón dice qué falta ("need 60", "no room").
///   · No se veía el inventario. Cada fila dice cuántos tienes ya.
///   · Los precios estaban escritos a mano en la escena y no coincidían con lo
///     que cobraba el código (el pack de vidas mostraba 50 y costaba 120).
///     Aquí todos salen de las constantes de EconomiaManager.
///
/// Se monta sola. Las compras las ejecuta TiendaManager, que sigue siendo el
/// dueño de la lógica.
/// </summary>
public class TiendaUI : MonoBehaviour
{
    private enum Pestana { Comodines, Vidas }

    private static TiendaUI _instancia;

    private VisualElement _capa, _fondo, _lista, _tabs;
    private Label _saldoMonedas, _saldoGemas, _saldoVidas;
    private Pestana _pestana = Pestana.Comodines;
    private bool _abierta;

    public static bool Abierta => _instancia != null && _instancia._abierta;

    public static void Abrir(bool enVidas = false)
    {
        var t = Instancia();
        t._pestana = enVidas ? Pestana.Vidas : Pestana.Comodines;
        t._abierta = true;
        t._capa.style.display = DisplayStyle.Flex;
        t.Refrescar();
    }

    public static void Cerrar()
    {
        if (_instancia == null) return;
        _instancia._abierta = false;
        _instancia._capa.style.display = DisplayStyle.None;
    }

    static TiendaUI Instancia()
    {
        if (_instancia != null) return _instancia;
        var go = new GameObject("TiendaUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<TiendaUI>();
        _instancia.Construir();
        return _instancia;
    }

    // ── Construcción ──────────────────────────────────────────────────────

    void Construir()
    {
        _capa = CapaUI.NuevaCapa("capa-tienda");

        // El fondo SÍ captura: es un panel modal, no debe poder jugarse detrás
        _fondo = new VisualElement { pickingMode = PickingMode.Position };
        _fondo.AddToClassList("tienda");
        _capa.Add(_fondo);

        // Cabecera con saldos: lo que más se echaba en falta
        var cabecera = new VisualElement { pickingMode = PickingMode.Ignore };
        cabecera.AddToClassList("tienda__cabecera");

        var cerrar = new Button(Cerrar);
        cerrar.AddToClassList("tienda__cerrar");
        cerrar.text = "✕";
        cabecera.Add(cerrar);

        var titulo = new Label("Store") { pickingMode = PickingMode.Ignore };
        titulo.AddToClassList("tienda__titulo");
        cabecera.Add(titulo);

        var saldos = new VisualElement { pickingMode = PickingMode.Ignore };
        saldos.AddToClassList("tienda__saldos");
        _saldoMonedas = Chip(saldos, "chip--monedas");
        _saldoGemas   = Chip(saldos, "chip--gemas");
        _saldoVidas   = Chip(saldos, "chip--vidas");
        cabecera.Add(saldos);
        _fondo.Add(cabecera);

        // Pestañas
        _tabs = new VisualElement { pickingMode = PickingMode.Ignore };
        _tabs.AddToClassList("tienda__tabs");
        _tabs.Add(Tab("Wildcards", Pestana.Comodines));
        _tabs.Add(Tab("Lives",     Pestana.Vidas));
        _fondo.Add(_tabs);

        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.AddToClassList("tienda__scroll");
        _lista = scroll.contentContainer;
        _fondo.Add(scroll);

        _capa.style.display = DisplayStyle.None;
    }

    static Label Chip(VisualElement padre, string clase)
    {
        var chip = new VisualElement { pickingMode = PickingMode.Ignore };
        chip.AddToClassList("chip");
        chip.AddToClassList(clase);
        var punto = new VisualElement { pickingMode = PickingMode.Ignore };
        punto.AddToClassList("chip__punto");
        chip.Add(punto);
        var valor = new Label("0") { pickingMode = PickingMode.Ignore };
        valor.AddToClassList("chip__valor");
        chip.Add(valor);
        padre.Add(chip);
        return valor;
    }

    Button Tab(string texto, Pestana cual)
    {
        var b = new Button(() => { _pestana = cual; Refrescar(); }) { text = texto };
        b.AddToClassList("tienda__tab");
        b.userData = cual;
        return b;
    }

    // ── Pintado ───────────────────────────────────────────────────────────

    void Refrescar()
    {
        var eco = EconomiaManager.Instance;
        if (eco == null) return;

        _saldoMonedas.text = eco.Monedas.ToString();
        _saldoGemas.text   = eco.Gemas.ToString();
        _saldoVidas.text   = eco.Vidas + "/" + EconomiaManager.MAX_VIDAS;

        foreach (var hijo in _tabs.Children())
            hijo.EnableInClassList("tienda__tab--activa", (Pestana)hijo.userData == _pestana);

        _lista.Clear();
        if (_pestana == Pestana.Comodines) PintarComodines(eco);
        else                               PintarVidas(eco);
    }

    void PintarComodines(EconomiaManager eco)
    {
        Fila(eco, "Perfect Snap", "Your next cube lands dead center",
             EconomiaManager.PRECIO_SNAP_MONEDAS, eco.ObtenerComodin(0), "cielo",
             () => TiendaManager.Instance?.ComprarSnap());

        Fila(eco, "+30 seconds", "Adds time during a run",
             EconomiaManager.PRECIO_TIEMPO_MONEDAS, eco.ObtenerComodin(1), "exito",
             () => TiendaManager.Instance?.ComprarTiempoExtra());

        Fila(eco, "Heavy Cube", "Next cube is lead: steadies your tower",
             EconomiaManager.PRECIO_PLOMO_MONEDAS, eco.ObtenerComodin(2), "plomo",
             () => TiendaManager.Instance?.ComprarCuboPlomo());

        Fila(eco, "Shield", "Keeps your life if you lose",
             EconomiaManager.PRECIO_ESCUDO_MONEDAS, eco.ObtenerComodin(3), "gema",
             () => TiendaManager.Instance?.ComprarEscudo());
    }

    void PintarVidas(EconomiaManager eco)
    {
        int hueco = EconomiaManager.MAX_VIDAS - eco.Vidas;

        // Cuánto falta para la siguiente vida gratis
        float seg = eco.SegundosHastaProximaVida();
        if (hueco > 0 && seg > 0f)
        {
            var aviso = new Label($"Next free life in {Mathf.FloorToInt(seg / 60f)}:{Mathf.FloorToInt(seg % 60f):00}")
                { pickingMode = PickingMode.Ignore };
            aviso.AddToClassList("tienda__aviso");
            _lista.Add(aviso);
        }

        Fila(eco, "1 Life", hueco > 0 ? "Fits now" : "Your lives are full",
             EconomiaManager.PRECIO_VIDA_MONEDAS, -1, "vida",
             () => TiendaManager.Instance?.ComprarVida1(),
             bloqueo: hueco < 1 ? "full" : null);

        Fila(eco, $"Life Pack ×{EconomiaManager.VIDAS_POR_PACK}",
             hueco >= EconomiaManager.VIDAS_POR_PACK ? "Fills three at once" : $"Only {hueco} would fit",
             EconomiaManager.PRECIO_PACK_VIDAS_MONEDAS, -1, "vida",
             () => TiendaManager.Instance?.ComprarVidaPack(),
             bloqueo: hueco < EconomiaManager.VIDAS_POR_PACK ? "no room" : null);

        var titulo = new Label("EXCHANGE GEMS") { pickingMode = PickingMode.Ignore };
        titulo.AddToClassList("tienda__seccion");
        _lista.Add(titulo);

        FilaGemas($"{EconomiaManager.GEMAS_POR_CAMBIO_MONEDAS} gem → {EconomiaManager.MONEDAS_POR_GEMA} coins",
                  EconomiaManager.GEMAS_POR_CAMBIO_MONEDAS, eco,
                  () => eco.CambiarGemasPorMonedas());

        FilaGemas($"{EconomiaManager.GEMAS_POR_VIDAS_LLENAS} gems → full lives",
                  EconomiaManager.GEMAS_POR_VIDAS_LLENAS, eco,
                  () => eco.CambiarGemasPorVidas(),
                  bloqueo: hueco < 1 ? "full" : null);
    }

    /// <summary>Una fila de compra. inventario &lt; 0 = no se muestra "You have".</summary>
    void Fila(EconomiaManager eco, string nombre, string detalle, int precio, int inventario,
              string color, Action comprar, string bloqueo = null)
    {
        bool sinMonedas = !eco.TieneMonedas(precio);
        bool disponible = bloqueo == null && !sinMonedas;

        var fila = new VisualElement { pickingMode = PickingMode.Ignore };
        fila.AddToClassList("fila");
        if (!disponible) fila.AddToClassList("fila--apagada");

        var icono = new VisualElement { pickingMode = PickingMode.Ignore };
        icono.AddToClassList("fila__icono");
        icono.AddToClassList("fila__icono--" + color);
        fila.Add(icono);

        var textos = new VisualElement { pickingMode = PickingMode.Ignore };
        textos.AddToClassList("fila__textos");

        var linea = new VisualElement { pickingMode = PickingMode.Ignore };
        linea.AddToClassList("fila__linea");
        var lblNombre = new Label(nombre) { pickingMode = PickingMode.Ignore };
        lblNombre.AddToClassList("fila__nombre");
        linea.Add(lblNombre);

        if (inventario >= 0)
        {
            var tienes = new Label("You have " + inventario) { pickingMode = PickingMode.Ignore };
            tienes.AddToClassList("fila__inventario");
            linea.Add(tienes);
        }
        textos.Add(linea);

        var lblDetalle = new Label(detalle) { pickingMode = PickingMode.Ignore };
        lblDetalle.AddToClassList("fila__detalle");
        textos.Add(lblDetalle);
        fila.Add(textos);

        // El botón DICE por qué no se puede, en vez de apagarse sin explicación
        string motivo = bloqueo ?? (sinMonedas ? "need " + (precio - eco.Monedas) : null);

        var boton = new Button(() => { comprar?.Invoke(); Refrescar(); });
        boton.AddToClassList("fila__boton");
        boton.SetEnabled(disponible);

        var precioLbl = new Label(precio.ToString()) { pickingMode = PickingMode.Ignore };
        precioLbl.AddToClassList("fila__precio");
        boton.Add(precioLbl);

        if (motivo != null)
        {
            var motivoLbl = new Label(motivo) { pickingMode = PickingMode.Ignore };
            motivoLbl.AddToClassList("fila__motivo");
            boton.Add(motivoLbl);
        }
        fila.Add(boton);
        _lista.Add(fila);
    }

    void FilaGemas(string texto, int coste, EconomiaManager eco, Func<bool> cambiar, string bloqueo = null)
    {
        bool sinGemas = !eco.TieneGemas(coste);
        bool disponible = bloqueo == null && !sinGemas;

        var fila = new VisualElement { pickingMode = PickingMode.Ignore };
        fila.AddToClassList("fila");
        if (!disponible) fila.AddToClassList("fila--apagada");

        var icono = new VisualElement { pickingMode = PickingMode.Ignore };
        icono.AddToClassList("fila__icono");
        icono.AddToClassList("fila__icono--gema");
        fila.Add(icono);

        var lbl = new Label(texto) { pickingMode = PickingMode.Ignore };
        lbl.AddToClassList("fila__nombre");
        lbl.style.flexGrow = 1;
        fila.Add(lbl);

        var boton = new Button(() => { if (cambiar()) Refrescar(); });
        boton.AddToClassList("fila__boton");
        boton.SetEnabled(disponible);

        var val = new Label(coste + (coste == 1 ? " gem" : " gems")) { pickingMode = PickingMode.Ignore };
        val.AddToClassList("fila__precio");
        boton.Add(val);

        string motivo = bloqueo ?? (sinGemas ? "need " + (coste - eco.Gemas) : null);
        if (motivo != null)
        {
            var motivoLbl = new Label(motivo) { pickingMode = PickingMode.Ignore };
            motivoLbl.AddToClassList("fila__motivo");
            boton.Add(motivoLbl);
        }

        fila.Add(boton);
        _lista.Add(fila);
    }
}

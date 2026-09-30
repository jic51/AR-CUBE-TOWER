using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// El mensaje grande del centro de la pantalla: "Well placed!", "Aligned x2",
/// "Fire Block", "LOCKED!".
///
/// Sustituye al texto del HUD viejo (MensajeFlotante), que estaba anclado en el
/// tercio superior de la escena y se cruzaba con los contadores de monedas y
/// gemas del HUD nuevo: en los vídeos se leía "Well placed" pisando el saldo.
/// Además era texto blanco suelto sobre la cámara, así que sobre una mesa clara
/// desaparecía.
///
/// Aquí baja al centro, donde no hay nada, y va sobre una píldora de vidrio
/// oscuro para que se lea sobre cualquier fondo real.
/// </summary>
public class MensajeUI : MonoBehaviour
{
    private static MensajeUI _instancia;

    private VisualElement _capa, _pildora;
    private Label         _texto;
    private float         _ocultarEn;

    public static void Mostrar(string texto, Color color, float duracion = 2.0f)
    {
        if (string.IsNullOrEmpty(texto)) return;
        Instancia()._Mostrar(texto, color, duracion);
    }

    /// <summary>Lo oculta de inmediato — al terminar la partida, por ejemplo.</summary>
    public static void Ocultar()
    {
        if (_instancia != null) _instancia._Ocultar();
    }

    static MensajeUI Instancia()
    {
        if (_instancia != null) return _instancia;
        var go = new GameObject("MensajeUI");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<MensajeUI>();
        _instancia.Construir();
        return _instancia;
    }

    void Construir()
    {
        _capa = CapaUI.NuevaCapa("capa-mensaje");
        _capa.AddToClassList("capa-mensaje");

        _pildora = new VisualElement { pickingMode = PickingMode.Ignore };
        _pildora.AddToClassList("mensaje");

        _texto = new Label("") { pickingMode = PickingMode.Ignore };
        _texto.AddToClassList("mensaje__texto");
        _pildora.Add(_texto);

        _capa.Add(_pildora);
        _capa.style.display = DisplayStyle.None;
    }

    void _Mostrar(string texto, Color color, float duracion)
    {
        _texto.text  = texto;
        _texto.style.color = color;

        // Borde del color del mensaje: identifica de un vistazo si es un cubo
        // nuevo, un combo o un comodín, sin tener que leerlo
        color.a = 0.55f;
        _pildora.style.borderTopColor    = color;
        _pildora.style.borderBottomColor = color;
        _pildora.style.borderLeftColor   = color;
        _pildora.style.borderRightColor  = color;

        _capa.style.display = DisplayStyle.Flex;
        _ocultarEn = Time.unscaledTime + Mathf.Max(0.5f, duracion);

        // Reinicia la animación de entrada aunque ya estuviera visible: los
        // mensajes se encadenan (cubo nuevo + combo) y sin esto el segundo
        // aparecía sin moverse, como si fuera el mismo de antes
        _pildora.RemoveFromClassList("mensaje--dentro");
        _pildora.schedule.Execute(() => _pildora.AddToClassList("mensaje--dentro")).StartingIn(10);
    }

    void _Ocultar()
    {
        if (_capa != null) _capa.style.display = DisplayStyle.None;
        _pildora?.RemoveFromClassList("mensaje--dentro");
    }

    void LateUpdate()
    {
        if (_capa == null || _capa.style.display == DisplayStyle.None) return;
        if (Time.unscaledTime >= _ocultarEn) _Ocultar();
    }
}

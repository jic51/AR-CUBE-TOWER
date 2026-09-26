using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Anima monedas y gemas (íconos) que vuelan desde un punto de origen hasta su
/// contador del header. El contador sube con cada ícono que llega.
///
/// SETUP en Unity Editor:
///   1. Crea un GameObject vacío en el Canvas raíz → agrega este script.
///   2. Asigna: prefabMoneda (Image con sprite de moneda), contenedor (RectTransform del Canvas raíz),
///              textoContador (TMP del total de monedas), destinoMoneda (ícono de monedas).
///   3. Las gemas NO necesitan campos propios: usan el mismo prefab con el sprite y el
///      destino del ícono de gemas de PlayerHeaderUI.
///   4. Llama: AnimadorMonedas.AnimarDesdeCentro(cantidad) o AnimarGemasDesdeCentro(cantidad)
/// </summary>
public class AnimadorMonedas : MonoBehaviour
{
    public static AnimadorMonedas Instance;

    [Header("Referencias")]
    [Tooltip("Prefab de la moneda — Image con sprite de moneda dorada")]
    public GameObject       prefabMoneda;
    [Tooltip("RectTransform del Canvas raíz donde se crean las monedas")]
    public RectTransform    contenedor;
    [Tooltip("Texto TMP que muestra el total de monedas (se actualiza al llegar cada moneda)")]
    public TextMeshProUGUI  textoContador;
    [Tooltip("RectTransform del ícono del contador — destino de las monedas")]
    public RectTransform    destinoMoneda;

    [Header("Configuración")]
    public int   monedasMaxSimultaneas = 8;
    public float velocidadVuelo        = 1.2f;
    public float delayEntreMonedas     = 0.08f;
    public float escalaInicio          = 1.2f;
    public AnimationCurve curvaTrayecto = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public enum Recompensa { Moneda, Gema }

    // Valores originales capturados UNA vez por objeto. Si se leyeran al empezar
    // cada pulso o destello, dos íconos que llegan seguidos capturarían el valor
    // ya alterado por el anterior: el ícono quedaba agrandado (bug ya visto) y
    // el texto se quedaba dorado para siempre.
    private readonly Dictionary<RectTransform, Vector3>   _escalaOriginal = new();
    private readonly Dictionary<RectTransform, Coroutine> _pulsos         = new();
    private readonly Dictionary<TMP_Text, Color>          _colorOriginal  = new();
    private readonly Dictionary<TMP_Text, Coroutine>      _destellos      = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── API ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Anima 'cantidad' monedas desde 'posOrigen' (coordenadas de pantalla) hacia
    /// el contador. Llamar ANTES de sumarlas en EconomiaManager.
    /// </summary>
    // Sin "?.": con objetos de Unity destruidos (recarga de escena) el operador
    // no los detecta como nulos. La comparación != null de Unity sí.
    public static void Animar(int cantidad, Vector2 posOrigen)
    {
        if (Instance != null) Instance.Lanzar(Recompensa.Moneda, cantidad, posOrigen);
    }

    /// <summary>Versión que parte desde el centro de la pantalla (para Game Over).</summary>
    public static void AnimarDesdeCentro(int cantidad) => Animar(cantidad, PuntoCentral());

    /// <summary>
    /// Igual que las monedas, hacia el contador de gemas. Con retraso, el contador
    /// se retiene YA (se puede sumar enseguida) pero los íconos salen después.
    /// </summary>
    public static void AnimarGemasDesdeCentro(int cantidad, float retraso = 0f)
    {
        if (Instance != null) Instance.Lanzar(Recompensa.Gema, cantidad, PuntoCentral(), retraso);
    }

    static Vector2 PuntoCentral() => new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);

    // ── Lanzamiento ───────────────────────────────────────────────────────────

    void Lanzar(Recompensa tipo, int cantidad, Vector2 posOrigen, float retraso = 0f)
    {
        if (cantidad <= 0 || prefabMoneda == null || contenedor == null) return;

        RectTransform destino = Destino(tipo);
        if (destino == null) return;   // sin destino, el contador muestra el total directamente

        // Retener ANTES de que se sumen: el contador arranca en el valor viejo y
        // sube con cada ícono que llega (ver PlayerHeaderUI.Retener*)
        if (tipo == Recompensa.Moneda) PlayerHeaderUI.RetenerMonedas(cantidad);
        else                           PlayerHeaderUI.RetenerGemas(cantidad);

        StartCoroutine(Secuencia(tipo, cantidad, posOrigen, destino, retraso));
    }

    RectTransform Destino(Recompensa tipo)
    {
        if (tipo == Recompensa.Moneda) return destinoMoneda;
        var icono = PlayerHeaderUI.Instance != null ? PlayerHeaderUI.Instance.iconoGemas : null;
        return icono != null ? icono.rectTransform : null;
    }

    IEnumerator Secuencia(Recompensa tipo, int total, Vector2 posOrigenPantalla, RectTransform destino, float retraso)
    {
        if (retraso > 0f) yield return new WaitForSecondsRealtime(retraso);

        int iconos = Mathf.Clamp(total, 1, Mathf.Max(1, monedasMaxSimultaneas));

        // Reparto exacto: la suma de todos los iconos es justo el total. Antes se
        // redondeaba hacia arriba por icono (75 monedas en 8 iconos = 10 cada uno
        // = 80), y el contador terminaba en un número distinto del real.
        int baseIcono = total / iconos;
        int resto     = total % iconos;

        // Convertir posición pantalla → Canvas local
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contenedor, posOrigenPantalla, null, out Vector2 posLocal);

        for (int i = 0; i < iconos; i++)
        {
            StartCoroutine(Volar(tipo, posLocal, baseIcono + (i < resto ? 1 : 0), destino));
            // Realtime: el GameOver puede llegar con timeScale 0 tras un anuncio
            yield return new WaitForSecondsRealtime(delayEntreMonedas);
        }
    }

    IEnumerator Volar(Recompensa tipo, Vector2 origen, int valorAlLlegar, RectTransform destino)
    {
        var go   = Instantiate(prefabMoneda, contenedor);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = origen + Random.insideUnitCircle * 30f; // dispersión leve

        // Las gemas reutilizan el prefab de moneda con el sprite del ícono de gemas
        if (tipo == Recompensa.Gema)
        {
            var img    = go.GetComponent<Image>();
            var sprite = PlayerHeaderUI.Instance != null && PlayerHeaderUI.Instance.iconoGemas != null
                       ? PlayerHeaderUI.Instance.iconoGemas.sprite : null;
            if (img != null && sprite != null) { img.sprite = sprite; img.preserveAspect = true; }
        }

        Vector2 posInicio = rect.anchoredPosition;
        float t = 0f;
        while (t < velocidadVuelo)
        {
            t += Time.unscaledDeltaTime;
            float p = curvaTrayecto.Evaluate(Mathf.Clamp01(t / velocidadVuelo));

            // El destino se recalcula cada fotograma: el header puede aparecer o
            // reacomodarse mientras el ícono vuela (bono diario en el menú)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                contenedor, RectTransformUtility.WorldToScreenPoint(null, destino.position),
                null, out Vector2 destLocal);

            Vector2 pos = Vector2.Lerp(posInicio, destLocal, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * 80f;   // arco: sube y baja
            rect.anchoredPosition = pos;

            // Escala: aparece grande, se hace pequeña al llegar
            rect.localScale = Vector3.one * Mathf.Lerp(escalaInicio, 0.4f, p);
            yield return null;
        }

        Destroy(go);
        Entregar(tipo, valorAlLlegar);
        Pulsar(destino);
    }

    // ── Llegada ───────────────────────────────────────────────────────────────

    void Entregar(Recompensa tipo, int cantidad)
    {
        // El header sube exactamente el valor de este ícono
        if (tipo == Recompensa.Moneda)
        {
            PlayerHeaderUI.LiberarMonedas(cantidad);
            if (textoContador != null)
            {
                textoContador.text = PlayerHeaderUI.MonedasMostradas.ToString();
                Destellar(textoContador, new Color(1f, 0.88f, 0.10f));   // dorado
            }
        }
        else
        {
            PlayerHeaderUI.LiberarGemas(cantidad);
            var txt = PlayerHeaderUI.Instance != null ? PlayerHeaderUI.Instance.textoGemas : null;
            if (txt != null) Destellar(txt, new Color(0.45f, 0.95f, 1f));   // cian
        }
    }

    void Destellar(TMP_Text texto, Color color)
    {
        if (!_colorOriginal.ContainsKey(texto)) _colorOriginal[texto] = texto.color;
        if (_destellos.TryGetValue(texto, out var previo) && previo != null) StopCoroutine(previo);
        _destellos[texto] = StartCoroutine(EjecutarDestello(texto, color));
    }

    IEnumerator EjecutarDestello(TMP_Text texto, Color color)
    {
        texto.color = color;
        yield return new WaitForSecondsRealtime(0.12f);
        if (texto != null) texto.color = _colorOriginal[texto];   // siempre al original
        _destellos[texto] = null;
    }

    void Pulsar(RectTransform destino)
    {
        if (destino == null) return;
        if (!_escalaOriginal.ContainsKey(destino)) _escalaOriginal[destino] = destino.localScale;

        // Cancelar el pulso anterior si sigue corriendo (evita acumulación de escala)
        if (_pulsos.TryGetValue(destino, out var previo) && previo != null)
        {
            StopCoroutine(previo);
            destino.localScale = _escalaOriginal[destino];
        }
        _pulsos[destino] = StartCoroutine(EjecutarPulso(destino));
    }

    IEnumerator EjecutarPulso(RectTransform destino)
    {
        Vector3 original = _escalaOriginal[destino];
        float dur = 0.18f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            destino.localScale = original * (1f + 0.25f * Mathf.Sin(Mathf.PI * t / dur));
            yield return null;
        }
        destino.localScale = original;   // siempre restaura al original
        _pulsos[destino] = null;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Anima monedas (íconos) que vuelan desde un punto de origen hasta el contador.
///
/// SETUP en Unity Editor:
///   1. Crea un GameObject vacío en el Canvas raíz → agrega este script.
///   2. Asigna: prefabMoneda (Image con sprite de moneda), contenedor (RectTransform del Canvas raíz),
///              textoContador (TMP del total de monedas).
///   3. Llama: AnimadorMonedas.Instance.Animar(cantidad, posOrigen)
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

    // Escala original del botón destino — se captura una sola vez en Start()
    // para que los pulsos concurrentes no la acumulen
    private Vector3 _escalaOriginalDestino = Vector3.one;
    private Coroutine _pulsarCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (destinoMoneda != null)
            _escalaOriginalDestino = destinoMoneda.localScale;
    }

    /// <summary>
    /// Anima 'cantidad' monedas desde 'posOrigen' (coordenadas de pantalla) hacia el contador.
    /// Al llegar cada moneda el contador se incrementa.
    /// </summary>
    public static void Animar(int cantidad, Vector2 posOrigen)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.SecuenciaMonedas(cantidad, posOrigen));
    }

    /// <summary>Versión que parte desde el centro de la pantalla (para Game Over).</summary>
    public static void AnimarDesdeCentro(int cantidad)
        => Animar(cantidad, new Vector2(Screen.width * 0.5f, Screen.height * 0.35f));

    IEnumerator SecuenciaMonedas(int total, Vector2 posOrigenPantalla)
    {
        if (prefabMoneda == null || contenedor == null || destinoMoneda == null) yield break;

        int monedasALanzar = Mathf.Min(total, monedasMaxSimultaneas);
        int monedasPorIcono = Mathf.Max(1, Mathf.CeilToInt((float)total / monedasALanzar));

        // Convertir posición pantalla → Canvas local
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contenedor, posOrigenPantalla, null, out Vector2 posLocal);

        for (int i = 0; i < monedasALanzar; i++)
        {
            StartCoroutine(VolarMoneda(posLocal, monedasPorIcono));
            yield return new WaitForSeconds(delayEntreMonedas);
        }
    }

    IEnumerator VolarMoneda(Vector2 origen, int valorAlLlegar)
    {
        // Crear ícono
        var go   = Instantiate(prefabMoneda, contenedor);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = origen + Random.insideUnitCircle * 30f; // dispersión leve

        // Obtener posición del destino en coordenadas locales del contenedor
        Vector2 destLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contenedor,
            RectTransformUtility.WorldToScreenPoint(null, destinoMoneda.position),
            null, out destLocal);

        Vector2 posInicio = rect.anchoredPosition;

        // Animar
        float t = 0f;
        while (t < velocidadVuelo)
        {
            t += Time.unscaledDeltaTime;
            float p = curvaTrayecto.Evaluate(Mathf.Clamp01(t / velocidadVuelo));

            // Trayectoria con arco
            Vector2 pos = Vector2.Lerp(posInicio, destLocal, p);
            float arco  = Mathf.Sin(p * Mathf.PI) * 80f; // sube y baja en arco
            pos.y      += arco;

            rect.anchoredPosition = pos;

            // Escala: aparece grande, se hace pequeña al llegar
            float escala = Mathf.Lerp(escalaInicio, 0.4f, p);
            rect.localScale = Vector3.one * escala;

            yield return null;
        }

        // Al llegar: incrementar contador y destruir ícono
        IncrementarContador(valorAlLlegar);
        Destroy(go);

        // Pulso en el destino
        StartCoroutine(PulsarDestino());
    }

    void IncrementarContador(int cantidad)
    {
        if (textoContador == null || EconomiaManager.Instance == null) return;
        // El contador refleja el valor real guardado en EconomiaManager
        textoContador.text = EconomiaManager.Instance.Monedas.ToString();
        StartCoroutine(FlashTexto());
    }

    IEnumerator FlashTexto()
    {
        if (textoContador == null) yield break;
        Color orig = textoContador.color;
        textoContador.color = new Color(1f, 0.88f, 0.1f); // dorado brillante
        yield return new WaitForSecondsRealtime(0.12f);
        textoContador.color = orig;
    }

    IEnumerator PulsarDestino()
    {
        if (destinoMoneda == null) yield break;

        // Cancelar pulso anterior si sigue corriendo (evita acumulación de escala)
        if (_pulsarCoroutine != null)
        {
            StopCoroutine(_pulsarCoroutine);
            destinoMoneda.localScale = _escalaOriginalDestino; // reset inmediato
        }
        _pulsarCoroutine = StartCoroutine(EjecutarPulso());
    }

    IEnumerator EjecutarPulso()
    {
        float dur = 0.18f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Sin(Mathf.PI * t / dur);
            destinoMoneda.localScale = _escalaOriginalDestino * (1f + 0.25f * p);
            yield return null;
        }
        destinoMoneda.localScale = _escalaOriginalDestino; // siempre restaura al original
        _pulsarCoroutine = null;
    }
}

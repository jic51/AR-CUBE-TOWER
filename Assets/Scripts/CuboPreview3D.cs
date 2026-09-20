using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows the next cube type as a live rotating 3D preview in the HUD.
/// Uses a RenderTexture + off-screen camera, so it works without extra Editor setup.
///
/// SETUP:
///   1. Replace (or complement) ProximoCuboIndicador on the "Next" panel.
///   2. Change the colored Image → RawImage (same size). Assign it to rawImagePreview.
///   3. Assign the GruaController and the TextMeshPro label.
///   4. Optional: in Tags & Layers, name layer 31 "CuboPreview" for clarity.
/// </summary>
public class CuboPreview3D : MonoBehaviour
{
    [Header("Referencias")]
    public GruaController grua;

    [Header("UI")]
    public RawImage         rawImagePreview;
    public TextMeshProUGUI  textoNombreTipo;
    public TextMeshProUGUI  textoEtiqueta;

    [Header("Preview")]
    public int   renderTextureSize  = 128;
    public float velocidadRotacion  = 40f;
    public Color colorFondo         = new Color(0.08f, 0.08f, 0.12f, 1f); // alpha=1 obligatorio

    [Header("Animación")]
    public bool animarAlCambiar = true;

    // ── Internals ─────────────────────────────────────────────────────────────
    private const int PREVIEW_LAYER = 31;

    private RenderTexture   rt;
    private Camera          previewCam;
    private GameObject      cuboGO;
    private Renderer        cuboRend;

    private TipoCubo        ultimoTipo  = (TipoCubo)(-1);
    private RectTransform   rect;
    private Vector3         escalaBase;
    private Coroutine       bounce;

    // Preview cube lives 10 000 m away — no AR camera has that far-clip
    private static readonly Vector3 ORIGIN_REMOTO = new Vector3(10000f, 10000f, 10000f);

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        escalaBase = rect != null ? rect.localScale : Vector3.one;
        if (textoEtiqueta != null) textoEtiqueta.text = "NEXT";
    }

    void Start()
    {
        CrearSistemaPreview();

        // Exclude preview layer from every camera except our own
        // (prevents the off-screen cube from leaking into the AR view)
        foreach (var cam in Camera.allCameras)
        {
            if (cam != previewCam)
                cam.cullingMask &= ~(1 << PREVIEW_LAYER);
        }
    }

    void LateUpdate()
    {
        if (grua == null || cuboGO == null) return;

        // Rotate the preview cube continuously
        cuboGO.transform.Rotate(Vector3.up,  velocidadRotacion * Time.deltaTime, Space.World);
        cuboGO.transform.Rotate(Vector3.right, velocidadRotacion * 0.3f * Time.deltaTime, Space.World);

        TipoCubo proximo = grua.tipoProximo;
        if (proximo == ultimoTipo) return;
        ultimoTipo = proximo;

        ActualizarTipo(proximo);
    }

    void OnDestroy()
    {
        if (cuboGO  != null) Destroy(cuboGO);
        if (previewCam != null) Destroy(previewCam.gameObject);
        if (rt != null) { rt.Release(); Destroy(rt); }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    void CrearSistemaPreview()
    {
        // RenderTexture
        rt = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        rt.Create();
        if (rawImagePreview != null) rawImagePreview.texture = rt;

        // Preview cube
        cuboGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuboGO.name  = "CuboPreview3D_Object";
        cuboGO.layer = PREVIEW_LAYER;
        cuboGO.transform.position = ORIGIN_REMOTO;
        cuboGO.transform.rotation = Quaternion.Euler(20f, 45f, 0f);
        Destroy(cuboGO.GetComponent<Collider>());
        cuboRend = cuboGO.GetComponent<Renderer>();

        // Preview camera — slightly above and in front of the cube
        var camGO = new GameObject("CuboPreviewCamera");
        camGO.transform.position = ORIGIN_REMOTO + new Vector3(0f, 0.6f, -2.2f);
        camGO.transform.LookAt(ORIGIN_REMOTO);

        previewCam = camGO.AddComponent<Camera>();
        previewCam.targetTexture  = rt;
        previewCam.cullingMask    = 1 << PREVIEW_LAYER;
        previewCam.clearFlags      = CameraClearFlags.SolidColor;
        // Forzar alpha=1 para evitar el cuadro blanco semitransparente en la RawImage
        previewCam.backgroundColor = new Color(colorFondo.r, colorFondo.g, colorFondo.b, 1f);
        previewCam.fieldOfView    = 35f;
        previewCam.nearClipPlane  = 0.1f;
        previewCam.farClipPlane   = 20f;
        previewCam.depth          = -10; // render before main cameras

        // Dedicated point light for the preview (only illuminates preview layer)
        var lightGO = new GameObject("CuboPreviewLight");
        lightGO.transform.position = ORIGIN_REMOTO + new Vector3(1.5f, 2f, -1f);
        var l = lightGO.AddComponent<Light>();
        l.type         = LightType.Point;
        l.range        = 8f;
        l.intensity    = 2.5f;
        l.color        = Color.white;
        l.cullingMask  = 1 << PREVIEW_LAYER;

        // Ambient fill from the other side
        var fillGO = new GameObject("CuboPreviewFill");
        fillGO.transform.position = ORIGIN_REMOTO + new Vector3(-1f, -0.5f, 1f);
        var f = fillGO.AddComponent<Light>();
        f.type        = LightType.Point;
        f.range       = 6f;
        f.intensity   = 0.8f;
        f.color       = new Color(0.6f, 0.7f, 1f);
        f.cullingMask = 1 << PREVIEW_LAYER;

        // Parent lights to camera so they don't drift
        lightGO.transform.SetParent(camGO.transform);
        fillGO.transform.SetParent(camGO.transform);
    }

    // ── Actualizar material al cambiar de tipo ────────────────────────────────

    void ActualizarTipo(TipoCubo tipo)
    {
        ConfigTipoCubo cfg = grua.ObtenerConfig(tipo);
        if (cfg == null) return;

        if (cuboRend != null)
            cuboRend.material = CrearMaterialPreview(tipo, cfg.colorEnVuelo);

        if (textoNombreTipo != null)
            textoNombreTipo.text = cfg.nombreMostrar.ToUpper();

        if (animarAlCambiar && rect != null)
        {
            if (bounce != null) StopCoroutine(bounce);
            bounce = StartCoroutine(AnimarBounce());
        }
    }

    Material CrearMaterialPreview(TipoCubo tipo, Color colorBase)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = colorBase };

        switch (tipo)
        {
            case TipoCubo.Plomo:
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.85f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.75f);
                break;
            case TipoCubo.Hielo:
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
                break;
            case TipoCubo.Fuego:
                if (mat.HasProperty("_Smoothness"))    mat.SetFloat("_Smoothness",    0.2f);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", colorBase * 0.6f);
                mat.EnableKeyword("_EMISSION");
                break;
            case TipoCubo.Plumas:
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
                break;
            default:
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
                if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.05f);
                break;
        }
        return mat;
    }

    // ── Bounce al cambiar tipo ────────────────────────────────────────────────

    System.Collections.IEnumerator AnimarBounce()
    {
        float dur = 0.25f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = escalaBase * (1f + 0.2f * Mathf.Sin(Mathf.PI * (t / dur)));
            yield return null;
        }
        rect.localScale = escalaBase;
        bounce = null;
    }
}

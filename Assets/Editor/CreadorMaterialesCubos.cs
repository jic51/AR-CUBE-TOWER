#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Herramienta de Editor para crear los materiales de cubos que faltan.
/// Menú: CubePile → Crear Materiales de Cubos Faltantes
/// </summary>
public static class CreadorMaterialesCubos
{
    private const string CARPETA = "Assets/Materials";
    private const string SHADER  = "Universal Render Pipeline/Lit";

    [MenuItem("CubePile/Crear Materiales de Cubos Faltantes")]
    public static void CrearTodos()
    {
        if (!AssetDatabase.IsValidFolder(CARPETA))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // ── Material Fuego (Fire) ────────────────────────────────────────────
        CrearURP("FireMaterial",
            color:      new Color(0.92f, 0.28f, 0.06f, 1f),
            metallic:   0f,
            smooth:     0.15f,
            emission:   true,
            emColor:    new Color(1.0f, 0.35f, 0.0f) * 1.5f);   // naranja brillante

        // ── Material Lava ────────────────────────────────────────────────────
        CrearURP("LavaMaterial",
            color:      new Color(0.70f, 0.12f, 0.02f, 1f),
            metallic:   0f,
            smooth:     0.08f,
            emission:   true,
            emColor:    new Color(0.90f, 0.25f, 0.0f) * 0.9f);   // rojo oscuro con brillo

        // ── Material Plumas (Feather) ────────────────────────────────────────
        CrearURP("FeatherMaterial",
            color:      new Color(0.95f, 0.92f, 0.78f, 1f),
            metallic:   0f,
            smooth:     0.10f,
            emission:   false);

        // ── Material Hierba (Grass) ──────────────────────────────────────────
        CrearURP("GrassMaterial",
            color:      new Color(0.22f, 0.65f, 0.18f, 1f),
            metallic:   0f,
            smooth:     0.12f,
            emission:   false);

        // ── Material Piedra (Stone) ──────────────────────────────────────────
        CrearURP("StoneMaterial",
            color:      new Color(0.40f, 0.40f, 0.42f, 1f),
            metallic:   0.05f,
            smooth:     0.08f,
            emission:   false);

        // ── Material Agua (Water) — semitransparente ──────────────────────────
        // Nota: RubberMaterial ya existe — Agua usa material nuevo translúcido
        CrearURPTransparente("WaterMaterial",
            color:      new Color(0.20f, 0.55f, 0.90f, 0.60f),
            smooth:     0.95f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ [CubePile] Materiales creados en Assets/Materials/. " +
                  "Arrastra cada uno al campo correspondiente en GruaController.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void CrearURP(string nombre, Color color,
                          float metallic, float smooth,
                          bool emission, Color emColor = default)
    {
        string ruta = $"{CARPETA}/{nombre}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(ruta) != null)
        {
            Debug.Log($"[CubePile] {nombre} ya existe → omitido.");
            return;
        }

        var shader = Shader.Find(SHADER);
        if (shader == null) { Debug.LogError($"Shader '{SHADER}' no encontrado. ¿URP instalado?"); return; }

        var mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))      mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic"))       mat.SetFloat("_Metallic",   metallic);
        if (mat.HasProperty("_Smoothness"))     mat.SetFloat("_Smoothness", smooth);

        if (emission && emColor != default)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emColor);
        }

        AssetDatabase.CreateAsset(mat, ruta);
        Debug.Log($"✅ Creado: {ruta}");
    }

    static void CrearURPTransparente(string nombre, Color color, float smooth)
    {
        string ruta = $"{CARPETA}/{nombre}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(ruta) != null)
        {
            Debug.Log($"[CubePile] {nombre} ya existe → omitido.");
            return;
        }

        var shader = Shader.Find(SHADER);
        if (shader == null) return;

        var mat = new Material(shader);

        // Configuración URP para superficie transparente
        mat.SetFloat("_Surface",  1f);    // Transparent
        mat.SetFloat("_Blend",    0f);    // Alpha
        mat.SetFloat("_ZWrite",   0f);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);

        AssetDatabase.CreateAsset(mat, ruta);
        Debug.Log($"✅ Creado: {ruta}");
    }
}
#endif

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Agrega este script al prefab del botón de nivel (prefabBotonNivel).
///
/// Asigna en el Inspector:
///   textoNombre      → el TMP que muestra "Nivel 1 [CONTRARRELOJ]"
///   textoDescripcion → el TMP que muestra la descripción o "[BLOQUEADO]"
///   imagenCandado    → la Image del candado que pusiste en el prefab
///   imagenFondo      → (opcional) Image de fondo del botón para cambiar color por tipo
///
/// LevelManager llama Configurar() al crear cada botón en MostrarSeleccionNivel().
/// </summary>
public class BotonNivelUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoDescripcion;
    public Image           imagenCandado;   // tu imagen de candado
    public Image           imagenFondo;     // fondo del botón (opcional, para colores por tipo)

    // ── Colores por tipo de nivel ──────────────────────────────────────────────
    private static readonly Color colorNormal      = new Color(0.20f, 0.20f, 0.30f, 1f); // gris oscuro
    private static readonly Color colorContrarreloj = new Color(0.80f, 0.20f, 0.15f, 1f); // rojo
    private static readonly Color colorEficiencia  = new Color(0.15f, 0.55f, 0.80f, 1f); // azul
    private static readonly Color colorPrecision   = new Color(0.60f, 0.20f, 0.80f, 1f); // púrpura
    private static readonly Color colorMaestro     = new Color(0.90f, 0.65f, 0.05f, 1f); // dorado
    private static readonly Color colorBloqueado   = new Color(0.15f, 0.15f, 0.20f, 0.7f);

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Configura el botón completo desde LevelManager.
    /// </summary>
    public void Configurar(string nombre, string etiqueta, string descripcion, bool desbloqueado)
    {
        // ── Nombre con etiqueta ───────────────────────────────────────────────
        string etiquetaTexto = string.IsNullOrEmpty(etiqueta) ? "" : $" [{etiqueta}]";
        if (textoNombre != null)
            textoNombre.text = nombre + etiquetaTexto;

        // ── Descripción o "[BLOQUEADO]" ───────────────────────────────────────
        if (textoDescripcion != null)
            textoDescripcion.text = desbloqueado ? descripcion : "BLOQUEADO";

        // ── Candado: visible solo cuando está bloqueado ───────────────────────
        if (imagenCandado != null)
            imagenCandado.gameObject.SetActive(!desbloqueado);

        // ── Color de fondo según tipo de nivel ────────────────────────────────
        if (imagenFondo != null)
        {
            imagenFondo.color = !desbloqueado ? colorBloqueado
                              : etiqueta == "CONTRARRELOJ" ? colorContrarreloj
                              : etiqueta == "EFICIENCIA"   ? colorEficiencia
                              : etiqueta == "PRECISION"    ? colorPrecision
                              : etiqueta == "MAESTRO"      ? colorMaestro
                              :                              colorNormal;
        }
    }
}

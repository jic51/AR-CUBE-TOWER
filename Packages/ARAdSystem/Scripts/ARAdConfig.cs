using UnityEngine;
using UnityEngine.Video;

namespace Layered.ARAdSystem
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ARAdConfig — ScriptableObject que define un AR Break completo.
    //
    //  El anunciante solo necesita configurar:
    //    1. El contenido (imagen / video / objeto 3D)
    //    2. El CTA (botón + URL)
    //    3. La recompensa (monedas para el jugador)
    //
    //  TODO LO DEMÁS SE AUTOAJUSTA:
    //    • Tamaño del ad según la distancia y el FOV del dispositivo
    //    • Borde oscuro (frame) proporcional al tamaño del ad
    //    • Texto de guía dinámico ("Gira a la derecha", "Mira arriba", etc.)
    //
    //  Cómo crear uno: Assets → Create → LAYERED → AR Ad Config
    // ─────────────────────────────────────────────────────────────────────────

    public enum TipoContenidoAd  { Imagen, Video, Objeto3D }
    public enum DireccionBreak   { Derecha, Izquierda, Arriba, Enfrente }

    [CreateAssetMenu(fileName = "NuevoAdConfig", menuName = "LAYERED/AR Ad Config", order = 0)]
    public class ARAdConfig : ScriptableObject
    {
        // ── Identificación ────────────────────────────────────────────────────

        [Header("Identificación")]
        [Tooltip("ID único — asignado por la plataforma LAYERED. Deja 'ad_000' para testing.")]
        public string adId        = "ad_000";
        [Tooltip("Nombre de la marca o campaña (solo para logs y analytics).")]
        public string nombreMarca = "Mi Marca";

        // ── Contenido ─────────────────────────────────────────────────────────

        [Header("Contenido AR")]
        public TipoContenidoAd tipoContenido = TipoContenidoAd.Imagen;

        [Tooltip("Textura PNG/JPG para modo Imagen. Cualquier resolución — se escala al tamaño del quad.")]
        public Texture2D  texturaImagen;

        [Tooltip("VideoClip .mp4 para modo Video.")]
        public VideoClip  videoClip;

        [Tooltip("Prefab 3D para modo Objeto3D. Debe tener sus propios materiales.")]
        public GameObject prefab3D;

        // ── Tamaño del ad ─────────────────────────────────────────────────────

        [Header("Tamaño (Auto recomendado)")]
        [Tooltip("ON = el tamaño se calcula automáticamente según FOV + distancia del dispositivo.\n" +
                 "OFF = usa los valores de Tamano Quad directamente.")]
        public bool  autoTamano         = true;

        [Tooltip("Porcentaje del ancho de pantalla que ocupa el ad (0.60 = 60%).\n" +
                 "Solo aplica cuando Auto Tamano está ON.")]
        [Range(0.15f, 0.85f)]
        public float porcentajePantalla = 0.60f;   // era 0.38: en pruebas se veía demasiado pequeño

        [Tooltip("Tamaño manual en metros (ancho × alto). Solo se usa cuando Auto Tamano está OFF.\n" +
                 "La relación de aspecto también se usa como referencia para el cálculo auto.")]
        public Vector2 tamanoQuad = new Vector2(0.80f, 0.45f);   // 16:9 por defecto

        // ── Posición en el mundo ──────────────────────────────────────────────

        [Header("Posición AR")]
        [Tooltip("Distancia en metros a la que aparece el ad frente al usuario.")]
        [Range(0.5f, 5f)]
        public float distanciaSpawn = 1.8f;

        [Tooltip("Desplazamiento vertical en metros respecto a la cámara (0 = nivel de ojos).")]
        [Range(-1f, 1f)]
        public float alturaOffset   = 0f;

        // ── Audio ─────────────────────────────────────────────────────────────

        [Header("Audio Espacial (opcional)")]
        [Tooltip("Se reproduce en 3D desde el objeto AR. Usa loops cortos.")]
        public AudioClip audioClip;

        [Range(0f, 1f)]
        public float volumenAudio = 0.8f;

        // ── Guía dinámica ─────────────────────────────────────────────────────

        [Header("Guía dinámica")]
        [Tooltip("Hacia dónde debe girar el usuario para revelar el ad.")]
        public DireccionBreak direccion = DireccionBreak.Enfrente;

        [Tooltip("Texto inicial antes de que el usuario encuentre el ad.\n" +
                 "Una vez el ad aparece, la guía se vuelve dinámica automáticamente:\n" +
                 "'Gira a la derecha', 'Mira arriba', etc.")]
        [TextArea(1, 2)]
        // El campo se llamaba "textoInstruccion". Sin este atributo, renombrarlo
        // descartó en silencio el texto de todos los ARAdConfig ya creados.
        [UnityEngine.Serialization.FormerlySerializedAs("textoInstruccion")]
        public string textoInstruccionInicial = "Move your phone to find the ad";

        // ── Timing ────────────────────────────────────────────────────────────

        [Header("Timing")]
        [Tooltip("Segundos desde que aparece el ad hasta que se muestra el botón Skip.\n" +
                 "Mínimo recomendado para anunciantes: 5 seg.")]
        [Range(3f, 30f)]
        public float duracionAntesDeSkipSegundos = 5f;

        [Tooltip("Duración máxima del break en segundos. Tras esto se cierra solo (cuenta como 'completo').")]
        [Range(5f, 60f)]
        public float duracionMaximaSegundos = 15f;

        // ── CTA ───────────────────────────────────────────────────────────────

        [Header("Call To Action")]
        [Tooltip("Texto del botón de acción principal.")]
        public string textoCTA = "Learn More";

        [Tooltip("URL o deep-link que se abre al pulsar el CTA.")]
        public string urlCTA   = "https://example.com";

        // ── Frame AR (borde oscuro) ────────────────────────────────────────────

        [Header("Frame AR — borde que encuadra el ad")]
        [Tooltip("Tamaño del borde oscuro como porcentaje del tamaño del ad (0.22 = 22% extra por lado).")]
        [Range(0.05f, 0.5f)]
        public float frameBordeRatio = 0.22f;

        [Tooltip("Suavidad del degradado borde↔transparente (0 = brusco, 0.15 = cinematográfico).")]
        [Range(0.01f, 0.3f)]
        public float frameSuavidad   = 0.08f;

        [Tooltip("Opacidad máxima del borde oscuro (0.9 = casi negro, deja ver el mundo por los bordes).")]
        [Range(0.3f, 1.0f)]
        public float frameOpacidad   = 0.88f;

        // ── Recompensa ────────────────────────────────────────────────────────

        [Header("Recompensa al jugador")]
        [Tooltip("Monedas que gana el jugador por ver el ad completo (sin skip).")]
        public int monedasRecompensaCompleto = 50;

        [Tooltip("Monedas que gana si hace skip (0 = sin recompensa por saltar).")]
        public int monedasRecompensaSkip     = 0;
    }
}

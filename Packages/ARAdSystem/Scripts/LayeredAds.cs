using System;
using UnityEngine;

namespace Layered.ARAdSystem
{
    // ─────────────────────────────────────────────────────────────────────────
    //  LayeredAds — API pública estática del SDK.
    //
    //  INTEGRACIÓN EN TU JUEGO (3 pasos):
    //
    //  1. INICIALIZAR — una sola vez al arrancar (ej: GameManager.Awake())
    //     LayeredAds.Inicializar("TU_API_KEY_AQUI");
    //
    //  2. CONFIGURAR ANUNCIO — pasa el ScriptableObject que definiste
    //     LayeredAds.SetAdConfig(miAdConfig);
    //
    //  3. MOSTRAR BREAK — cuando quieras (entre niveles, al perder, etc.)
    //     LayeredAds.MostrarBreak(resultado => {
    //         if (resultado.vioCompleto)
    //             EconomiaManager.Instance.GanarMonedas(resultado.monedasRecomendadas);
    //     });
    //
    //  El SDK crea y destruye el GameObject del break automáticamente.
    //  No necesitas agregar ningún GameObject a la escena.
    // ─────────────────────────────────────────────────────────────────────────

    public static class LayeredAds
    {
        // ── Estado interno ─────────────────────────────────────────────────────

        private static string     s_apiKey;
        private static ARAdConfig s_config;
        private static bool       s_ocupado;          // previene doble-spawn
        private static bool       s_inicializado;

        // ── Prefab del break (cargado desde Resources) ─────────────────────────
        // Crea Assets/LAYERED/ARAdSystem/Resources/ARAdBreakPrefab en el Editor
        // o asígnalo via LayeredAds.SetBreakPrefab() si no quieres usar Resources.
        private static GameObject s_breakPrefabOverride;

        // ── API PÚBLICA ────────────────────────────────────────────────────────

        /// <summary>
        /// Inicializa el SDK con tu API key de LAYERED.
        /// Llama esto UNA VEZ al inicio del juego.
        /// </summary>
        public static void Inicializar(string apiKey)
        {
            s_apiKey       = apiKey;
            s_inicializado = true;
            s_ocupado      = false;
            Debug.Log($"[LayeredAds] SDK inicializado. API key: {apiKey[..Math.Min(8, apiKey.Length)]}...");
        }

        /// <summary>
        /// Registra el ScriptableObject que define el anuncio a mostrar.
        /// Puedes cambiar este config entre llamadas para rotar creatividades.
        /// </summary>
        public static void SetAdConfig(ARAdConfig config)
        {
            s_config = config;
        }

        /// <summary>
        /// Permite sobrescribir el prefab del break (útil para testing).
        /// Si no lo llamas, el SDK busca "ARAdBreakPrefab" en Resources.
        /// </summary>
        public static void SetBreakPrefab(GameObject prefab)
        {
            s_breakPrefabOverride = prefab;
        }

        /// <summary>
        /// ¿El SDK está listo para mostrar un break?
        /// Comprueba esto antes de llamar MostrarBreak() si quieres ser explícito.
        /// </summary>
        public static bool EstaListo() => s_inicializado && s_config != null && !s_ocupado;

        /// <summary>
        /// Muestra el AR Break.
        /// El callback se llama cuando el break termina (completo, skip o error).
        /// </summary>
        /// <param name="onFinalizacion">
        ///   Callback que recibe el ARAdResult con el resultado de la interacción.
        ///   Se llama siempre, incluso si hay error.
        /// </param>
        public static void MostrarBreak(Action<ARAdResult> onFinalizacion)
        {
            if (!s_inicializado)
            {
                Debug.LogWarning("[LayeredAds] Llama Inicializar() antes de MostrarBreak().");
                onFinalizacion?.Invoke(ARAdResult.Error("sin_inicializar"));
                return;
            }

            if (s_config == null)
            {
                Debug.LogWarning("[LayeredAds] Sin AdConfig. Llama SetAdConfig() primero.");
                onFinalizacion?.Invoke(ARAdResult.Error("sin_config"));
                return;
            }

            if (s_ocupado)
            {
                // El callback DEBE invocarse siempre (contrato documentado arriba).
                // Si no lo hacemos, el juego que puso Time.timeScale = 0 antes de
                // llamarnos se queda congelado para siempre.
                Debug.LogWarning("[LayeredAds] Ya hay un break activo. Se ignora la llamada.");
                onFinalizacion?.Invoke(ARAdResult.Error("ocupado"));
                return;
            }

            s_ocupado = true;

            // Cargar el prefab del break
            GameObject prefab = s_breakPrefabOverride
                ?? Resources.Load<GameObject>("ARAdBreakPrefab");

            if (prefab == null)
            {
                Debug.LogError("[LayeredAds] No se encontró ARAdBreakPrefab en Resources/. " +
                               "Créalo en el Editor o usa SetBreakPrefab().");
                s_ocupado = false;
                onFinalizacion?.Invoke(ARAdResult.Error("sin_prefab"));
                return;
            }

            // Instanciar y configurar
            GameObject go      = UnityEngine.Object.Instantiate(prefab);
            var        breakMb = go.GetComponent<ARAdBreak>();

            if (breakMb == null)
            {
                Debug.LogError("[LayeredAds] El prefab no tiene componente ARAdBreak.");
                UnityEngine.Object.Destroy(go);
                s_ocupado = false;
                onFinalizacion?.Invoke(ARAdResult.Error("sin_componente"));
                return;
            }

            breakMb.Iniciar(s_config, resultado =>
            {
                s_ocupado = false;
                Debug.Log($"[LayeredAds] Break finalizado → {resultado}");
                onFinalizacion?.Invoke(resultado);
            });
        }

        // ── Helpers de log ─────────────────────────────────────────────────────

        /// <summary>Versión del SDK (SemVer).</summary>
        public static string Version => "1.0.0";
    }
}

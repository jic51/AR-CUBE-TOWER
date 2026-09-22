using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace Layered.ARAdSystem
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ARAdBreak — Gestiona el AR Break completo.
    //
    //  ARQUITECTURA DE CAPAS:
    //
    //  [Cámara AR]       → render del mundo real
    //  [FrameAR]         → quad world-space DETRÁS del ad:
    //                       borde oscuro, centro transparente, billboard, sin collider
    //  [AdAR]            → quad world-space con la imagen/video/3D, billboard
    //  [Canvas Overlay]  → texto instrucción dinámica + flecha + botones CTA/Skip
    //
    //  GUÍA DINÁMICA (nueva):
    //  • Mientras el usuario no mira al ad → muestra texto ("Turn right", "Look up")
    //    y flecha apuntando HACIA donde está el ad en pantalla.
    //  • En el momento en que apunta al ad (dot ≥ threshold) → texto y flecha desaparecen.
    //  • Si gira el teléfono y pierde el ad de vista → aparecen de nuevo.
    //  • Esto persiste durante todo el break, no solo al inicio.
    //
    //  AUTO-TAMAÑO:
    //  • Cuando ARAdConfig.autoTamano = true, el tamaño del ad se calcula
    //    automáticamente según el FOV del dispositivo y la distancia de spawn.
    //    El anunciante solo configura porcentajePantalla (0.38 = 38% del ancho).
    //  • El frame (borde oscuro) se dimensiona como porcentaje del ad (frameBordeRatio).
    //
    //  Estructura del prefab:
    //  ARAdBreakPrefab  [ARAdBreak + AudioSource]
    //  ├── PanelOscuro          (CanvasGroup — solo fade sutil de entrada)
    //  ├── TextoInstruccion     (TextMeshProUGUI)
    //  ├── ImagenFlecha         (Image)
    //  ├── BotonCTA             (Button + TextMeshProUGUI)
    //  ├── BotonSkip            (Button + TextMeshProUGUI)
    //  ├── TextoTimerSkip       (TextMeshProUGUI)
    //  └── ContenedorAR         (Transform vacío)
    // ─────────────────────────────────────────────────────────────────────────

    [RequireComponent(typeof(AudioSource))]
    public class ARAdBreak : MonoBehaviour
    {
        // ── Referencias UI ────────────────────────────────────────────────────

        [Header("Overlay (texto, flecha, botones)")]
        public CanvasGroup     panelOscuro;       // fade sutil al inicio
        public TextMeshProUGUI textoInstruccion;
        public Image           imagenFlecha;

        [Header("Botones")]
        public Button          botonCTA;
        public TextMeshProUGUI textoCTALabel;
        public Button          botonSkip;
        public TextMeshProUGUI textoTimerSkip;

        [Header("Contenido AR")]
        public Transform       contenedorAR;

        // ── Configuración técnica (raramente cambia) ──────────────────────────

        [Header("Técnico")]
        [Tooltip("Resolución de la textura del frame en píxeles (512 = suficiente para móvil).")]
        public int frameResolucion = 512;

        [Tooltip("Tamaño del frame para objetos 3D (metros, porque no tienen quad propio).")]
        public Vector2 frame3DSize = new Vector2(1.2f, 1.0f);

        // ── Estado interno ─────────────────────────────────────────────────────

        private ARAdConfig         _config;
        private Action<ARAdResult> _callback;
        private Transform          _camara;
        private AudioSource        _audio;
        private GameObject         _objetoAR;
        private GameObject         _frameAR;
        private VideoPlayer        _videoPlayer;
        private Texture2D          _texFrame;

        private Vector2 _tamanoAdCalculado;   // tamaño final del ad (auto o manual)

        private float _tiempoInicio;
        private bool  _esperandoDireccion;
        private bool  _guiaActiva;            // true = guía dinámica post-spawn activa
        private bool  _finalizado;

        private const float DotMinimo        = 0.92f;
        private const float OffsetFrameAtras = 0.03f;

        // Segundos máximos esperando a que el usuario apunte en la dirección pedida.
        // Al agotarse, el ad spawnea frente a la cámara igualmente: el usuario puede
        // estar acostado, contra una pared o con el tracking AR degradado, y el break
        // nunca debe quedar bloqueado sin salida.
        private const float SegundosMaxEspera = 8f;

        // ── Unity ──────────────────────────────────────────────────────────────

        void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.spatialBlend = 0f;
        }

        void Update()
        {
            if (_camara == null) return;
            BillboardLookAt(_objetoAR);
            BillboardLookAt(_frameAR);
            ActualizarGuia();    // guía dinámica: desaparece / reaparece según lo que mira el usuario
        }

        void OnDestroy()
        {
            if (_texFrame != null) { Destroy(_texFrame); _texFrame = null; }
        }

        static void BillboardLookAt(GameObject go)
        {
            if (go == null) return;
            go.transform.LookAt(Camera.main.transform);
            go.transform.Rotate(0f, 180f, 0f);
        }

        // ── API interna ────────────────────────────────────────────────────────

        // ── Layout de pantalla ─────────────────────────────────────────────────
        //
        // El anuncio vive en AR; la interfaz (Skip, CTA, instrucción) vive en la
        // pantalla. El prefab traía su canvas en tamaño de píxel constante con
        // referencia 800×600 y todo anclado al centro: en un teléfono de 1080×2400
        // los botones medían 120×25 px y quedaban en medio de la pantalla. Se
        // corrige aquí, en código, para que ningún anunciante ni juego tenga que
        // maquetar el prefab: el SDK se ve bien en cualquier resolución.

        const float RefAncho = 1080f, RefAlto = 1920f;   // unidades de diseño
        const float Margen   = 48f;

        void AjustarLayoutPantalla()
        {
            var scaler = GetComponentInChildren<CanvasScaler>(true);
            if (scaler != null)
            {
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(RefAncho, RefAlto);
                scaler.matchWidthOrHeight  = 0.5f;
            }

            // Anclas en coordenadas normalizadas del área segura: respetan la
            // muesca y la barra de gestos sin depender del factor de escala
            Rect  seg  = Screen.safeArea;
            float w    = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            var   arribaDer = new Vector2(seg.xMax / w, seg.yMax / h);
            float abajo     = seg.yMin / h;

            // Skip: arriba a la derecha, donde lo busca cualquiera que haya visto un anuncio
            Colocar(botonSkip,  arribaDer, arribaDer, new Vector2(1f, 1f),
                    new Vector2(-Margen, -Margen), new Vector2(320f, 110f));

            // CTA: abajo al centro, ancho y grande — es la acción que paga al anunciante
            Colocar(botonCTA, new Vector2(0.5f, abajo), new Vector2(0.5f, abajo), new Vector2(0.5f, 0f),
                    new Vector2(0f, Margen * 2f), new Vector2(640f, 140f));

            // Instrucción y flecha: tercio superior, sin tapar el centro donde aparece el ad
            if (textoInstruccion != null)
            {
                Colocar(textoInstruccion.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(940f, 180f));
                AutoTamanoTexto(textoInstruccion, 34f, 64f);
            }
            if (imagenFlecha != null)
                Colocar(imagenFlecha.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(200f, 300f));

            AutoTamanoTexto(botonSkip != null ? botonSkip.GetComponentInChildren<TMP_Text>(true) : null, 26f, 48f);
            AutoTamanoTexto(textoCTALabel, 30f, 58f);
        }

        static void Colocar(Component c, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 tam)
        {
            if (c == null) return;
            var rt = c.transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
        }

        static void AutoTamanoTexto(TMP_Text t, float min, float max)
        {
            if (t == null) return;
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
            t.alignment   = TextAlignmentOptions.Center;
        }

        public void Iniciar(ARAdConfig config, Action<ARAdResult> callback)
        {
            AjustarLayoutPantalla();

            _config   = config;
            _callback = callback;
            _camara   = Camera.main?.transform;

            if (_camara == null)
            {
                Debug.LogError("[ARAdBreak] No se encontró Camera.main.");
                Finalizar(ARAdResult.Error(config.adId));
                return;
            }

            // Calcular tamaño del ad una sola vez (auto o manual)
            _tamanoAdCalculado = CalcularTamanoAd();

            // Texto inicial de instrucción
            if (textoInstruccion != null)
                textoInstruccion.text = config.textoInstruccionInicial;

            if (textoCTALabel != null)
                textoCTALabel.text = config.textoCTA;

            // Flecha apunta a la dirección configurada (fija hasta que spawna el ad)
            if (imagenFlecha != null)
                imagenFlecha.transform.rotation = DireccionARotacion(config.direccion);

            botonCTA.onClick.AddListener(OnClic);
            botonSkip.onClick.AddListener(OnSkip);
            botonCTA.gameObject.SetActive(false);
            botonSkip.gameObject.SetActive(false);
            if (textoTimerSkip != null) textoTimerSkip.gameObject.SetActive(false);

            // Valor provisional; se reinicia en el spawn, cuando el ad se hace visible.
            _tiempoInicio       = Time.unscaledTime;
            _esperandoDireccion = true;
            _guiaActiva         = false;
            _finalizado         = false;

            StartCoroutine(FlujoBreak());
        }

        // ── Auto-tamaño ────────────────────────────────────────────────────────

        /// <summary>
        /// Calcula el tamaño óptimo del ad en metros.
        /// Si autoTamano=true: el ancho = X% del ancho visible a esa distancia según el FOV.
        /// La relación de aspecto se preserva de tamanoQuad (default 16:9).
        /// </summary>
        Vector2 CalcularTamanoAd()
        {
            if (!_config.autoTamano)
                return _config.tamanoQuad;

            var cam = _camara.GetComponent<Camera>();
            if (cam == null) return _config.tamanoQuad;

            // FOV horizontal desde el FOV vertical y el aspect del dispositivo
            float fovV = cam.fieldOfView * Mathf.Deg2Rad;
            float fovH = 2f * Mathf.Atan(Mathf.Tan(fovV * 0.5f) * cam.aspect);

            // Ancho visible a la distancia de spawn
            float anchoVisible = 2f * _config.distanciaSpawn * Mathf.Tan(fovH * 0.5f);
            float adWidth      = anchoVisible * _config.porcentajePantalla;

            // Preservar la relación de aspecto de tamanoQuad
            float ratio = (_config.tamanoQuad.x > 0.001f && _config.tamanoQuad.y > 0.001f)
                ? _config.tamanoQuad.x / _config.tamanoQuad.y
                : 16f / 9f;

            float adHeight = adWidth / ratio;
            return new Vector2(adWidth, adHeight);
        }

        // ── Guía dinámica ──────────────────────────────────────────────────────

        /// <summary>
        /// Llamada cada frame desde Update().
        /// Mientras la guía está activa (ad en AR), muestra u oculta el texto y la flecha
        /// según si el usuario está mirando al ad o no.
        /// La flecha rota dinámicamente hacia donde está el ad en la vista del usuario.
        /// </summary>
        void ActualizarGuia()
        {
            if (!_guiaActiva || _objetoAR == null) return;
            if (textoInstruccion == null && imagenFlecha == null) return;

            // ¿Está el usuario apuntando al ad?
            Vector3 dirAlAd = (_objetoAR.transform.position - _camara.position).normalized;
            float   dot     = Vector3.Dot(_camara.forward, dirAlAd);
            bool    viendo  = dot >= DotMinimo;

            bool mostrarGuia = !viendo;

            // Texto: activa/desactiva según visibilidad
            if (textoInstruccion != null && textoInstruccion.gameObject.activeSelf != mostrarGuia)
                textoInstruccion.gameObject.SetActive(mostrarGuia);

            // Flecha: activa/desactiva + orienta hacia el ad
            if (imagenFlecha != null)
            {
                if (imagenFlecha.gameObject.activeSelf != mostrarGuia)
                    imagenFlecha.gameObject.SetActive(mostrarGuia);

                if (mostrarGuia)
                {
                    // Proyectar la dirección al ad en el espacio local de la cámara
                    // camLocal.x > 0 = ad a la derecha, < 0 = izquierda
                    // camLocal.y > 0 = ad arriba, < 0 = abajo
                    Vector3 camLocal = _camara.InverseTransformDirection(dirAlAd);
                    float   angulo   = Mathf.Atan2(camLocal.x, camLocal.y) * Mathf.Rad2Deg;
                    imagenFlecha.transform.rotation = Quaternion.Euler(0f, 0f, -angulo);

                    // Pulso suave en la flecha
                    float pulso = 0.9f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
                    imagenFlecha.transform.localScale = Vector3.one * pulso;

                    // Texto dinámico según la dirección dominante
                    if (textoInstruccion != null)
                    {
                        float absX = Mathf.Abs(camLocal.x);
                        float absY = Mathf.Abs(camLocal.y);
                        textoInstruccion.text = (absX > absY)
                            ? (camLocal.x > 0f ? "Turn right"  : "Turn left")
                            : (camLocal.y > 0f ? "Look up"     : "Look down");
                    }
                }
            }
        }

        // ── Flujo principal ────────────────────────────────────────────────────

        IEnumerator FlujoBreak()
        {
            // 1. Fade-in muy sutil del overlay (background detrás del texto)
            yield return StartCoroutine(FadeAlpha(panelOscuro, 0f, 0.30f, 0.35f));

            // 2. Esperar a que el usuario apunte en la dirección inicial configurada
            yield return StartCoroutine(EsperarDireccion());

            // 3. Spawnar ad + frame en AR
            SpawnObjetoAR();

            // El reloj del anuncio arranca AQUÍ, no en Iniciar(): la duración
            // contratada por el anunciante es tiempo con el ad visible. Si el reloj
            // corriera durante la espera de dirección, un usuario que tarda en apuntar
            // cerraría el break al instante y se reportaría como impresión completa.
            _tiempoInicio = Time.unscaledTime;

            // Activar guía dinámica: a partir de aquí, ActualizarGuia() en Update()
            // se encarga de mostrar/ocultar texto y flecha automáticamente
            _guiaActiva = true;

            // 4. Fade-out del fondo del overlay (el texto/flecha los controla la guía)
            yield return StartCoroutine(FadeAlpha(panelOscuro, 0.30f, 0f, 0.25f));

            // 5. Skip countdown
            // El botón se ve desde ya pero NO se puede pulsar hasta que termine la
            // cuenta: antes estaba activo desde el segundo 0 y la cuenta era solo
            // decorativa. La cuenta atrás se muestra en el propio botón; el texto
            // suelto de arriba se oculta para no mostrar dos contadores distintos.
            var labelSkip = botonSkip.GetComponentInChildren<TMP_Text>(true);
            var textoCuenta = labelSkip != null ? labelSkip : textoTimerSkip;
            if (textoTimerSkip != null && textoTimerSkip != textoCuenta)
                textoTimerSkip.gameObject.SetActive(false);

            botonSkip.interactable = false;
            botonSkip.gameObject.SetActive(true);
            if (textoCuenta != null) textoCuenta.gameObject.SetActive(true);

            float timerSkip = _config.duracionAntesDeSkipSegundos;
            while (timerSkip > 0f && !_finalizado)
            {
                timerSkip -= Time.unscaledDeltaTime;
                if (textoCuenta != null)
                    textoCuenta.text = $"Skip in {Mathf.CeilToInt(timerSkip)}s";
                yield return null;
            }

            // 6. Skip y CTA habilitados
            if (!_finalizado)
            {
                botonSkip.interactable = true;
                if (textoCuenta != null) textoCuenta.text = "Skip";
                botonCTA.gameObject.SetActive(true);
            }

            // 7. Duración máxima del break
            float tiempoRestante = _config.duracionMaximaSegundos
                                   - (Time.unscaledTime - _tiempoInicio);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoRestante));

            if (!_finalizado)
                Finalizar(ARAdResult.Completo(TiempoVisualizacion(),
                                              _config.monedasRecompensaCompleto,
                                              _config.adId));
        }

        /// <summary>
        /// Espera a que el usuario apunte en la dirección configurada, con un tope
        /// de SegundosMaxEspera. Si no lo consigue a tiempo, el ad spawnea igualmente
        /// frente a la cámara — el break jamás debe quedarse esperando indefinidamente.
        /// </summary>
        IEnumerator EsperarDireccion()
        {
            float espera = 0f;

            while (_esperandoDireccion && espera < SegundosMaxEspera && !_finalizado)
            {
                espera += Time.unscaledDeltaTime;

                // La cámara puede desaparecer si se recarga la escena durante el break
                if (_camara == null) break;

                Vector3 camFwd = _camara.forward;
                camFwd.y = 0f;
                camFwd.Normalize();

                float dot = _config.direccion == DireccionBreak.Arriba
                    ? Vector3.Dot(_camara.forward, Vector3.up)
                    : Vector3.Dot(camFwd, DireccionAVector(_config.direccion));

                if (dot >= DotMinimo) _esperandoDireccion = false;

                // Pulso de la flecha durante la espera inicial
                if (imagenFlecha != null)
                {
                    float p = 0.9f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f);
                    imagenFlecha.transform.localScale = Vector3.one * p;
                }
                yield return null;
            }

            // Salida garantizada: por dirección alcanzada o por timeout.
            _esperandoDireccion = false;
            // No ocultar texto/flecha aquí → la guía dinámica toma el control al activarse
        }

        // ── Spawn: ad + frame ──────────────────────────────────────────────────

        void SpawnObjetoAR()
        {
            Vector3 spawnPos = _camara.position
                + _camara.forward * _config.distanciaSpawn
                + Vector3.up      * _config.alturaOffset;

            Vector2 adSize;

            switch (_config.tipoContenido)
            {
                case TipoContenidoAd.Imagen:
                    SpawnImagen(spawnPos);
                    adSize = _tamanoAdCalculado;
                    break;
                case TipoContenidoAd.Video:
                    SpawnVideo(spawnPos);
                    adSize = _tamanoAdCalculado;
                    break;
                case TipoContenidoAd.Objeto3D:
                    SpawnObjeto3D(spawnPos);
                    adSize = frame3DSize;
                    break;
                default:
                    adSize = _tamanoAdCalculado;
                    break;
            }

            // Frame AR: borde oscuro adaptado al tamaño del ad (desde config)
            SpawnFrameAR(spawnPos, adSize);

            // Audio espacial desde el ad
            if (_config.audioClip != null && _objetoAR != null)
            {
                var src = _objetoAR.AddComponent<AudioSource>();
                src.clip         = _config.audioClip;
                src.spatialBlend = 1.0f;
                src.volume       = _config.volumenAudio;
                src.loop         = true;
                src.Play();
            }

            // Animación de entrada: escala 0 → 110% → 100%
            if (_objetoAR != null) StartCoroutine(AnimarEntrada(_objetoAR.transform));
            if (_frameAR  != null) StartCoroutine(AnimarEntrada(_frameAR.transform, delay: 0.05f));
        }

        // ── Spawn del contenido ────────────────────────────────────────────────

        void SpawnImagen(Vector3 pos)
        {
            _objetoAR      = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _objetoAR.name = "ARAdImagen";
            ConfigurarObjetoAR(_objetoAR, pos, _tamanoAdCalculado);

            if (_config.texturaImagen != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.mainTexture = _config.texturaImagen;
                _objetoAR.GetComponent<Renderer>().material = mat;
            }
        }

        void SpawnVideo(Vector3 pos)
        {
            _objetoAR      = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _objetoAR.name = "ARAdVideo";
            ConfigurarObjetoAR(_objetoAR, pos, _tamanoAdCalculado);

            var rt = new RenderTexture(
                Mathf.RoundToInt(_tamanoAdCalculado.x * 512),
                Mathf.RoundToInt(_tamanoAdCalculado.y * 512), 0);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.mainTexture = rt;
            _objetoAR.GetComponent<Renderer>().material = mat;

            _videoPlayer = _objetoAR.AddComponent<VideoPlayer>();
            _videoPlayer.clip            = _config.videoClip;
            _videoPlayer.renderMode      = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture   = rt;
            _videoPlayer.isLooping       = true;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.Play();
        }

        void SpawnObjeto3D(Vector3 pos)
        {
            if (_config.prefab3D == null) { Debug.LogWarning("[ARAdBreak] Sin prefab3D."); return; }
            _objetoAR      = Instantiate(_config.prefab3D, pos, Quaternion.identity, contenedorAR);
            _objetoAR.name = "ARAdObjeto3D";
        }

        void ConfigurarObjetoAR(GameObject go, Vector3 pos, Vector2 size)
        {
            Destroy(go.GetComponent<MeshCollider>());
            go.transform.SetParent(contenedorAR, worldPositionStays: true);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        // ── Frame AR ──────────────────────────────────────────────────────────
        //
        //  Quad transparente/oscuro DETRÁS del ad que enfoca al usuario.
        //  Tamaño = tamanoAd + margen (frameBordeRatio × tamanoAd por lado).
        //  Centro = hole transparente del mismo ratio que el ad.
        //  Sin collider → nunca bloquea interacciones.
        // ─────────────────────────────────────────────────────────────────────

        void SpawnFrameAR(Vector3 adPos, Vector2 adSize)
        {
            // Borde en metros calculado como % del tamaño del ad (desde ARAdConfig)
            float bordeH = adSize.x * _config.frameBordeRatio;
            float bordeV = adSize.y * _config.frameBordeRatio;

            float frameW = adSize.x + bordeH * 2f;
            float frameH = adSize.y + bordeV * 2f;

            float holeU = adSize.x / frameW;   // ratio del hole en U (horizontal)
            float holeV = adSize.y / frameH;   // ratio del hole en V (vertical)

            _frameAR      = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _frameAR.name = "ARAdFrame";
            Destroy(_frameAR.GetComponent<MeshCollider>());

            _frameAR.transform.SetParent(contenedorAR, worldPositionStays: true);

            // 3cm más lejos de la cámara que el ad (siempre detrás, nunca delante)
            Vector3 dirACamera = (_camara.position - adPos).normalized;
            _frameAR.transform.position   = adPos - dirACamera * OffsetFrameAtras;
            _frameAR.transform.localScale = new Vector3(frameW, frameH, 1f);

            _texFrame = GenerarTexturaFrame(frameResolucion, holeU, holeV,
                                            _config.frameSuavidad, _config.frameOpacidad);
            _frameAR.GetComponent<Renderer>().material = CrearMaterialTransparente(_texFrame);
        }

        /// <summary>
        /// Textura con hole rectangular centrado:
        /// • Centro (hole) → alpha = 0 (transparente → el usuario ve el ad)
        /// • Borde exterior → alpha = frameOpacidad (oscuro → focaliza la atención)
        /// • Gradiente SmoothStep entre ambas zonas (frameSuavidad controla la transición)
        /// </summary>
        Texture2D GenerarTexturaFrame(int size, float holeU, float holeV,
                                       float suavidad, float alphaMax)
        {
            var tex       = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            tex.wrapMode  = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            float hL = 0.5f - holeU * 0.5f;   // borde izquierdo del hole (UV)
            float hR = 0.5f + holeU * 0.5f;   // borde derecho
            float hB = 0.5f - holeV * 0.5f;   // borde inferior
            float hT = 0.5f + holeV * 0.5f;   // borde superior

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float v = (float)y / (size - 1);

                    // Distancia al borde del hole (0 dentro del hole, > 0 en el borde)
                    float dx   = Mathf.Max(hL - u, u - hR, 0f);
                    float dy   = Mathf.Max(hB - v, v - hT, 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.SmoothStep(0f, alphaMax,
                                                   dist / Mathf.Max(suavidad, 0.001f));

                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: false);
            return tex;
        }

        static Material CrearMaterialTransparente(Texture2D tex)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface",  1f);
            mat.SetFloat("_Blend",    0f);
            mat.SetFloat("_ZWrite",   0f);
            mat.SetInt("_ZWrite",     0);
            mat.SetInt("_SrcBlend",   (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",   (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.mainTexture = tex;
            mat.color       = Color.white;
            return mat;
        }

        // ── Animación de entrada ───────────────────────────────────────────────

        IEnumerator AnimarEntrada(Transform t, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            float   dur         = 0.35f;
            float   elapsed     = 0f;
            Vector3 escalaFinal = t.localScale;
            t.localScale = Vector3.zero;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float p      = Mathf.Clamp01(elapsed / dur);
                float factor = p < 0.75f
                    ? Mathf.Lerp(0f,   1.1f, p / 0.75f)
                    : Mathf.Lerp(1.1f, 1.0f, (p - 0.75f) / 0.25f);
                t.localScale = escalaFinal * factor;
                yield return null;
            }
            t.localScale = escalaFinal;
        }

        // ── Botones ────────────────────────────────────────────────────────────

        void OnClic()
        {
            if (_finalizado) return;

            if (TryNormalizarUrl(_config.urlCTA, out string url))
                Application.OpenURL(url);
            else
                Debug.LogWarning($"[ARAdBreak] URL de CTA no válida en '{_config.adId}': \"{_config.urlCTA}\"");

            // El clic cuenta aunque la URL falle: el usuario interactuó con el anuncio
            Finalizar(ARAdResult.ConClic(TiempoVisualizacion(),
                                         _config.monedasRecompensaCompleto,
                                         _config.adId));
        }

        /// <summary>
        /// Acepta lo que un anunciante escribiría a mano ("www.marca.com",
        /// "marca.com/promo") y lo convierte en una URL abrible añadiendo https://.
        /// Solo permite http y https: bloquea intent://, tel:, market:// y deep
        /// links a otras apps, que no deben poder dispararse desde un anuncio.
        /// </summary>
        static bool TryNormalizarUrl(string entrada, out string url)
        {
            url = null;
            if (string.IsNullOrWhiteSpace(entrada)) return false;

            string s = entrada.Trim();
            if (!s.Contains("://")) s = "https://" + s;

            if (!Uri.TryCreate(s, UriKind.Absolute, out Uri uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;

            url = uri.AbsoluteUri;
            return true;
        }

        void OnSkip()
        {
            if (_finalizado) return;
            Finalizar(ARAdResult.Skip(TiempoVisualizacion(),
                                      _config.monedasRecompensaSkip,
                                      _config.adId));
        }

        // ── Finalización ───────────────────────────────────────────────────────

        void Finalizar(ARAdResult resultado)
        {
            if (_finalizado) return;
            _finalizado = true;
            _guiaActiva = false;
            StopAllCoroutines();
            StartCoroutine(FadeYDestruir(resultado));
        }

        IEnumerator FadeYDestruir(ARAdResult resultado)
        {
            // Ocultar texto y flecha al cerrar
            if (textoInstruccion != null) textoInstruccion.gameObject.SetActive(false);
            if (imagenFlecha     != null) imagenFlecha.gameObject.SetActive(false);

            if (panelOscuro != null)
                yield return StartCoroutine(FadeAlpha(panelOscuro, panelOscuro.alpha, 0f, 0.2f));

            yield return StartCoroutine(CerrarObjeto(_objetoAR));
            yield return StartCoroutine(CerrarObjeto(_frameAR));

            _callback?.Invoke(resultado);
            Destroy(gameObject);
        }

        IEnumerator CerrarObjeto(GameObject go)
        {
            if (go == null) yield break;
            float   t     = 0f;
            Vector3 escIn = go.transform.localScale;
            while (t < 0.22f)
            {
                t += Time.unscaledDeltaTime;
                go.transform.localScale = Vector3.Lerp(escIn, Vector3.zero, t / 0.22f);
                yield return null;
            }
            Destroy(go);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        float TiempoVisualizacion() => Time.unscaledTime - _tiempoInicio;

        static Vector3 DireccionAVector(DireccionBreak dir) => dir switch
        {
            DireccionBreak.Derecha   => Vector3.right,
            DireccionBreak.Izquierda => Vector3.left,
            DireccionBreak.Arriba    => Vector3.up,
            _                        => Vector3.forward
        };

        static Quaternion DireccionARotacion(DireccionBreak dir) => dir switch
        {
            DireccionBreak.Derecha   => Quaternion.Euler(0, 0, -90),
            DireccionBreak.Izquierda => Quaternion.Euler(0, 0,  90),
            DireccionBreak.Arriba    => Quaternion.Euler(0, 0,   0),
            _                        => Quaternion.identity
        };

        IEnumerator FadeAlpha(CanvasGroup cg, float desde, float hasta, float dur)
        {
            if (cg == null) yield break;
            float t = 0f;
            cg.alpha = desde;
            while (t < dur)
            {
                t       += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(desde, hasta, t / dur);
                yield return null;
            }
            cg.alpha = hasta;
        }
    }
}

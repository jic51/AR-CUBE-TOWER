using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class DatosNivel
{
    public string nombre;
    public float  metaAltura;           // metros que hay que alcanzar
    public float  tiempoLimite;         // segundos (0 = sin límite, modo survival)
    public int    cubosMaximos;         // 0 = ilimitado
    public float  intervaloEntrega;     // segundos entre cubo y cubo (default 1.0)
    public bool   snapDesactivado;      // true = sin ayuda de snap
    public string etiquetaEspecial;     // "TIME TRIAL", "EFFICIENCY", "PRECISION", etc.
    [TextArea(1, 2)]
    public string descripcion;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    // Alto de un cubo a escala 1. Las metas se escriben en cubos (N * CUBO) y en
    // partida se multiplican por la escala elegida de la plataforma, porque los
    // cubos crecen con ella: la dificultad no depende del tamaño que elija el jugador.
    public const float CUBO = 0.15f;

    // ── 40 niveles ───────────────────────────────────────────────────────────
    // Curva de dificultad (2026-09-22): de 3 a 41 cubos; el tiempo concedido baja
    // de 4.0 a 2.4 s por cubo y la grúa entrega de cada 1.2 s a cada 0.75 s.
    //   TIME TRIAL: 75 % del tiempo · EFFICIENCY: sin reloj, cubos limitados
    //   PRECISION / sin snap: +15 % de tiempo · MASTER: +15 % de cubos, 90 % del tiempo
    //
    // [NonSerialized] a propósito: esta tabla es la ÚNICA fuente de verdad. Siendo
    // un campo público serializado, la escena guardaba una copia vieja de 6 niveles
    // que pisaba esta y por eso nunca se vieron los 40.
    [System.NonSerialized]
    public DatosNivel[] niveles = new DatosNivel[]
    {
        // ── BLOQUE 1: Intro (1-5) ───────────────────────────────────────
        new DatosNivel {
            nombre = "Level 1 — First Tower", metaAltura = 3f * CUBO, tiempoLimite = 20f,
            cubosMaximos = 0, intervaloEntrega = 1.20f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Your first tower: 3 cubes high. Learn to aim and drop."
        },
        new DatosNivel {
            nombre = "Level 2 — Going Higher", metaAltura = 4f * CUBO, tiempoLimite = 25f,
            cubosMaximos = 0, intervaloEntrega = 1.19f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 4 cubes in 25s. Find your rhythm."
        },
        new DatosNivel {
            nombre = "Level 3 — First Wobble", metaAltura = 5f * CUBO, tiempoLimite = 30f,
            cubosMaximos = 0, intervaloEntrega = 1.18f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 5 cubes in 30s. Steady hands win."
        },
        new DatosNivel {
            nombre = "Level 4 — Steady Climb", metaAltura = 6f * CUBO, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 1.17f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 6 cubes in 35s. Build it clean."
        },
        new DatosNivel {
            nombre = "Level 5 — Level Up", metaAltura = 6f * CUBO, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 1.15f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 6 cubes in 35s. Stay calm and stack."
        },

        // ── BLOQUE 2: Velocidad + Eficiencia (6-10) ─────────────────────
        new DatosNivel {
            nombre = "Level 6 — Faster Blocks", metaAltura = 7f * CUBO, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 1.14f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 7 cubes in 35s. Keep it steady."
        },
        new DatosNivel {
            nombre = "Level 7 — Race", metaAltura = 8f * CUBO, tiempoLimite = 30f,
            cubosMaximos = 0, intervaloEntrega = 1.02f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Stack 8 cubes in 30s. Beat the clock."
        },
        new DatosNivel {
            nombre = "Level 8 — Steady Hands", metaAltura = 9f * CUBO, tiempoLimite = 45f,
            cubosMaximos = 0, intervaloEntrega = 1.12f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 9 cubes in 45s. Steady hands win."
        },
        new DatosNivel {
            nombre = "Level 9 — Count Your Blocks", metaAltura = 10f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 13, intervaloEntrega = 1.11f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 10 cubes using at most 13. Limited cubes. Don't waste one."
        },
        new DatosNivel {
            nombre = "Level 10 — The Real Test", metaAltura = 11f * CUBO, tiempoLimite = 50f,
            cubosMaximos = 0, intervaloEntrega = 1.10f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 11 cubes in 50s. Stay calm and stack."
        },

        // ── BLOQUE 3: Precisión (11-15) ─────────────────────────────────
        new DatosNivel {
            nombre = "Level 11 — No Safety Net", metaAltura = 12f * CUBO, tiempoLimite = 60f,
            cubosMaximos = 0, intervaloEntrega = 1.08f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 12 cubes in 60s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 12 — Master Builder", metaAltura = 12f * CUBO, tiempoLimite = 55f,
            cubosMaximos = 0, intervaloEntrega = 1.07f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Stack 12 cubes in 55s. Find your rhythm."
        },
        new DatosNivel {
            nombre = "Level 13 — Efficient Master", metaAltura = 13f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 17, intervaloEntrega = 1.06f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 13 cubes using at most 17. Limited cubes. Don't waste one."
        },
        new DatosNivel {
            nombre = "Level 14 — Architect", metaAltura = 14f * CUBO, tiempoLimite = 70f,
            cubosMaximos = 0, intervaloEntrega = 1.05f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 14 cubes in 70s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 15 — Legend", metaAltura = 17f * CUBO, tiempoLimite = 60f,
            cubosMaximos = 0, intervaloEntrega = 1.04f, snapDesactivado = false,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 17 cubes in 60s. Taller and faster. Masters only."
        },

        // ── BLOQUE 4: Élite (16-20) ─────────────────────────────────────
        new DatosNivel {
            nombre = "Level 16 — Sharp Eye", metaAltura = 16f * CUBO, tiempoLimite = 75f,
            cubosMaximos = 0, intervaloEntrega = 1.03f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 16 cubes in 75s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 17 — Efficient Stack", metaAltura = 17f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 21, intervaloEntrega = 1.02f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 17 cubes using at most 21. Limited cubes. Don't waste one."
        },
        new DatosNivel {
            nombre = "Level 18 — Master Class", metaAltura = 20f * CUBO, tiempoLimite = 70f,
            cubosMaximos = 0, intervaloEntrega = 1.00f, snapDesactivado = false,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 20 cubes in 70s. Taller and faster. Masters only."
        },
        new DatosNivel {
            nombre = "Level 19 — Ultra Efficient", metaAltura = 18f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 22, intervaloEntrega = 0.99f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 18 cubes using at most 22. Limited cubes. Don't waste one. No snap."
        },
        new DatosNivel {
            nombre = "Level 20 — Race The Clock", metaAltura = 19f * CUBO, tiempoLimite = 55f,
            cubosMaximos = 0, intervaloEntrega = 0.88f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Stack 19 cubes in 55s. Beat the clock."
        },

        // ── BLOQUE 5: Leyenda (21-25) ───────────────────────────────────
        new DatosNivel {
            nombre = "Level 21 — The Summit", metaAltura = 23f * CUBO, tiempoLimite = 85f,
            cubosMaximos = 0, intervaloEntrega = 0.97f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 23 cubes in 85s. Taller and faster. Masters only. No snap."
        },
        new DatosNivel {
            nombre = "Level 22 — Double Challenge", metaAltura = 24f * CUBO, tiempoLimite = 90f,
            cubosMaximos = 0, intervaloEntrega = 0.96f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 24 cubes in 90s. Taller and faster. Masters only. No snap."
        },
        new DatosNivel {
            nombre = "Level 23 — Peak Builder", metaAltura = 22f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 27, intervaloEntrega = 0.95f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 22 cubes using at most 27. Limited cubes. Don't waste one."
        },
        new DatosNivel {
            nombre = "Level 24 — Sky Reach", metaAltura = 23f * CUBO, tiempoLimite = 95f,
            cubosMaximos = 0, intervaloEntrega = 0.93f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 23 cubes in 95s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 25 — LEGEND FINAL", metaAltura = 26f * CUBO, tiempoLimite = 90f,
            cubosMaximos = 0, intervaloEntrega = 0.92f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 26 cubes in 90s. Taller and faster. Masters only. No snap."
        },

        // ── BLOQUE 6: Divino (26-30) ────────────────────────────────────
        new DatosNivel {
            nombre = "Level 26 — Efficiency God", metaAltura = 24f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 29, intervaloEntrega = 0.91f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 24 cubes using at most 29. Limited cubes. Don't waste one. No snap."
        },
        new DatosNivel {
            nombre = "Level 27 — Flash Builder", metaAltura = 25f * CUBO, tiempoLimite = 65f,
            cubosMaximos = 0, intervaloEntrega = 0.81f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Stack 25 cubes in 65s. Beat the clock."
        },
        new DatosNivel {
            nombre = "Level 28 — The Surgeon", metaAltura = 26f * CUBO, tiempoLimite = 100f,
            cubosMaximos = 0, intervaloEntrega = 0.89f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 26 cubes in 100s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 29 — Overdrive", metaAltura = 31f * CUBO, tiempoLimite = 90f,
            cubosMaximos = 0, intervaloEntrega = 0.88f, snapDesactivado = false,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 31 cubes in 90s. Taller and faster. Masters only."
        },
        new DatosNivel {
            nombre = "Level 30 — Iron Will", metaAltura = 32f * CUBO, tiempoLimite = 105f,
            cubosMaximos = 0, intervaloEntrega = 0.87f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 32 cubes in 105s. Taller and faster. Masters only. No snap."
        },

        // ── BLOQUE 7: Trascendente (31-35) ──────────────────────────────
        new DatosNivel {
            nombre = "Level 31 — Ghost Protocol", metaAltura = 28f * CUBO, tiempoLimite = 100f,
            cubosMaximos = 0, intervaloEntrega = 0.85f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 28 cubes in 100s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 32 — Blitz", metaAltura = 29f * CUBO, tiempoLimite = 70f,
            cubosMaximos = 0, intervaloEntrega = 0.76f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Stack 29 cubes in 70s. Beat the clock."
        },
        new DatosNivel {
            nombre = "Level 33 — Minimalist", metaAltura = 30f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 35, intervaloEntrega = 0.83f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 30 cubes using at most 35. Limited cubes. Don't waste one. No snap."
        },
        new DatosNivel {
            nombre = "Level 34 — The Architect", metaAltura = 36f * CUBO, tiempoLimite = 110f,
            cubosMaximos = 0, intervaloEntrega = 0.82f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 36 cubes in 110s. Taller and faster. Masters only. No snap."
        },
        new DatosNivel {
            nombre = "Level 35 — Clockwork", metaAltura = 32f * CUBO, tiempoLimite = 70f,
            cubosMaximos = 0, intervaloEntrega = 0.73f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Stack 32 cubes in 70s. Beat the clock."
        },

        // ── BLOQUE 8: Mítico (36-40) ────────────────────────────────────
        new DatosNivel {
            nombre = "Level 36 — Six Sigma", metaAltura = 33f * CUBO, tiempoLimite = 0f,
            cubosMaximos = 38, intervaloEntrega = 0.80f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "Stack 33 cubes using at most 38. Limited cubes. Don't waste one. No snap."
        },
        new DatosNivel {
            nombre = "Level 37 — Warp Speed", metaAltura = 34f * CUBO, tiempoLimite = 75f,
            cubosMaximos = 0, intervaloEntrega = 0.71f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Stack 34 cubes in 75s. Beat the clock."
        },
        new DatosNivel {
            nombre = "Level 38 — The Void", metaAltura = 34f * CUBO, tiempoLimite = 110f,
            cubosMaximos = 0, intervaloEntrega = 0.77f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "Stack 34 cubes in 110s. No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 39 — Singularity", metaAltura = 40f * CUBO, tiempoLimite = 110f,
            cubosMaximos = 0, intervaloEntrega = 0.76f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 40 cubes in 110s. Taller and faster. Masters only. No snap."
        },
        new DatosNivel {
            nombre = "Level 40 — INFINITE SKY", metaAltura = 41f * CUBO, tiempoLimite = 115f,
            cubosMaximos = 0, intervaloEntrega = 0.75f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "Stack 41 cubes in 115s. Taller and faster. Masters only. No snap."
        },
    };

    [Header("UI Selección de Nivel")]
    public GameObject panelSeleccionNivel;
    public Transform  contenedorBotones;
    public GameObject prefabBotonNivel;

    public static int  NivelSeleccionado = 0;
    public static bool preservarNivel    = false;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (!preservarNivel) NivelSeleccionado = 0;
        preservarNivel = false;

        if (panelSeleccionNivel != null)
            panelSeleccionNivel.SetActive(false);
    }

    // Alto de cada botón de nivel en la lista (unidades del canvas de 1080×1920).
    // El prefab mide 90, pequeño para tocar con el dedo.
    private const float AltoBotonNivel = 150f;

    private ScrollRect _scrollNiveles;

    /// <summary>
    /// Mete el contenedor de botones dentro de un área con scroll. La escena solo
    /// tenía el contenedor con layout, sin ScrollRect ni máscara: los botones que
    /// no cabían quedaban fuera del panel y solo se veían 5 o 6. Se construye en
    /// código para no depender de montarlo a mano en el Editor.
    /// </summary>
    void AsegurarScroll()
    {
        if (_scrollNiveles != null || contenedorBotones == null) return;

        var contenido = contenedorBotones as RectTransform;
        _scrollNiveles = contenido.GetComponentInParent<ScrollRect>();
        if (_scrollNiveles != null) return;   // alguien ya lo montó en la escena

        // Viewport: del pie del panel hasta justo debajo del título (el título
        // está anclado al centro a +544 con 244 de alto → su borde inferior ~+420)
        var vpGO = new GameObject("ViewportNiveles", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        var vp   = vpGO.GetComponent<RectTransform>();
        vp.SetParent(contenido.parent, false);
        vp.SetSiblingIndex(contenido.GetSiblingIndex());
        vp.anchorMin = new Vector2(0f, 0f);
        vp.anchorMax = new Vector2(1f, 0.5f);
        vp.offsetMin = new Vector2(60f, 80f);
        vp.offsetMax = new Vector2(-60f, 400f);
        // Imagen transparente: permite arrastrar también desde los huecos
        vpGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        contenido.SetParent(vp, false);
        contenido.anchorMin = new Vector2(0f, 1f);
        contenido.anchorMax = new Vector2(1f, 1f);
        contenido.pivot     = new Vector2(0.5f, 1f);
        contenido.anchoredPosition = Vector2.zero;
        contenido.sizeDelta = new Vector2(0f, contenido.sizeDelta.y);

        var layout = contenido.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 16f;
            layout.childControlWidth      = true;
            layout.childForceExpandWidth  = true;
            layout.childControlHeight     = true;    // usa el alto del LayoutElement
            layout.childForceExpandHeight = false;
        }
        var fitter = contenido.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollNiveles = vpGO.AddComponent<ScrollRect>();
        _scrollNiveles.viewport          = vp;
        _scrollNiveles.content           = contenido;
        _scrollNiveles.horizontal        = false;
        _scrollNiveles.vertical          = true;
        _scrollNiveles.movementType      = ScrollRect.MovementType.Elastic;
        _scrollNiveles.scrollSensitivity = 30f;
    }

    public void MostrarSeleccionNivel(int nivelMaxDesbloqueado)
    {
        if (panelSeleccionNivel == null) return;
        panelSeleccionNivel.SetActive(true);
        AsegurarScroll();

        // Sacarlos del contenedor antes de destruirlos: Destroy actúa al final del
        // fotograma y, mientras, seguirían contando en la altura de la lista
        for (int h = contenedorBotones.childCount - 1; h >= 0; h--)
        {
            Transform hijo = contenedorBotones.GetChild(h);
            hijo.SetParent(null, false);
            Destroy(hijo.gameObject);
        }

        for (int i = 0; i < niveles.Length; i++)
        {
            int  indice       = i;
            bool desbloqueado = i <= nivelMaxDesbloqueado;

            GameObject boton = Instantiate(prefabBotonNivel, contenedorBotones);

            // Sin "??": en el Editor GetComponent devuelve un falso nulo de Unity
            if (!boton.TryGetComponent(out LayoutElement le)) le = boton.AddComponent<LayoutElement>();
            le.preferredHeight = AltoBotonNivel;
            le.minHeight       = AltoBotonNivel;

            // ── Intentar usar BotonNivelUI (nuevo sistema con candado y colores) ──
            var botonUI = boton.GetComponent<BotonNivelUI>();
            if (botonUI != null)
            {
                botonUI.Configurar(
                    niveles[i].nombre,
                    niveles[i].etiquetaEspecial,
                    niveles[i].descripcion,
                    desbloqueado);
            }
            else
            {
                // ── Fallback: sistema anterior por texto (sin BotonNivelUI) ────
                var textos  = boton.GetComponentsInChildren<TextMeshProUGUI>();
                string etiq = string.IsNullOrEmpty(niveles[i].etiquetaEspecial)
                              ? "" : " [" + niveles[i].etiquetaEspecial + "]";

                if (textos.Length > 0) textos[0].text = niveles[i].nombre + etiq;
                if (textos.Length > 1) textos[1].text = desbloqueado ? niveles[i].descripcion : "LOCKED";
            }

            // ── Botón interactivo y click ─────────────────────────────────────
            var btn = boton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = desbloqueado;
                if (desbloqueado)
                    btn.onClick.AddListener(() => SeleccionarNivel(indice));
            }
        }

        // Desplazar la lista hasta el último nivel desbloqueado: con 40 niveles,
        // obligar a bajar a mano hasta el nivel 23 cada vez sería molesto
        if (_scrollNiveles != null && niveles.Length > 1)
        {
            Canvas.ForceUpdateCanvases();
            int objetivo = Mathf.Clamp(nivelMaxDesbloqueado, 0, niveles.Length - 1);
            _scrollNiveles.verticalNormalizedPosition = 1f - (float)objetivo / (niveles.Length - 1);
        }
    }

    public void SeleccionarNivel(int indice)
    {
        if (indice < 0 || indice >= niveles.Length) return;
        NivelSeleccionado = indice;

        if (panelSeleccionNivel != null)
            panelSeleccionNivel.SetActive(false);

        if (GameManager.Instance != null)
        {
            var d = niveles[indice];
            GameManager.Instance.metaAlturaNivel  = d.metaAltura;
            GameManager.Instance.tiempoLimite     = d.tiempoLimite > 0 ? d.tiempoLimite : 999f;
            GameManager.Instance.cubosMaximos     = d.cubosMaximos;
            GameManager.Instance.intervaloEntrega = d.intervaloEntrega > 0 ? d.intervaloEntrega : 1f;
            GameManager.Instance.snapDesactivado  = d.snapDesactivado;
            GameManager.Instance.BotonIrASetup();
        }
    }

    public void MostrarSeleccionNivelDesdeMenu()
    {
        PlayerData datos = SaveSystem.Cargar();
        MostrarSeleccionNivel(datos.nivelMaximoDesbloqueado);
    }

    public DatosNivel ObtenerNivelActual()
        => niveles[Mathf.Clamp(NivelSeleccionado, 0, niveles.Length - 1)];

    public static float ObtenerToleranciaSnap()
    {
        if (Instance != null)
        {
            var nivel = Instance.ObtenerNivelActual();
            if (nivel.snapDesactivado) return 0f; // sin snap — nivel de precisión
        }

        // Tolerancias reducidas: hay que apuntar bien para que el snap ayude
        // (antes eran 3.5 / 3.0 / 2.0 / 1.5 cm — demasiado fácil)
        int n = NivelSeleccionado + 1;
        if (n <= 5)  return 0.012f;  // 1.2 cm — pequeña ayuda en niveles iniciales
        if (n <= 10) return 0.009f;  // 0.9 cm
        if (n <= 15) return 0.006f;  // 0.6 cm
        return 0.004f;               // 0.4 cm — casi pura precisión
    }
}

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

    // ── 25 niveles ───────────────────────────────────────────────────────────
    [Header("Niveles")]
    public DatosNivel[] niveles = new DatosNivel[]
    {
        // ── BLOQUE 1: Intro (1-5) ────────────────────────────────────────────
        new DatosNivel {
            nombre = "Level 1 — First Tower", metaAltura = 0.30f, tiempoLimite = 90f,
            cubosMaximos = 0, intervaloEntrega = 1.2f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Your first tower. Take your time and learn to stack."
        },
        new DatosNivel {
            nombre = "Level 2 — Going Higher", metaAltura = 0.50f, tiempoLimite = 80f,
            cubosMaximos = 0, intervaloEntrega = 1.1f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Half a meter. Keep it steady."
        },
        new DatosNivel {
            nombre = "Level 3 — Three Quarters", metaAltura = 0.75f, tiempoLimite = 75f,
            cubosMaximos = 0, intervaloEntrega = 1.0f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "75 cm. The tower starts to wobble."
        },
        new DatosNivel {
            nombre = "Level 4 — The Meter", metaAltura = 1.00f, tiempoLimite = 70f,
            cubosMaximos = 0, intervaloEntrega = 1.0f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "One full meter. You're getting the hang of it."
        },
        new DatosNivel {
            nombre = "Level 5 — Level Up", metaAltura = 1.25f, tiempoLimite = 65f,
            cubosMaximos = 0, intervaloEntrega = 1.0f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "A meter and a quarter. Focus!"
        },

        // ── BLOQUE 2: Speed + Efficiency (6-10) ─────────────────────────────
        new DatosNivel {
            nombre = "Level 6 — Tower & Half", metaAltura = 1.50f, tiempoLimite = 60f,
            cubosMaximos = 0, intervaloEntrega = 0.95f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Blocks arrive faster. Stay calm."
        },
        new DatosNivel {
            nombre = "Level 7 — Race", metaAltura = 1.50f, tiempoLimite = 45f,
            cubosMaximos = 0, intervaloEntrega = 0.9f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "Same height, half the time. Go!"
        },
        new DatosNivel {
            nombre = "Level 8 — Almost Two", metaAltura = 1.75f, tiempoLimite = 60f,
            cubosMaximos = 0, intervaloEntrega = 1.0f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Almost two meters. Steady hands."
        },
        new DatosNivel {
            nombre = "Level 9 — Count Your Blocks", metaAltura = 1.75f, tiempoLimite = 0f,
            cubosMaximos = 15, intervaloEntrega = 1.0f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "15 blocks. Don't waste a single one."
        },
        new DatosNivel {
            nombre = "Level 10 — Two Meters", metaAltura = 2.00f, tiempoLimite = 55f,
            cubosMaximos = 0, intervaloEntrega = 0.9f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "Two meters. The real test begins here."
        },

        // ── BLOQUE 3: Precisión (11-15) ──────────────────────────────────────
        new DatosNivel {
            nombre = "Level 11 — No Safety Net", metaAltura = 2.00f, tiempoLimite = 55f,
            cubosMaximos = 0, intervaloEntrega = 1.0f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "No snap assist. Aim true."
        },
        new DatosNivel {
            nombre = "Level 12 — Master Builder", metaAltura = 2.50f, tiempoLimite = 60f,
            cubosMaximos = 0, intervaloEntrega = 0.85f, snapDesactivado = false,
            etiquetaEspecial = "",
            descripcion = "2.5 meters. You are a builder."
        },
        new DatosNivel {
            nombre = "Level 13 — Efficient Master", metaAltura = 2.50f, tiempoLimite = 0f,
            cubosMaximos = 12, intervaloEntrega = 0.85f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "12 blocks for 2.5 m. Every block counts."
        },
        new DatosNivel {
            nombre = "Level 14 — Architect", metaAltura = 3.00f, tiempoLimite = 65f,
            cubosMaximos = 0, intervaloEntrega = 0.8f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "3 meters. No snap. Pure skill."
        },
        new DatosNivel {
            nombre = "Level 15 — Legend", metaAltura = 3.50f, tiempoLimite = 60f,
            cubosMaximos = 0, intervaloEntrega = 0.75f, snapDesactivado = false,
            etiquetaEspecial = "MASTER",
            descripcion = "3.5 meters in 60 seconds. Legendary."
        },

        // ── BLOQUE 4: Elite (16-20) ───────────────────────────────────────────
        new DatosNivel {
            nombre = "Level 16 — Sharp Eye", metaAltura = 4.00f, tiempoLimite = 55f,
            cubosMaximos = 0, intervaloEntrega = 0.8f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "4 meters. No snap. Eyes like a hawk."
        },
        new DatosNivel {
            nombre = "Level 17 — Efficient Stack", metaAltura = 4.50f, tiempoLimite = 0f,
            cubosMaximos = 12, intervaloEntrega = 0.75f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "12 blocks to reach 4.5 m. Waste nothing."
        },
        new DatosNivel {
            nombre = "Level 18 — Master Class", metaAltura = 5.00f, tiempoLimite = 50f,
            cubosMaximos = 0, intervaloEntrega = 0.75f, snapDesactivado = false,
            etiquetaEspecial = "MASTER",
            descripcion = "5 meters in 50 seconds. Masters only."
        },
        new DatosNivel {
            nombre = "Level 19 — Ultra Efficient", metaAltura = 5.50f, tiempoLimite = 0f,
            cubosMaximos = 10, intervaloEntrega = 0.7f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "10 blocks, no snap, 5.5 m. Brutal efficiency."
        },
        new DatosNivel {
            nombre = "Level 20 — Race The Clock", metaAltura = 6.00f, tiempoLimite = 45f,
            cubosMaximos = 0, intervaloEntrega = 0.7f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "6 meters. 45 seconds. Don't blink."
        },

        // ── BLOQUE 5: Legendary (21-25) ───────────────────────────────────────
        new DatosNivel {
            nombre = "Level 21 — The Summit", metaAltura = 6.50f, tiempoLimite = 45f,
            cubosMaximos = 8, intervaloEntrega = 0.7f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "8 blocks, no snap. Reach the summit."
        },
        new DatosNivel {
            nombre = "Level 22 — Double Challenge", metaAltura = 7.00f, tiempoLimite = 40f,
            cubosMaximos = 0, intervaloEntrega = 0.65f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "7 meters, no snap, 40 seconds. Insane."
        },
        new DatosNivel {
            nombre = "Level 23 — Peak Builder", metaAltura = 8.00f, tiempoLimite = 40f,
            cubosMaximos = 7, intervaloEntrega = 0.65f, snapDesactivado = false,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "7 blocks. 8 meters. Every block must land."
        },
        new DatosNivel {
            nombre = "Level 24 — Sky Reach", metaAltura = 9.00f, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 0.6f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "9 meters, no snap, 35 seconds. Sky is the limit."
        },
        new DatosNivel {
            nombre = "Level 25 — LEGEND FINAL", metaAltura = 10.00f, tiempoLimite = 30f,
            cubosMaximos = 0, intervaloEntrega = 0.6f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "10 meters. No snap. 30 seconds. You are the LEGEND."
        },

        // ── BLOQUE 6: GODLIKE (26-30) ─────────────────────────────────────────
        new DatosNivel {
            nombre = "Level 26 — Efficiency God", metaAltura = 6.00f, tiempoLimite = 0f,
            cubosMaximos = 6, intervaloEntrega = 0.55f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "6 blocks. 6 meters. No snap. Perfect or nothing."
        },
        new DatosNivel {
            nombre = "Level 27 — Flash Builder", metaAltura = 8.00f, tiempoLimite = 25f,
            cubosMaximos = 0, intervaloEntrega = 0.5f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "8 meters in 25 seconds. Blocks arrive like bullets."
        },
        new DatosNivel {
            nombre = "Level 28 — The Surgeon", metaAltura = 9.00f, tiempoLimite = 0f,
            cubosMaximos = 9, intervaloEntrega = 0.55f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "9 blocks, 9 meters, no snap. Surgical precision required."
        },
        new DatosNivel {
            nombre = "Level 29 — Overdrive", metaAltura = 11.00f, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 0.45f, snapDesactivado = false,
            etiquetaEspecial = "MASTER",
            descripcion = "11 meters. Blocks come non-stop. Are you fast enough?"
        },
        new DatosNivel {
            nombre = "Level 30 — Iron Will", metaAltura = 12.00f, tiempoLimite = 40f,
            cubosMaximos = 0, intervaloEntrega = 0.5f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "12 meters. No snap. 40 seconds. Unbreakable focus."
        },

        // ── BLOQUE 7: TRANSCENDENT (31-35) ────────────────────────────────────
        new DatosNivel {
            nombre = "Level 31 — Ghost Protocol", metaAltura = 10.00f, tiempoLimite = 0f,
            cubosMaximos = 7, intervaloEntrega = 0.5f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "7 blocks for 10m. No snap. No margin for error."
        },
        new DatosNivel {
            nombre = "Level 32 — Blitz", metaAltura = 13.00f, tiempoLimite = 30f,
            cubosMaximos = 0, intervaloEntrega = 0.4f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "13 meters, 30 seconds. The fastest hands win."
        },
        new DatosNivel {
            nombre = "Level 33 — Minimalist", metaAltura = 8.00f, tiempoLimite = 0f,
            cubosMaximos = 5, intervaloEntrega = 0.5f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "5 blocks. 8 meters. No snap. Efficiency redefined."
        },
        new DatosNivel {
            nombre = "Level 34 — The Architect", metaAltura = 14.00f, tiempoLimite = 45f,
            cubosMaximos = 0, intervaloEntrega = 0.45f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "14 meters. No snap. Fast delivery. The greatest builder."
        },
        new DatosNivel {
            nombre = "Level 35 — Clockwork", metaAltura = 12.00f, tiempoLimite = 28f,
            cubosMaximos = 0, intervaloEntrega = 0.4f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "12 meters in 28 seconds. Like clockwork, never miss a beat."
        },

        // ── BLOQUE 8: MYTHIC (36-40) ──────────────────────────────────────────
        new DatosNivel {
            nombre = "Level 36 — Six Sigma", metaAltura = 12.00f, tiempoLimite = 0f,
            cubosMaximos = 6, intervaloEntrega = 0.45f, snapDesactivado = true,
            etiquetaEspecial = "EFFICIENCY",
            descripcion = "6 blocks. 12 meters. No snap. Statistical perfection."
        },
        new DatosNivel {
            nombre = "Level 37 — Warp Speed", metaAltura = 15.00f, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 0.35f, snapDesactivado = false,
            etiquetaEspecial = "TIME TRIAL",
            descripcion = "15 meters in 35 seconds. Faster than thought itself."
        },
        new DatosNivel {
            nombre = "Level 38 — The Void", metaAltura = 15.00f, tiempoLimite = 0f,
            cubosMaximos = 8, intervaloEntrega = 0.4f, snapDesactivado = true,
            etiquetaEspecial = "PRECISION",
            descripcion = "8 blocks. 15 meters. No snap. Pure void, pure will."
        },
        new DatosNivel {
            nombre = "Level 39 — Singularity", metaAltura = 18.00f, tiempoLimite = 40f,
            cubosMaximos = 0, intervaloEntrega = 0.35f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "18 meters. No snap. 40 seconds. A singular achievement."
        },
        new DatosNivel {
            nombre = "Level 40 — INFINITE SKY", metaAltura = 20.00f, tiempoLimite = 35f,
            cubosMaximos = 0, intervaloEntrega = 0.3f, snapDesactivado = true,
            etiquetaEspecial = "MASTER",
            descripcion = "20 meters. No snap. 35 seconds. The sky has no ceiling."
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

    public void MostrarSeleccionNivel(int nivelMaxDesbloqueado)
    {
        if (panelSeleccionNivel == null) return;
        panelSeleccionNivel.SetActive(true);

        foreach (Transform hijo in contenedorBotones)
            Destroy(hijo.gameObject);

        for (int i = 0; i < niveles.Length; i++)
        {
            int  indice       = i;
            bool desbloqueado = i <= nivelMaxDesbloqueado;

            GameObject boton = Instantiate(prefabBotonNivel, contenedorBotones);

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

using System;

[Serializable]
public class PlayerData
{
    // ── Perfil ──────────────────────────────────────────────────────────────
    public string nombreUsuario;
    public int    avatarId;
    public string fechaNacimiento;
    public bool   tutorialVisto;

    // ── Progreso ─────────────────────────────────────────────────────────────
    public int   nivelMaximoDesbloqueado;
    public int   totalCubosUsadosHistorico;
    public float mejorAltura;

    // ── Economía ─────────────────────────────────────────────────────────────
    public int  vidas;                      // máximo 5, se regeneran 1 cada 30 min
    public int  monedas;                    // moneda básica, se gana jugando
    public int  gemas;                      // moneda premium, recompensas especiales
    public long timestampUltimaVidaGastada; // Unix ms — para calcular regeneración

    // ── Comodines (inventario) ───────────────────────────────────────────────
    // [0]=SnapPerfecto [1]=+30s [2]=CuboPlomo [3]=Escudo
    public int[] comodinesInventario;

    // ── Daily bonus ──────────────────────────────────────────────────────────
    public string ultimoDiaJugado; // "yyyy-MM-dd" — para el bonus diario

    // ────────────────────────────────────────────────────────────────────────
    public PlayerData()
    {
        nombreUsuario               = "Constructor";
        avatarId                    = 0;
        nivelMaximoDesbloqueado     = 0;
        totalCubosUsadosHistorico   = 0;
        mejorAltura                 = 0f;
        tutorialVisto               = false;

        vidas                       = 5;
        monedas                     = 100; // monedas iniciales de bienvenida
        gemas                       = 0;
        timestampUltimaVidaGastada  = 0;

        comodinesInventario         = new int[4] { 1, 0, 0, 0 };
        ultimoDiaJugado             = "";
    }
}

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

    // ── Anuncios ─────────────────────────────────────────────────────────────
    // La recompensa por anuncio decrece dentro del mismo día: ver anuncios en
    // bucle deja de ser rentable sin penalizar al jugador ocasional.
    public string ultimoDiaAds;  // "yyyy-MM-dd" — resetea el contador cada día
    public int    adsVistosHoy;

    // ── Gemas (reglas del 2026-09-22) ────────────────────────────────────────
    public int[] estrellasNivel;     // mejores estrellas (0-3) por índice de nivel
    public float mejorAlturaCubos;   // récord medido en cubos: no depende del tamaño de plataforma
    public int   rachaDias;          // días seguidos abriendo el juego

    // ────────────────────────────────────────────────────────────────────────
    public PlayerData()
    {
        nombreUsuario               = "Builder";
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
        ultimoDiaAds                = "";
        adsVistosHoy                = 0;

        estrellasNivel              = new int[0];   // crece al completar niveles
        mejorAlturaCubos            = 0f;
        rachaDias                   = 0;
    }
}

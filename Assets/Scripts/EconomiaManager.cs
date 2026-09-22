using UnityEngine;
using System;

/// <summary>
/// Singleton que gestiona toda la economía del juego: vidas, monedas, gemas y comodines.
/// Usa GameManager como fuente única de PlayerData para evitar conflictos de guardado.
/// </summary>
public class EconomiaManager : MonoBehaviour
{
    public static EconomiaManager Instance;

    // ── Constantes ───────────────────────────────────────────────────────────
    public const int MAX_VIDAS           = 5;
    public const int SEGUNDOS_POR_VIDA   = 1800; // 30 minutos

    // Precios tienda de emergencia
    public const int PRECIO_RESCATE_MONEDAS  = 75;   // +30s desde GameOver
    public const int PRECIO_RESCATE_GEMAS    = 1;    // alternativa con gemas
    public const int PRECIO_SNAP_MONEDAS     = 50;
    public const int PRECIO_TIEMPO_MONEDAS   = 75;
    public const int PRECIO_PLOMO_MONEDAS    = 60;
    public const int PRECIO_ESCUDO_MONEDAS   = 80;

    // Vidas en la tienda
    public const int PRECIO_VIDA_MONEDAS       = 50;
    public const int PRECIO_PACK_VIDAS_MONEDAS = 120;
    public const int VIDAS_POR_PACK            = 3;

    // Recompensas por jugar
    public const int MONEDAS_POR_GANAR       = 50;
    public const int MONEDAS_BONUS_RECORD     = 25;
    public const int GEMAS_POR_RECORD         = 1;

    // ── Propiedades acceso a datos (a través de GameManager) ─────────────────
    private PlayerData Datos => GameManager.Instance?.GetDatos();

    public int  Monedas => Datos?.monedas ?? 0;
    public int  Gemas   => Datos?.gemas   ?? 0;
    public int  Vidas   => Datos?.vidas   ?? 0;

    // Eventos para actualizar UI automáticamente
    public event Action<int> OnMonedasCambiadas;
    public event Action<int> OnVidasCambiadas;
    public event Action<int> OnGemasCambiadas;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// Llamar al inicio de la app (desde GameManager.Start) para calcular
    /// cuántas vidas se regeneraron mientras el jugador estuvo offline.
    /// </summary>
    public void InicializarAlArrancar()
    {
        var d = Datos;
        if (d == null) return;

        if (d.vidas < MAX_VIDAS && d.timestampUltimaVidaGastada > 0)
        {
            long ahora        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long transcurrido = ahora - d.timestampUltimaVidaGastada;
            int vidasGanadas  = (int)(transcurrido / 1000L / SEGUNDOS_POR_VIDA);

            if (vidasGanadas > 0)
            {
                d.vidas = Mathf.Min(d.vidas + vidasGanadas, MAX_VIDAS);

                if (d.vidas >= MAX_VIDAS)
                    d.timestampUltimaVidaGastada = 0; // Timer se detiene al llegar a max
                else
                    d.timestampUltimaVidaGastada += (long)vidasGanadas * SEGUNDOS_POR_VIDA * 1000L;

                SaveSystem.Guardar(d);
                OnVidasCambiadas?.Invoke(d.vidas);
            }
        }
    }

    // ── MONEDAS ──────────────────────────────────────────────────────────────

    /// <summary>Gasta monedas. Devuelve false si no hay suficientes.</summary>
    public bool GastarMonedas(int cantidad)
    {
        var d = Datos;
        if (d == null || d.monedas < cantidad) return false;
        d.monedas -= cantidad;
        SaveSystem.Guardar(d);
        OnMonedasCambiadas?.Invoke(d.monedas);
        return true;
    }

    public void GanarMonedas(int cantidad)
    {
        var d = Datos;
        if (d == null) return;
        d.monedas += cantidad;
        SaveSystem.Guardar(d);
        OnMonedasCambiadas?.Invoke(d.monedas);
    }

    public bool TieneMonedas(int cantidad) => (Datos?.monedas ?? 0) >= cantidad;

    // ── RECOMPENSA POR ANUNCIOS (decreciente) ────────────────────────────────

    // Porcentaje de la recompensa base según cuántos anuncios lleva el jugador
    // hoy. El último valor se aplica a todos los anuncios siguientes.
    private static readonly float[] FactorRecompensaAd = { 1.00f, 0.70f, 0.40f, 0.20f };

    /// <summary>
    /// Convierte la recompensa base de un anuncio en la recompensa efectiva,
    /// aplicando decrecimiento dentro del mismo día. Con una base de 50 monedas:
    /// 1.º anuncio 50, 2.º 35, 3.º 20, 4.º y siguientes 10.
    ///
    /// Llamar UNA sola vez por anuncio completado: incrementa el contador diario.
    /// </summary>
    public int CalcularRecompensaAd(int monedasBase)
    {
        if (monedasBase <= 0) return 0;

        var d = Datos;
        if (d == null) return monedasBase;

        string hoy = DateTime.Now.ToString("yyyy-MM-dd");
        if (d.ultimoDiaAds != hoy)
        {
            d.ultimoDiaAds = hoy;
            d.adsVistosHoy = 0;
        }

        int   indice = Mathf.Min(d.adsVistosHoy, FactorRecompensaAd.Length - 1);
        float factor = FactorRecompensaAd[indice];

        d.adsVistosHoy++;
        SaveSystem.Guardar(d);

        // Mínimo 1 moneda: un anuncio visto nunca debe recompensar con nada
        return Mathf.Max(1, Mathf.RoundToInt(monedasBase * factor));
    }

    // ── GEMAS ────────────────────────────────────────────────────────────────

    public bool GastarGemas(int cantidad)
    {
        var d = Datos;
        if (d == null || d.gemas < cantidad) return false;
        d.gemas -= cantidad;
        SaveSystem.Guardar(d);
        OnGemasCambiadas?.Invoke(d.gemas);
        return true;
    }

    public void GanarGemas(int cantidad)
    {
        var d = Datos;
        if (d == null) return;
        d.gemas += cantidad;
        SaveSystem.Guardar(d);
        OnGemasCambiadas?.Invoke(d.gemas);
    }

    public bool TieneGemas(int cantidad) => (Datos?.gemas ?? 0) >= cantidad;

    // ── VIDAS ────────────────────────────────────────────────────────────────

    /// <summary>Gasta 1 vida. Devuelve false si ya está en 0.</summary>
    public bool GastarVida()
    {
        var d = Datos;
        if (d == null || d.vidas <= 0) return false;
        d.vidas--;
        d.timestampUltimaVidaGastada = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        SaveSystem.Guardar(d);
        OnVidasCambiadas?.Invoke(d.vidas);
        return true;
    }

    public void GanarVida(int cantidad = 1)
    {
        var d = Datos;
        if (d == null) return;
        d.vidas = Mathf.Min(d.vidas + cantidad, MAX_VIDAS);
        if (d.vidas >= MAX_VIDAS) d.timestampUltimaVidaGastada = 0;
        SaveSystem.Guardar(d);
        OnVidasCambiadas?.Invoke(d.vidas);
    }

    public bool TieneVidas() => (Datos?.vidas ?? 0) > 0;

    /// <summary>Segundos que faltan para regenerar la próxima vida.</summary>
    public float SegundosHastaProximaVida()
    {
        var d = Datos;
        if (d == null || d.vidas >= MAX_VIDAS || d.timestampUltimaVidaGastada == 0) return 0;

        long ahora        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long transcurrido = ahora - d.timestampUltimaVidaGastada;
        float restante    = SEGUNDOS_POR_VIDA - (transcurrido / 1000f);
        return Mathf.Max(0, restante);
    }

    // ── COMODINES ────────────────────────────────────────────────────────────
    // Slots: [0]=SnapPerfecto [1]=TiempoExtra [2]=CuboPlomo [3]=Escudo

    public int ObtenerComodin(int slot)
    {
        var d = Datos;
        if (d?.comodinesInventario == null || slot < 0 || slot >= d.comodinesInventario.Length) return 0;
        return d.comodinesInventario[slot];
    }

    /// <summary>Descuenta 1 unidad de un comodín. Devuelve false si no hay.</summary>
    public bool UsarComodin(int slot)
    {
        var d = Datos;
        if (d?.comodinesInventario == null || slot < 0 || slot >= d.comodinesInventario.Length) return false;
        if (d.comodinesInventario[slot] <= 0) return false;
        d.comodinesInventario[slot]--;
        SaveSystem.Guardar(d);
        return true;
    }

    public void GanarComodin(int slot, int cantidad = 1)
    {
        var d = Datos;
        if (d?.comodinesInventario == null || slot < 0 || slot >= d.comodinesInventario.Length) return;
        d.comodinesInventario[slot] += cantidad;
        SaveSystem.Guardar(d);
    }

    // ── COMPRAS TIENDA DE EMERGENCIA ─────────────────────────────────────────

    /// <summary>Compra +30s con monedas. Devuelve true si la compra fue exitosa.</summary>
    public bool ComprarTiempoExtra()
    {
        return GastarMonedas(PRECIO_TIEMPO_MONEDAS);
    }

    /// <summary>Compra un comodín con monedas.</summary>
    public bool ComprarComodin(int slot)
    {
        int[] precios = { PRECIO_SNAP_MONEDAS, PRECIO_TIEMPO_MONEDAS, PRECIO_PLOMO_MONEDAS, PRECIO_ESCUDO_MONEDAS };
        if (slot < 0 || slot >= precios.Length) return false;
        if (!GastarMonedas(precios[slot])) return false;
        GanarComodin(slot);
        return true;
    }
}

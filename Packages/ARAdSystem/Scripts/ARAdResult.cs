namespace Layered.ARAdSystem
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ARAdResult — devuelto vía callback cuando el break termina.
    //
    //  Uso en el juego:
    //
    //    LayeredAds.MostrarBreak(resultado => {
    //        if (resultado.vioCompleto)
    //            EconomiaManager.Instance.GanarMonedas(resultado.monedasRecomendadas);
    //        if (resultado.hizoClic)
    //            Debug.Log("El usuario abrió la app de la marca — registro de conversión");
    //    });
    // ─────────────────────────────────────────────────────────────────────────

    public readonly struct ARAdResult
    {
        // ── Resultado de la interacción ───────────────────────────────────────

        /// <summary>El usuario vio el anuncio hasta el final (no hizo skip).</summary>
        public readonly bool vioCompleto;

        /// <summary>El usuario pulsó el botón CTA (Call to Action).</summary>
        public readonly bool hizoClic;

        /// <summary>El usuario pulsó Skip antes de que terminara.</summary>
        public readonly bool hizoSkip;

        /// <summary>El break fue cancelado por error interno o sin conexión.</summary>
        public readonly bool fueError;

        // ── Métricas ──────────────────────────────────────────────────────────

        /// <summary>Tiempo en segundos que el usuario mantuvo el anuncio visible.</summary>
        public readonly float tiempoVisualizacionSegundos;

        // ── Recompensa recomendada ─────────────────────────────────────────────

        /// <summary>
        /// Monedas que el juego debería dar al jugador.
        /// Ya calculado por el SDK según si vio completo o hizo skip.
        /// El juego decide si las entrega o no.
        /// </summary>
        public readonly int monedasRecomendadas;

        /// <summary>ID del anuncio mostrado (para analytics del juego).</summary>
        public readonly string adId;

        // ── Constructor interno ────────────────────────────────────────────────

        internal ARAdResult(
            bool   vioCompleto,
            bool   hizoClic,
            bool   hizoSkip,
            bool   fueError,
            float  tiempoVisualizacion,
            int    monedasRecomendadas,
            string adId)
        {
            this.vioCompleto               = vioCompleto;
            this.hizoClic                  = hizoClic;
            this.hizoSkip                  = hizoSkip;
            this.fueError                  = fueError;
            this.tiempoVisualizacionSegundos = tiempoVisualizacion;
            this.monedasRecomendadas       = monedasRecomendadas;
            this.adId                      = adId;
        }

        // ── Factory methods ────────────────────────────────────────────────────

        internal static ARAdResult Completo(float tiempo, int monedas, string adId) =>
            new ARAdResult(true, false, false, false, tiempo, monedas, adId);

        internal static ARAdResult ConClic(float tiempo, int monedas, string adId) =>
            new ARAdResult(true, true, false, false, tiempo, monedas, adId);

        internal static ARAdResult Skip(float tiempo, int monedasSkip, string adId) =>
            new ARAdResult(false, false, true, false, tiempo, monedasSkip, adId);

        internal static ARAdResult Error(string adId) =>
            new ARAdResult(false, false, false, true, 0f, 0, adId);

        public override string ToString() =>
            $"[ARAdResult] id={adId} completo={vioCompleto} clic={hizoClic} " +
            $"skip={hizoSkip} tiempo={tiempoVisualizacionSegundos:F1}s monedas={monedasRecomendadas}";
    }
}

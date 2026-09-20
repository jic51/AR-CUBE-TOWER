using UnityEngine;

/// <summary>
/// Tipos de cubo disponibles. Cada uno tiene física, visual y comportamiento únicos.
///
/// BLOQUES DE CONTENIDO:
///   V1 (activos):   Normal, Plomo, Plumas, Hielo, Fuego
///   V2 (nuevos):    Gelatina, Lava, Hierba, Agua, Nube
///   V3 (futuro):    Obsidiana, Madera, Electricidad, Arena, Espejo...
/// </summary>
public enum TipoCubo
{
    // ── V1 — Activos ──────────────────────────────────────────────────────────
    Normal,     // piedra/concreto — masa 10, estable, polvo beige
    Plomo,      // metal pesado — masa 40, aplasta, impacto lateral fuerte
    Plumas,     // plumas — masa 0.8, flota, rebota suave, puede quemarse
    Hielo,      // hielo — resbaladizo, se derrite con fuego, shards al romperse
    Fuego,      // fuego — explosión radial, quema plumas, derrite hielo, emisión pulsante

    // ── V2 — Nuevos (implementar de a poco) ──────────────────────────────────
    Gelatina,   // translúcido verde/amarillo — rebota, absorbe impactos, no rompe nada
    Lava,       // naranja oscuro emisivo — variante pesada de fuego, derrite todo lo que toca
    Hierba,     // verde matte — muy estable, adhesivo, difícil de derribar
    Agua,       // azul transparente — fluido, se desliza, apaga fuego/lava
    Nube,       // blanco semitransparente — ultra liviano, flota, puede ser soplado
    Piedra,     // gris oscuro matte — masa 18, muy estable, nada lo mueve fácilmente
}

/// <summary>
/// Configuración serializable para un tipo de cubo.
/// </summary>
[System.Serializable]
public class ConfigTipoCubo
{
    public TipoCubo tipo;

    [Tooltip("Nombre mostrado en el HUD (inglés)")]
    public string nombreMostrar;

    [Tooltip("Color base del cubo — en vuelo Y en reposo (no cambia al aterrizar)")]
    public Color colorEnVuelo;

    [Tooltip("Peso relativo de aparición. 0.50 = aparece el 50% de las veces")]
    [Range(0f, 1f)]
    public float probabilidad;

    // ── Descripción para el jugador ───────────────────────────────────────────
    [Tooltip("Texto corto que aparece en el HUD al descubrir este tipo")]
    [TextArea(1, 2)]
    public string descripcionJugador;
}

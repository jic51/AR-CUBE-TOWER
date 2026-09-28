using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Cubo 3D girando, dibujado directamente en la interfaz.
///
/// Por qué así y no con una cámara y un RenderTexture (que es como lo hacía la
/// app anterior): esa versión necesitaba una cámara extra renderizando cada
/// fotograma, un layer reservado, luces propias y que ninguna otra cámara viera
/// el cubo escondido a 10 km. En AR, donde la batería y los fotogramas son lo
/// que más cuesta, eso es caro para un adorno de 86 píxeles.
///
/// Aquí se proyectan a mano los 8 vértices, se descartan las caras que miran
/// hacia atrás y se pintan las visibles con Painter2D, sombreando cada una
/// según su normal. El resultado es el mismo cubo girando de siempre, sin
/// cámaras, sin luces y sin depender de ningún shader.
/// </summary>
public static class CuboGiratorio
{
    // Vértices de un cubo centrado en el origen, lado 1
    private static readonly Vector3[] V =
    {
        new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
        new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
        new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
    };

    // Las 6 caras en orden antihorario vistas desde fuera
    private static readonly int[][] Caras =
    {
        new[] { 4, 5, 6, 7 },  // +Z
        new[] { 1, 0, 3, 2 },  // -Z
        new[] { 5, 1, 2, 6 },  // +X
        new[] { 0, 4, 7, 3 },  // -X
        new[] { 3, 7, 6, 2 },  // +Y (tapa)
        new[] { 0, 1, 5, 4 },  // -Y
    };

    private static readonly Vector3[] Normales =
    {
        Vector3.forward, Vector3.back, Vector3.right,
        Vector3.left,    Vector3.up,   Vector3.down,
    };

    // Luz fija arriba-izquierda-delante, como en la ilustración de la landing
    private static readonly Vector3 Luz = new Vector3(-0.35f, 0.82f, 0.45f).normalized;

    // Reutilizados entre fotogramas: esto se repinta 60 veces por segundo y no
    // debe generar basura para el recolector
    private static readonly Vector2[] _proy = new Vector2[8];
    private static readonly Vector3[] _vr   = new Vector3[8];

    /// <summary>
    /// Pinta el cubo dentro del elemento. <paramref name="giro"/> son grados
    /// alrededor del eje vertical; la inclinación es fija para que la tapa
    /// siempre se vea, que es lo que hace que se lea como un cubo y no como un
    /// hexágono girando.
    /// </summary>
    public static void Dibujar(MeshGenerationContext ctx, Color color, float giro)
    {
        Rect r = ctx.visualElement.contentRect;
        if (r.width < 8f || r.height < 8f) return;

        float lado = Mathf.Min(r.width, r.height);
        float k = lado * 0.52f;
        var centro = new Vector2(r.width * 0.5f, r.height * 0.5f);

        Quaternion rot = Quaternion.Euler(-22f, giro, 0f);

        // Proyección ortográfica: la perspectiva a este tamaño no se nota y
        // deforma las caras traseras
        for (int i = 0; i < 8; i++)
        {
            _vr[i] = rot * V[i];
            _proy[i] = centro + new Vector2(_vr[i].x * k, -_vr[i].y * k);
        }

        color.a = 1f;
        var p = ctx.painter2D;

        // Pintar de atrás hacia delante entre las caras visibles evita el
        // parpadeo en los ángulos donde dos caras casi se solapan
        System.Span<int> orden = stackalloc int[6];
        int n = 0;
        for (int f = 0; f < 6; f++)
        {
            Vector3 nrm = rot * Normales[f];
            if (nrm.z <= 0.001f) continue;   // mira hacia atrás
            orden[n++] = f;
        }

        for (int a = 0; a < n - 1; a++)
            for (int b = a + 1; b < n; b++)
            {
                if (ProfundidadCara(orden[b]) > ProfundidadCara(orden[a])) continue;
                (orden[a], orden[b]) = (orden[b], orden[a]);
            }

        for (int i = 0; i < n; i++)
        {
            int f = orden[i];
            Vector3 nrm = rot * Normales[f];

            // Media luz ambiente + media difusa: sin ambiente las caras en
            // sombra quedaban negras y el cubo parecía partido
            float luz = 0.55f + 0.55f * Mathf.Max(0f, Vector3.Dot(nrm, Luz));

            p.fillColor = new Color(Mathf.Min(1f, color.r * luz),
                                    Mathf.Min(1f, color.g * luz),
                                    Mathf.Min(1f, color.b * luz), 1f);
            p.BeginPath();
            int[] c = Caras[f];
            p.MoveTo(_proy[c[0]]);
            for (int j = 1; j < 4; j++) p.LineTo(_proy[c[j]]);
            p.ClosePath();
            p.Fill();
        }
    }

    static float ProfundidadCara(int cara)
    {
        int[] c = Caras[cara];
        return (_vr[c[0]].z + _vr[c[1]].z + _vr[c[2]].z + _vr[c[3]].z) * 0.25f;
    }
}

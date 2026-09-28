using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Iconos vectoriales dibujados con Painter2D.
///
/// Por qué dibujados y no imágenes: los cuadros de color planos no decían nada
/// ("¿esto es una vida o una moneda?"), pero meter PNGs traería un problema de
/// licencias que ya arrastramos con los iconos de la tienda vieja. Dibujarlos
/// aquí nos da símbolos propios, nítidos a cualquier tamaño y sin peso en el
/// APK, y además pueden pintarse del color que pida el tema.
///
/// Todas las formas se definen en un cuadrado de 0..1 y se escalan al tamaño
/// real del elemento, así el mismo icono sirve para un chip de 22 px y para la
/// fila de la tienda de 74 px.
/// </summary>
public static class Iconos
{
    public enum Icono
    {
        Moneda,
        Gema,
        Corazon,
        Mira,      // Perfect Snap
        Reloj,     // +30 segundos
        Pesa,      // Cubo de plomo
        Escudo,
        Cubo,
    }

    /// <summary>Crea un elemento cuadrado que se pinta solo. Nunca captura toques.</summary>
    public static VisualElement Crear(Icono icono, Color color, float lado)
    {
        var el = new VisualElement { pickingMode = PickingMode.Ignore };
        el.style.width  = lado;
        el.style.height = lado;
        el.style.flexShrink = 0f;
        el.generateVisualContent += ctx => Dibujar(ctx, icono, color);
        return el;
    }

    static void Dibujar(MeshGenerationContext ctx, Icono icono, Color color)
    {
        Rect r = ctx.visualElement.contentRect;
        if (r.width < 4f || r.height < 4f) return;

        float lado = Mathf.Min(r.width, r.height);
        float ox = (r.width  - lado) * 0.5f;
        float oy = (r.height - lado) * 0.5f;

        // Traduce coordenadas normalizadas (0..1) al rectángulo real
        Vector2 P(float x, float y) => new Vector2(ox + x * lado, oy + y * lado);

        var p = ctx.painter2D;
        color.a = 1f;

        switch (icono)
        {
            case Icono.Moneda:  Moneda(p, P, lado, color);  break;
            case Icono.Gema:    Gema(p, P, color);          break;
            case Icono.Corazon: Corazon(p, P, color);       break;
            case Icono.Mira:    Mira(p, P, lado, color);    break;
            case Icono.Reloj:   Reloj(p, P, lado, color);   break;
            case Icono.Pesa:    Pesa(p, P, lado, color);    break;
            case Icono.Escudo:  Escudo(p, P, color);        break;
            case Icono.Cubo:    Cubo(p, P, color);          break;
        }
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    delegate Vector2 Punto(float x, float y);

    static Color Aclarar(Color c, float f) =>
        new Color(Mathf.Min(1f, c.r * f), Mathf.Min(1f, c.g * f), Mathf.Min(1f, c.b * f), 1f);

    static Color Oscurecer(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, 1f);

    static void Poligono(Painter2D p, Color relleno, params Vector2[] puntos)
    {
        p.fillColor = relleno;
        p.BeginPath();
        p.MoveTo(puntos[0]);
        for (int i = 1; i < puntos.Length; i++) p.LineTo(puntos[i]);
        p.ClosePath();
        p.Fill();
    }

    static void Disco(Painter2D p, Color relleno, Vector2 centro, float radio)
    {
        p.fillColor = relleno;
        p.BeginPath();
        p.Arc(centro, radio, 0f, 360f);
        p.ClosePath();
        p.Fill();
    }

    // ── Formas ────────────────────────────────────────────────────────────

    static void Moneda(Painter2D p, Punto P, float lado, Color c)
    {
        Vector2 centro = P(0.5f, 0.5f);
        Disco(p, c, centro, lado * 0.46f);
        Disco(p, Oscurecer(c, 0.78f), centro, lado * 0.34f);

        // Barra central: lee como "moneda" sin depender de una tipografía
        Poligono(p, c,
            P(0.46f, 0.26f), P(0.54f, 0.26f), P(0.54f, 0.74f), P(0.46f, 0.74f));
    }

    static void Gema(Painter2D p, Punto P, Color c)
    {
        // Cuerpo
        Poligono(p, c,
            P(0.26f, 0.20f), P(0.74f, 0.20f), P(0.96f, 0.44f),
            P(0.50f, 0.92f), P(0.04f, 0.44f));

        // Tabla superior más clara y facetas laterales más oscuras: sin estos
        // dos tonos el rombo se veía plano y se confundía con el corazón
        Poligono(p, Aclarar(c, 1.35f),
            P(0.26f, 0.20f), P(0.74f, 0.20f), P(0.62f, 0.44f), P(0.38f, 0.44f));
        Poligono(p, Oscurecer(c, 0.72f),
            P(0.62f, 0.44f), P(0.96f, 0.44f), P(0.50f, 0.92f));
    }

    static void Corazon(Painter2D p, Punto P, Color c)
    {
        p.fillColor = c;
        p.BeginPath();
        p.MoveTo(P(0.50f, 0.90f));
        p.BezierCurveTo(P(0.10f, 0.62f), P(0.02f, 0.36f), P(0.19f, 0.20f));
        p.BezierCurveTo(P(0.33f, 0.07f), P(0.46f, 0.14f), P(0.50f, 0.28f));
        p.BezierCurveTo(P(0.54f, 0.14f), P(0.67f, 0.07f), P(0.81f, 0.20f));
        p.BezierCurveTo(P(0.98f, 0.36f), P(0.90f, 0.62f), P(0.50f, 0.90f));
        p.ClosePath();
        p.Fill();
    }

    static void Mira(Painter2D p, Punto P, float lado, Color c)
    {
        Vector2 centro = P(0.5f, 0.5f);

        p.strokeColor = c;
        p.lineWidth   = Mathf.Max(1.5f, lado * 0.09f);
        p.BeginPath();
        p.Arc(centro, lado * 0.34f, 0f, 360f);
        p.Stroke();

        // Cuatro marcas hacia fuera
        void Marca(float x1, float y1, float x2, float y2)
        {
            p.BeginPath();
            p.MoveTo(P(x1, y1));
            p.LineTo(P(x2, y2));
            p.Stroke();
        }
        Marca(0.5f, 0.02f, 0.5f, 0.20f);
        Marca(0.5f, 0.80f, 0.5f, 0.98f);
        Marca(0.02f, 0.5f, 0.20f, 0.5f);
        Marca(0.80f, 0.5f, 0.98f, 0.5f);

        Disco(p, c, centro, lado * 0.09f);
    }

    static void Reloj(Painter2D p, Punto P, float lado, Color c)
    {
        Vector2 centro = P(0.5f, 0.52f);

        p.strokeColor = c;
        p.lineWidth   = Mathf.Max(1.5f, lado * 0.09f);
        p.BeginPath();
        p.Arc(centro, lado * 0.40f, 0f, 360f);
        p.Stroke();

        // Agujas a las 10:10, la pose que se lee como reloj de un vistazo
        p.BeginPath();
        p.MoveTo(centro);
        p.LineTo(P(0.50f, 0.22f));
        p.Stroke();

        p.BeginPath();
        p.MoveTo(centro);
        p.LineTo(P(0.72f, 0.60f));
        p.Stroke();
    }

    static void Pesa(Painter2D p, Punto P, float lado, Color c)
    {
        // Barra
        p.strokeColor = c;
        p.lineWidth   = Mathf.Max(2f, lado * 0.10f);
        p.BeginPath();
        p.MoveTo(P(0.12f, 0.50f));
        p.LineTo(P(0.88f, 0.50f));
        p.Stroke();

        // Discos a los lados
        Poligono(p, c, P(0.04f, 0.26f), P(0.22f, 0.26f), P(0.22f, 0.74f), P(0.04f, 0.74f));
        Poligono(p, c, P(0.78f, 0.26f), P(0.96f, 0.26f), P(0.96f, 0.74f), P(0.78f, 0.74f));
        Poligono(p, Oscurecer(c, 0.75f), P(0.24f, 0.34f), P(0.36f, 0.34f), P(0.36f, 0.66f), P(0.24f, 0.66f));
        Poligono(p, Oscurecer(c, 0.75f), P(0.64f, 0.34f), P(0.76f, 0.34f), P(0.76f, 0.66f), P(0.64f, 0.66f));
    }

    static void Escudo(Painter2D p, Punto P, Color c)
    {
        p.fillColor = c;
        p.BeginPath();
        p.MoveTo(P(0.50f, 0.04f));
        p.LineTo(P(0.92f, 0.20f));
        p.LineTo(P(0.92f, 0.50f));
        p.BezierCurveTo(P(0.92f, 0.78f), P(0.72f, 0.90f), P(0.50f, 0.97f));
        p.BezierCurveTo(P(0.28f, 0.90f), P(0.08f, 0.78f), P(0.08f, 0.50f));
        p.LineTo(P(0.08f, 0.20f));
        p.ClosePath();
        p.Fill();

        // Mitad izquierda más clara: da volumen y distingue el escudo del corazón
        p.fillColor = Aclarar(c, 1.28f);
        p.BeginPath();
        p.MoveTo(P(0.50f, 0.04f));
        p.LineTo(P(0.08f, 0.20f));
        p.LineTo(P(0.08f, 0.50f));
        p.BezierCurveTo(P(0.08f, 0.78f), P(0.28f, 0.90f), P(0.50f, 0.97f));
        p.ClosePath();
        p.Fill();
    }

    /// <summary>Cubo isométrico de tres caras, la misma geometría de la landing.</summary>
    static void Cubo(Painter2D p, Punto P, Color c)
    {
        Poligono(p, Aclarar(c, 1.30f), P(0.5f, 0.02f), P(0.98f, 0.26f), P(0.5f, 0.50f), P(0.02f, 0.26f));
        Poligono(p, c,                 P(0.02f, 0.26f), P(0.5f, 0.50f), P(0.5f, 0.98f), P(0.02f, 0.74f));
        Poligono(p, Oscurecer(c, 0.62f), P(0.5f, 0.50f), P(0.98f, 0.26f), P(0.98f, 0.74f), P(0.5f, 0.98f));
    }
}

using System.Collections.Generic;
using UnityEngine;

public class CuboInteligente : MonoBehaviour
{
    // ── Lista global de cubos activos ─────────────────────────────────────────
    public static readonly List<CuboInteligente> cubosActivos = new List<CuboInteligente>();

    // ── Sistema de combo ──────────────────────────────────────────────────────
    public static int   comboConsecutivo    = 0;
    private static float ultimoBueno        = 0f;
    private const  float tiempoLimiteCombo  = 5f;  // segundos sin buen cubo → combo se rompe

    private static readonly string[] frasesBuenas = {
        "Good!", "Nice!", "Clean drop!", "Keep it up!", "Well placed!"
    };

    // ── Tipo de cubo (lo asigna GruaController antes de soltar) ──────────────
    [HideInInspector] public TipoCubo tipo = TipoCubo.Normal;

    // ── Materiales ────────────────────────────────────────────────────────────
    [Header("Materiales")]
    public PhysicsMaterial materialNormal;
    public PhysicsMaterial materialPegajoso;
    [Tooltip("Opcional — si está vacío se crea en Start() con bounciness 0.35")]
    public PhysicsMaterial materialBotador;     // Plumas
    [Tooltip("Opcional — si está vacío se crea en Start() con friction 0.02")]
    public PhysicsMaterial materialResbaladizo; // Hielo

    // ── Efectos ───────────────────────────────────────────────────────────────
    [Header("Efectos")]
    public GameObject prefabExplosion;
    public GameObject prefabDust;

    // ── Estado ────────────────────────────────────────────────────────────────
    private bool  haAterrizado          = false;
    private bool  yaExploto             = false;
    private float alturaMaximaAlcanzada = float.MinValue;
    private Rigidbody rb;
    private Collider  col;

    /// <summary>True cuando el cubo ya tocó suelo/plataforma/otro cubo desde arriba.</summary>
    public bool HaAterrizado => haAterrizado;

    // El cubo explota si cae más de este margen por DEBAJO de la plataforma.
    // Se recalcula cada frame con la posición real en AR.
    private const float margenCaidaFuera = 0.40f;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Start()
    {
        cubosActivos.Add(this);

        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        col.material     = materialNormal;
        rb.linearDamping = 0.5f;

        // Crear materiales de física para tipos especiales si no se asignaron en Inspector
        if (materialBotador == null)
        {
            materialBotador = new PhysicsMaterial("Plumas")
            {
                dynamicFriction = 0.7f,
                staticFriction  = 0.7f,
                bounciness      = 0.35f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine   = PhysicsMaterialCombine.Maximum
            };
        }
        if (materialResbaladizo == null)
        {
            materialResbaladizo = new PhysicsMaterial("Hielo")
            {
                dynamicFriction = 0.02f,
                staticFriction  = 0.02f,
                bounciness      = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine   = PhysicsMaterialCombine.Maximum
            };
        }
    }

    void OnDestroy()
    {
        cubosActivos.Remove(this);
    }

    void Update()
    {
        // IMPORTANTE: no bloquear por haAterrizado.
        // Un cubo que aterrizó en la torre y luego se cae del borde también debe explotar.
        // El check de posición Y es seguro para cubos en la torre (están SOBRE el suelo, no bajo él).
        if (yaExploto) return;

        // Rastrear la Y más alta alcanzada para detectar caída desde la torre
        if (haAterrizado && transform.position.y > alturaMaximaAlcanzada)
            alturaMaximaAlcanzada = transform.position.y;

        if (GameManager.Instance != null)
        {
            float suelo = GameManager.Instance.ObtenerAlturaSuelo();
            if (transform.position.y < suelo - margenCaidaFuera)
                ExplotarYDestruir(porCaida: true);
        }
    }

    // ── Explosión ─────────────────────────────────────────────────────────────

    /// <summary>
    /// porCaida = true cuando el cubo cae físicamente de la torre.
    /// false cuando es destruido por interacción (fuego, hielo, etc.).
    /// </summary>
    void ExplotarYDestruir(bool porCaida = false)
    {
        yaExploto = true;

        // Notificar al GameManager que un cubo cayó de la torre (da gracia al pozo)
        if (porCaida && haAterrizado)
            GameManager.Instance?.NotificarCuboCaido();

        // Partícula personalizada (si está asignada en el Inspector)
        if (prefabExplosion != null)
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);

        // Siempre lanzar mini-cubos (efecto dramático garantizado aunque no haya prefab)
        SpawnMiniCubos();

        var rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        Destroy(gameObject, 0.1f);
    }

    void SpawnMiniCubos()
    {
        Color colorBase = Color.white;
        var rendOrig = GetComponent<Renderer>();
        if (rendOrig != null && rendOrig.material != null)
            colorBase = rendOrig.material.color;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Cada tipo tiene su propio comportamiento de fragmentos
        int   numFrags  = tipo == TipoCubo.Plomo ? 8 : tipo == TipoCubo.Hielo ? 10 : 5;
        float tamFrag   = transform.localScale.x * (tipo == TipoCubo.Plomo ? 0.20f : 0.28f);
        float lifetime  = tipo == TipoCubo.Hielo  ? 2.0f
                        : tipo == TipoCubo.Plumas  ? 1.4f
                        : tipo == TipoCubo.Fuego   ? 0.5f
                        : tipo == TipoCubo.Plomo   ? 0.5f : 0.8f;
        float masa      = tipo == TipoCubo.Plomo   ? 0.5f
                        : tipo == TipoCubo.Plumas   ? 0.01f : 0.1f;

        for (int i = 0; i < numFrags; i++)
        {
            var mini = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mini.name  = "Fragmento";
            mini.layer = LayerMask.NameToLayer("Default");
            mini.transform.position   = transform.position + Random.insideUnitSphere * transform.localScale.x * 0.3f;
            mini.transform.rotation   = Random.rotation;
            mini.transform.localScale = Vector3.one * tamFrag;

            if (shader != null)
            {
                var mat = new Material(shader);
                float h, s, v;
                Color.RGBToHSV(colorBase, out h, out s, out v);
                mat.color = Color.HSVToRGB(h, s, v * Random.Range(0.7f, 1.0f));
                // Hielo: fragmentos semitransparentes
                if (tipo == TipoCubo.Hielo)
                {
                    Color c = mat.color; c.a = 0.6f; mat.color = c;
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                }
                // Fuego: fragmentos con brillo emisivo
                if (tipo == TipoCubo.Fuego && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", colorBase * 0.8f);
                    mat.EnableKeyword("_EMISSION");
                }
                mini.GetComponent<Renderer>().material = mat;
            }

            var rb = mini.AddComponent<Rigidbody>();
            rb.mass           = masa;
            rb.linearDamping  = tipo == TipoCubo.Plumas ? 3.0f : 0.4f;
            rb.angularDamping = 0.5f;

            // ── Dirección según tipo ────────────────────────────────────────────
            Vector3 dir;
            float speed;
            switch (tipo)
            {
                case TipoCubo.Hielo:
                    // Shards de hielo caen HACIA ABAJO (astillas que se precipitan)
                    dir   = (Random.insideUnitSphere * 0.6f + Vector3.down * 1.8f).normalized;
                    speed = Random.Range(0.8f, 1.6f);
                    break;
                case TipoCubo.Fuego:
                    // Chispas van HACIA ARRIBA con velocidad alta
                    dir   = (Random.insideUnitSphere * 0.5f + Vector3.up * 2.0f).normalized;
                    speed = Random.Range(0.8f, 1.4f);
                    break;
                case TipoCubo.Plomo:
                    // Impacto pesado: fragmentos van a los LADOS con fuerza
                    Vector3 lateral = Random.insideUnitSphere;
                    lateral.y = Random.Range(-0.1f, 0.2f);
                    dir   = lateral.normalized;
                    speed = Random.Range(1.0f, 2.0f);
                    break;
                case TipoCubo.Plumas:
                    // Plumas flotan suavemente hacia ARRIBA
                    dir   = (Random.insideUnitSphere * 0.2f + Vector3.up).normalized;
                    speed = Random.Range(0.05f, 0.2f);
                    break;
                default: // Normal
                    dir   = (Random.insideUnitSphere + Vector3.up * 0.5f).normalized;
                    speed = Random.Range(0.4f, 0.9f);
                    break;
            }
            rb.AddForce(dir * speed, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);

            var colMini = mini.GetComponent<Collider>();
            foreach (var cubo in cubosActivos)
                if (cubo != null)
                    Physics.IgnoreCollision(colMini, cubo.GetComponent<Collider>(), true);

            Destroy(mini, lifetime);
        }
    }

    // ── Colisión ──────────────────────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        // ── Cubo de torre que cayó desde altura: explotar al volver a impactar ─
        if (haAterrizado)
        {
            // Detect a falling cube landing on top of me → trigger interaction
            var cuboEncima = collision.gameObject.GetComponent<CuboInteligente>();
            if (cuboEncima != null && !cuboEncima.HaAterrizado)
                AplicarEfectoDeImpacto(cuboEncima.tipo);

            if (!yaExploto && alturaMaximaAlcanzada > float.MinValue)
            {
                float caida     = alturaMaximaAlcanzada - transform.position.y;
                float velBajada = rb != null ? Mathf.Max(0f, -rb.linearVelocity.y) : 0f;
                if (caida > transform.localScale.y * 3f && velBajada > 2.0f)
                    ExplotarYDestruir(porCaida: true);
            }
            return;
        }

        bool esSuelo = collision.gameObject.CompareTag("Plataforma")
                    || collision.gameObject.CompareTag("Cubo");
        if (!esSuelo) return;

        // Solo consideramos aterrizaje si golpeamos DESDE ARRIBA
        ContactPoint contacto    = collision.GetContact(0);
        bool cayoDesdeArriba     = Vector3.Dot(contacto.normal, Vector3.up) > 0.6f;
        if (!cayoDesdeArriba) return;

        bool sobreOtroCubo = collision.gameObject.CompareTag("Cubo");

        // ── Snap assist ──────────────────────────────────────────────────────
        float tolerancia = LevelManager.ObtenerToleranciaSnap();
        Vector3 posSoporte = collision.transform.position;

        float distXZ = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(posSoporte.x, posSoporte.z));

        if (tolerancia > 0f && distXZ < tolerancia)
        {
            transform.position = new Vector3(posSoporte.x, transform.position.y, posSoporte.z);
            distXZ = 0f;
        }

        // ── Física de inestabilidad ──────────────────────────────────────────
        // Solo cuando cae sobre OTRO CUBO (sobre la plataforma siempre es estable).
        float overhangRatio = 0f;
        if (sobreOtroCubo)
        {
            // IMPORTANTE: no usar posSoporte != Vector3.zero como condición
            // (si el cubo está en world origin (0,0,0) la comparación falla).
            // Siempre usar localScale del objeto colisionado.
            float anchoSoporte = collision.transform.localScale.x;

            // ratio: 0 = perfectamente centrado
            //        0.5 = borde del cubo nuevo sobre borde del cubo debajo (punto de vuelco)
            //        > 0.5 = más de la mitad colgando → caerá
            overhangRatio = anchoSoporte > 0.001f ? distXZ / anchoSoporte : 0f;
        }

        Aterrizar(overhangRatio, posSoporte, sobreOtroCubo);
    }

    // ── Aterrizaje con física de estabilidad ──────────────────────────────────
    //
    //  overhangRatio:
    //    < 0.50  → cubo centrado o ligeramente desplazado: ESTABLE, rotación congelada
    //    ≥ 0.50  → más de la mitad del cubo colgando (punto de vuelco real): INESTABLE
    //
    //  El punto de vuelco físico de un cubo uniforme es exactamente cuando su centro
    //  de masa sale del borde del soporte, es decir a 0.5 * anchoSoporte.
    //  Usar 0.50 como umbral es físicamente correcto y da la mejor jugabilidad.
    //
    //  IMPORTANTE: se eliminó el caso intermedio con FreezePositionY porque causaba
    //  que cubos empujados lateralmente "flotaran" al no poder bajar en Y.
    //
    void Aterrizar(float overhangRatio, Vector3 posSoporte, bool sobreOtroCubo = false)
    {
        haAterrizado = true;
        alturaMaximaAlcanzada = transform.position.y; // base para detectar caída futura

        // ── Física según tipo ─────────────────────────────────────────────────
        AplicarFisicaAterrizaje();

        // ── Efecto especial Fuego: sacudir cubos vecinos ──────────────────────
        if (tipo == TipoCubo.Fuego)
            EfectoFuego();

        if (overhangRatio < 0.50f)
        {
            // ── ESTABLE: menos de la mitad colgando ──────────────────────────
            // Rotación completamente congelada. El cubo NUNCA se caerá por sí solo.
            // Puede recibir cubos pesados encima sin moverse.
            rb.constraints    = RigidbodyConstraints.FreezeRotation;
            rb.linearDamping  = 8.0f;
            rb.angularDamping = 20.0f;
        }
        else
        {
            // ── INESTABLE: más de la mitad colgando → tiende a caer ──────────
            // Sin restricciones. Baja amortiguación para que caiga de forma dramática.
            // Un cubo que cae encima lo tumbará con seguridad.
            rb.constraints    = RigidbodyConstraints.None;
            rb.linearDamping  = 1.5f;
            rb.angularDamping = 2.0f;

            // Torque inicial en la dirección del colgante (inicia la caída visualmente)
            Vector3 dirColgar = new Vector3(
                transform.position.x - posSoporte.x,
                0f,
                transform.position.z - posSoporte.z);

            if (dirColgar.sqrMagnitude > 0.0001f)
            {
                dirColgar.Normalize();
                // Aplicar torque en el PRÓXIMO fixed frame.
                // SoltarCubo activa FreezeRotationX|Z y Unity los sigue aplicando durante
                // el frame de física del impacto — AddTorque inline no tiene efecto.
                // Deferirlo un WaitForFixedUpdate garantiza que los constraints ya estén
                // en RigidbodyConstraints.None cuando el torque se aplica.
                StartCoroutine(AplicarTorqueSiguienteFrame(dirColgar, overhangRatio));
            }
        }

        // Polvo de aterrizaje en la base del cubo, con el color del tipo
        if (prefabDust != null)
        {
            Vector3 base3D = transform.position - Vector3.up * (transform.localScale.y * 0.5f);
            var dustGO = Instantiate(prefabDust, base3D, Quaternion.Euler(180f, 0f, 0f));
            var ps = dustGO.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ConfigTipoCubo cfg = GruaController.Instance?.ObtenerConfig(tipo);
                if (cfg != null) { var m = ps.main; m.startColor = cfg.colorEnVuelo; }
            }
        }

        // Evaluar combo: solo cuenta cuando cae sobre OTRO CUBO, no sobre la plataforma
        EvaluarCombo(overhangRatio, sobreOtroCubo);

        // Tutorial paso 4: primer cubo que aterriza → último paso antes de completar
        TutorialManager.Instance?.IrAPaso(4);

        // Tutorial completo después de un momento (deja al jugador leer el mensaje)
        if (TutorialManager.Instance != null)
            StartCoroutine(CompletarTutorialTras(3.5f));
    }

    System.Collections.IEnumerator CompletarTutorialTras(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        TutorialManager.Instance?.Completar();
    }

    System.Collections.IEnumerator AplicarTorqueSiguienteFrame(Vector3 dir, float ratio)
    {
        yield return new WaitForFixedUpdate();
        if (rb == null || yaExploto) yield break;
        Vector3 ejeTorque = Vector3.Cross(Vector3.up, dir);
        rb.AddTorque(ejeTorque * Mathf.Clamp(ratio * 0.8f, 0f, 0.8f), ForceMode.Impulse);
    }

    // ── Física de aterrizaje por tipo ─────────────────────────────────────────

    void AplicarFisicaAterrizaje()
    {
        switch (tipo)
        {
            case TipoCubo.Plomo:
                col.material      = materialPegajoso;
                rb.mass           = 40f;   // sigue pesado tras aterrizar
                rb.linearDamping  = 8.0f;
                rb.angularDamping = 20.0f;
                break;

            case TipoCubo.Plumas:
                col.material      = materialBotador;
                rb.mass           = 0.8f;  // sigue liviano — puede ser empujado
                rb.linearDamping  = 5.0f;
                rb.angularDamping = 8.0f;
                break;

            case TipoCubo.Hielo:
                col.material      = materialResbaladizo;
                rb.mass           = 5f;
                rb.linearDamping  = 1.0f;  // poca resistencia: puede deslizarse
                rb.angularDamping = 2.0f;
                break;

            case TipoCubo.Fuego:
                col.material      = materialPegajoso;
                rb.mass           = 10f;
                rb.linearDamping  = 8.0f;
                rb.angularDamping = 20.0f;
                StartCoroutine(PulsarEmisionFuego());   // emisión viva al reposar
                break;

            default: // Normal
                col.material      = materialPegajoso;
                rb.mass           = 10.0f;
                break;
        }
    }

    // ── Efecto Fuego: mini-explosión radial ───────────────────────────────────

    void EfectoFuego()
    {
        float radio = transform.localScale.x * 2.2f;
        Collider[] vecinos = Physics.OverlapSphere(transform.position, radio);
        foreach (var c in vecinos)
        {
            if (c.gameObject == gameObject) continue;
            Rigidbody rbVecino = c.GetComponent<Rigidbody>();
            if (rbVecino == null || rbVecino.isKinematic) continue;

            Vector3 dir  = (c.transform.position - transform.position);
            float   dist = Mathf.Max(dir.magnitude, 0.01f);
            dir = dir.normalized;
            dir.y = Mathf.Max(dir.y, 0.25f); // siempre un poco hacia arriba

            // Fuerza inversamente proporcional a la distancia, clampada
            float fuerza = Mathf.Clamp(0.5f / dist, 0.05f, 0.45f);
            rbVecino.AddForce(dir * fuerza, ForceMode.Impulse);
        }

        // Flash naranja breve en el renderer del cubo de fuego
        StartCoroutine(FlashFuego());
    }

    // Fuego: emisión que pulsa suavemente una vez aterrizado (parece que sigue ardiendo)
    System.Collections.IEnumerator PulsarEmisionFuego()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null || !rend.material.HasProperty("_EmissionColor")) yield break;

        rend.material.EnableKeyword("_EMISSION");
        Color colorBase = rend.material.color;
        float t = 0f;

        while (!yaExploto && rend != null)
        {
            t += Time.deltaTime;
            // Pulso irregular: mezcla de 2 senos con frecuencias distintas
            float intensidad = 0.3f + 0.25f * Mathf.Sin(t * 3.5f) + 0.1f * Mathf.Sin(t * 7.3f + 1.2f);
            rend.material.SetColor("_EmissionColor", colorBase * intensidad);
            yield return null;
        }

        // Apagar emisión al destruirse
        if (rend != null && rend.material.HasProperty("_EmissionColor"))
            rend.material.SetColor("_EmissionColor", Color.black);
    }

    System.Collections.IEnumerator FlashFuego()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) yield break;
        Color orig = rend.material.color;
        rend.material.color = new Color(1f, 0.45f, 0.1f, 1f);
        yield return new WaitForSeconds(0.18f);
        if (rend != null) rend.material.color = orig;
    }

    // ── Sistema de combo ──────────────────────────────────────────────────────

    void EvaluarCombo(float overhangRatio, bool sobreOtroCubo)
    {
        // Primer cubo sobre la plataforma: nunca cuenta como "bien puesto"
        // El combo solo empieza cuando un cubo cae sobre OTRO CUBO
        if (!sobreOtroCubo)
        {
            comboConsecutivo = 0;
            return;
        }

        // Criterio estricto: máximo 12% de overhang para ser "bien puesto"
        // (anteriormente 20% — demasiado permisivo)
        bool bueno = overhangRatio < 0.12f;

        // Reiniciar combo si pasó demasiado tiempo
        if (Time.time - ultimoBueno > tiempoLimiteCombo)
            comboConsecutivo = 0;

        if (bueno)
        {
            comboConsecutivo++;
            ultimoBueno = Time.time;
            MostrarMensajeCombo();
        }
        else if (overhangRatio >= 0.40f)
        {
            // Claramente descentrado → romper combo
            comboConsecutivo = 0;
        }
        // Entre 0.12 y 0.40: ni suma ni rompe (zona neutral)
    }

    void MostrarMensajeCombo()
    {
        string msg;
        Color  col;

        switch (comboConsecutivo)
        {
            case 1:
                msg = frasesBuenas[UnityEngine.Random.Range(0, frasesBuenas.Length)];
                col = Color.white;
                break;
            case 2:
                msg = "Aligned  x2!";
                col = new Color(0.45f, 1f, 0.45f);
                break;
            case 3:
                msg = "x3  Excellent!";
                col = new Color(0.2f, 1f, 0.2f);
                break;
            case 4:
                msg = "x4  Great stack!";
                col = Color.cyan;
                break;
            case 5:
                msg = "x5  PERFECT!";
                col = new Color(0f, 1f, 1f);
                break;
            default:
                msg = $"x{comboConsecutivo}  UNSTOPPABLE!";
                col = Color.yellow;
                break;
        }

        MensajeFlotante.Mostrar(msg, col, 1.5f);
    }

    // ── Referencia de altura para el pozo ─────────────────────────────────────

    public void AjustarAlturaReferencia(float delta)
    {
        if (alturaMaximaAlcanzada > float.MinValue)
            alturaMaximaAlcanzada += delta;
    }

    // ── Interacciones entre cubos ─────────────────────────────────────────────

    void AplicarEfectoDeImpacto(TipoCubo tipoCuboArriba)
    {
        if (yaExploto) return;
        switch (tipo)
        {
            case TipoCubo.Plumas when tipoCuboArriba == TipoCubo.Plomo:
                StartCoroutine(ComprimirPlumas()); break;
            case TipoCubo.Hielo  when tipoCuboArriba == TipoCubo.Plomo:
                ExplotarYDestruir(); break;
            case TipoCubo.Plumas when tipoCuboArriba == TipoCubo.Fuego:
                StartCoroutine(QuemarPlumas()); break;
            case TipoCubo.Hielo  when tipoCuboArriba == TipoCubo.Fuego:
                StartCoroutine(DerretirHielo()); break;
            case TipoCubo.Normal when tipoCuboArriba == TipoCubo.Fuego:
                StartCoroutine(CalentarBase()); break;
        }
    }

    System.Collections.IEnumerator ComprimirPlumas()
    {
        // Nube de plumas flotando HACIA ARRIBA al ser aplastadas
        if (prefabDust != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * transform.localScale.y * 0.5f;
            var puff = Instantiate(prefabDust, spawnPos, Quaternion.identity);
            var ps   = puff.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor      = new Color(0.95f, 0.92f, 0.78f, 0.9f);
                main.gravityModifier = -0.6f;   // flotan hacia ARRIBA
                main.startSpeed      = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
                main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
                main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            }
            Destroy(puff, 2.0f);
        }

        float elapsed = 0f, dur = 0.3f;
        Vector3 inicio = transform.localScale;
        Vector3 fin    = new Vector3(inicio.x, inicio.y * 0.6f, inicio.z);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(inicio, fin, elapsed / dur);
            yield return null;
        }
        transform.localScale = fin;
    }

    System.Collections.IEnumerator QuemarPlumas()
    {
        // Humo negro subiendo mientras la pluma se quema
        if (prefabDust != null)
        {
            var smoke = Instantiate(prefabDust, transform.position, Quaternion.identity);
            smoke.transform.SetParent(transform);
            var ps = smoke.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor      = new Color(0.08f, 0.08f, 0.08f, 0.9f);
                main.gravityModifier = -1.2f;   // humo sube
                main.startSpeed      = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
                main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            }
            Destroy(smoke, 0.8f);
        }
        yield return new WaitForSeconds(0.5f);
        if (!yaExploto) ExplotarYDestruir();
    }

    System.Collections.IEnumerator DerretirHielo()
    {
        // Goteo azul claro cayendo hacia ABAJO mientras se derrite
        if (prefabDust != null)
        {
            var drip = Instantiate(prefabDust, transform.position, Quaternion.identity);
            drip.transform.SetParent(transform);
            var ps = drip.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor      = new Color(0.55f, 0.82f, 0.96f, 0.85f);
                main.gravityModifier = 3.5f;    // goteo cae rápido hacia ABAJO
                main.startSpeed      = new ParticleSystem.MinMaxCurve(0.02f, 0.15f);
                main.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
                main.startSize       = new ParticleSystem.MinMaxCurve(0.005f, 0.02f);
            }
            Destroy(drip, 2.5f);
        }

        float elapsed = 0f, dur = 2f;
        Vector3 inicio = transform.localScale;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(inicio, Vector3.zero, elapsed / dur);
            yield return null;
        }
        if (!yaExploto) ExplotarYDestruir();
    }

    System.Collections.IEnumerator CalentarBase()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) yield break;
        Color inicio = rend.material.color;
        Color meta   = new Color(1f, 0.3f, 0.1f, 1f);
        float elapsed = 0f, dur = 1.5f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            if (rend != null)
                rend.material.color = Color.Lerp(inicio, meta, elapsed / dur);
            yield return null;
        }
    }
}

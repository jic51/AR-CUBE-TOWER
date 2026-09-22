using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Mantiene la torre pegada al suelo real cuando ARCore corrige su tracking.
///
/// El problema: ARCore calcula la posición del teléfono combinando cámara e
/// IMU. La IMU acumula error y, cuando la cámara reconoce una zona ya vista,
/// ARCore corrige de golpe. Todo lo que esté colocado en coordenadas fijas del
/// mundo virtual "salta" respecto al suelo real — la plataforma y los cubos se
/// desplazan juntos hacia un lado, como si el plano de debajo se hubiera movido.
///
/// La solución (la misma que usan las apps de muebles en AR): un ARAnchor en el
/// punto donde se ancló la plataforma. ARCore mantiene el ancla fija respecto al
/// mundo real y la mueve cuando corrige. Aquí detectamos ese movimiento y
/// aplicamos exactamente el mismo desplazamiento a la plataforma y a todos los
/// cubos a la vez, así la torre sigue al ancla sin deformarse.
///
/// No se hace hija la torre del ancla a propósito: los cubos son rigidbodies
/// dinámicos, y moverlos por jerarquía los teletransporta sin control. Aplicar
/// el delta a mano mantiene la física estable y avisa al GameManager para que
/// el pozo y la detección de caídas corrijan sus alturas de referencia.
/// </summary>
public class AnclaTorre : MonoBehaviour
{
    // Umbrales por debajo de los cuales no se aplica corrección. El ancla se
    // refina continuamente en fracciones de milímetro; aplicar cada una haría
    // vibrar la torre. Como la referencia no se actualiza hasta aplicar, los
    // pequeños ajustes se acumulan y no se pierde ninguno.
    private const float UmbralMetros = 0.003f;
    private const float UmbralGrados = 0.3f;

    private ARAnchor  _ancla;
    private Transform _plataforma;
    private bool      _tieneReferencia;
    private Vector3   _posRef;
    private float     _yawRef;

    /// <summary>
    /// Crea el ancla en la pose actual de la plataforma. origenXR es el objeto
    /// XR Origin (donde viven los managers de trackables). Si la escena no tiene
    /// ARAnchorManager, se añade aquí: sin él ARCore no rastrea las anclas.
    /// </summary>
    public static AnclaTorre Crear(Transform plataforma, GameObject origenXR)
    {
        if (plataforma == null || origenXR == null) return null;

        if (origenXR.GetComponent<ARAnchorManager>() == null)
            origenXR.AddComponent<ARAnchorManager>();

        var go = new GameObject("AnclaTorre");
        go.transform.SetPositionAndRotation(plataforma.position, plataforma.rotation);

        var comp = go.AddComponent<AnclaTorre>();
        comp._plataforma = plataforma;
        comp._ancla      = go.AddComponent<ARAnchor>();
        return comp;
    }

    void LateUpdate()
    {
        if (_ancla == null || _plataforma == null) return;

        // Mientras ARCore no rastrea el ancla, su pose no es fiable: no corregir
        if (_ancla.trackingState != TrackingState.Tracking) return;

        Vector3 pos = _ancla.transform.position;
        float   yaw = _ancla.transform.eulerAngles.y;

        // La primera pose rastreada es la referencia de partida
        if (!_tieneReferencia)
        {
            _posRef = pos;
            _yawRef = yaw;
            _tieneReferencia = true;
            return;
        }

        float dYaw = Mathf.DeltaAngle(_yawRef, yaw);
        if ((pos - _posRef).sqrMagnitude < UmbralMetros * UmbralMetros &&
            Mathf.Abs(dYaw) < UmbralGrados) return;

        // Solo giro en Y: el suelo es horizontal, y aplicar inclinación hacía
        // que el ruido del ancla ladeara la torre entera.
        Quaternion giro = Quaternion.Euler(0f, dYaw, 0f);
        AplicarCorreccion(_plataforma, null, giro, pos);

        foreach (var cubo in CuboInteligente.cubosActivos)
        {
            if (cubo == null) continue;
            AplicarCorreccion(cubo.transform, cubo.GetComponent<Rigidbody>(), giro, pos);
        }

        float dy = pos.y - _posRef.y;
        GameManager.Instance?.NotificarCorreccionAncla(dy);

        _posRef = pos;
        _yawRef = yaw;
    }

    /// <summary>Mueve el objeto como un sólido rígido junto con el ancla.</summary>
    void AplicarCorreccion(Transform t, Rigidbody rb, Quaternion giro, Vector3 posNueva)
    {
        Vector3    p = posNueva + giro * (t.position - _posRef);
        Quaternion r = giro * t.rotation;

        if (rb != null)
        {
            rb.position = p;
            rb.rotation = r;
            if (!rb.isKinematic) rb.linearVelocity = giro * rb.linearVelocity;
        }
        t.SetPositionAndRotation(p, r);
    }
}

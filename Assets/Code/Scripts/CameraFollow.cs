using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Arrastra tu Player aquí
    public Vector3 offset = new Vector3(0f, 3f, -6f); // Distancia fija respecto al jugador
    public float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Calcula la posición deseada manteniendo la distancia fija
        Vector3 desiredPosition = target.position + offset;

        // Mueve la cámara suavemente hacia esa posición
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Hace que la cámara siempre apunte al jugador
        transform.LookAt(target.position);
    }
}
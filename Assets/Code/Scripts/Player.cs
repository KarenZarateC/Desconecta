using System.Linq.Expressions;
using UnityEngine;

public class Player: MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 5f;
    // movimiento del player en eje x y z

    private void Awake()
    {
        Debug.Log("Entriiiiiioo");
        // es un meotdo que se ejecuta una unica vez cuando se inicia  y se hace antes del start
        rb = GetComponent<Rigidbody>();


    }

    private void Update()
    {
        Debug.Log("Entrooooooo");
        // asignacion de las teclas a la variable
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveHorizontal * speed, rb.linearVelocity.y, moveVertical * speed);
        rb.linearVelocity = move;

    }





}

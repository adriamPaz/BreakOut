using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PelotaController : MonoBehaviour
{
    Rigidbody2D rb;
    // Mantenemos un registro de los golpes con la pala.
    int contadorGolpes = 0;

    // Acumulamos los ladrillos que destruimos 
    int brickCount;

    // Definimos la fuerza a aplicar para aumentar la velocidad.
    [SerializeField] float fuerzaIncrementada;
    [SerializeField] float delay;
    [SerializeField] float force;
    [SerializeField] AudioClip sfxPaddel;  // Sonido al chocar con la pala
    [SerializeField] AudioClip sfxBrick;   // Sonido al chocar con un ladrillo
    [SerializeField] AudioClip sfxWall;    // Sonido al chocar con una pared
    [SerializeField] AudioClip sfxFail;    // Sonido al salir por la pared inferior
    [SerializeField] GameObject pala;
    bool halved = false;

    AudioSource sfx;  // Componente AudioSource

    // ID de la escena 
    int sceneId;

    // Estructura donde almacenaremos las etiquetas y la puntuación de cada ladrillo
    Dictionary<string, int> ladrillos = new Dictionary<string, int>(){
    {"Ladrillo-Amarillo", 10},
    {"Ladrillo-Verde", 15},
    {"Ladrillo-Naranja", 20},
    {"Ladrillo-Rojo", 25},
    {"Ladrillo-Atravesable", 25},
    };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sceneId = SceneManager.GetActiveScene().buildIndex;

        Invoke("LanzarPelota", delay);
    }

    void Update()
    {

    }

    private void LanzarPelota()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        float dirX, dirY = -1;
        dirX = Random.Range(0, 2) == 0 ? -1 : 1;
        Vector2 dir = new Vector2(dirX, dirY);
        dir.Normalize();

        rb.AddForce(dir * force, ForceMode2D.Impulse);
    }




    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si atravesamos un ladrillo rojo atravesable 
        if (other.tag == "Ladrillo-Atravesable")
        {
            //Sumamos puntos
            GameManager.UpdateScore(ladrillos[other.tag]);
            //Sonido del ladrillo
            sfx.clip = sfxBrick;
            sfx.Play();
            //Se desactiva el collider para que la pelota no detecte el "Trigger" y no sumar puntos
            other.enabled = false;
        }
        // Comprobamos si el objeto que estamos atravesando es la pared inferior 
        if (other.tag == "ParedInferior")
        {

            if (halved)
            {
                HalvePaddle(false);

            }
            // Actualizamos el contador de vidas
            GameManager.UpdateLives();
            sfx.clip = sfxFail;
            sfx.Play();
            // Si ya no quedan vidas
            if (GameManager.Lives <= 0)
            {
                //Se detiene el movimiento de la pelota
                rb.linearVelocity = Vector2.zero;
                //Se desactiva la pelota
                gameObject.SetActive(false);
                //Se sale del método para que no se relance
                return;
            }

            // Si aún quedan vidas se vuelve a lanzar la pelota
            Invoke("LanzarPelota", delay);
        }
    }


    void DestroyBrick(GameObject obj)
    {
        sfx.clip = sfxBrick;
        sfx.Play();
        // Actualizamos la puntuación 
        GameManager.UpdateScore(ladrillos[obj.tag]);
        // Se destruye el objeto
        Destroy(obj);
        // Actualizamos el contador de ladrillos destruidos
        ++brickCount;
        // Comprobamos si hemos alcanzado el máximo de ladrillos. Necesitamos el índice de la escena en la que nos encontramos para saber cuántos ladrillos tenemos. 
        if (brickCount == GameManager.totalBricks[sceneId])
        {
            // Reproducimos el sonido de transición
            sfx.clip = sfxBrick;
            sfx.Play();
            // Detenemos el movimiento de la pelota
            rb.linearVelocity = Vector2.zero;
            // Invocamos el método para pasar a la siguiente escena después de 3 segundos
            Invoke("NextScene", 3);
        }
    }

    void NextScene()
    {
        int nextId = sceneId + 1;
        if (nextId == SceneManager.sceneCountInBuildSettings)
        {
            nextId = 0;
        }
        SceneManager.LoadScene(nextId);
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        // Almacenamos la etiqueta del objeto con el que estamos colisionando
        string tag = other.gameObject.tag;
        if (!halved && tag == "ParedSuperior")
        {
            HalvePaddle(true);
        }


        // Comprobamos si la etiqueta es un ladrillo 
        if (ladrillos.ContainsKey(tag))
        {
            // Actualizamos la puntuación utilizando el valor del diccionario
            GameManager.UpdateScore(ladrillos[tag]);
            // Destruimos el objeto
            if (ladrillos.ContainsKey(tag) && tag != "Ladrillo-Atravesable")
            {
                DestroyBrick(other.gameObject);
            }
        }

        if (tag == "Pala")
        {
            // Obtenemos la posición de la pala
            Vector3 pala = other.gameObject.transform.position;
            // Obtenemos el punto de contacto. Cuando colisionan dos objetos, colisionan en una superficie, y devolvería todos los puntos donde colisionan. Nos quedamos con el primero 
            Vector2 contact = other.GetContact(0).point;

            // Incrementamos el contador de golpes cada vez que la pelota golpea la pala.
            contadorGolpes++;

            // Si el contador de golpes es un múltiplo de 4, incrementamos la velocidad.
            if (contadorGolpes % 4 == 0)
            {
                // Aplicamos una fuerza adicional en la dirección actual de movimiento de la pelota.
                rb.AddForce(rb.linearVelocity * fuerzaIncrementada, ForceMode2D.Impulse);
            }

            // Comprobamos la dirección en x (para saber si está viajando hacia la izquierda o a la derecha)
            // Si la pelota está viajando desde la izquierda hacia la derecha y está golpeando con la parte derecha de la pala
            // o si la pelota está viajando desde la derecha hacia la izquierda y está golpeando con la parte izquierda de la pala
            if (rb.linearVelocity.x < 0 && contact.x > pala.x ||
                    rb.linearVelocity.x > 0 && contact.x < pala.x)
            {
                rb.linearVelocity = new Vector2(-rb.linearVelocityX, rb.linearVelocityY);
            }
        }


        if (tag == "Pala")
        {
            sfx.clip = sfxPaddel;
            sfx.Play();
        }
        else if (ladrillos.ContainsKey(tag))
        {
            sfx.clip = sfxBrick;
            sfx.Play();
        }
        else if (tag == "ParedDerecha" || tag == "ParedIzquierda" || tag == "ParedSuperior" || tag == "Ladrillo-Indestructible")
        {
            sfx.clip = sfxWall;
            sfx.Play();
        }
    }

    public void HalvePaddle(bool reducir)
    {
        halved = reducir;
        Vector3 escalaActual = pala.transform.localScale;
        pala.transform.localScale = reducir ?
            new Vector3(escalaActual.x * 0.5f, escalaActual.y, escalaActual.z) :
            new Vector3(escalaActual.x * 2f, escalaActual.y, escalaActual.z);
    }
}
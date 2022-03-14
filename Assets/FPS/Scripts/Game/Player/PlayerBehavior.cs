using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    private Rigidbody rBody;
    private bool isWorld;
    private bool isJumping;
    [SerializeField]
    private AudioClip footAudio;
    [SerializeField]
    private AudioClip jumpAudio;
    AudioSource audio;

    void Awake()
    {
        ActorsManager actorsManager = FindObjectOfType<ActorsManager>();
        if (actorsManager != null)
            actorsManager.SetPlayer(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isWorld)
        {
            isJumping = true;
            rBody.AddForce(Vector3.up * 200);
            audio.Stop();
            audio.PlayOneShot(jumpAudio);
        }
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        if ((horizontal != 0 || vertical != 0) && isWorld && !isJumping)
        {
            if (!audio.isPlaying)
                audio.PlayOneShot(footAudio);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "World")
        {
            isWorld = true;
            isJumping = false;
        }
            
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.tag == "World")
        {
            isWorld = false;
        }
            
    }
}

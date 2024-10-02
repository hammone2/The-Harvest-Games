using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviourPun
{
    public int id;
    public Player photonPlayer;
    private int curAttackerId;

    [Header("Movement Stuff")]
    public float walkSpeed;
    public float sprintSpeed;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float jumpForce;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting = false;
    private float currentSpeed;

    [Header("Stats")]
    public int curHp;
    public int maxHp;
    public int kills;
    public bool dead;
    private bool flashingDamage;
    public PlayerWeapon weapon;

    [Header("Components")]
    public CharacterController controller;
    public MeshRenderer mr;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        if (!photonView.IsMine || dead)
            return;

        // Check if the player is grounded
        isGrounded = controller.isGrounded;

        // Get movement input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Create a movement vector
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Normalize the movement vector
        if (move.magnitude > 1)
        {
            move.Normalize();
        }


        // Determine speed (only sprint when moving forward)
        if (isGrounded)
        {
            //velocity.y = 0;
            // Handle jumping
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isGrounded = false;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (moveZ > 0)
                {
                    isSprinting = true;
                }
                else
                {
                    isSprinting = false;
                }
            }
            else
            {
                isSprinting = false;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
        
        if (!isSprinting)
        {
            currentSpeed = walkSpeed; // Default to walk speed
        }
        else
        {
            currentSpeed = sprintSpeed; // Sprint speed when moving forward
        }

        //Debug.Log(currentSpeed);

        // Apply movement
        Vector3 movement = move * currentSpeed * Time.deltaTime;
        controller.Move(movement);

        // Handle jumping
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            isGrounded = false;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        //velocity.y += gravity * Time.deltaTime;

        // Move the player vertically
        controller.Move(velocity * Time.deltaTime);


        // Handle Shooting
        if (Input.GetMouseButtonDown(0))
            weapon.TryShoot();
    }

    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        GameManager.instance.players[id - 1] = this;
        // is this not our local player?
        if (!photonView.IsMine)
        {
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            //rig.isKinematic = true;
        }
        else
        {
            GameUI.instance.Initialize(this);
        }
    }

    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead)
            return;
        curHp -= damage;
        curAttackerId = attackerId;
        // flash the player red
        photonView.RPC("DamageFlash", RpcTarget.Others);
        // update the health bar UI
        GameUI.instance.UpdateHealthBar();
        // die if no health left
        if (curHp <= 0)
            photonView.RPC("Die", RpcTarget.All);
    }

    [PunRPC]
    void DamageFlash()
    {
        if (flashingDamage)
            return;
        StartCoroutine(DamageFlashCoRoutine());
        IEnumerator DamageFlashCoRoutine()
        {
            flashingDamage = true;
            Color defaultColor = mr.material.color;
            mr.material.color = Color.red;
            yield return new WaitForSeconds(0.05f);
            mr.material.color = defaultColor;
            flashingDamage = false;
        }
    }

    [PunRPC]
    void Die()
    {
        curHp = 0;
        dead = true;
        GameManager.instance.alivePlayers--;
        // host will check win condition
        if (PhotonNetwork.IsMasterClient)
            GameManager.instance.CheckWinCondition();
        // is this our local player?
        if (photonView.IsMine)
        {
            if (curAttackerId != 0)
                GameManager.instance.GetPlayer(curAttackerId).photonView.RPC("AddKill", RpcTarget.All);
            // set the cam to spectator
            GetComponentInChildren<CameraController>().SetAsSpectator();
            // disable the physics and hide the player
            //rig.isKinematic = true;
            transform.position = new Vector3(0, -50, 0);
        }
    }

    [PunRPC]
    public void AddKill()
    {
        kills++;
        GameUI.instance.UpdatePlayerInfoText();
    }

    [PunRPC]
    public void Heal(int amountToHeal)
    {
        curHp = Mathf.Clamp(curHp + amountToHeal, 0, maxHp);
        // update the health bar UI
        GameUI.instance.UpdateHealthBar();
    }
}

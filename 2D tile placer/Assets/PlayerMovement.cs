using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityStandardAssets.CrossPlatformInput;

public class PlayerMovement : MonoBehaviour
{
    public int playerID;

    [Header("General stats")]
    [SerializeField] private int gameLifes;
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float slideSpeed;
    [SerializeField] private int amountOfJumps = 2;
    [SerializeField] private int jumpCount = 0;

    //under the hood stats
    private float currentMovementSpeed;
    [SerializeField] private bool grounded;
    public bool isInAir = false;
    private float beginGravity;

    [Header("Collision")]
    public LayerMask groundLayer;
    public float collisionRadius = 0.25f;
    public Vector2 rightOffset, leftOffset, onGroundCirle;
    public Vector2 wallJumpClimb, wallJumpOff;
    [Space]
    public bool onWall;
    public bool onRightWall;
    public bool onLeftWall;
    public bool onGround;

    //GameObject stuff
    [Header("GameObject stuff")]
    private Rigidbody2D rb;
    private BoxCollider2D collider;
    private float hitDirectionX;
    private float hitDirectionY;

    //keyCodes
    [HideInInspector] public KeyCode baseAttackCode;
    [HideInInspector] public KeyCode specialAttackCode;
    private KeyCode jumpCode;
    private KeyCode startButton;

    //other
    private int wallDirX;
    public Vector2 input;
    private PlayerMovement characterThatHitYou;
    public float damageTaken;
    [SerializeField] private bool knockbackHit;
    [SerializeField] private float knockbackHitTimer;
    [SerializeField] private float damageTimeMultiplayer;
    private float currentKnockbackHitTimer;
    [SerializeField] private float damageMultiplier;

    [Space]
    [SerializeField] private bool wallJump;
    [SerializeField] private float wallJumpTimer;
    private float currentWallJumpTimer;

    //private AnimationFalse animFalse;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();
        beginGravity = rb.gravityScale;
        ConfigureControlButtons();

        currentKnockbackHitTimer = knockbackHitTimer;
        currentWallJumpTimer = wallJumpTimer;
    }

    // Update is called once per frame
    private void Update()
    {

        if (knockbackHit)
        {
            return;
        }

        if (wallJump)
        {
            return;
        }

        Jumping();
    }

    void FixedUpdate()
    {
        if (wallJump)
        {
            currentWallJumpTimer = Timer(currentWallJumpTimer);

            if (currentWallJumpTimer < 0)
            {
                wallJump = false;
                rb.isKinematic = false;
                currentWallJumpTimer = wallJumpTimer;
            }
            return;
        }

        onWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer)
            || Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        onRightWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer);
        onLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        onGround = Physics2D.OverlapCircle((Vector2)transform.position + onGroundCirle, collisionRadius, groundLayer);

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        wallDirX = onLeftWall ? -1 : 1;

        if (onLeftWall && input.x < -0.5f || onRightWall && input.x > 0.5f)
        {
            Ledge();
        }
        
        //Jumping();

        Moving();
        //FlipCharacter();
    }

    private float Timer(float timer)
    {
        timer -= Time.deltaTime;
        return timer;
    }

    private void Moving()
    {
        currentMovementSpeed = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
        if (input.x > 0.2f || input.x < -0.2f || input.y > 0.2f || input.y < -0.2f)
        {
            rb.velocity = new Vector2(currentMovementSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void Jumping()
    {
        
        if (Input.GetKeyDown(KeyCode.Space) && grounded && !onWall && onGround)
        {
            isInAir = true;
            jumpCount++;
            //rb.velocity = new Vector2(rb.velocity.x, jumpPower);

            Vector2 dir = Vector2.up;
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.velocity += dir * jumpPower;

            if (jumpCount >= amountOfJumps)
            {
                grounded = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space) && onWall)
        {
            wallJump = true;
            rb.isKinematic = true;
            if (wallDirX == input.x)
            {
                rb.velocity = new Vector2(-wallDirX * wallJumpClimb.x, wallJumpClimb.y);
            }
            else if (input.x == 0)
            {
                rb.velocity = new Vector2(-wallDirX * wallJumpOff.x, wallJumpOff.y);
            }
        }

        //fast falling
        if (Input.GetAxis("Vertical") > 0.9f && isInAir)
        {
            rb.gravityScale = fallSpeed;
        }
        else
        {
            rb.gravityScale = beginGravity;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "DeathZone")
        {
            Respawn();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            grounded = true;
            isInAir = false;
            jumpCount = 0;
        }
        else
        {
            isInAir = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            isInAir = true;
            jumpCount++;
        }
    }

    private void Ledge()
    {
        if(onLeftWall && currentMovementSpeed < -0.5f || onRightWall && currentMovementSpeed > 0.5f)
        {
            jumpCount = 0;
            rb.velocity = new Vector2(rb.velocity.x, -slideSpeed);
            Debug.Log("wall sliding");
        }
    }

    private void Respawn()
    {
        gameLifes--;
        if (gameLifes <= 0)
        {
            Destroy(this.gameObject);
        }
        damageTaken = 0;
        this.gameObject.transform.position = new Vector2(0, 0);
        Debug.Log(gameLifes);
    }

    private void FlipCharacter()
    {
        Vector3 characterScale = transform.localScale;

        if(currentMovementSpeed < -0.1f)
        {
            characterScale.x = -1;
        }
        else if (currentMovementSpeed > 0.1f)
        {
            characterScale.x = 1;
        }
        transform.localScale = characterScale;
    }

    public void ConfigureControlButtons()
    {
        //controller identification for the buttons
        switch (playerID)
        {
            case 1:
                baseAttackCode = KeyCode.Joystick1Button1;
                specialAttackCode = KeyCode.Joystick1Button2;
                jumpCode = KeyCode.Joystick1Button0;
                break;
            case 2:
                baseAttackCode = KeyCode.Joystick2Button1;
                specialAttackCode = KeyCode.Joystick2Button2;
                jumpCode = KeyCode.Joystick2Button0;
                break;
            case 3:
                baseAttackCode = KeyCode.Joystick3Button1;
                specialAttackCode = KeyCode.Joystick3Button2;
                jumpCode = KeyCode.Joystick3Button0;
                break;
            case 4:
                baseAttackCode = KeyCode.Joystick4Button1;
                specialAttackCode = KeyCode.Joystick4Button2;
                jumpCode = KeyCode.Joystick4Button0;
                break;
            default:
                baseAttackCode = KeyCode.C;
                jumpCode = KeyCode.Space;
                specialAttackCode = KeyCode.X;
                startButton = KeyCode.Escape;
                break;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        var positions = new Vector2[] { rightOffset, leftOffset };

        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + onGroundCirle, collisionRadius);
    }
}

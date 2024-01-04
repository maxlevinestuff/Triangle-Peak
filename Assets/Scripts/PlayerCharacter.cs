
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{

    [SerializeField]
    private float movementSpeed;
    [SerializeField]
    private float groundCheckRadius;
    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private float slopeCheckDistance;
    [SerializeField]
    private float maxSlopeAngle;
    [SerializeField]
    private Transform groundCheck;
    [SerializeField]
    private LayerMask whatIsGround;
    [SerializeField]
    private PhysicsMaterial2D noFriction;
    [SerializeField]
    private PhysicsMaterial2D fullFriction;

    private float xInput;
    private float yInput;
    private float slopeDownAngle;
    private float slopeSideAngle;
    private float lastSlopeAngle;

    private int facingDirection = 1;

    private bool isGrounded;
    private bool isOnSlope;
    private bool isJumping;
    private bool canWalkOnSlope;
    private bool canJump;

    private Vector2 newVelocity;
    private Vector2 newForce;
    private Vector2 capsuleColliderSize;

    private Vector2 slopeNormalPerp;

    private Rigidbody2D rb;
    private CapsuleCollider2D cc;

    NetworkHandler networkHandler;

    Question question;

    Vector3 startPos;

    private void Start()
    {
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();
        question = GameObject.Find("Question").GetComponent<Question>();

        startPos = transform.position;

        Physics2D.gravity = Vector2.zero;

        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();

        capsuleColliderSize = cc.size;
    }

    private void Update()
    {
        if (networkHandler.editSetting != NetworkHandler.EditSetting.book)
        {
            if (! question.freeMode)
            {
                CheckInput();
            } else
            {
                transform.position += new Vector3(Input.GetAxisRaw("Horizontal") / 10, Input.GetAxisRaw("Vertical") / 10, 0);
                rb.gravityScale = 0;
            }

        }
    }

    private void FixedUpdate()
    {
        if (networkHandler.editSetting != NetworkHandler.EditSetting.book)
        {
            if (! question.freeMode)
            {
                CheckGround();
                SlopeCheck();
                ApplyMovement();

                if (transform.position.y < -4.51f)
                    transform.position = startPos;
            }

        }
    }

    private void CheckInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (xInput == 1 && facingDirection == -1)
        {
            Flip();
        }
        else if (xInput == -1 && facingDirection == 1)
        {
            Flip();
        }

        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }

    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        if (rb.velocity.y <= 0.0f)
        {
            isJumping = false;
        }

        if (isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle)
        {
            canJump = true;
        }

    }

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position - (Vector3)(new Vector2(0.0f, capsuleColliderSize.y / 2));

        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, whatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, whatIsGround);

        if (slopeHitFront)
        {
            isOnSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);

        }
        else if (slopeHitBack)
        {
            isOnSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0.0f;
            isOnSlope = false;
        }

    }

    private void SlopeCheckVertical(Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, whatIsGround);

        if (hit)
        {

            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != lastSlopeAngle)
            {
                isOnSlope = true;
            }

            lastSlopeAngle = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);

        }

        if (slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }

        if (isOnSlope && canWalkOnSlope && xInput == 0.0f)
        {
            rb.sharedMaterial = fullFriction;
        }
        else
        {
            rb.sharedMaterial = noFriction;
        }
    }

    private void Jump()
    {
        if (canJump)
        {
            canJump = false;
            isJumping = true;
            newVelocity.Set(0.0f, 0.0f);
            rb.velocity = newVelocity;
            newForce.Set(0.0f, jumpForce);
            rb.AddForce(newForce, ForceMode2D.Impulse);
        }
    }

    public LayerMask whatIsLadder;

    bool justReleased = false;

    private void ApplyMovement()
    {
        RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.up, 1f, whatIsLadder); //still need to make these raycasts cast in direction of (nearest?) ladder, not player

        //RaycastHit2D hitInfoReachedTopOfLadder = Physics2D.Raycast(transform.position + new Vector3(0,1,0), Vector2.up, .5f, whatIsLadder);
        //if (hitInfoReachedTopOfLadder.collider == null && yInput == 1)
        //    yInput = 0;

        if (isGrounded && !isOnSlope && !isJumping) //if not on slope
        {
            Debug.Log("This one");
            newVelocity.Set(movementSpeed * xInput, 0.0f);
            rb.velocity = newVelocity;
        }
        else if (isGrounded && isOnSlope && canWalkOnSlope && !isJumping) //If on slope
        {
            newVelocity.Set(movementSpeed * slopeNormalPerp.x * -xInput, movementSpeed * slopeNormalPerp.y * -xInput);
            rb.velocity = newVelocity;
            Debug.Log("IN AIR");
        }
        else if (!isGrounded) //If in air
        {
            newVelocity.Set(movementSpeed * xInput, rb.velocity.y - .1f);
            rb.velocity = newVelocity;
            
        }

        if (hitInfo.collider != null)
        {
            if (yInput != 0) justReleased = false;

            Vector3 ladderDirection = hitInfo.transform.up * movementSpeed;
            Vector3 ladderAdd = new Vector3(0,0,0);
            if (yInput == 1)
                ladderAdd = new Vector3(ladderDirection.x, ladderDirection.y, 0);
            else if (yInput == -1)
                ladderAdd = new Vector3(-ladderDirection.x, -ladderDirection.y, 0);

            float x = rb.velocity.x + ladderAdd.x;
            float y = Mathf.Max(rb.velocity.y, 0f, ladderAdd.y);
            if (yInput == -1 || yInput == 1)
                y = ladderAdd.y;

            if (!justReleased && yInput == 0)
            {
                justReleased = true;
                y = 0;
            }

            newVelocity.Set(x, y);
            rb.velocity = newVelocity;
            //rb.gravityScale = 0;
        }
    }

    private void Flip()
    {
        //facingDirection *= -1;
        //transform.Rotate(0.0f, 180.0f, 0.0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

}
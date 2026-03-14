using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rigid;
    SpriteRenderer spr;
    Animator animator;
    public float speed;
    private float curTime;
    public float coolTime = 0.5f;
    public float jumppower;
    private bool isGrounded;
    private bool isAttacking = false;
    public float groundCheckDistance = 0.8f;
    public Transform feetPos;
    public float checkRadius;
    public LayerMask whatIsGround;
    public Transform pos;
    public Vector2 boxSize;
    private bool isDead = false;
    private bool wasGroundedLastFrame;
    bool isDamaged = false;
    bool isSliding = false;
    public float slideSpeed = 10f;
    public float slideDurantion = 1.5f;
    private float nextSllideTime;
    public float slideCooldown = 1f;
    public float maxMoveSpeed = 5f;
    float slideEndDelay = 0.1f;
    private float slideEndTime = 0f;
    
    // Start is called before the first frame update
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update()
    {
        if (isDead) return;
        Move();
        Attack();
        slide();
    }


    void slide ()
    {
        bool currentIsGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);
        
        if (Input.GetKeyDown(KeyCode.C) && currentIsGrounded && !isSliding && Time.time >= nextSllideTime)
        {
            StartCoroutine(StartSlide());
            nextSllideTime = Time.time + slideCooldown; // ���� �����̵� ���� �ð� ������Ʈ
        }
        
        IEnumerator StartSlide()
        {
            isSliding = true;
            animator.SetTrigger("slide");
            float sldieDirection = spr.flipX ? -1f : 1f;
            rigid.linearVelocity = new Vector2(sldieDirection * slideSpeed, rigid.linearVelocity.y);
            yield return new WaitForSeconds(slideDurantion);
            float h = Input.GetAxisRaw("Horizontal");
            float targetVelX = h * speed;

            if (Mathf.Abs(h) > 0)
                rigid.linearVelocity = new Vector2(targetVelX, rigid.linearVelocity.y);
            else
                rigid.linearVelocity = new Vector2(targetVelX * 0.8f, rigid.linearVelocity.y);

            isSliding = false;
            slideEndTime = Time.time + slideEndDelay;
        }
    }
    void Attack()
    {
        if (curTime <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                animator.SetTrigger("Attack");

                curTime = coolTime;

                isAttacking = true;
            }
        }
        else
        {
            curTime -= Time.deltaTime;
        }
    }
    public void ApplyPlayerDamage()
    {
        Debug.Log(" 플레이어가 데미지를 줍니다.");

        Collider2D[] collider2Ds = Physics2D.OverlapBoxAll(pos.position, boxSize, 0);

        foreach (Collider2D collider in collider2Ds)
        {
            if (collider.CompareTag("Enemy"))
            {
                monster1 enemyMonster = collider.GetComponent<monster1>();
                if(enemyMonster != null)
                {
                    enemyMonster.TakeDamage(1);
                }
            }
        }
    }
    public void OnAttackAnimationEnd()
    {
        Debug.Log("공격 애니메이션 종료!");
        isAttacking = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(pos.position, boxSize);
    }

    void Move()
    {
        if (Time.time < slideEndTime)
        {
            float slideDir = Mathf.Sign(rigid.linearVelocity.x);
            rigid.linearVelocity = new Vector2(slideDir * Mathf.Abs(rigid.linearVelocity.x), rigid.linearVelocity.y);
            return;
        }
        //1.��������Ȯ��
        bool currentIsGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);

        if(Input.GetKeyDown(KeyCode.Space) && currentIsGrounded)
        {
            rigid.AddForce(Vector2.up * jumppower, ForceMode2D.Impulse);
            animator.SetBool("isjumping", true);
        }


        else if (currentIsGrounded && !wasGroundedLastFrame) // - ����
        {
            animator.SetBool("isjumping", false);
            Debug.Log(" isjumping = false ");
        }

        wasGroundedLastFrame = currentIsGrounded;
        isGrounded = currentIsGrounded;

        if (isAttacking || isDamaged)
        {
            animator.SetBool("Run", false);
            rigid.linearVelocity = Vector2.zero; // ����/�ǰ� �߿� ����.
            return;
        }

        if (isSliding)
        {
            animator.SetBool("Run", true);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        Vector2 targetVelocity = new Vector2(h * speed, rigid.linearVelocity.y);
        rigid.linearVelocity = Vector2.Lerp(rigid.linearVelocity, targetVelocity, 0.1f);

        float currentVelX = rigid.linearVelocity.x;
        float targetVelX = h * speed;


        if (Mathf.Abs(currentVelX) > Mathf.Abs(targetVelX) && Mathf.Sign(currentVelX) == Mathf.Sign(targetVelX))
        {
            
            rigid.linearVelocity = new Vector2(currentVelX, rigid.linearVelocity.y);
        }
        else 
        {
            
            rigid.linearVelocity = new Vector2(targetVelX, rigid.linearVelocity.y);
        }


        // ���� sprite - flipx�� bool������ ����


        if (h != 0)
            spr.flipX = h == -1;
            // Animation walk
        if (Mathf.Abs(rigid.linearVelocity.x) < 0.1f) // ���� �� �����̸� idle
            animator.SetBool("Run", false);
        else
            animator.SetBool("Run", true);
        
    }
    void FixedUpdate()
    {
        if (isDead || isAttacking || isDamaged)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        //rigid.velocity = new Vector2(h * speed, rigid.velocity.y);

        if (rigid.linearVelocity.y < 0)
        {
            Debug.DrawRay(rigid.position, Vector3.down, new Color(0, 1, 0));
            RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, Vector3.down, 1, LayerMask.GetMask("Platform"));
            if (rayHit.collider != null && rayHit.distance < 0.5f)
            {
                isGrounded = true;
            }
        }
    }
    public void SetAttackingState(int state)
    {
        isAttacking = (state == 1);
    }

    public int hp = 10;
    public void TakeDamage(int damage, Vector2 attackerPos)
    {
        if (isDead) return;
        hp -= damage;

        if (hp <= 0)
        {
            hp = 0; // ������ ���� �ʵ��� 0���� ����
            isDead = true; // ���� ���·� ����
            Debug.Log("게임오버");
            animator.SetTrigger("playerdie");

            rigid.linearVelocity = Vector2.zero; // ������ ����
            rigid.gravityScale = 0f;
            // �ݶ��̴� ��Ȱ��ȭ
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            return;
        }
        if (!isDamaged)
        {
            OnDamaged(attackerPos);
        }
        else
        {
            return;
        }
    }

    void OnCollisionEnter2D (Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
            OnDamaged(collision.transform.position);
    }

    void OnDamaged(Vector2 targetPos)
    {
        isDamaged = true;

        rigid.linearVelocity = Vector2.zero;

        spr.color = new Color(1, 1, 1, 0.5f);

        int dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
        rigid.AddForce(new Vector2(dirc, 1) * 5, ForceMode2D.Impulse);

        animator.SetTrigger("hurt");
        Invoke("OffDamaged", 0.5f);
    }

    void OffDamaged()
    {
        isDamaged = false;
        gameObject.layer = 9;
        spr.color = new Color(1, 1, 1, 1);
    }
}
    



using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class monster1 : MonoBehaviour
{
    public float speed = 1.5f;          // 몬스터 이동 속도
    public float stopDistance = 3f;     // 플레이어에게 다가가다 멈출 최소 거리
    

    public float attackRange = 2f;      // 플레이어에게 근접 공격을 시도할 범위 (예: stopDistance보다 작게)
    public int meleeAttackDamage = 1; // 근접 공격 데미지 변수
    public float meleeAttackCooldownTime = 3.0f; // 근접 공격 후 다음 공격까지 기다릴 시간

    public Vector2 meleeAttackOffset = new Vector2(1.0f, 0f);
    public Vector2 meleeAttackBoxsize = new Vector2(1.5f, 1.0f);

    public float rangedAttackRange = 10f; // 원거리 공격을 시작할 범위
    public float castingTime = 1.0f;         // 마법 시전 시간
    public int rangedAttackDamage = 2;       // 원거리 공격 데미지
    public float rangedAttackCooldownTime = 2.0f; // 원거리 공격 후 다음 공격까지 기다릴 시간

    public GameObject lightningEffectPrefab; // 번개 이펙트 프리팹 (Inspector에서 할당!)
    public float effectDuration = 1.5f;       // 번개 이펙트 유지 시간
    public float damageDelay = 0.5f;          // 이펙트 시작 후 데미지가 들어갈까지의 딜레이
    public Vector2 damageBoxSize = new Vector2(1.5f, 1.5f);// 번개 데미지 범위
    public Vector2 damageBoxOffset = new Vector2(0f, -0.5f);
    public float lightingOffsetY = 1.0f;

    // === 몬스터 체력 관련 ===
    
    [SerializeField] public int _maxHealth = 5; // 최대 체력 (인스펙터에서 조절)
    public int MaxHealth { get { return _maxHealth; } }
    private int _currentHealth; // 현재 체력
    public int CurrentHealth
    {
        get { return _currentHealth; }
        private set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 체력이 0 미만이거나 MaxHealth 초과하지 않도록 Clamp!
            if (_currentHealth <= 0)
            {
                Debug.Log("몬스터 처치!");
                animator.SetTrigger("die"); // 죽음 애니메이션 트리거
                spriteRenderer.color = new Color(1, 1, 1, 1f); // 반투명하게
                Destroy(gameObject, 1.8f); // 0.5초 뒤 오브젝트 파괴
            }
        }
    }

    // === 내부 상태 변수들 ===
    private bool _isCasting = false;         // 현재 마법 시전 중인지 여부 (이름 변경!)
    private float _currentCastingTime = 0f;  // 현재 시전 시간 측정용
    private float _currentRangedAttackCooldown = 0f; // 현재 원거리 공격 쿨타임 (이름 변경!)
    private float _currentMeleeAttackCooldown = 0f; // 현재 근접 공격 쿨타임 (이름 변경!)

    // === 내부 참조용 컴포넌트들 ===
    private Transform player;           // 플레이어 Transform
    private Rigidbody2D rb;             // 몬스터 Rigidbody2D
    private Animator animator;          // 몬스터 Animator
    private SpriteRenderer spriteRenderer; // 몬스터 SpriteRenderer

    // === 초기화 함수 ===
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // 자식에 있다면 InChildren, 본인에게 있다면 GetComponent

        CurrentHealth = MaxHealth; // 몬스터 생성 시 현재 체력을 최대 체력으로 설정
    }

    // === 매 프레임 실행되는 AI 로직 ===
    void Update()
    {
        // 1. 플레이어 찾기 (없으면 행동 중지)
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                animator.SetBool("isMoving", false);
                return; // 플레이어를 찾을 수 없으면 아무것도 하지 않고 함수 종료
            }
        }

        // 2. 쿨타임 관리 (플레이어 유무 확인 후, 다른 행동 이전에 먼저 처리)
        if (_currentMeleeAttackCooldown > 0)
        {
            _currentMeleeAttackCooldown -= Time.deltaTime;
        }
        if (_currentRangedAttackCooldown > 0)
        {
            _currentRangedAttackCooldown -= Time.deltaTime;
        }

        // 3. 몬스터 AI 행동 결정! (우선순위 기반!)
        // ⭐️ 가장 먼저 마법 시전 중인지 체크! (다른 모든 행동보다 우선)
        if (_isCasting)
        {
            rb.linearVelocity = Vector2.zero; // 시전 중에는 몬스터 이동 멈춤
            _currentCastingTime -= Time.deltaTime;
            if (_currentCastingTime <= 0)
            {
                _isCasting = false; // 시전 끝!
                CastLightning();    // 마법 발사 함수 호출!
            }
            // animator.SetBool("isCasting", true); // 캐스팅 애니메이션 유지 (Trigger를 사용하면 한 번만 호출됨)
            LookAtPlayer(player); // 시전 중에도 플레이어를 바라보게 함
            animator.SetBool("isMoving", false); // 시전 중에는 움직임 애니메이션 꺼야 함!
            return; // 시전 중이면 다른 모든 행동 로직 스킵!
        }

        // 시전 중이 아니라면 다른 행동을 결정!
        LookAtPlayer(player); // 몬스터가 플레이어를 바라보게 함 (행동 결정 전에 미리)
        float distanceToPlayer = Vector2.Distance(transform.position, player.position); // 플레이어와의 거리 계산

        bool isCurrentlyMoving = false; // 현재 몬스터가 이동해야 하는지 여부 (이번 프레임 행동에 따라 결정)

        // ⭐️⭐️ AI 행동 결정 트리 (거리에 따라 우선순위!) ⭐️⭐️
        // 1순위: 플레이어가 원거리 공격 사거리 안에 있고 (stopDistance보다 멀고), 쿨타임이 돌았으면 원거리 마법 공격 시도!
        if (distanceToPlayer > stopDistance && distanceToPlayer <= rangedAttackRange && CanRangedAttack())
        {
            _isCasting = true; // 시전 시작!
            _currentCastingTime = castingTime; // 시전 시간 설정
            rb.linearVelocity = Vector2.zero; // 시전 중에는 이동 멈춤
            animator.SetTrigger("casting"); // 캐스팅 애니메이션 트리거! (애니메이터에 'casting' Trigger 파라미터 필요!)
            // LookAtPlayer(player); // 이미 위에서 바라보게 함

            // isCurrentlyMoving은 기본값인 false 유지 (움직이지 않음)
        }
        // 2순위: 원거리 공격을 못 했고, 플레이어가 근접 공격 사거리 안에 있고 쿨타임 돌았으면 근접 공격 시도!
        else if (distanceToPlayer <= attackRange && CanMeleeAttack()) // CanMeleeAttack으로 이름 변경!
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetTrigger("attack"); // 근접 공격 애니메이션 트리거
            StartMeleeAttackCooldown(); // StartMeleeAttackCooldown으로 이름 변경!
           
        }
        // 3순위: 둘 다 공격할 수 없고, 플레이어가 stopDistance 밖에 있으면 추격 이동!
        else if (distanceToPlayer > stopDistance)
        {
            float directionX = Mathf.Sign(player.position.x - rb.position.x);
            rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);

            isCurrentlyMoving = true; // 이동 중으로 설정
        }
        // 4순위: 모든 조건에 해당 안 하면 멈춤! (stopDistance 안에 있지만 공격 쿨타임이 안 돈 경우 등)
        else
        {
            rb.linearVelocity = Vector2.zero;
            // isCurrentlyMoving은 기본값인 false 유지 (멈춤)
        }

        // 최종적으로 결정된 isCurrentlyMoving 상태를 애니메이터에 전달
        animator.SetBool("isMoving", isCurrentlyMoving);
    } // End of Update() function! (이 }가 이전 Update 끝 부분이야!)

    public void DealMeleeDamage()
    {
        Vector2 currentMeleeAttackOffset = meleeAttackOffset;
        if (spriteRenderer.flipX)
        {
            currentMeleeAttackOffset.x *= -1;
        }

        Vector2 attackBoxCenter = (Vector2)transform.position + currentMeleeAttackOffset;

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(attackBoxCenter, meleeAttackBoxsize, 0f, LayerMask.GetMask("Player"));
        foreach ( Collider2D hitCollider in hitColliders)
        {
            Player playerComponent = hitCollider.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.TakeDamage(meleeAttackDamage, transform.position);
                Debug.Log("플레이어에게 근접 공격 데미지! 데미지: " + meleeAttackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Vector2 currentMeleeAttackOffset = meleeAttackOffset;
            if ( spriteRenderer != null && spriteRenderer.flipX)
            {
                currentMeleeAttackOffset.x *= -1;
            }
            Vector2 attackBoxCenter = (Vector2)transform.position + currentMeleeAttackOffset;
            Gizmos.DrawWireCube(attackBoxCenter, meleeAttackBoxsize);
        }
    }
    

    // === 원거리 공격 가능 여부 체크 함수 ===
    public bool CanRangedAttack()
    {
        return _currentRangedAttackCooldown <= 0;
    }

    // === 근접 공격 가능 여부 체크 함수 === (이름 변경!)
    public bool CanMeleeAttack()
    {
        return _currentMeleeAttackCooldown <= 0;
    }

    // === 근접 공격 쿨타임 시작 함수 === (이름 변경!)
    public void StartMeleeAttackCooldown()
    {
        _currentMeleeAttackCooldown = meleeAttackCooldownTime;
    }

    // === 플레이어 방향 바라보기 함수 ===
    public void LookAtPlayer(Transform playerTransform)
    {
        if (playerTransform == null || spriteRenderer == null) return;
        spriteRenderer.flipX = (playerTransform.position.x > transform.position.x);
    }

    // === 마법 공격 발사/처리 함수 ===
    public void CastLightning() // ⭐️ 애니메이션 이벤트로 호출할 함수!
    {
        //if (lightningEffectPrefab == null)
        //{
        //    Debug.LogError("Lightning Effect Prefab is not assigned for monster1!");
        //    return;
        //}
        Vector3 targetPosition = player.position + Vector3.up * lightingOffsetY; // 플레이어의 현재 위치를 타겟

        GameObject effectGO = Instantiate(lightningEffectPrefab, targetPosition, Quaternion.identity);

        LightningEffect lightningEffect = effectGO.GetComponent<LightningEffect>();
        if (lightningEffect != null)
        {
            lightningEffect.damageBoxsize = this.damageBoxSize;
            lightningEffect.lifeTime = this.effectDuration;
            lightningEffect.damageBoxOffset = this.damageBoxOffset;
        }
        
        // 콜라이더 중심 별도 계산
        Vector3 colliderCenter = targetPosition + new Vector3(damageBoxOffset.x, damageBoxOffset.y, 0f);


        StartCoroutine(ApplyLightningDamageAfterDelay(colliderCenter, rangedAttackDamage, damageBoxSize, damageDelay));
        _currentRangedAttackCooldown = rangedAttackCooldownTime; // 원거리 공격 쿨타임 시작
    }

    // === 번개 데미지를 딜레이 후에 적용하는 코루틴 ===
    IEnumerator ApplyLightningDamageAfterDelay(Vector3 center, int damage, Vector2 boxSize, float delay)
    {
        yield return new WaitForSeconds(delay);

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(center, boxSize, 0f, LayerMask.GetMask("Player")); // 0f (회전) 추가
        foreach (Collider2D hitCollider in hitColliders) // 변수명 통일
        {
            Player playerComponent = hitCollider.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.TakeDamage(damage, center);
                Debug.Log("플레이어가 번개 공격을 받음! 데미지: " + damage + " (지점: " + center + ")");
            }
        }
    }
    // === 데미지 받는 함수 ===
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage; // CurrentHealth 속성을 통해 체력 감소
        Debug.Log(gameObject.name + " 데미지 입음: " + damage + ", 남은 체력: " + CurrentHealth);
    }
}




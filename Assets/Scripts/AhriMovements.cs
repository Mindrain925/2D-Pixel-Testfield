using System.Collections;
using UnityEngine;

public class AhriMovements : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f; // 대시 거리 (dashRange)
    [SerializeField] private float dashTime = 0.2f;   // 대시 소요 시간
    [SerializeField] private float dashCooldown = 1f; // 대시 쿨타임 (옵션)

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.1f;

    // State Variables
    private bool _isGrounded;
    private bool _isDashing;
    private bool _canDash = true;

    // Components
    private Rigidbody2D _rb;
    private SpriteRenderer _spr;

    // Input Variables
    private float _horizontalInput;
    private bool _jumpInput;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 1. 사용자 입력은 Update에서 받습니다 (반응성 보장)
        ProcessInputs();

        // 2. 스프라이트 방향 전환 (시각적 요소)
        FlipSprite();
    }

    void FixedUpdate()
    {
        // 3. 물리 연산은 FixedUpdate에서 처리합니다
        CheckGround();
        
        if (!_isDashing) // 대시 중이 아닐 때만 일반 이동/점프 처리
        {
            Move();
            Jump();
        }
    }

    private void ProcessInputs()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _jumpInput = true;
        }

        if (Input.GetKeyDown(KeyCode.Q) && _canDash && !_isDashing)
        {
            // 방향키 입력이 없으면 바라보는 방향으로, 있으면 입력 방향으로 대시
            float dashDirection = _horizontalInput == 0 ? (_spr.flipX ? -1 : 1) : _horizontalInput;
            StartCoroutine(DashRoutine(dashDirection));
        }
    }

    private void CheckGround()
    {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Move()
    {
        // Unity 6 사용 시: _rb.linearVelocity = ...
        _rb.linearVelocity = new Vector2(_horizontalInput * walkSpeed, _rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (_jumpInput)
        {
            // ForceMode2D.Impulse는 순간적인 힘을 가할 때 적합
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _jumpInput = false; // 점프 처리 후 플래그 초기화
        }
    }

    private void FlipSprite()
    {
        if (!_isDashing && _horizontalInput != 0) // 대시 중에는 방향 전환 방지
        {
            if (_horizontalInput > 0) _spr.flipX = false;
            else if (_horizontalInput < 0) _spr.flipX = true;
        }
    }

    // 대시 로직을 코루틴으로 분리하여 가독성과 제어력을 높임
    private IEnumerator DashRoutine(float direction)
    {
        _isDashing = true;
        _canDash = false;

        // 대시 초기화
        // 대시 중 중력 영향 무시 (직선 대시를 위해)
        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero; // 기존 가속도 제거

        // 정밀 계산을 위한 변수 설정
        // 속도 = 거리 / 시간
        float dashSpeed = dashDistance / dashTime;
        float currentMovedDistance = 0f;

        // 이동 루프 (거리 기반)
        while (currentMovedDistance < dashDistance)
        {
            // 이번 프레임 이동량 계산
            float moveStep = dashSpeed * Time.fixedDeltaTime;

            // 거리 보정 (남은 거리가 이번 이동량보다 작으면 남은 만큼만 이동)
            if (currentMovedDistance + moveStep > dashDistance)
            {
                moveStep = dashDistance - currentMovedDistance;
            }

            // Rigidbody 이동 (Physics와 충돌 처리를 유지하며 이동)
            Vector2 nextPosition = _rb.position + new Vector2(direction * moveStep, 0);
            _rb.MovePosition(nextPosition);

            // 누적 거리 갱신
            currentMovedDistance += moveStep;

            // 물리 업데이트 대기 (WaitForSeconds 대신 사용해야 MovePosition과 동기화됨)
            yield return new WaitForFixedUpdate();
        }

        // 대시 종료 정리
        _rb.gravityScale = originalGravity;
        _rb.linearVelocity = Vector2.zero;
        _isDashing = false;

        // 쿨타임
        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }
    
    // 에디터에서 GroundCheck 범위를 시각적으로 확인하기 위함
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
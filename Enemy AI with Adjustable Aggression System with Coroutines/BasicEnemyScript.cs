using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BasicEnemyScript : MonoBehaviour
{
    private Animator _anim;
    private GameObject _animBody;
    private NavMeshAgent _agent;
    [SerializeField] private GameObject _player;

    [SerializeField] private bool _idleMode;
    [SerializeField] private bool _patrolMode;
    [SerializeField] private Vector3 _patrolPoint;
    [SerializeField] private float _patrolMinWaitTime;
    [SerializeField] private float _patrolMaxWaitTime;

    [Space(10)]

    [SerializeField] private float _health;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _poise;
    [SerializeField] private float _poiseRecovery;

    [Space(10)]

    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _turnSpeed;
    [SerializeField] private float _sprintSpeed;
    [SerializeField] private float _strafeSpeed;
    [SerializeField] private float _patrolSpeed;
    [SerializeField] private float _attackStepSpeed;
    [SerializeField] private float _attackTurnSpeed;

    [Space(10)]

    [SerializeField] private int _damageTolerance;
    [SerializeField] private int _attackStreakLimit;
    [Range(0f, 1f)]
    [SerializeField] private float _detectionAgression;
    [SerializeField] private float _minWaitTime;
    [SerializeField] private float _maxWaitTime;
    [SerializeField] private float _staggerBlockTime;
    [SerializeField] private float _attackRange;
    [SerializeField] private float _resetRange;
    [Range(0f, 1f)]
    [SerializeField] private float _facingAngleDot;

    [Space(10)]

    [Range(0f, 1f)]
    [SerializeField] private float _heavyChance;
    [Range(0f, 0.1f)]
    [SerializeField] private float _heavyChanceModifier;

    private bool _combatMode = false;   
    private bool _strafeState = false;
    private bool _gapCloseState = false;
    private bool _resetState = false;
    private bool _staggerState = false;
    private bool _guardDownState = false;
    private bool _deathState = false;
    private bool _staggerBlock = false;

    private bool _guardDownEnded = false;
    private bool _playerDetected = false;
    private bool _playerOutOfRange = false;
    private bool _inAttackRange = false;
    private bool _isAttacking = false;
    private bool _poiseDamaged = false;
    private bool _startToDesignated = true;
    private bool _waitAndTurn = false;

    private int _currentDamageCount = 0;
    private int _currentAttackCount = 0;
    private float _currentAttackStepSpeed;
    private float _currentTurnSpeed;
    private float _playerDamage = 20f;
    private float _playerPoiseDamage = 35f;
    private float _currentHeavyChance;
    private float _waitTimer = 0f;
    private float _currentHealth;
    private float _currentPoise;

    private int Attack1;
    private int Attack2;
    private int Attack3;
    private int Attack4;
    private int HeavyAttack;
    private int Stagger;
    private int GuardDown;
    private int Death;

    private Vector3 _strafeDirection;
    private Vector3 _startPoint;


    void Start()
    {
        _animBody = transform.Find("EnemyBody").gameObject;
        _anim = _animBody.GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;

        _currentHeavyChance = _heavyChance;
        _currentTurnSpeed = _turnSpeed;
        _startPoint = transform.position;
        _currentHealth = _health;
        _currentPoise = _poise;

        if (_patrolMode)
        {
            _agent.SetDestination(_patrolPoint);
            StartCoroutine(PatrolRoutine());
        }

        Attack1 = Animator.StringToHash("Attack1");
        Attack2 = Animator.StringToHash("Attack2");
        Attack3 = Animator.StringToHash("Attack3");
        Attack4 = Animator.StringToHash("Attack4");
        HeavyAttack = Animator.StringToHash("HeavyAttack");
        Stagger = Animator.StringToHash("Stagger");
        GuardDown = Animator.StringToHash("GuardDown");
        Death = Animator.StringToHash("Death");
    }

    void Update()
    {
        TurnHandler();
        MovementHandler();
        RoutineHandler();

        if (Input.GetKeyDown(KeyCode.M))
        {
            DetectionHandler();
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (_patrolMode)
        {
            float waitAndTurnTimer = 0f;
            Vector3 facedDirection = transform.forward;
            facedDirection.z *= -1;
            facedDirection.x *= -1;

            while (_waitAndTurn)
            {
                waitAndTurnTimer += Time.deltaTime;

                if (waitAndTurnTimer >= _patrolMinWaitTime)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facedDirection), Time.deltaTime * _turnSpeed);
                }

                if (waitAndTurnTimer >= _patrolMaxWaitTime)
                {
                    _waitAndTurn = false;

                    if (_startToDesignated)
                    {
                        _agent.SetDestination(_patrolPoint);
                    }
                    else
                    {
                        _agent.SetDestination(_startPoint);
                    }
                }

                yield return null;
            }

            if (_startToDesignated)
            {
                if ((_patrolPoint - transform.position).sqrMagnitude < 1f)
                {
                    _waitAndTurn = true;
                    _startToDesignated = false;
                    Debug.Log("Arrived at designated point");
                }
            }
            else
            {
                if ((_startPoint - transform.position).sqrMagnitude < 1f)
                {
                    _waitAndTurn = true;
                    _startToDesignated = true;
                    Debug.Log("Arrived at starting point");
                }
            }

            yield return null;
        }
    }

    private IEnumerator CombatRoutine()
    {
        _agent.ResetPath();

        if ((_player.transform.position - transform.position).sqrMagnitude < _attackRange)
        {
            _inAttackRange = true;
        }
        else
        {
            if (Random.value <= _detectionAgression)
            {
                _gapCloseState = true;
            }
            else
            {
                _strafeState = true;
            }
        }

        while (_combatMode)
        {
            StrafeRandomizer();

            while (_strafeState && !_staggerState)
            {
                float playerDirectionSqrMag = (_player.transform.position - transform.position).sqrMagnitude;
                float dotProduct = Vector3.Dot(transform.forward.normalized, (_player.transform.position - transform.position).normalized);

                if (dotProduct >= _facingAngleDot)
                {
                    _agent.Move(Time.deltaTime * _strafeSpeed * transform.TransformDirection(_strafeDirection).normalized);
                }

                _waitTimer += Time.deltaTime;

                if (_waitTimer >= _maxWaitTime)
                {
                    Debug.Log("Strafe Ended, closing the gap");
                    _waitTimer = 0f;
                    _gapCloseState = true;
                    _strafeState = false;
                }
                else if (_waitTimer >= _minWaitTime && playerDirectionSqrMag < _attackRange)
                {
                    Debug.Log("Player in attack range, interrupting the wait time");
                    _waitTimer = 0f;
                    _inAttackRange = true;
                    _strafeState = false;
                }

                yield return null;
            }

            while (_gapCloseState && !_staggerState)
            {
                _agent.Move(Time.deltaTime * _sprintSpeed * (_player.transform.position - transform.position).normalized);

                if ((_player.transform.position - transform.position).sqrMagnitude < _attackRange)
                {
                    _gapCloseState = false;
                    _inAttackRange = true;
                }

                if ((_player.transform.position - transform.position).sqrMagnitude >= _resetRange)
                {
                    _gapCloseState = false;
                    _playerOutOfRange = true;
                }

                yield return null;
            }

            while (_inAttackRange)
            {
                _currentAttackCount++;
                _isAttacking = true;
                
                AttackLogic();

                while (_isAttacking)
                {
                    if (_staggerState)
                    {
                        _isAttacking = false;

                        while (_staggerState)
                        {
                            yield return null;
                        }
                    }
                    yield return null;
                }

                if (_currentAttackCount < _attackStreakLimit)
                {
                    if ((_player.transform.position - transform.position).sqrMagnitude < _attackRange)
                    {
                        Debug.Log("In attack range to attack again");
                    }
                    else
                    {
                        Debug.Log("Out of attack range, gap closing to attack again");

                        _isAttacking = false;
                        _inAttackRange = false;
                        _gapCloseState = true;
                    }
                }
                else
                {
                    _currentAttackCount = 0;
                    _inAttackRange = false;
                    _strafeState = true;
                }

                yield return null;
            }

            yield return null;
        }
    }

    private IEnumerator StaggerBlockRoutine()
    {
        Debug.Log("Stagger Block Started");

        yield return new WaitForSeconds(_staggerBlockTime);

        Debug.Log("Stagger Block Ended");
        _staggerBlock = false;
    }

    private IEnumerator PoiseRoutine()
    {
        _currentPoise = _poise;

        while (_combatMode)
        {
            while (_poiseDamaged)
            {
                _poiseDamaged = false;

                yield return new WaitForSeconds(3);
            }

            _currentPoise += _poiseRecovery;

            if (_currentPoise > _poise)
            {
                _currentPoise = _poise;
            }

            yield return new WaitForSeconds(3);

            yield return null;
        }
    }

    private IEnumerator ResetRoutine()
    {
        _agent.SetDestination(_startPoint);

        _strafeState = false;
        _gapCloseState = false;
        _inAttackRange = false;

        _currentAttackCount = 0;
        _waitTimer = 0;


        while (_resetState)
        {
            if ((_startPoint - transform.position).sqrMagnitude < 1f)
            {
                _resetState = false;

                if (_patrolMode)
                {
                    _startToDesignated = true;
                    StartCoroutine(PatrolRoutine());
                }               
            }

            yield return null;
        }
    }

    private void RoutineHandler()
    {
        if (!_deathState)
        {

            if (_playerDetected)
            {
                _resetState = false;
                _combatMode = true;
                _playerDetected = false;

                Debug.Log("Player detected, combat initiated");

                StopAllCoroutines();
                InitiateCombatRoutines();
            }

            if (_combatMode && _playerOutOfRange)
            {
                _resetState = true;
                _combatMode = false;
                _playerOutOfRange = false;

                Debug.Log("Player out of range, combat ended");

                StopAllCoroutines();
                StartCoroutine(ResetRoutine());
            }

            if (_guardDownEnded)
            {
                _guardDownEnded = false;

                Debug.Log("Guard Down Ended");

                InitiateCombatRoutines();
            }
        }
        else
        {
            DeathHandler();
        }
    }

    private void InitiateCombatRoutines()
    {
        _strafeState = false;
        _gapCloseState = false;
        _inAttackRange = false;

        StartCoroutine(CombatRoutine());
        StartCoroutine(PoiseRoutine());
    }

    private void AttackLogic()
    {
        if (Random.value >= _currentHeavyChance)
        {
            if (_currentAttackCount % 2 == 0)
            {
                if (Random.value <= 0.5f)
                {
                    AttackSelection(1);
                }
                else
                {
                    AttackSelection(2);
                }
            }
            else
            {
                if (Random.value <= 0.5f)
                {
                    AttackSelection(3);
                }
                else
                {
                    AttackSelection(4);
                }
            }

            Debug.Log("BasicAttack");

            _currentHeavyChance += _heavyChanceModifier;
        }
        else
        {
            Debug.Log("HeavyAttack");

            _staggerBlock = true;

            _currentAttackCount = _attackStreakLimit;
            _currentHeavyChance = _heavyChance;
            AttackSelection(0);
        }
    }

    private void AttackSelection(int num)
    {
        switch (num)
        {
            case 0:
                _anim.CrossFade(HeavyAttack, 0.15f, 0);
                break;
            case 1:
                _anim.CrossFade(Attack1, 0.15f, 0); 
                break;
            case 2:
                _anim.CrossFade(Attack2, 0.15f, 0); 
                break;
            case 3:
                _anim.CrossFade(Attack3, 0.15f, 0);
                break;
            case 4:
                _anim.CrossFade(Attack4, 0.15f, 0);
                break;
        }
    }

    private void MovementHandler()
    {
        if (_combatMode && _isAttacking)
        {
            _agent.Move(Time.deltaTime * _currentAttackStepSpeed * transform.forward);
        }
    }

    private void TurnHandler()
    {
        if (_combatMode)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_player.transform.position - transform.position), Time.deltaTime * _currentTurnSpeed);
        }
    }

    private void DeathHandler()
    {
        _combatMode = false;
        _playerDetected = false;

        StopAllCoroutines();

        _anim.CrossFade(Death, 0.15f, 0);
        Debug.Log("Died");

        this.enabled = false;
    }

    private void StrafeRandomizer()
    {
        float directionX = Random.Range(-1f, 1f);

        _strafeDirection = new(directionX, 0, 0);
    }

    private void DamageHandler()
    {
        _currentHealth -= _playerDamage;

        if (!_guardDownState)
        {
            _currentPoise -= _playerPoiseDamage;
        }

        _poiseDamaged = true;

        if (_currentDamageCount >= _damageTolerance && _currentHealth > 0f && _currentPoise > 0f)
        {
            Debug.Log("Damage Tolerance Exceeded");

            _currentDamageCount = 0;
            _staggerBlock = true;

            if (_strafeState)
            {
                _strafeState = false;
                _gapCloseState = true;

                _currentHeavyChance = 1f;
            }

            StartCoroutine(StaggerBlockRoutine());
        }

        if (_currentPoise <= 0f && _currentHealth > 0f)
        {
            _anim.CrossFade(GuardDown, 0.15f, 0);

            _currentPoise = _poise;

            _staggerState = false;
            _staggerBlock = false;
            _isAttacking = false;

            _guardDownState = true;

            _currentTurnSpeed = 0f;
            _currentAttackStepSpeed = 0f;

            Debug.Log("Guard Down");;

            StopAllCoroutines();
        }

        if (_currentHealth <= 0f)
        {
            _deathState = true;
        }
    }

    public void StaggerEnd()
    {
        _currentTurnSpeed = _turnSpeed;
        _currentAttackStepSpeed = _attackStepSpeed;

        if (_guardDownState)
        {
            GuardDownHandler();
        }

        _staggerState = false;
    }

    public void AttackEnd()
    {
        _currentTurnSpeed = _turnSpeed;
        _currentAttackStepSpeed = _attackStepSpeed;

        _staggerBlock = false;
        _isAttacking = false;
    }

    public void GuardDownHandler()
    {
        _currentTurnSpeed = _turnSpeed;
        _currentAttackStepSpeed = _attackStepSpeed;

        _guardDownState = false;
        _guardDownEnded = true;
    }

    public void TurnSpeedHandler(int num)
    {
        switch(num)
        {
            case 0:
                _currentTurnSpeed = 0f;
                break;
            case 1:
                _currentTurnSpeed = _turnSpeed / 2f;
                break;
            case 2:
                _currentTurnSpeed = _attackTurnSpeed;
                break;
        }
    }

    public void AttackStepSpeedHandler(int num)
    {
        switch (num)
        {
            case 0:
                _currentAttackStepSpeed = 0f;
                break;
            case 1:
                _currentAttackStepSpeed = _attackStepSpeed / 3f;
                break;
            case 2:
                _currentAttackStepSpeed = _attackStepSpeed;
                break;
        }
    }

    public void Damaged()
    {
        if (!_combatMode) 
        {
            _playerDetected = true;
        }

        if (!_staggerBlock)
        {
            _currentTurnSpeed = 0f;
            _currentAttackStepSpeed = 0f;

            _staggerState = true;

            _currentDamageCount++;

            _anim.CrossFade(Stagger, 0.15f, 0);
        }

        DamageHandler();
    }

    public void DetectionHandler()
    {
        if (!_combatMode)
        {
            _playerDetected = true;
        }
    }
}
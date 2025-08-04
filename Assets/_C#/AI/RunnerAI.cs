using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityStandardAssets.Characters.ThirdPerson;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class RunnerAI : MonoBehaviour
{
    public NavMeshAgent agent;
    [Range(0, 100)] public float speed;
    [Range(0, 100)] public float walkRadius;
    public float unfreezeRange = 3f;
    public float evasionDistance = 5f;
    public float powerupCooldown = 5f;
    public float minDistanceForSpeedBoost = 8f;
    public float minDistanceForInvisibility = 10f;
    public float minDistanceForClone = 12f;

    private ThirdPersonCharacter _character;
    private FieldOfView _fov;
    private PowerUpHolder _powerUpHolder;
    private bool _temp = false;
    private bool _startRun = false;
    private float _lastPowerupTime = 0f;
    private float _lastTargetUpdateTime = 0f;
    private float _targetUpdateInterval = 1f;
    private Vector3 _lastSafePosition;
    private float _stuckTime = 0f;
    private float _maxStuckTime = 3f;

    private void OnEnable()
    {
        Actions.GameStart += GameStart;
    }

    private void OnDisable()
    {
        Actions.GameStart -= GameStart;
    }

    public void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        _character = this.GetComponent<ThirdPersonCharacter>();
        _fov = this.GetComponent<FieldOfView>();
        _powerUpHolder = this.GetComponent<PowerUpHolder>();
        _lastSafePosition = transform.position;
    }

    private void GameStart()
    {
        if (!agent) return;
        agent.SetDestination(RandomNavMeshLocation());
        _startRun = true;
    }

    private void Update()
    {
        if (!_startRun) return;

        // Check if stuck
        if (Vector3.Distance(transform.position, _lastSafePosition) < 0.1f)
        {
            _stuckTime += Time.deltaTime;
            if (_stuckTime >= _maxStuckTime)
            {
                agent.SetDestination(RandomNavMeshLocation());
                _stuckTime = 0f;
            }
        }
        else
        {
            _stuckTime = 0f;
            _lastSafePosition = transform.position;
        }

        // Update target periodically
        if (Time.time - _lastTargetUpdateTime >= _targetUpdateInterval)
        {
            UpdateBehavior();
            _lastTargetUpdateTime = Time.time;
        }

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            _character.Move(agent.desiredVelocity, false, false);
        }
        else
        {
            agent.SetDestination(RandomNavMeshLocation());
            _character.Move(Vector3.zero, false, false);
        }
    }

    private void UpdateBehavior()
    {
        if (_temp == false)
        {
            // Find all frozen teammates (not self)
            GameObject[] allRunners = GameObject.FindGameObjectsWithTag("Runner");
            bool foundFrozen = false;
            foreach (var runner in allRunners)
            {
                if (runner != this.gameObject && runner.layer == LayerMask.NameToLayer("Freeze"))
                {
                    foundFrozen = true;
                    agent.SetDestination(runner.transform.position);
                    if (Vector3.Distance(transform.position, runner.transform.position) < unfreezeRange)
                    {
                        // Unfreeze logic (same as before)
                        runner.layer = LayerMask.NameToLayer("Runner");
                        if (runner.TryGetComponent<NavMeshAgent>(out _))
                        {
                            runner.GetComponent<RunnerAI>().enabled = true;
                            runner.GetComponent<NavMeshAgent>().enabled = true;
                            runner.GetComponent<ThirdPersonCharacter>().enabled = true;
                        }
                        else
                        {
                            runner.GetComponent<PlayerInput>().enabled = true;
                            runner.GetComponent<ThirdPersonController>().enabled = true;
                            runner.GetComponent<Attack>().enabled = true;
                        }
                        runner.GetComponent<Animator>().enabled = true;
                        runner.GetComponent<FieldOfView>().enabled = true;
                    }
                }
            }
            if (!foundFrozen)
            {
                // Wander if no frozen teammates
                agent.SetDestination(RandomNavMeshLocation());
            }
            _temp = true;
            StartCoroutine(Delay());
        }
    }

    private Vector3 RandomNavMeshLocation()
    {
        var finalPosition = Vector3.zero;
        var randomPosition = Random.insideUnitSphere * walkRadius;
        randomPosition += transform.position;
        if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, walkRadius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }

    private void UnfreezeRunner()
    {
        var _target = _fov.freezeRunners[0];

        _target.gameObject.layer = LayerMask.NameToLayer("Runner");

        if (_target.TryGetComponent<NavMeshAgent>(out _))
        {
            _target.GetComponent<RunnerAI>().enabled = true;
            _target.GetComponent<NavMeshAgent>().enabled = true;
            _target.GetComponent<ThirdPersonCharacter>().enabled = true;
        }
        else
        {
            _target.GetComponent<PlayerInput>().enabled = true;
            _target.GetComponent<ThirdPersonController>().enabled = true;
            _target.GetComponent<Attack>().enabled = true;
        }

        _target.GetComponent<Animator>().enabled = true;
        _target.GetComponent<FieldOfView>().enabled = true;
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);
        _temp = false;
    }
}

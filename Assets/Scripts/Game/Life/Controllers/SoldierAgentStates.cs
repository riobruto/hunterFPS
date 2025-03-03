using Game.Life;
using Game.Life.WaypointPath;
using Life.StateMachines;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Life.Controllers
{
    public class SoldierCoverFromGrenadeState : BaseState
    {
        private SoldierAgentController _soldier;

        public SoldierCoverFromGrenadeState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public bool Safe => Vector3.Distance(_targetPosition, _soldier.transform.position) < 1f;

        private Vector3 _targetPosition;

        public override void Start()
        {
            _targetPosition = _soldier.FindCoverFromGrenade().Position;
            _soldier.SetTarget(_targetPosition);
            _soldier.SetMovementType(SoldierMovementType.RUN);
            _soldier.FaceTarget = false;
            _soldier.SetAllowFire(false);
        }

        public override void Think()
        {
        }
    }

    public class SoldierEngagePlayerState : BaseState
    {
        private Vector3 _destination;
        private SoldierAgentController _soldier;

        //creates a timespan so shooting is not inmediate
        public float _waitForChase;

        private float _timeWhenStarted;
        private float _chaseWaitTime = .05f;

        //creates a timeout for not wanting to shoot anymore
        private float _engageTimeOut;

        private float _shotgunnerMoveTime;

        public SoldierEngagePlayerState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _timeWhenStarted = Time.time;
            _engageTimeOut = Random.Range(4f, 8f);
            _shotgunnerMoveTime = 0;
            FindAttackPoint();
            _soldier.SetLookTarget(_soldier.AttackPoint);

            if (_soldier.HasPlayerVisual)
            {
                _soldier.FaceTarget = true;
                _soldier.SetMovementType(SoldierMovementType.WALK);
            }
            else
            {
                _soldier.SetMovementType(SoldierMovementType.RUN);
                _soldier.FaceTarget = false;
            }
        }

        public override void Think()
        {
            //forces the npc to exit attack after a certain time
            // if (Time.time - _timeWhenStarted > _engageTimeOut) { _soldier.Squad.ReleaseAttackSlot(_soldier); return; }
            bool hasReachedAttackPoint = Vector3.Distance(_destination, _soldier.transform.position) < 2f;
            _soldier.SetLookTarget(_soldier.AttackPoint);
            //esto es para forzarlo a correr antes de llegar al punto

            _soldier.SetAllowFire(_soldier.IsPlayerInViewAngle(.25f) && _soldier.IsPlayerVisible() && _soldier.FaceTarget && Time.time - _timeWhenStarted > 1f);

            _shotgunnerMoveTime += .1f;

            if (_soldier.IsPlayerVisible())
            {
                _soldier.FaceTarget = true;
                _soldier.SetMovementType(SoldierMovementType.WALK);
                _soldier.AttackPoint = _soldier.PlayerHeadPosition;
                _waitForChase = 0;

                if (_soldier.SoldierType == SoldierType.SHOTGUNNER)
                {
                    if (_shotgunnerMoveTime > 2)
                    {
                        FindAttackPoint();
                        _shotgunnerMoveTime = 0;
                    }
                }

                //retornamos, ya que la accion esta definida
                return;
            }

            //si perdi al jugador y no tengo recorrido hacia un nuevo punto valido
            //esto previene recalcular querys sin haber ido al punto deseado primero

            //ya que no pudimos ver al jugador, decidimos si correr o caminar al siguiente punto

            if (_soldier.SoldierType != SoldierType.HEAVY)
            {
                _soldier.FaceTarget = hasReachedAttackPoint;
                _soldier.SetMovementType(!hasReachedAttackPoint ? SoldierMovementType.RUN : SoldierMovementType.WALK);
            }
            else
            {
                _soldier.FaceTarget = true;
                _soldier.SetMovementType(SoldierMovementType.WALK);
            }

            //si llego al punto de ataque, y aun asi, no puede dispararle al jugador
            if (!_soldier.IsPlayerVisible() && hasReachedAttackPoint)
            {
                if (_soldier.SoldierType == SoldierType.SHOTGUNNER)
                {
                    FindAttackPoint();
                    return;
                }

                //probamos lanzar una granada
                if (_soldier.TryThrowGrenade())
                {
                    _soldier.SetTarget(_soldier.transform.position);
                    //reset reaction time for a pause
                    _timeWhenStarted = Time.time;
                    return;
                }

                DoChaseTimeout();
            }
        }

        private void DoChaseTimeout()
        {
            if (_waitForChase > _chaseWaitTime)
            {
                FindAttackPoint();
                _waitForChase = 0;
            }
            else _waitForChase += .01f;
        }

        private void FindAttackPoint()
        {
            _shotgunnerMoveTime = 0;
            _destination = _soldier.FindAgressivePosition(true);
            _soldier.SetTarget(_destination);
        }
    }

    public class SoldierNearThreatAttackState : BaseState
    {
        private SoldierAgentController _observer;

        public SoldierNearThreatAttackState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetMovementType(SoldierMovementType.RUN);
            _observer.SetAllowFire(true);
        }

        public override void Think()
        {
            _observer.SetLookTarget(_observer.NearThreatAgent.Head.position);
            _observer.SetTarget(_observer.transform.position + (_observer.transform.position - _observer.NearThreatAgent.transform.position).normalized * 4f);
        }
    }

    public class SoldierDieState : BaseState
    {
        private SoldierAgentController _observer;
        private float _waitForRagdollTime = .4f;
        private float _time;
        private bool _done;

        public SoldierDieState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetAllowFire(false);
            _observer.DropWeapon();
            _observer.RagdollBody();
        }

        public override void Think()
        {
        }
    }

    public class SoldierEnterState : BaseState
    {
        private float _enterTime = 0;

        private SoldierAgentController _observer;

        private bool _waited;

        private float _waitTime = 1000;

        public SoldierEnterState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public bool Reached => Vector3.Distance(_observer.transform.position, _observer.EnterPoint) < 1 && _waited;

        public override void DrawGizmos()
        {
            Gizmos.DrawCube(_observer.EnterPoint, Vector3.one);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetTarget(_observer.EnterPoint);
            _observer.FaceTarget = false;
            _observer.SetMovementType(SoldierMovementType.RUN);
            _enterTime = Time.time;
        }

        public override void Think()
        {
            if (_waited) return;
            if (Time.time - _enterTime > _waitTime) { _waited = true; }
        }
    }

    public class SoldierPatrolState : BaseState
    {
        private Waypoint _currentWaypoint;

        private float _lastWaitTime;

        private SoldierAgentController _observer;

        private Vector3 _targetPos;

        private float _waitTime;

        public SoldierPatrolState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
            Gizmos.DrawSphere(_targetPos, 0.33f);
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.SetMovementType(SoldierMovementType.WALK);
            _observer.Animator.SetBool("WARNING", true);
            //_observer.Animator.SetLayerWeight(3, 0);
            //_observer.Animator.SetLayerWeight(2, 0);
            ;
            _currentWaypoint = _observer.Waypoints.CurrentWaypoint;
            if (FindNewTarget()) _observer.SetTarget(_targetPos);
            _observer.FaceTarget = false;
        }

        public override void Think()
        {
            if (ReachedTarget())
            {
                _waitTime += Time.deltaTime;

                if (_waitTime > (!_observer.UseWaypoints ? 5 : _currentWaypoint.WaitTime))
                {
                    _waitTime = 0;
                    if (FindNewTarget())
                    {
                        _observer.SetTarget(_targetPos);
                    }
                }
            }
        }

        private bool FindNewTarget()
        {
            if (!_observer.UseWaypoints)
            {
                int randomTries = 5;
                for (int i = 0; i < randomTries; i++)
                {
                    if (NavMesh.SamplePosition(_observer.transform.position + Random.insideUnitSphere * 5f, out NavMeshHit rhit, 5, NavMesh.AllAreas))
                    {
                        _targetPos = rhit.position;

                        return true;
                    }
                }
                return false;
            }
            _currentWaypoint = _currentWaypoint.NextWaypoint;

            if (NavMesh.SamplePosition(_currentWaypoint.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas))
            {
                _targetPos = hit.position;

                return true;
            }

            return false;
        }

        private bool ReachedTarget()
        {
            Vector3 pos = _targetPos;
            pos.y = _observer.transform.position.y;
            return Vector3.Distance(_observer.transform.position, pos) < 1.5f;
        }
    }

    public class SoldierReportState : BaseState
    {
        private bool _hasReported;

        private SoldierAgentController _observer;

        private float _time;

        public SoldierReportState(AgentController context) : base(context)
        {
            _observer = context as SoldierAgentController;
        }

        public bool Done { get => _hasReported; }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _observer.Shout();
            _observer.SetTarget(_observer.transform.position);
            _time = Time.time;
            _observer.Animator.SetBool("WARNING", true);
            _observer.Animator.SetTrigger("SURPRISE");
            _observer.FaceTarget = true;
            _observer.SetTarget(_observer.transform.position + -_observer.transform.forward);
            _hasReported = false;
        }

        public override void Think()
        {
            _observer.SetLookTarget(_observer.PlayerHeadPosition);
            if (Time.time - _time > 1)
            {
                _hasReported = true;
            }
        }
    }

    public class SoldierRetreatCoverState : BaseState
    {
        private Vector3 _destination;
        private float _lastFindCover;
        private SoldierAgentController _soldier;

        public SoldierRetreatCoverState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public bool IsCurrentPositionValid()
        {
            return _soldier.HasPlayerVisual;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
            _soldier.HurtEvent -= OnHurt;
        }

        public override void Start()
        {
            _soldier.SetMovementType(SoldierMovementType.RUN);

            _soldier.Animator.SetTrigger("COVER");
            _soldier.SetAllowFire(false);
            _soldier.FaceTarget = false;
            _soldier.HurtEvent += OnHurt;

            if (IsCurrentPositionValid())
            {
                _soldier.SetAllowReload(true);
                _soldier.SetMovementType(SoldierMovementType.CROUCH);
            }
            else MoveToCover();
        }

        private void OnHurt(AgentHurtPayload payload, AgentController controller)
        {
            MoveToCover();
        }

        public override void Think()
        {
            if (Time.time - _lastFindCover < 2) return;

            if (Vector3.Distance(_destination, _soldier.transform.position) < 1.1)
            {
                if (_soldier.CurrentCoverSpot != null || IsCurrentPositionValid())
                {
                    _soldier.SetAllowReload(true);
                    _soldier.SetMovementType(SoldierMovementType.CROUCH);
                }
                else if (!IsCurrentPositionValid())
                {
                    MoveToCover();
                }
            }

            _soldier.SetLookTarget(_soldier.AttackPoint);
        }

        private void MoveToCover()
        {
            _lastFindCover = Time.time;
            _destination = _soldier.FindCoverFromPlayer(true);
            _soldier.SetTarget(_destination);
            _soldier.SetMovementType(SoldierMovementType.RUN);
        }
    }

    public class SoldierGoToPlayer : BaseState
    {
        private SoldierAgentController _soldier;
        private Vector3 _destination;
        private Vector3 _lookPoint;

        public SoldierGoToPlayer(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
        }

        public override void Start()
        {
            _soldier.SetMovementType(SoldierMovementType.WALK);
            _soldier.Animator.SetBool("WARNING", true);
            _soldier.Animator.SetTrigger("SUSPECT");
            _soldier.SetTarget(_destination = _soldier.AttackPoint);
            _lookPoint = _soldier.AttackPoint;
            _soldier.SetAllowFire(false);
        }

        private void SearchRandom()
        {
            if (Vector3.Distance(_soldier.transform.position, _destination) < 2)
            {
                SpatialDataQuery newQuery = new SpatialDataQuery(new SpatialQueryPrefs(_soldier, _soldier.AttackPoint, _soldier.PlayerHeadPosition, 1));
                _soldier.SetTarget(_destination = newQuery.AllPoints[Random.Range(0, newQuery.AllPoints.Count)].Position);
                _lookPoint = _destination + Vector3.up * 1.75f;
            }
        }

        public override void Think()
        {
            _soldier.SetLookTarget(_lookPoint);
            SearchRandom();
        }
    }

    public class SoldierActBusyState : BaseState
    {
        private SoldierAgentController _soldier;
        private Vector3 _destination;
        private Vector3 _lookPoint;

        // todo: idle state, unalerted, scare
        public SoldierActBusyState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void DrawGizmos()
        {
        }

        public override void End()
        {
            _soldier.Animator.SetBool("RELAX", false);
        }

        public override void Start()
        {
            _soldier.Animator.SetBool("RELAX", true);
            _soldier.SetTarget(_soldier.transform.position);
            _soldier.FaceTarget = false;
            if (_soldier.UseWaypoints)
            {
            }
        }

        public override void Think()
        {
        }
    }

    public class SoldierInvestigateState : BaseState
    {
        private SoldierAgentController _soldier;
        private bool _reached => Vector3.Distance(_soldier.transform.position, _soldier.InvestigateLocation) < 2;

        public bool Ready { get => _investigated; }

        private bool _investigated;
        private float _waitTime = 10;
        private float _currentWaitTime;
        private Vector3 _soldierStartPoint;

        public SoldierInvestigateState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void Start()
        {
            _currentWaitTime = 0;
            _soldierStartPoint = _soldier.transform.position;
            _soldier.SetTarget(_soldier.InvestigateLocation);
        }

        public override void Think()
        {
            if (_investigated) return;
            if (_currentWaitTime > _waitTime)
            {
                _investigated = true;
                _soldier.SetTarget(_soldierStartPoint);
                return;
            }
            if (_reached && !_investigated)
            {
                _currentWaitTime += 0.1f;
                return;
            }
        }

        public override void End()
        {
        }

        public override void DrawGizmos()
        {
        }
    }

    public class SoldierHoldPositionState : BaseState
    {
        private SoldierAgentController _soldier;
        private Vector3 _destination;
        private bool _reached => Vector3.Distance(_destination, _soldier.transform.position) < 2;

        public SoldierHoldPositionState(AgentController context) : base(context)
        {
            _soldier = context as SoldierAgentController;
        }

        public override void Start()
        {
            _destination = _soldier.FindCoverSpot().transform.position;
            _soldier.SetTarget(_destination);
        }

        public override void Think()
        {
            if (_reached)
            {
                _soldier.SetMovementType(SoldierMovementType.CROUCH);
            }
        }

        public override void End()
        {
            _soldier.SetMovementType(SoldierMovementType.WALK);
        }

        public override void DrawGizmos()
        {
            throw new System.NotImplementedException();
        }
    }
}
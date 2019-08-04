using System;
using Animancer;
using Animation;
using Player;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Enemy
{
    public class EnemyAi : MonoBehaviour, EnemyHealth.IDeath
    {
        public AiStateMachine stateMachine;
        public EnemyProperties properties;
        [AnimationCollection.ValidateAttribute(typeof(EnemyAnimation))]
        public AnimationCollection animations;

        private NavMeshPath path;

        private bool isDead;
        
        private void Start()
        {
            EnemyShared shared = new EnemyShared()
            {
                    parent = this,
                    animancer = GetComponentInChildren<EventfulAnimancerComponent>(),
                    agent = GetComponent<NavMeshAgent>(),
                    rb = GetComponent<Rigidbody>()
            };
            
            stateMachine = new AiStateMachine(transform)
            {
                    shared = shared,
                    target = PlayerController.Instance.transform
            };
            
            shared.animancer.onEvent = new AnimationEventReceiver(null, x => stateMachine.OnEvent(x.stringParameter));
            
            stateMachine.RegisterState(new Idle(), "Idle");
            stateMachine.RegisterState(new AnimationState(EnemyAnimation.Stare, "Notice", 
                    state => state.Face((LanternIntensityTweaker.Light.transform.position - transform.position).normalized)), "Stare");
            stateMachine.RegisterState(new AnimationState(EnemyAnimation.Taunt, "Chase"), "Notice");
            stateMachine.RegisterState(new Chase(), "Chase");
            stateMachine.RegisterState(new Stopped(), "Stopped");
//            stateMachine.RegisterState(new Leap(), "Leap");
            stateMachine.RegisterState(new Attack(), "Attack");
            stateMachine.RegisterState(new Retreat(), "Retreat");
            
            stateMachine.RegisterTransition("Idle", "Stare", machine => CanSeeLight());
            stateMachine.RegisterTransition("Idle", "Notice", machine => machine.HorizontalDistanceToTarget <= machine.Get<float>(nameof(EnemyProperties.noticePlayerDistance)));

//            stateMachine.RegisterTransition("Chase", "Leap", 
//                    machine => machine.HorizontalDistanceToTarget <= machine.Get<float>(nameof(EnemyProperties.leapRange)) && 
//                               machine.HorizontalDistanceToTarget > machine.Get<float>(nameof(EnemyProperties.leapRange)) / 2F);
            
            stateMachine.RegisterTransition("Chase", "Stopped", machine => machine.HorizontalDistanceToTarget <= machine.Get<float>(nameof(EnemyProperties.meleeRange)));
            stateMachine.RegisterTransition("Stopped", "Chase", machine => machine.HorizontalDistanceToTarget > machine.Get<float>(nameof(EnemyProperties.meleeRange)) * 1.5F);
            
            stateMachine.RegisterTransition("Stopped", "Attack", 
                    machine => machine.GetAddSet("AttackTimer", -Time.deltaTime, machine.Get<float>(nameof(EnemyProperties.attackCooldown))) <= 0F,
                    machine => machine.Set("AttackTimer", machine.Get<float>(nameof(EnemyProperties.attackCooldown))));
            
            /*
             * Manual transitions:
             * Stare->Notice
             * Notice->Chasing
             * Leap->Chasing
             * 
             * Attack->Retreat
             * Attack->Stopped
             */
            
            stateMachine.ImportProperties(properties);
            stateMachine.SetDefaultState("Idle");
            
            stateMachine.RegisterVisualizer($"Enemy:{name}");
        }

        #region States
        
        private abstract class EnemyState : AiStateMachine.State
        {
            protected EnemyShared shared;
            private float faceVelocity;

            public override void OnInit()
            {
                base.OnInit();
                
                shared = Machine.shared as EnemyShared;
            }

            protected AnimationClip GetClip(EnemyAnimation clip)
            {
                return shared.parent.animations.GetClip(clip);
            }

            public void Face(Vector3 directionToTarget, float dampSpeed = .1F)
            {
                float intendedAngle = Quaternion.LookRotation(directionToTarget, Vector3.up).eulerAngles.y;

                transform.eulerAngles = transform.eulerAngles.Set(Mathf.SmoothDampAngle(transform.eulerAngles.y, intendedAngle, ref faceVelocity, dampSpeed), Utility.Axis.Y);
            }
        }

        private class Idle : EnemyState
        {
            private AnimancerState idle;

            public override void OnInit()
            {
                base.OnInit();

                idle = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.Idle));
            }

            public override void OnEnter()
            {
                base.OnEnter();

                shared.animancer.Play(idle);
            }
        }

        private class AnimationState : EnemyState
        {
            private AnimancerState state;
            private readonly string exitState;

            private readonly Action<AnimationState> onUpdate;
            private readonly EnemyAnimation animation;
            
            public AnimationState(EnemyAnimation animation, string exitState, Action<AnimationState> onUpdate = null)
            {
                this.animation = animation;
                this.exitState = exitState;
                this.onUpdate = onUpdate;
            }

            public override void OnInit()
            {
                base.OnInit();
                state = shared.animancer.GetOrCreateState(GetClip(animation));
            }

            public override void OnEnter()
            {
                base.OnEnter();

                shared.animancer.CrossFadeFromStart(state).OnEnd = () =>
                {
                    state.OnEnd = null;
                    Machine.EnterState(exitState);
                };
            }

            public override void OnTick()
            {
                base.OnTick();
                
                onUpdate?.Invoke(this);
            }
        }

        private class Chase : EnemyState
        {
            private AnimancerState chase;

            public override void OnInit()
            {
                base.OnInit();

                chase = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.Scuttle));
            }

            public override void OnEnter()
            {
                base.OnEnter();

                shared.animancer.Play(chase);
            }
            
            public override void OnTick()
            {
                base.OnTick();

                shared.agent.speed = Machine.Get<float>(nameof(EnemyProperties.chaseSpeed));
                shared.agent.SetDestination(target.position);
                
                Face(HorizontalDirectionToTarget, .2F);
            }

            public override void OnExit()
            {
                base.OnExit();
                
                shared.agent.SetDestination(transform.position);
            }
        }

        private class Stopped : EnemyState
        {
            public override void OnInit()
            {
                base.OnInit();

                backgroundState = Machine.FindState("Idle");
            }

            public override void OnTick()
            {
                base.OnTick();
                
                Face(HorizontalDirectionToTarget);
            }
        }

        private class Attack : EnemyState
        {
            private AnimancerState attack;

            public override void OnInit()
            {
                base.OnInit();

                attack = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.Attack));
            }

            public override void OnEnter()
            {
                base.OnEnter();
                
                shared.animancer.CrossFadeFromStart(attack, .1F).OnEnd = OnEnd;
            }

            private void OnEnd()
            {
                attack.OnEnd = null;

                if (Machine.GetAddSet<int>("AttackCount", 1) > Machine.Get<int>(nameof(EnemyProperties.attacksBeforeRetreat)))
                {
                    Machine.Set("AttackCount", 0);
                    Machine.EnterState("Retreat");
                }
                else
                {
                    Machine.EnterState("Stopped");
                }
                
            }

            public override void OnEvent(string id)
            {
                base.OnEvent(id);

                if ("Attack".Equals(id))
                    if(HorizontalDistanceToTarget <= Machine.Get<float>(nameof(EnemyProperties.meleeRange)) * 1.5F)
                        PlayerController.Instance.Damage(15F, transform);
            }
        }

        private class Leap : EnemyState
        {
            private float timer;
            
            private AnimancerState leap;
            private AnimancerState midair;
            private AnimancerState land;

            public override void OnInit()
            {
                base.OnInit();

                leap = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.Leap));
                midair = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.LeapMidair));
                land = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.LeapLand));
            }

            public override void OnEnter()
            {
                base.OnEnter();

                transform.forward = HorizontalDirectionToTarget;
                
                shared.animancer.CrossFadeFromStart(leap, .1F).OnEnd = () =>
                {
                    leap.OnEnd = null;
                    shared.animancer.CrossFadeFromStart(midair, .05F);
                    shared.rb.velocity = transform.forward * 4F + transform.up * 1F;
                };

                shared.agent.isStopped = true;
                timer = 0F;
            }

            public override void OnTick()
            {
                base.OnTick();
                timer += Time.deltaTime;
                
                if(timer > 5F)
                    Machine.EnterState("Chase");
            }

            public override void OnExit()
            {
                base.OnExit();

                shared.agent.isStopped = false;
                shared.agent.SetDestination(transform.position);
            }

            public override void OnEventCollide(Collision other)
            {
                base.OnEventCollide(other);

                shared.animancer.CrossFadeFromStart(land, .05F).OnEnd = () =>
                {
                    land.OnEnd = null;
                    Machine.EnterState("Chase");
                };
            }
        }

        private class Retreat : EnemyState
        {
            private AnimancerState run;
            private float timer;

            public override void OnInit()
            {
                base.OnInit();

                run = shared.animancer.GetOrCreateState(GetClip(EnemyAnimation.Scuttle));
            }

            public override void OnEnter()
            {
                base.OnEnter();

                shared.animancer.Play(run);
                
                shared.agent.SetDestination(Random.insideUnitCircle.RemapXZ() * 15F);
                shared.agent.speed = Machine.Get<float>(nameof(EnemyProperties.retreatSpeed));
                timer = 0F;
            }

            public override void OnTick()
            {
                base.OnTick();

                timer += Time.deltaTime;

                if (timer > 2.5F)
                    Machine.EnterState("Chase");
            }

            public override void OnExit()
            {
                base.OnExit();
                
                run.Stop();
            }
        }

        #endregion
        
        private void Update()
        {
            if (isDead || stateMachine.DistanceToTarget > 150F)
                return;
            
            stateMachine.Tick();
        }

        private void LateUpdate()
        {
            if (isDead || stateMachine.DistanceToTarget > 150F)
                return;
            
            stateMachine.LateTick();
        }

        private void OnCollisionEnter(Collision other)
        {
            if(!isDead)
                stateMachine.OnEventCollide(other);
        }

        private bool CanSeeLight()
        {
            if (Player.PlayerController.Instance.LanternHeld)
                return false;
            
            LanternIntensityTweaker light = LanternIntensityTweaker.Light;
            float distance = (light.transform.position - transform.position).magnitude;

            if (distance > light.GetLightRadius())
                return false;

            if (distance <= Mathf.Epsilon)
                return light.GetLightLevel() >= properties.noticeLightLevel;

            return light.GetLightLevel() * 1 / Mathf.Pow(distance, 2) >= properties.noticeLightLevel;
        }

        [Serializable]
        public class EnemyProperties
        {
            public float noticeLightLevel = 1F;
            public float noticePlayerDistance = 4F;
            public float leapRange = 8F;
            public float meleeRange = 2F;

            public int attacksBeforeRetreat = 3;
            public float attackCooldown = .25F;

            [Header("Speeds")]
            public float chaseSpeed = 15F;
            public float retreatSpeed = 20F;
        }
        
        private class EnemyShared
        {
            public EnemyAi parent;
            public EventfulAnimancerComponent animancer;
            public NavMeshAgent agent;
            public Rigidbody rb;
        }

        [AnimationCollection.Enum("Enemy Animations")]
        public enum EnemyAnimation
        {
            Idle,
            Attack,
            Die,
            Taunt,
            
            [Header("Mobility")]
            Stare,
            Scuttle,
            
            [Header("Leap")]
            Leap,
            LeapMidair,
            LeapLand
            
        }

        public void OnDying(ref bool cancel)
        {
            stateMachine.Exit();
            isDead = true;

            cancel = true;
            Destroy(this);
            Destroy(GetComponent<NavMeshAgent>());
            Destroy(gameObject, 4F);

            AnimancerComponent animancer = GetComponentInChildren<AnimancerComponent>();
            animancer.Play(animations.GetClip(EnemyAnimation.Die));
        }
    }
}

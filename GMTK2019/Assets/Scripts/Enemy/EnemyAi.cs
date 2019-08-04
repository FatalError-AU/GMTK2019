using System;
using Animancer;
using UnityEngine;

namespace Enemy
{
    public class EnemyAi : MonoBehaviour
    {
        public AiStateMachine stateMachine;
        public EnemyProperties properties;

        private void Start()
        {
            stateMachine = new AiStateMachine(transform)
            {
                    shared = new EnemyShared(),
                    target = Player.PlayerController.Instance.transform
            };
            
            stateMachine.RegisterState(null, "Idle");
            stateMachine.RegisterState(null, "Stare");
            stateMachine.RegisterState(null, "Notice");
            stateMachine.RegisterState(null, "Chase");
            stateMachine.RegisterState(null, "Stopped");
            stateMachine.RegisterState(null, "Leap");
            stateMachine.RegisterState(null, "Attack");
            stateMachine.RegisterState(null, "Retreat");
            stateMachine.RegisterState(null, "Taunt");
            
            stateMachine.RegisterTransition("Idle", "Stare", machine => CanSeeLight());
            stateMachine.RegisterTransition("Idle", "Notice", machine => machine.HorizontalDistanceToTarget <= machine.Get<float>(nameof(EnemyProperties.noticePlayerDistance)));

            stateMachine.RegisterTransition("Chasing", "Leap", 
                    machine => machine.HorizontalDistanceToTarget <= machine.Get<float>(nameof(EnemyProperties.leapRange)) && 
                               machine.HorizontalDistanceToTarget > machine.Get<float>(nameof(EnemyProperties.leapRange)) / 2F);
            
            stateMachine.RegisterTransition("Chasing", "Stopped", machine => machine.HorizontalDistanceToTarget <= machine.Get<float>(nameof(EnemyProperties.meleeRange)));
            stateMachine.RegisterTransition("Stopped", "Chasing", machine => machine.HorizontalDistanceToTarget > machine.Get<float>(nameof(EnemyProperties.meleeRange)));
            
            stateMachine.RegisterTransition("Stopped", "Attack", 
                    machine => machine.GetAddSet("AttackTimer", -Time.deltaTime, machine.Get<float>(nameof(EnemyProperties.attackCooldown))) <= 0F,
                    machine => machine.Set("AttackTimer", machine.Get<float>(nameof(EnemyProperties.attackCooldown))));
            
            /*
             * Manual transitions:
             * Stare->Notice
             * Notice->Chasing
             * Leap->Chasing
             * 
             * Attack->Taunt
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
            public EnemyShared shared;

            public override void OnInit()
            {
                base.OnInit();
                
                shared = Machine.shared as EnemyShared;
            }
        }

        private class Idle : EnemyState
        {
            
        }

        #endregion
        
        private void Update()
        {
            if (stateMachine.DistanceToTarget > 200F)
                return;
            
            stateMachine.Tick();
        }

        private void LateUpdate()
        {
            if (stateMachine.DistanceToTarget > 150F)
                return;
            
            stateMachine.LateTick();
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
            public float attackCooldown;
            public float tauntChance = .1F;
        }
        
        private class EnemyShared
        {
            public AnimancerComponent animancer;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Mouledoux.Components
{
    public sealed class StateMachine
    {
        public State currentState { get; private set; }
        public State anyState { get; private set; }
        private List<Condition> allConditions = new List<Condition>();

        public void Initialize(State initState)
        {
            currentState = initState;
        }

        public bool Update(bool autoUpdateConditions = true)
        {
            currentState.onStateUpdate();

            if (autoUpdateConditions) { UpdateAllConditions(); }
            
            return PerformTransistion(GetValidTransition(currentState.availableTransitions)) ||
                PerformTransistion(GetValidTransition(anyState.availableTransitions));
        }

        public void UpdateAllConditions()
        {
            foreach (Condition c in allConditions)
            {
                c.CheckResults();
            }
        }

        private Transistion GetValidTransition(Transistion[] transitions)
        {
            bool pass;
            
            foreach (Transistion t in transitions)
            {
                if(t.targetState == anyState) { continue; }

                pass = true;

                foreach (int i in t.transitionConditions)
                {
                    if (i < 0 || i > allConditions.Count) { continue; }
                    
                    pass = allConditions[i].lastCheckedResult & pass;
                }

                if (pass) return t;
            }

            return default;
        }

        private bool PerformTransistion(Transistion t)
        {
            if (t == null || t == default) return false;

            currentState.onStateExit?.Invoke();
            currentState = t.targetState;
            currentState.onStateEnter?.Invoke();

            return true;
        }


        public int GetConditionIndex(Condition cond)
        {
            if (!allConditions.Contains(cond)) { return -1; }
            
            return allConditions.IndexOf(cond);
        }


        public sealed class State
        {
            public Action onStateEnter { get; set; }
            public Action onStateUpdate { get; set; }
            public Action onStateExit { get; set; }

            public State()
            {
                onStateEnter = default;
                onStateUpdate = default;
                onStateExit = default;
            }

            public State(Action onEnter, Action onUpdate, Action onExit)
            {
                onStateEnter = onEnter;
                onStateUpdate = onUpdate;
                onStateExit = onExit;
            }

            public Transistion[] availableTransitions { get; set; }
        }


        public sealed class Transistion
        {
            public State targetState { get; private set; }
            public int[] transitionConditions { get; private set; }

            public Transistion(State target, int[] condIndex)
            {
                targetState = target;
                transitionConditions = condIndex;
            }
        }


        public sealed class Condition
        {
            public bool lastCheckedResult { get; private set; }

            private Func<bool> conditionDelegate = default;

            public Condition(StateMachine sm, Func<bool> cond)
            {
                conditionDelegate = cond == null ? default : cond;

                if (!sm.allConditions.Contains(this))
                {
                    sm.allConditions.Add(this);
                }
            }

            public bool CheckResults()
            {
                lastCheckedResult = conditionDelegate.Invoke();
                return lastCheckedResult;
            }
        }





        private sealed class PacGhost
        {
            public bool edible = false;
            public float pacDist;

            public float homeTimer = 10f;
            public float homeDist;


            private StateMachine ghostSM;

            private StateMachine.State sInit;
            private StateMachine.State sAtHome;
            private StateMachine.State sLeavingHome;
            private StateMachine.State sChasing;
            private StateMachine.State sFleeing;
            private StateMachine.State sWander;
            private StateMachine.State sDead;

            private StateMachine.Condition isDead;
            private StateMachine.Condition isHome;

            private StateMachine.Transistion toDead;
            private StateMachine.Transistion toHome;


            public void OnStart()
            {
                isDead = new Condition(ghostSM, () => { return edible && pacDist < 1; });
                isHome = new Condition(ghostSM, () => { return homeDist < 1; });


                toDead = new Transistion(sDead, new int[]{ 0 });
                toHome = new Transistion(sDead, new int[]{ 1 });


                ghostSM.anyState.availableTransitions = new Transistion[] { toDead };


                sInit.availableTransitions = new Transistion[] { };
                sAtHome.availableTransitions = new Transistion[] { toHome };
                sLeavingHome.availableTransitions = new Transistion[] { };
                sChasing.availableTransitions = new Transistion[] { };
                sFleeing.availableTransitions = new Transistion[] { };
                sWander.availableTransitions = new Transistion[] { };
                sDead.availableTransitions = new Transistion[] { };


                sAtHome.onStateEnter += () => { homeTimer -= 0.016f; };
                sAtHome.onStateUpdate += () => { homeTimer -= 0.016f; };
                

            }

            public void OnUpdate()
            {

            }

        }
























    }
}

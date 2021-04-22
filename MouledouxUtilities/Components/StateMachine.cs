using System;
using System.Collections.Generic;

namespace Mouledoux.Components
{
    public sealed class StateMachine
    {
        public State currentState { get; private set; }
        public State anyState { get; private set; }


        public void Initialize(State initState)
        {
            currentState = initState;
        }

        public bool Update(bool autoPerformConditionUpdates = false)
        {
            currentState.onStateUpdate();
            PerformTransistion(currentState.GetNextValidTransistion(autoPerformConditionUpdates));
            return true;
        }


        private bool PerformTransistion(Transistion t)
        {
            if (t == null || t == default) return false;

            currentState.onStateExit?.Invoke();
            currentState = t.targetState;
            currentState.onStateEnter?.Invoke();

            return true;
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

            public Transistion GetNextValidTransistion(bool performCondition = false)
            {
                foreach(Transistion t in availableTransitions)
                {
                    if(t.CheckConditionResults(performCondition)) { return t; }
                }

                return default;
            }

            public Transistion[] availableTransitions { get; set; }
        }


        public sealed class Transistion
        {
            public State targetState { get; private set; }
            public Condition[] transitionConditions { get; private set; }

            public Transistion(State target, Condition[] condIndex)
            {
                targetState = target;
                transitionConditions = condIndex;
            }


            public bool CheckConditionResults(bool performCondition = false)
            {
                bool pass = true;
                
                if(performCondition)
                {
                    foreach(Condition c in transitionConditions)
                    {
                        pass = c.CheckResults() && pass;
                    }
                }
                else
                {
                    foreach (Condition c in transitionConditions)
                    {
                        pass = c.lastCheckedResult && pass;
                    }
                }

                return pass;
            }
        }



        public sealed class Condition
        {
            public bool lastCheckedResult { get; private set; }

            private Func<bool> conditionDelegate = default;

            public Condition(Func<bool> cond)
            {
                conditionDelegate = cond == null ? default : cond;
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
            private StateMachine.State sLeaving;
            private StateMachine.State sChasing;
            private StateMachine.State sFleeing;
            private StateMachine.State sWander;
            private StateMachine.State sDead;

            private StateMachine.Condition isDead;
            private StateMachine.Condition isHome;
            private StateMachine.Condition isLeaving;
            private StateMachine.Condition isFlee;
            private StateMachine.Condition notFlee;
            private StateMachine.Condition isChase;
            private StateMachine.Condition isWander;


            private StateMachine.Transistion toDead;
            private StateMachine.Transistion toHome;
            private StateMachine.Transistion toLeaving;
            private StateMachine.Transistion toFlee;
            private StateMachine.Transistion toChase;
            private StateMachine.Transistion toWander;


            public void OnStart()
            {
                isDead = new Condition(() => { return edible && pacDist < 1; });
                isHome = new Condition(() => { return homeDist < 1; });
                isLeaving = new Condition(() => { return homeTimer < 1; });
                isWander = new Condition(() => { return pacDist > 5f; });
                isChase = new Condition(() => { return pacDist < 6f; });
                isFlee = new Condition(() => { return edible; });
                notFlee = new Condition(() => { return !edible; });


                toDead = new Transistion(sDead, new Condition[]{ isDead });
                toHome = new Transistion(sAtHome, new Condition[]{ isHome });
                toLeaving = new Transistion(sAtHome, new Condition[]{ isLeaving });
                toWander = new Transistion(sWander, new Condition[] { notFlee, isWander });
                toChase = new Transistion(sChasing, new Condition[] {notFlee, isChase });
                toFlee = new Transistion(sFleeing, new Condition[] { isFlee });


                sInit.availableTransitions = new Transistion[] { toHome };
                sAtHome.availableTransitions = new Transistion[] { toLeaving };
                sLeaving.availableTransitions = new Transistion[] { toChase, toWander };
                sWander.availableTransitions = new Transistion[] { toChase, toFlee };
                sChasing.availableTransitions = new Transistion[] { toWander, toFlee };
                sFleeing.availableTransitions = new Transistion[] { toDead, toChase, toWander };
                sDead.availableTransitions = new Transistion[] { toHome };


                sAtHome.onStateEnter += () => { homeTimer = 8f; };
                sAtHome.onStateUpdate += () => { homeTimer -= 0.016f; };

                sLeaving.onStateEnter += () => { /*do animation*/ };

                sWander.onStateUpdate += () => { /*do path-finding*/ };
                sChasing.onStateUpdate += () => { /*do different path-finding*/ };
                sFleeing.onStateUpdate += () => { /*do backwards path-finding*/ };

                sDead.onStateEnter += () => { /*sprite swap*/ };
                sDead.onStateUpdate += () => { /*go home*/ };
                sDead.onStateExit += () => { /*sprite swap*/ };
            }

            public void OnUpdate()
            {
                ghostSM.Update();
            }

        }
























    }
}

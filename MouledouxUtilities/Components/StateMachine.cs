using System;
using System.Linq;
using System.Collections.Generic;

namespace Mouledoux.Components
{
    public sealed class StateMachine
    {
        public State currentState { get; private set; }
        public State anyState { get; private set; }


        public void Initialize(State a_initState)
        {
            currentState = a_initState;
        }

        public bool Update(bool a_autoInvokeConditions = false)
        {
            currentState.onStateUpdate();
            PerformTransistion(currentState.GetNextValidTransistion(a_autoInvokeConditions));
            return true;
        }

        private bool PerformTransistion(Transistion a_transition)
        {
            if (a_transition == null || a_transition == default) return false;

            currentState.onStateExit?.Invoke();
            currentState = a_transition.targetState;
            currentState.onStateEnter?.Invoke();

            return true;
        }
        // end StateMachine Class // ---------- ---------- ---------- 




        public sealed class State
        {
            public Action onStateEnter { get; set; }
            public Action onStateUpdate { get; set; }
            public Action onStateExit { get; set; }

            public Transistion[] availableTransitions { get; set; }


            public State()
            {
                onStateEnter = default;
                onStateUpdate = default;
                onStateExit = default;
            }

            public State(Action a_onEnter, Action a_onUpdate, Action a_onExit)
            {
                onStateEnter = a_onEnter;
                onStateUpdate = a_onUpdate;
                onStateExit = a_onExit;
            }

            public Transistion GetNextValidTransistion(bool a_invokeConditions = false)
            {
                foreach(Transistion _potentialTransition in availableTransitions)
                {
                    if(_potentialTransition.CheckConditionResults(a_invokeConditions))
                    {
                        return _potentialTransition;
                    }
                }

                return default;
            }

        }
        // end State class ---------- ---------- ---------- 


        public sealed class Transistion
        {
            public State targetState { get; private set; }
            public Condition[] transitionConditions { get; private set; }

            public Transistion(State a_targetState, Condition[] a_conditions)
            {
                targetState = a_targetState;
                transitionConditions = a_conditions;
            }


            public bool CheckConditionResults(bool a_invokeConditions = false)
            {
                bool _conditionResults = true;
                
                if(a_invokeConditions)
                {
                    foreach(Condition _condition in transitionConditions)
                    {
                        _conditionResults &= a_invokeConditions
                            ? _condition.InvokeCheckConditionResults()
                            : _condition.lastCheckedResult;
                    }
                }

                return _conditionResults;
            }
        }
        // end Transition class ---------- ---------- ---------- 



        public sealed class Condition
        {
            public bool lastCheckedResult { get; private set; }

            private Func<bool> conditionDelegate = default;

            public Condition(Func<bool> a_condition)
            {
                conditionDelegate = a_condition == null ? default : a_condition;
            }

            public bool InvokeCheckConditionResults()
            {
                return lastCheckedResult =
                conditionDelegate.GetInvocationList().All(a_condition =>
                {
                    return a_condition is Func<bool> _fb && _fb.Invoke();
                });
            }

            public static explicit operator Condition (Func<bool> a_func)
            {
                return new Condition(a_func);
            }
            
            public static explicit operator Condition (Func<bool>[] a_funcs)
            {
                Func<bool> _newConditionDelegate = default;
                Condition _newCondition = new Condition(_newConditionDelegate);

                foreach(Func<bool> _fb in a_funcs)
                {
                    _newConditionDelegate += _fb;
                }

                return _newCondition;
            }
        }
        // end Condition class ---------- ---------- ---------- 





        private sealed class PacGhost
        {
            public bool edible = false;
            public float pacDist;

            public float homeTimer = 10f;
            public float homeDist;


            private StateMachine ghostSM;

            private State sInit;
            private State sAtHome;
            private State sLeaving;
            private State sChasing;
            private State sFleeing;
            private State sWander;
            private State sDead;

            private Condition isDead;
            private Condition isHome;
            private Condition isLeaving;
            private Condition isFlee;
            private Condition notFlee;
            private Condition isChase;
            private Condition isWander;


            private Transistion toDead;
            private Transistion toHome;
            private Transistion toLeaving;
            private Transistion toFlee;
            private Transistion toChase;
            private Transistion toWander;


            public void OnStart()
            {
                Func<bool> chaseZac = ChaseTarget;
                chaseZac += ChaseTarget;
                chaseZac += ChaseTarget;
                chaseZac += ChaseTarget;
                chaseZac += ChaseTarget;
                chaseZac += ChaseTarget;
                chaseZac += ChaseTarget;
                chaseZac += ChaseTarget;

                isDead = new Condition(() => { return edible && pacDist < 1; });
                isHome = new Condition(() => { return homeDist < 1; });
                isLeaving = new Condition(() => { return homeTimer < 1; });
                isWander = new Condition(() => { return pacDist > 5f; });
                isChase = (Condition)new[] { chaseZac, chaseZac, chaseZac, ChaseTarget };
                isFlee = new Condition(() => { return edible; });
                notFlee = new Condition(() => { return !edible; });

              
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

                ghostSM.Initialize(sInit);
            }

            public void OnUpdate()
            {
                ghostSM.Update();
            }

            public bool ChaseTarget()
            {
                return homeDist < 3;
            }

        }
























    }
}

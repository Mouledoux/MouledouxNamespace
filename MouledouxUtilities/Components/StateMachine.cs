using System;
using System.Linq;

namespace Mouledoux.Components
{
    public sealed class StateMachine
    {
        private State currentState;
        public State CurrentState
        {
            get
            {
                return currentState;
            }
            private set
            {
                currentState = value;
            }
        }


        private State anyState;
        public State AnyState
        {
            get
            {
                return anyState;
            }
            private set
            {
                anyState = value;
            }
        }


        private bool anyStateEnabled = true;
        public bool AnyStateEnabled
        {
            get
            {
                return anyStateEnabled;
            }
            private set
            {
                anyStateEnabled = value;
            }
        }


        public StateMachine(State a_initState, bool a_enableAnyState = true)
        {
            Initialize(a_initState, a_enableAnyState);
        }

        public void Initialize(State a_initState, bool a_enableAnyState = true)
        {
            CurrentState = a_initState;
            AnyStateEnabled = a_enableAnyState;
            anyState = default;
        }


        public bool Update(bool a_autoInvokeConditions = false)
        {
            bool _results = false;

            if(CurrentState != null && CurrentState.StateIsValid)
            {
                CurrentState.OnStateUpdate();

                Transition _nextTransition = default;
                bool _isFromAnyState = false;

                if (AnyStateEnabled)
                {
                    _isFromAnyState = true;
                    _nextTransition = GetNextValidOrAnyTransition(CurrentState, a_autoInvokeConditions);
                }
                else
                {
                    CurrentState.GetNextValidTransition(a_autoInvokeConditions);
                }

                _results = PerformTransition(_nextTransition, _isFromAnyState);
            }

            return _results;
        }


        private bool PerformTransition(Transition a_transition, bool a_isFromAnyState = false)
        {
            bool _validTransition = a_transition.TransitionIsValid;
            bool _isToAnyState = a_transition.TargetState == AnyState;    // transitions to the anyState are not valid/supported
            bool _results = _validTransition && !_isToAnyState;

            if (_results)
            {
                CurrentState.OnStateExit?.Invoke();
                CurrentState = a_transition.TargetState;
                CurrentState.OnStateEnter?.Invoke();
            }

            if(a_isFromAnyState && AnyStateEnabled)
            {
                AnyState?.OnStateEnter?.Invoke();
                AnyState?.OnStateUpdate?.Invoke();
                AnyState?.OnStateExit?.Invoke();
            }

            return _results;
        }


        private Transition GetNextValidOrAnyTransition(State a_state, bool a_autoInvokeConditions)
        {
            Transition _nextTransition = a_state.GetNextValidTransition(a_autoInvokeConditions);

            _nextTransition = _nextTransition.TransitionIsValid
                ? _nextTransition
                : GetAnyStateTransition(a_autoInvokeConditions);

            return _nextTransition;
        }


        private Transition GetAnyStateTransition(bool a_autoInvokeConditions = false)
        {
            if (AnyStateEnabled == false)
            {
                return default;
            }

            Transition _targetTransition = AnyState.GetNextValidTransition(a_autoInvokeConditions);

            return _targetTransition;
        }
        // end StateMachine Class // ---------- ---------- ---------- 




        public sealed class State
        {
            private Action onStateEnter;
            public Action OnStateEnter
            {
                get
                {
                    return onStateEnter;
                }
                set
                {
                    onStateEnter = value;
                }
            }


            private Action onStateUpdate;
            public Action OnStateUpdate
            {
                get
                {
                    return onStateUpdate;
                }
                set
                {
                    onStateUpdate = value;
                }
            }


            private Action onStateExit;
            public Action OnStateExit
            {
                get
                {
                    return onStateExit;
                }
                set
                {
                    onStateExit = value;
                }
            }


            private Transition[] availableTransitions;
            public Transition[] AvailableTransitions
            {
                get
                {
                    return availableTransitions;
                }
                set
                {
                    availableTransitions = value;
                }
            }


            public bool StateIsValid => this != default;




            public State()
            {
                OnStateEnter = default;
                OnStateUpdate = default;
                OnStateExit = default;
            }


            public State(Action a_onEnter, Action a_onUpdate, Action a_onExit)
            {
                OnStateEnter = a_onEnter;
                OnStateUpdate = a_onUpdate;
                OnStateExit = a_onExit;
            }


            public Transition GetNextValidTransition(bool a_invokeConditions = false)
            {
                Transition _results = default;

                foreach(Transition _potentialTransition in AvailableTransitions)
                {
                    if(_potentialTransition.CheckConditionResults(a_invokeConditions))
                    {
                        _results = _potentialTransition;
                        break;
                    }
                }

                return _results;
            }
        }
        // end State class // ---------- ---------- ---------- 


        public sealed class Transition
        {
            private State targetState;
            public State TargetState
            {
                get
                {
                    return targetState;
                }
                private set
                {
                    targetState = value;
                }
            }


            private Condition[] transitionConditions;
            public Condition[] TransitionConditions
            {
                get
                {
                    return transitionConditions;
                }
                private set
                {
                    transitionConditions = value;
                }
            }


            public bool TransitionIsValid => this != default;



            public Transition(State a_targetState, Condition[] a_conditions)
            {
                TargetState = a_targetState;
                TransitionConditions = a_conditions;
            }


            public bool CheckConditionResults(bool a_invokeConditions = false)
            {
                bool _results = a_invokeConditions
                    ? TransitionConditions.All(_cond => _cond.InvokeCheckConditionResults())
                    : TransitionConditions.All(_cond => _cond.LastCheckedResult);

                return _results;
            }
        }
        // end Transition class ---------- ---------- ---------- 



        public sealed class Condition
        {
            private bool lastCheckedResult;
            public bool LastCheckedResult
            {
                get
                {
                    return lastCheckedResult;
                }
                private set
                {
                    lastCheckedResult = value;
                }
            }


            private Func<bool> conditionDelegate = default;



            public Condition(Func<bool> a_condition)
            {
                conditionDelegate = a_condition == null ? default : a_condition;
            }


            public bool InvokeCheckConditionResults()
            {
                bool _results  =
                conditionDelegate.GetInvocationList().All(a_condition =>
                {
                    return a_condition is Func<bool> _fb && _fb.Invoke();
                });

                LastCheckedResult = _results;

                return _results;
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


            private Transition toDead;
            private Transition toHome;
            private Transition toLeaving;
            private Transition toFlee;
            private Transition toChase;
            private Transition toWander;

            bool ChaseZacc()
            {
                return true;
            }

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
                isChase = (Condition)new[] { chaseZac, chaseZac, chaseZac, ChaseTarget, ChaseZacc };
                isFlee = new Condition(() => { return edible; });
                notFlee = new Condition(() => { return !edible; });

              
                toHome = new Transition(sAtHome, new Condition[]{ isHome });
                toLeaving = new Transition(sAtHome, new Condition[]{ isLeaving });
                toWander = new Transition(sWander, new Condition[] { notFlee, isWander });
                toChase = new Transition(sChasing, new Condition[] {notFlee, isChase });
                toFlee = new Transition(sFleeing, new Condition[] { isFlee });

                ghostSM.anyState.AvailableTransitions = new Transition[] { toFlee };
                sInit.AvailableTransitions = new Transition[] { toHome };
                sAtHome.AvailableTransitions = new Transition[] { toLeaving };
                sLeaving.AvailableTransitions = new Transition[] { toChase, toWander };
                sWander.AvailableTransitions = new Transition[] { toChase, toFlee };
                sChasing.AvailableTransitions = new Transition[] { toWander, toFlee };
                sFleeing.AvailableTransitions = new Transition[] { toDead, toChase, toWander };
                sDead.AvailableTransitions = new Transition[] { toHome };


                sAtHome.OnStateEnter += () => { homeTimer = 8f; };
                sAtHome.OnStateUpdate += () => { homeTimer -= 0.016f; };

                sLeaving.OnStateEnter += () => { /*do animation*/ };

                sWander.OnStateUpdate += () => { /*do path-finding*/ };
                sChasing.OnStateUpdate += () => { /*do different path-finding*/ };
                sFleeing.OnStateUpdate += () => { /*do backwards path-finding*/ };

                sDead.OnStateEnter += () => { /*sprite swap*/ };
                sDead.OnStateUpdate += () => { /*go home*/ };
                sDead.OnStateExit += () => { /*sprite swap*/ };

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

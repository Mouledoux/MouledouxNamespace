using System;
using System.Linq;

namespace Mouledoux.Components
{
public sealed class StateMachine
    {
        private State _currentState;
        public State CurrentState { get; private set; }

        private State _anyState;
        public State AnyState { get; private set; }

        private bool _anyStateEnabled = true;
        public bool AnyStateEnabled { get; private set; }

        private Transition _nextPotentialTransition;

        public StateMachine(State initialState, bool enableAnyState = true)
        {
            Initialize(initialState, enableAnyState);
        }

        public void Initialize(State initialState, bool enableAnyState = true)
        {
            CurrentState = initialState;
            AnyStateEnabled = enableAnyState;
            _anyState = new State();
        }

        public bool Update()
        {
            _nextPotentialTransition = null;
            bool isFromAnyState = false;
            bool results = false;

            if (CurrentState != null && CurrentState.StateIsValid)
            {
                CurrentState.TryInvokeOnUpdate();

                if (AnyStateEnabled)
                {
                    AnyState.TryInvokeOnUpdate();
                    _nextPotentialTransition = GetNextValidOrAnyTransition(CurrentState);
                    isFromAnyState = _nextPotentialTransition != default;
                }
                else
                {
                    _nextPotentialTransition = CurrentState.GetNextValidTransition();
                }

                results = TryPerformTransition(_nextPotentialTransition, isFromAnyState);
            }

            return results;
        }

        private bool TryPerformTransition(Transition transition, bool isFromAnyState = false)
        {
            if (transition == null || transition == default)
            {
                return false;
            }

            bool validTransition = transition.EvaluateConditions();
            bool isToAnyState = transition.TargetState == AnyState;

            bool results = validTransition && !isToAnyState;

            if (results)
            {
                CurrentState.TryInvokeOnExit();
                CurrentState = transition.TargetState;
                CurrentState.TryInvokeOnEnter();

                if (AnyStateEnabled)
                {
                    AnyState.TryInvokeOnExit();
                    AnyState.TryInvokeOnEnter();
                }
            }

            return results;
        }

        private Transition GetNextValidOrAnyTransition(State state)
        {
            Transition nextTransition = state.GetNextValidTransition();

            if (nextTransition != null || nextTransition != default)
                return nextTransition.EvaluateConditions() ? nextTransition : GetAnyStateTransition();

            return nextTransition;
        }

        private Transition GetAnyStateTransition()
        {
            if (!AnyStateEnabled)
            {
                return default;
            }

            return AnyState.GetNextValidTransition();
        }

        private void ClearMachine()
        {
            CurrentState = null;
            AnyState = null;
            _nextPotentialTransition = null;
        }
        // end StateMachine Class // ---------- ---------- ---------- 




        public sealed class State : IDisposable
        {
            private List<WeakReference<Transition>> _transitionReferences;

            private WeakReference<Action> OnEnterActionReference;
            private WeakReference<Action> OnUpdateActionReference;
            private WeakReference<Action> OnExitActionReference;

            public bool StateIsValid => this != default;

            public State()
            {
                _transitionReferences = new List<WeakReference<Transition>>();
            }

            public void SetTransitions(Transition[] transitions)
            {
                _transitionReferences.Clear();
                foreach (var transition in transitions)
                {
                    _transitionReferences.Add(new WeakReference<Transition>(transition));
                }
            }

            public void AddTransition(Transition transition)
            {
                if (transition != null)
                {
                    _transitionReferences.Add(new WeakReference<Transition>(transition));
                }
            }

            public void RemoveTransition(Transition transition)
            {
                if (transition != null)
                {
                    var weakRef = _transitionReferences.FirstOrDefault(t => t.TryGetTarget(out var target) && target == transition);
                    if (weakRef != null && weakRef != default)
                    {
                        _transitionReferences.Remove(weakRef);
                    }
                }
            }

            public void AddOnEnterListener(Action action)
            {
                if(action == null)
                {
                    return;
                }

                Action onEnterAction;

                if(OnEnterActionReference == null)
                {
                    OnEnterActionReference = new WeakReference<Action>(action);
                }

                else if (OnEnterActionReference.TryGetTarget(out onEnterAction))
                {
                    onEnterAction += action;
                    OnEnterActionReference.SetTarget(onEnterAction);
                }
            }

            public void AddOnUpdateListener(Action action)
            {
                if (action == null)
                {
                    return;
                }

                Action onUpdateAction;

                if (OnUpdateActionReference == null)
                {
                    OnUpdateActionReference = new WeakReference<Action>(action);
                }

                else if (OnUpdateActionReference.TryGetTarget(out onUpdateAction))
                {
                    onUpdateAction += action;
                    OnUpdateActionReference.SetTarget(onUpdateAction);
                }
            }

            public void AddOnExitListener(Action action)
            {
                if (action == null)
                {
                    return;
                }

                Action onExitAction;

                if (OnExitActionReference == null)
                {
                    OnExitActionReference = new WeakReference<Action>(action);
                }

                else if (OnExitActionReference.TryGetTarget(out onExitAction))
                {
                    onExitAction += action;
                    OnExitActionReference.SetTarget(onExitAction);
                }
            }

            public void TryInvokeOnEnter()
            {
                if(OnEnterActionReference == null)
                {
                    return;
                }

                Action onEnterAction;
                if (OnEnterActionReference.TryGetTarget(out onEnterAction))
                {
                    onEnterAction?.Invoke();
                }
            }

            public void TryInvokeOnUpdate()
            {
                if (OnUpdateActionReference == null)
                {
                    return;
                }

                Action onUpdateAction;
                if (OnUpdateActionReference.TryGetTarget(out onUpdateAction))
                {
                    onUpdateAction?.Invoke();
                }
            }

            public void TryInvokeOnExit()
            {
                if (OnExitActionReference == null)
                {
                    return;
                }

                Action onExitAction;
                if (OnExitActionReference.TryGetTarget(out onExitAction))
                {
                    onExitAction?.Invoke();
                }
            }
            public Transition GetNextValidTransition()
            {
                foreach (var weakRef in _transitionReferences)
                {
                    if (weakRef.TryGetTarget(out var transition) && transition.EvaluateConditions())
                    {
                        return transition;
                    }
                }
                return null;
            }

            public void ClearState()
            {
                OnEnterActionReference = null;
                OnUpdateActionReference = null;
                OnExitActionReference = null;
                _transitionReferences.Clear();
            }

            public void Dispose()
            {
                ClearState();
            }
        }
        // end State class // ---------- ---------- ---------- 


        public sealed class Transition : IDisposable
        {
            private WeakReference<State> _targetStateReference;
            private List<WeakReference<Condition>> _transitionConditionReferences;

            public State TargetState
            {
                get
                {
                    State targetState;
                    _targetStateReference.TryGetTarget(out targetState);
                    return targetState;
                }
            }
            public bool TryGetTargetState(out State targetState)
            {
               return _targetStateReference.TryGetTarget(out targetState);
            }

            public Transition(State targetState, Condition[] conditions)
            {
                if (targetState == null)
                {
                    throw new ArgumentNullException(nameof(targetState));
                }

                if (conditions == null || conditions.Length == 0)
                {
                    throw new ArgumentException("At least one condition must be provided.", nameof(conditions));
                }

                _targetStateReference = new WeakReference<State>(targetState);
                _transitionConditionReferences = conditions.Select(c => new WeakReference<Condition>(c)).ToList();
            }

            public bool EvaluateConditions()
            {
                foreach (var conditionRef in _transitionConditionReferences)
                {
                    Condition condition;
                    if (conditionRef.TryGetTarget(out condition))
                    {
                        if (!condition.Evaluate())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // a referenced condition object has been deleted
                        return false;
                    }
                }
                return true; // All conditions passed
            }

            public IReadOnlyList<Condition> TransitionConditions
            {
                get
                {
                    List<Condition> conditions = new List<Condition>();
                    foreach (var conditionRef in _transitionConditionReferences)
                    {
                        Condition condition;
                        if (conditionRef.TryGetTarget(out condition))
                        {
                            conditions.Add(condition);
                        }
                    }
                    return conditions;
                }
            }

            public void Dispose()
            {
                _targetStateReference = null;
                _transitionConditionReferences.Clear();
            }

        }
        // end Transition class ---------- ---------- ---------- 


        public sealed class Condition : IDisposable
        {
            private WeakReference<Func<bool>> _conditionDelegateReference;

            public Condition(Func<bool> condition)
            {
                if (condition == null)
                {
                    throw new ArgumentNullException(nameof(condition));
                }

                _conditionDelegateReference = new WeakReference<Func<bool>>(condition);
            }

            public bool Evaluate()
            {
                Func<bool> conditionDelegate;
                if (_conditionDelegateReference.TryGetTarget(out conditionDelegate))
                {
                    return conditionDelegate.Invoke();
                }
                else
                {
                    return false;
                }
            }

			private void ClearCondition()
			{
				_conditionDelegateReference = null;
			}

            public static explicit operator Condition(Func<bool> conditionDelegate)
            {
                return new Condition(conditionDelegate);
            }

            public static explicit operator Func<bool>(Condition condition)
            {
                Func<bool> conditionDelegate;
                if (condition._conditionDelegateReference.TryGetTarget(out conditionDelegate))
                {
                    return conditionDelegate;
                }
                else
                {
                    return () => false;
                }
            }

            public void Dispose()
            {
                _conditionDelegateReference = null;
            }

        }
        // end Condition class ---------- ---------- ---------- 
    }



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

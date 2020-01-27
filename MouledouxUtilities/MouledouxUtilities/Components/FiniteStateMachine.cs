namespace Mouledoux.Components
{
    public class FiniteStateMachine<T>
    {
        /// <summary>
        /// Constructor for the FSM
        /// </summary>
        /// 
        /// <param name="initState">Initial state for the object</param>
        /// 
        /// Notes: This constructor ensures there is atleast 1 state for the machine at the begining
        public FiniteStateMachine(T initState)
        {
            AddState(m_anyState);
            AddState(initState);
            m_currentState = initState;
        }


        /// <summary>
        /// Returns the current state of the object
        /// </summary>
        /// 
        /// <returns>
        /// Returns the current state
        /// </returns>
        public T GetCurrentState()
        {
            return m_currentState;
        }


        /// <summary>
        /// Adds a state to the list of possible states
        /// </summary>
        /// 
        /// <param name="aState">State to be added to the list of possible states</param>
        /// 
        /// <returns>
        /// 0 the state was added to the list,
        /// 1 the state already exist,
        /// -1 the state already exist as the "any" state
        /// </returns>
        public int AddState(T aState)
        {
            if (aState.ToString().ToLower() == m_anyState.ToString().ToLower())
                return -1;

            if (m_states.Contains(aState))
                return 1;

            m_states.Add(aState);
            return 0;
        }


        /// <summary>
        /// Removes a state from the list of possible states,
        /// and all transitions involving the state
        /// </summary>
        /// 
        /// <param name="aState">State to be Removed from the list of possible states</param>
        /// 
        /// <returns>
        /// 0 the state(and transitions) were removed from the list(s),
        /// 1 the state did not exist, or is the "any" state
        /// -1 the object is currently in that state
        /// </returns>
        public int RemoveState(T aState)
        {
            if (m_currentState.ToString().ToLower() == aState.ToString().ToLower())
                return -1;

            if (!m_states.Contains(aState) || aState.ToString().ToLower() == m_anyState.ToString().ToLower())
                return 1;

            foreach(T bState in m_states)
            {
                if(CheckTransition(aState, bState))
                    RemoveTransition(aState, bState);

                if (CheckTransition(bState, aState))
                    RemoveTransition(bState, aState);
            }
            
            m_states.Remove(aState);
            return 0;
        }


        /// <summary>
        /// Adds a transition of 2 states to the list of valid transitions,
        /// and a delegate to be invoked on successful transition
        /// </summary>
        /// 
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object will be transitioning to</param>
        /// <param name="aHandler">Delegate to be invoked on successful transition</param>
        /// 
        /// <returns>
        /// 0 the transition was successfully added to the list,
        /// 1 the transition already exists,
        /// -1 either state is invalid
        /// </returns>
        public int AddTransition(T aState, T bState, System.Delegate aHandler)
        {
            if (!m_states.Contains(aState) || !m_states.Contains(bState))
                return -1;

            string transitionKey = aState.ToString() + "->" + bState.ToString();
            transitionKey = transitionKey.ToLower();

            if (m_transitions.ContainsKey(transitionKey))
                return 1;

            m_transitions.Add(transitionKey, aHandler);
            return 0;
        }


        public int AddTransitionFromAnyTo(T bState, System.Delegate aHandler)
        {
            return 1;
        }

        public int AddTransitionToAnyFrom(T aState)
        {
            return 1;
        }

        /// <summary>
        /// Removes a transition of 2 states from the list of valid transitions
        /// </summary>
        /// 
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object would be transitioning to</param>
        /// 
        /// <returns>
        /// 0 the transition was successfully removed from the list,
        /// 1 the transition did not exist,
        /// -1 the states are invalid
        /// </returns>
        public int RemoveTransition(T aState, T bState)
        {
            if (!m_states.Contains(aState) || !m_states.Contains(bState))
                return -1;

            string transitionKey = aState.ToString() + "->" + bState.ToString();
            transitionKey = transitionKey.ToLower();

            if (!m_transitions.ContainsKey(transitionKey))
                return 1;

            m_transitions.Remove(transitionKey);
            return 0;
        }


        /// <summary>
        /// Checks if a transition between 2 states is valid
        /// </summary>
        /// 
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object would be transitioning to</param>
        /// 
        /// <returns>
        /// TRUE the transition is valid,
        /// FALSE the transition is not valid
        /// </returns>
        public bool CheckTransition(T aState, T bState)
        {
            string transitionKey = aState.ToString() + "->" + bState.ToString();
            transitionKey = transitionKey.ToLower();

            return (m_transitions.ContainsKey(transitionKey));
        }


        /// <summary>
        /// Makes all valid transitions and their handler invokes
        /// </summary>
        /// 
        /// <param name="bState">State to transition to from the current</param>
        /// 
        /// <returns>
        /// 0 the transition was successful,
        /// 1 it's not a valid transition,
        /// -1 if the new state is invalid
        /// </returns>
        public int MakeTransitionTo(T bState)
        {
            if (!m_states.Contains(bState))
                return -1;

            string transitionKey = m_currentState.ToString() + "->" + bState.ToString();
            transitionKey = transitionKey.ToLower();

            if (!m_transitions.ContainsKey(transitionKey))
                return 1;

            m_transitions[transitionKey].DynamicInvoke();
            m_currentState = bState;

            return 0;
        }



        // Variables //////////
        #region Variables      
        /// <summary>
        /// The current state of the object
        /// </summary>
        private T m_currentState;

        /// <summary>
        /// Empty state for arbitrary transitions
        /// </summary>
        private T m_anyState;

        /// <summary>
        /// List of possible states
        /// </summary>
        private System.Collections.Generic.List<T> m_states =
            new System.Collections.Generic.List<T>();

        /// <summary>
        /// Dictionary of transitions with associated transition handlers
        /// </summary>
        private System.Collections.Generic.Dictionary<string, System.Delegate> m_transitions =
            new System.Collections.Generic.Dictionary<string, System.Delegate>();
        #endregion
    }




    public sealed class SuperiorStateMachine<T>
    {
        private T _currentState;
        public T currentState
        {
            public get { return _currentState; }

            private set
            {
                _currentState = value;
                availableTransitions.Clear();

                foreach (Transition t in allTransitions.Keys)
                {
                    if (value.Equals(t.GetAState()))
                        availableTransitions.Add(t);
                }
            }
        }

        private System.Collections.Generic.Dictionary<Transition, System.Action> allTransitions =
            new System.Collections.Generic.Dictionary<Transition, System.Action>();

        private System.Collections.Generic.List<Transition> availableTransitions =
            new System.Collections.Generic.List<Transition>();


        public void ProcessTransitions()
        {
            foreach (Transition t in availableTransitions)
            {
                if (t.CheckPreRequisits())
                {
                    allTransitions[t].Invoke();
                    currentState = t.GetBState();
                    return;
                }
            }
        }


        public void AddTransition(T _aState, T _bState, System.Func<bool>[] _preReqs, System.Action _onTransition)
        {
            Transition Transition = new Transition(_aState, _bState, _preReqs);
            AddTransition(Transition, _onTransition);
        }

        private int AddTransition(Transition _newTransition, System.Action _onTransition)
        {
            allTransitions.Add(_newTransition, _onTransition);
            return 0;
        }


        private sealed class Transition
        {
            private T aState;
            public T GetAState()
            {
                return aState;
            }

            private T bState;
            public T GetBState()
            {
                return bState;
            }

            private System.Func<bool>[] preReqs;

            public Transition(T _aState, T _bState, System.Func<bool>[] _preReqs)
            {
                aState = _aState;
                bState = _bState;

                preReqs = _preReqs;
            }

            public bool CheckPreRequisits()
            {
                bool passPreReqs = true;

                foreach (System.Func<bool> pr in preReqs)
                {
                    passPreReqs = passPreReqs & pr.Invoke();
                }
                return passPreReqs;
            }
        }



        /* Example Usage

        public class PlayerClass
        {
            public int health;
            public int mana;
            public int experience;
            public int experienceToNextLevel;

            SuperiorStateMachine<string> playerFSM = new SuperiorStateMachine<string>();



            public bool IsAlive()
            {
                return health > 0;
            }
            public bool CanLevelUp()
            {
                return experience >= experienceToNextLevel;
            }

            private void KillPlayer()
            {
                // do something
            }
            private void LevelUp()
            {
                health += 10;
                mana += 15;
                experience = 0;
                experienceToNextLevel += (int)((float)experienceToNextLevel * 0.2f);
            }

            public void Initialize()
            {
                playerFSM.AddTransition("idle", "levelUp",
                new System.Func<bool>[]
                {
                        IsAlive,        // Since both of these return true when we need them to,
                        CanLevelUp,     // They can just be passed as a method group
                },
                () => LevelUp());


                playerFSM.AddTransition("idle", "dead",
                new System.Func<bool>[]
                {
                        new System.Func<bool>(() => !IsAlive()),    // But if we WANT them to be false,
                                                                    // they need to be made into a new method group
                },
                () => KillPlayer());


                playerFSM.AddTransition("levelUp", "idle",
                new System.Func<bool>[]
                {
                    // A Transition doesnt have to have requirements,
                    // but it will make the reansistion immeadetly
                },
                null);  // They also dont have to do anything when the Transition occurs                                          
            }
        }
        */

    }
}
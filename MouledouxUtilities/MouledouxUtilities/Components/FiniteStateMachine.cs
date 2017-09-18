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
        /// <returns>Returns the current state</returns>
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
        /// Returns 1 if state was added to the list,
        /// 0 if it already exist,
        /// and -1 if it already exist as the "any" state
        /// </returns>
        public int AddState(T aState)
        {
            if (aState.ToString().ToLower() == m_anyState.ToString().ToLower())
                return -1;

            if (m_states.Contains(aState))
                return 0;

            m_states.Add(aState);
            return 1;
        }


        /// <summary>
        /// Removes a state from the list of possible states,
        /// and all transistions involving the state
        /// </summary>
        /// 
        /// <param name="aState">State to be Removed from the list of possible states</param>
        /// 
        /// <returns>
        /// Returns 1 if the state(and transistions) was removed from the list(s),
        /// 0 if the state did not exist, or is the "any" state
        /// and -1 if the object is currently in that state
        /// </returns>
        public int RemoveState(T aState)
        {
            if (m_currentState.ToString().ToLower() == aState.ToString().ToLower())
                return -1;

            if (!m_states.Contains(aState) || aState.ToString().ToLower() == m_anyState.ToString().ToLower())
                return 0;

            foreach(T bState in m_states)
            {
                if(CheckTransition(aState, bState))
                    RemoveTransition(aState, bState);

                if (CheckTransition(bState, aState))
                    RemoveTransition(bState, aState);
            }
            
            m_states.Remove(aState);
            return 1;
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
        /// Returns 1 if the transition was successfuly added to the list,
        /// 0 if it already exists,
        /// and -1 if either state is invalid
        /// </returns>
        public int AddTransition(T aState, T bState, System.Delegate aHandler)
        {
            if (!m_states.Contains(aState) || !m_states.Contains(bState))
                return -1;

            string transistionKey = aState.ToString() + "->" + bState.ToString();
            transistionKey = transistionKey.ToLower();

            if (m_transitions.ContainsKey(transistionKey))
                return 0;

            m_transitions.Add(transistionKey, aHandler);
            return 1;
        }


        public int AddTransistionFromAnyTo(T bState, System.Delegate aHandler)
        {
            return 1;
        }

        public int AddTransistionToAnyFrom(T aState)
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
        /// Returns 1 if the transition was successfuly removed from the list,
        /// 0 if it did not exist,
        /// and -1 if the states are invalid
        /// </returns>
        public int RemoveTransition(T aState, T bState)
        {
            if (!m_states.Contains(aState) || !m_states.Contains(bState))
                return -1;

            string transistionKey = aState.ToString() + "->" + bState.ToString();
            transistionKey = transistionKey.ToLower();

            if (!m_transitions.ContainsKey(transistionKey))
                return 0;

            m_transitions.Remove(transistionKey);
            return 1;
        }


        /// <summary>
        /// Checks if a transition between 2 states is valid
        /// </summary>
        /// 
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object would be transistioning to</param>
        /// 
        /// <returns>Returns TRUE if the transistion is valid,
        /// and FALSE if the transistion is not valid
        /// </returns>
        public bool CheckTransition(T aState, T bState)
        {
            string transistionKey = aState.ToString() + "->" + bState.ToString();
            transistionKey = transistionKey.ToLower();

            return (m_transitions.ContainsKey(transistionKey));
        }


        /// <summary>
        /// Makes all valid transistions and their handeler invokes
        /// </summary>
        /// 
        /// <param name="bState">State to transistion to from the currrent</param>
        /// 
        /// <returns>
        /// Returns 1 if the transistion was successfun,
        /// 0 if it was not a valid transistion,
        /// and -1 if the new state is invalid
        /// </returns>
        public int MakeTransistionTo(T bState)
        {
            if (!m_states.Contains(bState))
                return -1;

            string transistionKey = m_currentState.ToString() + "->" + bState.ToString();
            transistionKey = transistionKey.ToLower();

            if (!m_transitions.ContainsKey(transistionKey))
                return 0;

            m_transitions[transistionKey].DynamicInvoke();
            m_currentState = bState;

            return 1;
        }

        
        
        // Vairables //////////
         
        /// <summary>
        /// The current state of the object
        /// </summary>
        private T m_currentState;

        /// <summary>
        /// Empty state for arbituary transistions
        /// </summary>
        private T m_anyState;

        /// <summary>
        /// List of possible states
        /// </summary>
        private System.Collections.Generic.List<T> m_states =
            new System.Collections.Generic.List<T>();

        /// <summary>
        /// Dictionary of transistions with associated transistion handlers
        /// </summary>
        private System.Collections.Generic.Dictionary<string, System.Delegate> m_transitions =
            new System.Collections.Generic.Dictionary<string, System.Delegate>();
    }
}

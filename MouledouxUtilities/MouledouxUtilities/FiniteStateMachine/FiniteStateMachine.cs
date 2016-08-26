namespace Mouledoux.Components
{
    /// <summary>
    /// Interface for the Finite STate Machine
    /// </summary>
    /// <typeparam name="I"></typeparam>
    internal interface IFiniteStateMachine<I>
    {
        /// <summary>
        /// Returns the current state of the object
        /// </summary>
        /// <returns></returns>
        I GetCurrentState();

        /// <summary>
        /// Adds a state to the list of possible states
        /// </summary>
        /// <param name="aState">State to be added to the list of possible states</param>
        /// <returns>Returns 1 if state was added to the list, and 0 if it already exists</returns>
        int AddState(I aState);

        /// <summary>
        /// Removes a state from the list of possible states and all transistions involving the state
        /// </summary>
        /// <param name="aState">State to be Removed from the list of possible states</param>
        /// <returns>Returns 1 if the state(and transistions) was removed from the list(s), and 0 if it did not exist</returns>
        int RemoveState(I aState);

        /// <summary>
        /// Adds a transition of 2 states to the list of valid transitions and a delegate to be invoked on successful transition
        /// </summary>
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object will be transitioning to</param>
        /// <param name="aHandler">Delegate to be invoked on successful transition</param>
        /// <returns>Returns 1 if the transition was successfuly added to the list, 0 if it already exists, and -1 if the states are invalid</returns>
        int AddTransition(I aState, I bState, System.Delegate aHandler);

        /// <summary>
        /// Removes a transition of 2 states from the list of valid transitions
        /// </summary>
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object would be transitioning to</param>
        /// <returns>Returns 1 if the transition was successfuly removed from the list, 0 if it did not exist, and -1 if the states are invalid</returns>
        int RemoveTransition(I aState, I bState);

        /// <summary>
        /// Checks if a transition between 2 states is valid
        /// </summary>
        /// <param name="aState">State the object would be starting in</param>
        /// <param name="bState">State the object would be transistioning to</param>
        /// <returns>Returns TRUE if the transistion is valid, and FALSE if the transistion is not valid</returns>
        bool CheckTransition(I aState, I bState);

        /// <summary>
        /// Makes all valid transistions and their handeler invokes
        /// </summary>
        /// <param name="bState">State to transistion to from the currrent</param>
        /// <returns>Returns 1 if the transistion was successfun, and 0 if it was not valid</returns>
        int MakeTransistionTo(I bState);
    }

    public class FiniteStateMachine<T> : IFiniteStateMachine<T>
    {
        public FiniteStateMachine(T initState)
        {
            AddState(initState);
            m_currentState = initState;
        }



        public T GetCurrentState()
        {
            return m_currentState;
        }
        


        public int AddState(T aState)
        {
            if (m_states.Contains(aState))
                return 0;

            m_states.Add(aState);
            return 1;
        }



        public int RemoveState(T aState)
        {
            if (!m_states.Contains(aState))
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



        public bool CheckTransition(T aState, T bState)
        {
            string transistionKey = aState.ToString() + "->" + bState.ToString();
            transistionKey = transistionKey.ToLower();

            return (m_transitions.ContainsKey(transistionKey));
        }



        public int MakeTransistionTo(T bState)
        {
            string transistionKey = m_currentState.ToString() + "->" + bState.ToString();
            transistionKey = transistionKey.ToLower();

            if (!m_transitions.ContainsKey(transistionKey))
                return 0;

            m_transitions[transistionKey].DynamicInvoke();
            m_currentState = bState;

            return 1;
        }



        private T m_currentState;

        private System.Collections.Generic.List<T> m_states =
            new System.Collections.Generic.List<T>();

        private System.Collections.Generic.Dictionary<string, System.Delegate> m_transitions =
            new System.Collections.Generic.Dictionary<string, System.Delegate>();
    }
}

namespace Mouledoux.Component
{
    public sealed class SuperiorStateMachine<T>
    {
        private bool initialized = false;
        
        private T _anyState;
        
        private T _currentState;
        public T currentState
        {
            get { return _currentState; }

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

        
        
        public SuperiorStateMachine(T initState, T anyState)
        {
            _currentState = initState;
            _anyState = anyState;
        }
        

        public void ProcessTransitions()
        {
            if(!initialized)
            {
                currentState = currentState;
                initialized = true;
            }
            
            foreach (Transition t in availableTransitions)
            {
                if (t.CheckPreRequisits())
                {
                    currentState = t.GetBState();
                    allTransitions[t].Invoke();
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



        private sealed class EXAMPLE_PacManGhost
        {

        }
    }
}

namespace Mouledoux.Component
{
    public sealed class SuperiorStateMachine<T>
    {
        private bool initialized = false;
        
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



        private sealed class EXAMPLE_PlayerClass
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
    }
}

namespace Mouledoux.Components
{
    public sealed class SuperiorStateMachine<T>
    {
        private bool initialized = false;
        
        private T _anyState;
        
        private T _currentState;
        public T GetCurrentState()
        {
            return _currentState;
        }
        private void SetCurrentState(T newState)
        {
            _currentState = newState;
            availableTransitions.Clear();

            foreach (Transition t in allTransitions.Keys)
            {
                if (t.GetAState().Equals(newState) || t.GetAState().Equals(_anyState))
                {
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
            // You can't start in the AnyState
            //if(initState.Equals(anyState))
              //  Throw
        
            _currentState = initState;
            _anyState = anyState;
        }
        
        public void AddTransition(T _aState, T _bState, System.Func<bool>[] _preReqs, System.Action _onTransition)
        {
            // You can't transition to AnyState, only from
            //if(_bState.Equals(anyState))
              //  Throw
        
            Transition Transition = new Transition(_aState, _bState, _preReqs);
            AddTransition(Transition, _onTransition);
        }
        
        public T Update()
        {
            if(!initialized) Initialize();
            
            Transition transition;
            if(CheckForAvailableTransition(out transition))
            {   
                MakeTransition(transition);
            }
            return _currentState;
        }
        
        
        
        
        private void Initialize()
        {
            SetCurrentState(_currentState);
            initialized = true;
        }

        private void AddTransition(Transition _newTransition, System.Action _onTransition)
        {
            allTransitions.Add(_newTransition, _onTransition);
        }

        private void MakeTransition(Transition transition)
        {
            SetCurrentState(transition.GetBState());
            allTransitions[transition].Invoke();
        }

        private bool CheckForAvailableTransition(out Transition validTransition)
        {
            foreach (Transition transition in availableTransitions)
            {
                if (transition.CheckPrerequisites())
                {
                    validTransition = transition;
                    return true;
                }
            }
            
            validTransition = null;
            return false;
        }





        //  ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
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

            public bool CheckPrerequisites()
            {
                bool passPreReqs = true;

                if (preReqs != null)
                {
                    foreach (System.Func<bool> pr in preReqs)
                    {
                        passPreReqs = passPreReqs & pr.Invoke();
                    }
                }

                return passPreReqs;
            }
        }




        // EXAMPLE // PacMan Ghost ---------- ---------- ---------- ---------- ----------
        //  ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        private sealed class EXAMPLE_PacManGhost
        {
            SuperiorStateMachine<GhostStates> stateMachine =
                new SuperiorStateMachine<GhostStates>(GhostStates.INIT, GhostStates.ANY);

            int currentPosX;
            int currentPosY;

            int targetPosX;
            int targetPosY;

            float movementSpeed;



            public void InitializeGhost()
            {
                System.Func<bool>[] canLeaveHome =
                {
                    //() => !BoardManager.GetTile(BoardManager.GhostDoorPosX, BoardManager.GhostDoorPosY).isOccupied;
                    //GameManager.CheckNextGhostAtHome(this);
                };
                System.Func<bool>[] readyToChase =
                {
                    //() => currentPosX == BoardManager.GhostDoorPosX;
                    //() => currentPosY == BoardManager.GhostDoorPosY;
                };
                System.Func<bool>[] shouldFlee =
                {
                    //() => GameManager.GetState() == GameState.POWER_PILL;
                    //() => !SetTargetTile(GameManager.PacMan.posX, GameManager.PacMan.posY);
                };
                System.Func<bool>[] isDead =
                {
                    //() => GameManager.GetState() == GameState.POWER_PILL;
                    //() => (GameManager.PacMan.posX == currentPosX && GameManager.PacMan.posY == currentPosY;
                };

                System.Action leaveHome = () =>
                {
                    //SetColor(Color.default);
                    //SetTargetTile(BoardManager.GhostDoorPosX, BoardManager.GhostDoorPosY);
                    //GameManager.RemoveGhostFromHome(this);
                };
                System.Action chase = () =>
                {
                    //SetColor(Color.default);
                    SetSpeed(4f);
                };
                System.Action flee = () =>
                {
                    //SetColor(Color.blue);
                    SetSpeed(2f);
                };
                System.Action die = () =>
                {
                    //SetColor(Color.clear);
                    //SetTargetTile(BoardManager.GhostHomePosX, BoardManager.GhostHomePosY);
                    //GamemManager.AddGhostToHome(this);
                };

                stateMachine.AddTransition(GhostStates.INIT, GhostStates.AT_HOME, null, null);
                stateMachine.AddTransition(GhostStates.AT_HOME, GhostStates.LEAVING_HOME, canLeaveHome, leaveHome);
                stateMachine.AddTransition(GhostStates.LEAVING_HOME, GhostStates.CHASING, readyToChase, chase);
                stateMachine.AddTransition(GhostStates.CHASING, GhostStates.FLEEING, shouldFlee, flee);
                stateMachine.AddTransition(GhostStates.FLEEING, GhostStates.CHASING, new System.Func<bool>[] { /*() => GameManager.GetState() == GameState.NORMAL,*/ }, chase);
                stateMachine.AddTransition(GhostStates.FLEEING, GhostStates.DEAD, isDead, die);
                stateMachine.AddTransition(GhostStates.DEAD, GhostStates.LEAVING_HOME, canLeaveHome, leaveHome);

                stateMachine.AddTransition(GhostStates.ANY, GhostStates.RESET, new System.Func<bool>[] { /*() => GameManager.GetState() == GameState.LEVEL_END,*/ }, null);
                stateMachine.AddTransition(GhostStates.RESET, GhostStates.INIT, new System.Func<bool>[] { /*() => GameManager.GetState() == GameState.LEVEL_START,*/ }, null);
            }
            

            void UpdateGhost()
            {
                stateMachine.Update();
                MoveToTargetPosition();
            }

              
            void MoveToTargetPosition()
            {
                // Move to targetPosX and targetPosY
            }
            

            bool SetTargetTile(int x, int y)
            {
                targetPosX = x;
                targetPosY = y;

                return currentPosX == targetPosX && currentPosY == targetPosY;
            }
            
            void SetSpeed(float newSpeed)
            {
                movementSpeed = newSpeed; 
            }

            public enum GhostStates
            {
                ANY,
                INIT,
                AT_HOME,
                LEAVING_HOME,
                CHASING,
                FLEEING,
                DEAD,
                RESET,
            }
        }
    }
}

namespace Mouledoux.Components
{
    public sealed class SuperiorStateMachine<T>
    {
        // Properties // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        #region ----- Properties -----


        /// <summary>
        /// If the SSM has been initialized. Will be set true after first Update call
        /// </summary>
        private bool initialized = false;
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- initialized

        /// <summary>
        /// The current state the machine is in.
        /// </summary>
        public T currentState { get; private set; }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- currentState

        /// <summary>
        /// A predefined T variable to act as a target for abstract transitions
        /// </summary>
        public T anyState { get; private set; }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- anyState


        /// <summary>
        /// Collection of all possible transitions in the machine.
        /// </summary>
        private System.Collections.Generic.Dictionary<Transition, System.Action> allTransitions =
            new System.Collections.Generic.Dictionary<Transition, System.Action>();
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- allTransitions

        /// <summary>
        /// Collection of all possible transitions FROM the currentState
        /// </summary>
        private System.Collections.Generic.List<Transition> availableTransitions =
            new System.Collections.Generic.List<Transition>();
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- availableTransitions


        /// <summary>
        /// A transition to hold global prerequisites to transition FROM the currentState
        /// </summary>
        private Transition anyTransition;
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- anyTransition

        /// <summary>
        /// An action delegate called on a successful transition AFTER the state-specific action delegate
        /// </summary>
        private System.Action onAnyTransition;
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- onAnyTransitions

        #endregion
        // Properties \\ ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------

            

        // Methods // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        #region ----- Methods -----


        /// <summary>
        /// Constructor for SusperiorStateMachine
        /// SMM is NOT initialized in its constructor, and will be done at runtime
        /// </summary>
        /// <param name="a_initState">The state the machine will initialize in</param>
        /// <param name="a_anyState">A state to represent "any" state in an abstract transition</param>
        public SuperiorStateMachine(T a_initState, T a_anyState)
        {
            // You can't start in the AnyState
            if (a_initState.Equals(a_anyState))
            {
                throw new System.NotSupportedException("The INIT, and the ANY state must be different");
            }

            currentState = a_initState;
            anyState = a_anyState;
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- SuperiorStateMachine



        /// <summary>
        /// Sets the currentState, and populates the availableTransitions FROM currentState
        /// </summary>
        /// <param name="a_newState">The new currentState</param>
        private void SetCurrentState(T a_newState)
        {
            currentState = a_newState;
            availableTransitions.Clear();

            anyTransition = null;
            onAnyTransition = null;

            foreach (Transition t in allTransitions.Keys)
            {
                // aState is valid if it is the newState, OR if it is the anyState
                bool aStateValid = t.aState.Equals(a_newState) || t.aState.Equals(anyState);
                // bState is valid if it is NOT the anyState, NOR is it the newState
                bool bStateValid = !t.bState.Equals(anyState) && !t.bState.Equals(a_newState);
                // An 'any' transition is valid if aState is the newState, AND bState is the anyState
                bool anyStateValid = t.aState.Equals(a_newState) && t.bState.Equals(anyState);
                

                if (aStateValid && bStateValid)
                {
                    availableTransitions.Add(t);
                }
                else if (anyStateValid)
                {
                    anyTransition = t;
                    onAnyTransition = allTransitions[t];
                }
            }
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- SetCurrentState



        /// <summary>
        /// Attempt to advance currentState from availableTransitions
        /// </summary>
        /// <returns>Returns the currentState</returns>
        public T Update()
        {
            // Initialize on first cycle
            if (!initialized) Initialize();

            bool canAnyTransition = true;
            Transition transition;

            if (anyTransition != null)
            {
                canAnyTransition = anyTransition.CheckPrerequisites();
            }

            if (GetAvailableTransition(out transition) && canAnyTransition)
            {
                PreformTransition(transition);
            }
            return currentState;
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- Update



        /// <summary>
        /// Initializes the SSM to populate availableTransitions
        /// </summary>
        private void Initialize()
        {
            SetCurrentState(currentState);
            initialized = true;
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- Initialize



        /// <summary>
        /// Defines a transition to be added to the state machine
        /// </summary>
        /// <param name="a_aState">State the machine is transitioning OUT of</param>
        /// <param name="a_bState">State the machine is transitioning IN to</param>
        /// <param name="a_preReqs">Requirements for the transition to happen</param>
        /// <param name="a_onTransition">Action to preform on successful transition</param>
        /// <returns>
        /// 0 = the transition was valid and has been added to the machine
        /// 1 = the transition has matching A and B states and will NOT be added
        /// 2 = the transitions from A to B already exist and will NOT be added
        /// </returns>
        public int AddTransition(T a_aState, T a_bState, System.Func<bool>[] a_preReqs, System.Action a_onTransition)
        {
            Transition transition = new Transition(a_aState, a_bState, a_preReqs);
            return AddTransition(transition, a_onTransition);
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- AddTransition (public)



        /// <summary>
        /// Adds a defined transition to the state machine
        /// </summary>
        /// <param name="a_newTransition">Transition to be added</param>
        /// <param name="a_onTransition">Action to preform on successful transition</param>
        /// <returns>
        /// 0 = the transition was valid and has been added to the machine
        /// 1 = the transition has matching A and B states and will NOT be added
        /// 2 = the transitions from A to B already exist and will NOT be added
        /// </returns>
        private int AddTransition(Transition a_newTransition, System.Action a_onTransition)
        {
            if (a_newTransition.aState.Equals(a_newTransition.bState)) return 1;
            if (allTransitions.ContainsKey(a_newTransition)) return 2;
            allTransitions.Add(a_newTransition, a_onTransition);
            return 0;
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- AddTransition (private)



        /// <summary>
        /// Defines a transition to be removed from the state machine
        /// </summary>
        /// <param name="a_aState">State A of the transition to be removed</param>
        /// <param name="a_bState">State B of the transition to be removed</param>
        public void RemoveTransition(T a_aState, T a_bState)
        {
            Transition transition = new Transition(a_aState, a_bState, null);
            RemoveTransition(transition);
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- RemoveTransition (public)



        /// <summary>
        /// Removes a defined transition from the state machine
        /// </summary>
        /// <param name="a_transition">Transition to be removed</param>
        private void RemoveTransition(Transition a_transition)
        {
            Transition transition = null;
            foreach (Transition t in allTransitions.Keys)
            {
                if (t.Equals(a_transition))
                {
                    transition = t;
                    break;
                }
            }
            if (transition != null)
            {
                allTransitions.Remove(transition);
            }
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- RemoveTransition (private)



        /// <summary>
        /// Preforms a transition, valid or not, and advances the machine to the new state
        /// </summary>
        /// <param name="a_transition">Transition to be preformed</param>
        private void PreformTransition(Transition a_transition)
        {
            allTransitions[a_transition]?.Invoke();
            onAnyTransition?.Invoke();
            SetCurrentState(a_transition.bState);
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- PreformTransition



        /// <summary>
        /// Checks all the requirements for all availableTransitions, and 'outs' one if all preReqs return true
        /// In the event multiple transitions meet requirements, the one with the most preReqs takes priority
        /// </summary>
        /// <param name="a_validTransition">An empty transition to be populated should a valid transition be available</param>
        /// <returns>
        /// true = a transition is available and has ben set to the 'out'
        /// false = no transition has met the preReqs and will not be set to 'out'
        /// </returns>
        private bool GetAvailableTransition(out Transition a_validTransition)
        {
            a_validTransition = new Transition(anyState, anyState, null);

            foreach (Transition transition in availableTransitions)
            {
                if (transition.CheckPrerequisites())
                {
                    a_validTransition = transition.preReqCount > a_validTransition.preReqCount ? transition : a_validTransition;
                }
            }

            return !a_validTransition.aState.Equals(a_validTransition.bState);
        }
        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- GetAvailableTransition


        #endregion
        // Methods \\  ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------




        // Transition class // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------
        #region Transition


        /// <summary>
        /// Container for state transitons and their required conditions
        /// </summary>
        private sealed class Transition : System.IEquatable<Transition>
        {
            public T aState { get; private set; }
            public T bState { get; private set; }


            private System.Func<bool>[] preReqs;
            public int preReqCount => preReqs != null ? preReqs.Length : 0;


            public Transition(T a_aState, T a_bState, System.Func<bool>[] a_preReqs)
            {
                aState = a_aState;
                bState = a_bState;

                preReqs = a_preReqs;
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

            public bool Equals(Transition a_other)
            {
                return aState.Equals(a_other.aState) && bState.Equals(a_other.bState);
            }
        }

        #endregion
        // Transition class \\ ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------




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

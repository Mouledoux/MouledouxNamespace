/*  Created by: Eric Mouleoux
*   Contact: EricMouledoux@gmail.com
*   
*   Summary:
*   
*    
*   Usage:
*  
*  
*   Notes:
*/

namespace Mouledoux.Components
{
    /// <summary>
    /// Static class for all mediation.
    /// </summary>
    public sealed class Mediator
    {
        /// The below code is a standard singleton
        #region Singleton
        private Mediator() { }

        private static Mediator _instance;

        public static Mediator instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Mediator();
                }

                return _instance;
            }
        }
        #endregion Singleton

        /// <summary>
        /// Callback action to be used by all subscribers
        /// </summary>
        /// <param name="data">Predefined data Packet to act as potential arguments for subscriptions</param>
        public delegate void Callback(Packet data);

        /// <summary>
        /// Dictionary of subscription strings and associated delegate callbacks
        /// </summary>
        private System.Collections.Generic.Dictionary<string, Callback> subscriptions =
            new System.Collections.Generic.Dictionary<string, Callback>();

        /// <summary>
        /// Checks to see if their are any Subscribers to the broadcasted message
        /// and invokes ALL callbacks associated with it
        /// </summary>
        /// <param name="message">The message to be broadcasted (case sensitive)</param>
        /// <param name="data">Packet of information to be used by ALL recieving parties</param>
        public void NotifySubscribers(string message, Packet data)
        {
            // Temporary delegate container for modifying subscription delegates 
            Callback cb;

            // Check to see if the message has any valid subscriptions
            if (instance.subscriptions.TryGetValue(message, out cb))
            {
                // Invokes ALL associated delegates with the data Packet as the argument
                cb.Invoke(data);
            }
        }



        /// <summary>
        /// Base class for all entities that will be listing for broadcasts
        /// </summary>
        public sealed class Subscriptions
        {
            /// <summary>
            /// Personal, internal record of all active subscriptions
            /// </summary>
            private System.Collections.Generic.Dictionary<string, Callback> localSubscriptions =
                 new System.Collections.Generic.Dictionary<string, Callback>();

            /// <summary>
            /// Links a custom delegate to a message in a SPECIFIC subscription dictionary
            /// </summary>
            /// <param name="container">Refrence to the dictionary of subscriptions we want to modify</param>
            /// <param name="message">The message to subscribe to (case sensitive)</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            private void Subscribe(ref System.Collections.Generic.Dictionary<string, Callback> container, string message, Callback callback)
            {
                // Temporary delegate container for modifying subscription delegates 
                Callback cb;

                // Check to see if there is not already a subscription to this message
                if (!container.TryGetValue(message, out cb))
                {
                    // If there is not, then make one with the message and currently empty callback delegate
                    container.Add(message, cb);
                }

                /// If the subscription does already exist,
                /// then cb is populated with all associated delegates,
                /// if it does not, cb is empty.

                // Add the delegate to cb (new or populated)
                cb += callback;
                // Set the delegate linked to the message to cb
                container[message] = cb;
            }


            /// <summary>
            /// Links a custom delegate to a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to subscribe to (case sensitive)</param>
            /// <param name="callback">The delegate to be linked to the broadcast message</param>
            public void Subscribe(string message, Callback callback)
            {
                // First, adds the subscription to the internaal records
                Subscribe(ref localSubscriptions, message, callback);
                // Then, adds the subcription to the public records
                Subscribe(ref instance.subscriptions, message, callback);
            }


            /// <summary>
            /// Unlinks a custom delegate from a message in a SPECIFIC subscription dictionary
            /// </summary>
            /// <param name="container">Refrence to the dictionary of subscriptions we want to modify</param>
            /// <param name="message">The message to unsubscribe from (case sensitive)</param>
            /// <param name="callback">The delegate to be removed from the broadcast message</param>
            public void Unsubscribe(ref System.Collections.Generic.Dictionary<string, Callback> container, string message, Callback callback)
            {
                // Temporary delegate container for modifying subscription delegates 
                Callback cb;

                // Check to see if there is a subscription to this message
                if (container.TryGetValue(message, out cb))
                {
                    /// If the subscription does already exist,
                    /// then cb is populated with all associated delegates.
                    /// Otherwise nothing will happen

                    // Remove the selected delegate from the callback
                    cb -= callback;

                    // Check the modified cb to see if there are any delegates left
                    if (cb == null)
                    {
                        // If tere is not, then remove the subscription completely
                        container.Remove(message);
                    }
                    else
                    {
                        // If there are some left, reset the callback to the now lesser cb
                        container[message] = cb;
                    }
                }
            }


            /// <summary>
            /// Unlinks a custom delegate from a message that may be breadcasted via a Publisher
            /// </summary>
            /// <param name="message">The message to unsubscribe from (case sensitive)</param>
            /// <param name="callback">The delegate to be unlinked from the broadcast message</param>
            public void Unsubscribe(string message, Callback callback)
            {
                // First, remove the subscription from the internal records
                Unsubscribe(ref localSubscriptions, message, callback);
                // Then, remove the subcription from the public records
                Unsubscribe(ref instance.subscriptions, message, callback);
            }


            /// <summary>
            /// Unlinks all (local) delegates from given broadcast message
            /// </summary>
            /// <param name="message">The message to unsubscribe from (case sensitive)</param>
            public void UnsubcribeAllFrom(string message)
            {
                Unsubscribe(message, localSubscriptions[message]);
            }


            /// !!! IMPORTANT !!! ///
            /// The method below - UnsubscribeAll()
            /// MUST BE CALLED whenever a class inheriting from subscriber is removed
            /// If it is not, you WILL get NULL REFRENCE ERRORS

            /// <summary>
            /// Unlinks all (local) delegates from every (local) broadcast message
            /// </summary>
            public void UnsubscribeAll()
            {
                foreach (string message in localSubscriptions.Keys)
                {
                    Unsubscribe(ref instance.subscriptions, message, localSubscriptions[message]);
                }

                localSubscriptions.Clear();
            }

            ~Subscriptions()
            {
                UnsubscribeAll();
            }
        }
    }



    /// !!! ATTENTION !!!
    /// The below class is a "Data Packet" for sharing useful infomation between publishers and subscribers
    /// It currently only has 4 basic variables for communication
    /// Should you neeed a special data type added, do so at your own risk
    /// While adding or removing any arrays SHOULDN'T cause problems, make sure you make adjustment elsewhere too
    #region Packet Class

    /// <summary>
    /// Collecion of basic variables to be sent via delegates
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// All of the intigers to be used
        /// </summary>
        public int[] ints;
        /// <summary>
        /// All of the boolens to be used
        /// </summary>
        public bool[] bools;
        /// <summary>
        /// All of the floating point numbers to be used
        /// </summary>
        public float[] floats;
        /// <summary>
        /// All of the text strings to be used
        /// </summary>
        public string[] strings;

        /// <summary>
        /// Default constructor
        /// To be used to send empty packets
        /// </summary>
        public Packet()
        {
            this.ints = new int[0];
            this.bools = new bool[0];
            this.floats = new float[0];
            this.strings = new string[0];
        }

        /// <summary>
        /// Constructor to ensure all arrays are set
        /// </summary>
        /// <param name="ints">Predefined array of ints</param>
        /// <param name="bools">Predefined array of bools</param>
        /// <param name="floats">Predefined array of floats</param>
        /// <param name="strings">Predefined array of strings</param>
        public Packet(int[] ints, bool[] bools, float[] floats, string[] strings)
        {
            this.ints = ints;
            this.bools = bools;
            this.floats = floats;
            this.strings = strings;
        }
    }

    #endregion Packet Class




    /// !!! EXAMPLE CLASSES !!! ///
    /// The classes below are for EXAMPLE ONLY
    /// They are internal and sealed so that in the event they are not removed,
    /// they cannot be used or inherited from
    /// 
    /// HOWEVER, they should still be removed or commented out
    /*
    internal sealed class Button
    {
        // ID of the connected object
        string interactionID;

        void Interact()
        {
            Mediator.instance.NotifySubscribers(interactionID, new Packet());
        }


        // The button interaction should be handeled internally
        // For this example we're using the unity trigger collider
        private void OnTriggerEnter(UnityEngine.Collider other)
        {
            Interact();
        }
    }

    internal sealed class Light
    {
        // Unique ID for this door
        string uniqueID;
        // Is the light on?
        bool isOn;
        // What to do when the light is activated
        Mediator.Callback onInteract;

        Mediator.Subscriptions sub = new Mediator.Subscriptions();

        // Constructor
        public Light()
        {
            // Set the door to close by default
            isOn = false;
            // Adds the OpenClose method to the interaction delegate
            onInteract += Toggle;
            // Subscribes to its unique ID
            sub.Subscribe(uniqueID, onInteract);
        }

        void Toggle(Packet p)
        {
            // Inverts the current door state
            isOn = !isOn;
        }

        // For this unity example, a deconstructor will not work
        // So we need to manually unsubscribe on destroy
        private void OnDestroy()
        {
            // We NEED to remove its subscriptions from the records
            sub.UnsubscribeAll();
        }
    }
    //*/
}

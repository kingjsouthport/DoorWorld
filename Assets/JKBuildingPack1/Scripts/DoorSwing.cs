using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JKBuildingPack
{

    /// ------------------------------------------
    /// <summary>
    /// 
    ///     Makes a door swing open when someone
    ///     enters a trigger zone.
    ///     
    ///     Attach this script to the trigger zone 
    ///     (GameObject with Collider.isTrigger
    ///     enabled) -- usually an invisible cube.
    ///     
    ///     When the first-person controller (or
    ///     any rigidbody) enters the zone, the
    ///     door will swing open.
    ///     
    /// </summary>
    /// <remarks>
    /// 
    ///     Updated to work with new Input System
    ///     (as well as old Input Manager).
    /// 
    /// </remarks>
    /// ------------------------------------------
    public class DoorSwing : MonoBehaviour
    {

        [Header("Door")]

        public GameObject door;
        public Handle handle;

        [Header("Movement")]

        public float swingAngle = -120; // in degrees
        public float swingSpeed = 1.5f;
        public Vector3 swingAxis = new Vector3(0, 1, 0);
        public Vector3 doorHinge = new Vector3(-0.5f, 0, 0);
#if ENABLE_INPUT_SYSTEM
        public Key keyboard = Key.None;
#else
        public KeyCode keyboard = KeyCode.None;
#endif
        public bool isOpen = false;
        public bool keepOpen = false;

        [Header("Sound Effects")]

        public AudioClip openingSound;
        public AudioClip closingSound;

        private Vector3 pivotAbsolute;
        private float travel;           // a proportion between 0 and 1
        private float prevTravel;
        private bool wasOpen;
        private bool triggered = false;
        private AudioSource audiosource;
        //public float volume = 1.0f;

        void Start()
        {

            // Work out the hinge position
            if (door) pivotAbsolute = door.transform.TransformPoint(doorHinge);

            // Set up audio
            if (door)
            {
                audiosource = door.AddComponent<AudioSource>();
            }
            else
            {
                audiosource = gameObject.AddComponent<AudioSource>();
            }

            // initialise
            travel = isOpen ? 1 : 0;
            prevTravel = travel;
            wasOpen = isOpen;

        }

        void OnTriggerEnter(Collider other)
        {
            triggered = true;
            if (IsNone(keyboard))
            {
                isOpen = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            triggered = false;
            if (IsNone(keyboard))
            {
                isOpen = false;
            }
        }

        void Update()
        {

            // Check for a keypress
            if (triggered && IsKeyPressed(keyboard))
            {
                isOpen = !isOpen;
            }

            // Override open state if keeping open
            if (keepOpen && wasOpen)
            {
                isOpen = true;
            }

            // Check if the open state has changed
            if (isOpen && !wasOpen)
            {
                if (openingSound)
                {
                    //audiosource.volume = volume;
                    audiosource.PlayOneShot(openingSound);
                }
                handle.Start();
            }
            else if (!isOpen && wasOpen)
            {
                if (closingSound)
                {
                    //audiosource.volume = volume;
                    audiosource.PlayOneShot(closingSound);
                }
            }
            wasOpen = isOpen;

            // Work out where the door should be
            if (isOpen && travel < 1)
            {
                travel += swingSpeed * Time.deltaTime;
                if (travel > 1) travel = 1;
            }
            else if (!isOpen && travel > 0)
            {
                travel -= swingSpeed * Time.deltaTime;
                if (travel < 0)
                {
                    travel = 0;
                    if (handle.turnWhenClosing) handle.Start();
                }
            }
            if (door)
            {
                door.transform.RotateAround(pivotAbsolute, swingAxis, (travel - prevTravel) * swingAngle);
            }
            prevTravel = travel;
            handle.Update();

        }

        /// <summary>
        /// Check if a key has been pressed.
        /// </summary>
        /// <param name="k">Key on keyboard.</param>
        /// <returns>True if pressed; false if not.<returns>
#if ENABLE_INPUT_SYSTEM
        private bool IsKeyPressed(Key k)
        {
            // Check before lookup; current[Key.None] would cause an error
            if (k != Key.None) {     
                return Keyboard.current[k].wasPressedThisFrame;
            }
            return false;
        }
#else
        private bool IsKeyPressed(KeyCode k)
        {
            return Input.GetKeyDown(k);
        }
#endif

        /// <summary>
        /// Check if a key is set to "None".
        /// </summary>
        /// <param name="k">Key on keyboard.</param>
        /// <returns>True if it matches "None"; otherwise false.<returns>
#if ENABLE_INPUT_SYSTEM
        private bool IsNone(Key k)
        {
            return (k == Key.None);
        }
#else
        private bool IsNone(KeyCode k)
        {
            return (k == KeyCode.None);
        }
#endif

        /// <summary>
        /// Handle for door.
        /// </summary>
        [System.Serializable]
        public class Handle : System.Object
        {

            public GameObject handleObject;
            public Vector3 rotation = new Vector3(0, 0, 45);
            public float turnSpeed = 3.0f;
            public float releaseSpeed = 2.0f;
            public bool turnWhenClosing = false;

            private AutoRotator autorotator;

            private float countdown;
            private enum HandleState { Idle, Turn, Unturn };
            private HandleState state = HandleState.Idle;

            public void Start()
            {
                state = HandleState.Turn;
                autorotator = new AutoRotator(handleObject, rotation, turnSpeed, false, true);
            }

            public void Update()
            {
                if (state == HandleState.Turn)
                {
                    autorotator.Update();
                    if (autorotator.Finished())
                    {
                        autorotator = new AutoRotator(handleObject, rotation, releaseSpeed, true, true);
                        state = HandleState.Unturn;
                    }
                }
                else if (state == HandleState.Unturn)
                {
                    autorotator.Update();
                    if (autorotator.Finished())
                    {
                        autorotator = null;
                        state = HandleState.Idle;
                    }
                }
            }

        }

        /// <summary>
        /// A class that automatically rotates an object.
        /// </summary>
        private class AutoRotator
        {

            public GameObject thing;
            public Vector3 rotation;
            public float speed;
            public bool reverse = false;
            public bool natural = true;

            private float progress = 0;

            public AutoRotator(GameObject thing, Vector3 rotation, float speed, bool reverse, bool natural)
            {
                this.thing = thing;
                this.rotation = rotation;
                this.speed = speed;
                this.natural = natural;
                this.reverse = reverse;
            }

            public void Update()
            {
                if (progress < 1)
                {
                    progress += Time.deltaTime * speed;
                }
                float proportion = progress;
                if (reverse) proportion = 1.0f - proportion;
                if (natural) proportion = 0.5f - Mathf.Cos(proportion * 180.0f * Mathf.Deg2Rad) / 2;
                Quaternion angleA = Quaternion.Euler(Vector3.zero);
                Quaternion angleB = Quaternion.Euler(rotation);
                if (thing) thing.transform.localRotation = Quaternion.Lerp(angleA, angleB, proportion);
            }

            public bool Finished()
            {
                return (progress >= 1);
            }

        }

    }
}

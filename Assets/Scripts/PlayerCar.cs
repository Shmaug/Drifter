using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCar : MonoBehaviour
{
    private Vector3 accel;
    public float throttle;
    private float deadZone = 0.01f;
    private Vector3 myRight;
    private Vector3 velo;
    private Vector3 flatVelo;
    private Vector3 relativeVelocity;
    private Vector3 dir;
    private Vector3 flatDir;
    private Vector3 carUp;
    private Transform carTransform;
    private Rigidbody carRigidbody;
    private Vector3 engineForce;

    private Vector3 turnVec;
    private Vector3 imp;
    private float rev;
    private float actualTurn;
    private float carMass;
    private Vector3 com = new Vector3(0f, -0.7f, .35f);
    private Transform[] wheelTransforms = new Transform[4]; // transforms for the 4 wheels
    public float actualGrip;
    public float horizontal; // horizontal input control
    private float maxSpeedToTurn = 0.2f; // keeps car from turning until it reaches this value

    // the physical transforms for the car's wheels
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    // the transform parents for the front wheels
    public Transform LFWheelTransform;
    public Transform RFWheelTransform;

    //car physics adjustments
    public float power = 1500f;
    public float maxSpeed = 50f;
    public float carGrip = 70f; // 70 is on the slippery side good for drifting 
    public float turnSpeed = 3f; // keep this value somewhere between 2.5 and 6.0

    private float slideSpeed;
    private float mySpeed;

    private Vector3 carRight;
    private Vector3 carFWD;
    private Vector3 tempVEC;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        carTransform = transform;
        carRigidbody = GetComponent<Rigidbody>();
        carUp = carTransform.up;
        carMass = GetComponent<Rigidbody>().mass;
        carFWD = Vector3.forward;
        carRight = Vector3.right;
        SetUpWheels();

        carRigidbody.centerOfMass = com; //center of mass defined neg value used to keep car from flipping
    }

    // Update is called once per frame
    void Update()
    {
        CarPhysicsUpdate();

        CheckInput();
    }

    void LateUpdate()
    {
        RotateVisualWheels();

        EngineSound();
    }

    void SetUpWheels()
    {
        if (frontLeftWheel == null || frontRightWheel == null || rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogError("One of more of the wheel transforms have not been plugged into the car");
            Debug.Break();
        }
        else
        {
            wheelTransforms[0] = frontLeftWheel;
            wheelTransforms[1] = rearLeftWheel;
            wheelTransforms[2] = frontRightWheel;
            wheelTransforms[3] = rearRightWheel;
        }
    }

    private Vector3 rotationAmount;

    void RotateVisualWheels()
    {
        //PROB BROKEN new vector3 is proably whats wrong

        //LFWheelTransform.localEulerAngles.y = horizontal * 30f;

        LFWheelTransform.localEulerAngles = new Vector3(0f, horizontal * 30f, 0f);
        RFWheelTransform.localEulerAngles = new Vector3(0f, horizontal * 30f, 0f);

        rotationAmount = carRight * (relativeVelocity.z * 1.6f * Time.deltaTime * Mathf.Rad2Deg);

        wheelTransforms[0].Rotate(rotationAmount);
        wheelTransforms[1].Rotate(rotationAmount);
        wheelTransforms[2].Rotate(rotationAmount);
        wheelTransforms[3].Rotate(rotationAmount);
    }

    private float deviceAccelerometerSensitivity = 2; // How sensitive the mobile accelerometer would be 

    void CheckInput()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer || (Application.platform == RuntimePlatform.Android))
        {
            accel = Input.acceleration * deviceAccelerometerSensitivity;

            if (accel.x > deadZone || accel.x < -deadZone)
            {
                horizontal = accel.x;
            }
            else
            {
                horizontal = 0;
            }
            throttle = 0;

            foreach (Touch touch in Input.touches)
            {
                if (touch.position.x > Screen.width - Screen.width / 3 && touch.position.y < Screen.height / 3)
                {
                    throttle = 1;
                }
                else if (touch.position.x < Screen.width / 3 && touch.position.y < Screen.height / 3)
                {
                    throttle = -1;
                }
            }
        }
        else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
             || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
        {
            // Use the keyboard for car input
            horizontal = Input.GetAxis("Horizontal");
            throttle = Input.GetAxis("Vertical");
        }
    }

    void CarPhysicsUpdate()
    {
        // grab all the physics info we need to calc everything
        myRight = carTransform.right;

        // find out velocity
        velo = carRigidbody.velocity;

        tempVEC = new Vector3(velo.x, 0f, velo.z);

        // figure out velocity without y movement - our flat velocity
        dir = transform.TransformDirection(carFWD);

        tempVEC = new Vector3(dir.x, 0, dir.z); 

        // calculate our direction, removing y movement - our flat direction
        flatDir = Vector3.Normalize(tempVEC);

        // calculate relative velocity
        relativeVelocity = carTransform.InverseTransformDirection(flatVelo);

        // calculate how much we are sliding (find out movement along x axis)
        slideSpeed = Vector3.Dot(myRight, flatVelo);

        // calculate current speed
        mySpeed = flatVelo.magnitude;

        // check if we're moving in reverse
        rev = Mathf.Sign(Vector3.Dot(flatVelo, flatDir));

        // calculate engine force with our flat direction vector and acceleration 
        engineForce = (flatDir * (power * throttle) * carMass);

        // do turning
        actualTurn = horizontal;

        if (rev < 0.1f)
        {
            actualTurn =- actualTurn;
        }

        turnVec = (((carUp * turnSpeed) * actualTurn) * carMass) * 800; // basically how fast the car turns

        // BROKEN ACTUAL GRIP ISN'T WORKING
        actualGrip = Mathf.Lerp(100, carGrip, mySpeed * 0.02f);
        imp = myRight * (-slideSpeed * carMass * actualGrip);
    }

    void SlowVelocity()
    {
        carRigidbody.AddForce(-flatVelo * 0.8f);
    }

    //PROB BROKEN where is the ref to audio clip?
    void EngineSound()
    {
        GetComponent<AudioSource>().pitch = 0.30f + mySpeed * 0.025f;

        if (mySpeed > 30)
        {
            GetComponent<AudioSource>().pitch = 0.25f + mySpeed * 0.015f;
        }

        if (mySpeed > 40)
        {
            GetComponent<AudioSource>().pitch = 0.20f + mySpeed * 0.013f;
        }

        if (mySpeed > 49)
        {
            GetComponent<AudioSource>().pitch = 0.15f + mySpeed * 0.011f;
        }

        // setting a max value for the pitch
        if (GetComponent<AudioSource>().pitch > 2.0)
        {
            GetComponent<AudioSource>().pitch = 2.0f;
        }
    }

    void FixedUpdate()
    {
        if (mySpeed < maxSpeed)
        {
            carRigidbody.AddForce(engineForce * Time.deltaTime);
        }

        if (mySpeed > maxSpeedToTurn)
        {
            carRigidbody.AddTorque(turnVec * Time.deltaTime);
        }
        else if (mySpeed < maxSpeedToTurn)
        {
            return;
        }

        carRigidbody.AddForce(imp * Time.deltaTime);
    }
}
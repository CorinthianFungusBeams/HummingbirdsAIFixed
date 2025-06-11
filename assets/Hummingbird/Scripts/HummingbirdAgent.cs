using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HummingbirdAgent : Agent
{
    public float moveForce = 2f;

    public float pitchSpeed = 100f;

    public float yawSpeed = 100f;

    public Transform beakTip;

    public Camera agentCamera;

    public bool trainingMode;

    new private Rigidbody rigidbody;

    private FlowerArea flowerArea;

    private Flower nearestFlower;

    private float smoothPitchChange = 0f;

    private float smoothYawChange = 0f;

    private const float MaxPitchAngle = 80f;

    private const float BeakTipRadius = 0.008f;

    private bool frozen = false;

    //nectar gained in an episode
    public float NectarObtained {get; private set;}



    //funcs here
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        if (!trainingMode) MaxStep = 0;
    }

    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            //reset flower in training when 1 agent
            flowerArea.ResetFlowers();
        }

        NectarObtained = 0f;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower); 

        UpdateNearestFlower();
    }

    //what happens when an action is received by NN or player
    public override void OnActionReceived(ActionBuffers actions)
    {
        // frozen
        if (frozen) return;

        var continuousActions = actions.ContinuousActions;
        //calc movement vectors
        Vector3 move = new Vector3(continuousActions[0], continuousActions[1], continuousActions[2]);

        //move with the vector
        rigidbody.AddForce(move*moveForce);

        //current rot
        Vector3 rotationalVector = transform.rotation.eulerAngles;

        // ptich yaw
        float pitchChange = continuousActions[3];
        float yawChange = continuousActions[4];

        //calc smooth rotate
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f*Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f*Time.fixedDeltaTime);

        //calc pitch yaw
        float pitch = rotationalVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch>180f) pitch -=360f;
        pitch = Mathf.Clamp(pitch,-MaxPitchAngle,MaxPitchAngle);

        float yaw = rotationalVector.y +smoothYawChange *Time.fixedDeltaTime * yawSpeed;

        transform.rotation = Quaternion.Euler(pitch,yaw,0f);
    }

    /// <summary>
    /// collect vector observations from the env
    /// </summary>
    /// <param name="sensor"> the vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        if(nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }
        //observe agent local rotation(4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        //get a vector from beak tip to nearest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        //vector to nearest flower observe(3 observations)
        sensor.AddObservation(toFlower.normalized);

        //add dot product observation whether beaktip is infront of flower(1 observations
        //(+1 means beaktip infront of flower, -1 means behind
        sensor.AddObservation(Vector3.Dot(toFlower.normalized,-nearestFlower.FlowerUpVector.normalized));

        //dot product indicates whether beak is pointing toward flower(1 observations
        //(+1 means pointing, -1 means away
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // distance beak tip to flower 1 obs
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        //10 total observation
    }
    /// <summary>
    /// when behavior type is set to heuristic only, this function will be called. Return will be called into 
    /// </summary>
    /// <param name="actionsOut">output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        if (Input.GetKey(KeyCode.S)) forward = -transform.forward;


        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        if (Input.GetKey(KeyCode.D)) left = transform.right;


        if (Input.GetKey(KeyCode.E)) up = transform.up;
        if (Input.GetKey(KeyCode.Q)) up = -transform.up;


        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        Vector3 combined = (forward + left + up).normalized;

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = combined.x;
        continuousActionsOut[1] = combined.y;
        continuousActionsOut[2] = combined.z;
        continuousActionsOut[3] = pitch;
        continuousActionsOut[4] = yaw;

    }

    /// <summary>
    /// freeze the agent basically
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }
    /// <summary>
    /// unfreeze, basically
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;

        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                float distanceFromFlower = UnityEngine.Random.Range(.1f,.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower,Vector3.up);
            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f,2.5f);

                float radius = UnityEngine.Random.Range(2f,7f);

                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f,180f),0f);

                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                float pitch = UnityEngine.Random.Range(-60f,60f);
                float yaw = UnityEngine.Random.Range(-180f,180f);
                potentialRotation = Quaternion.Euler(pitch,yaw,0f);
            }

        
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "could not find safe pos:(");

        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null  && flower.HasNectar)
            {
                nearestFlower=flower;
            }
            else if (flower.HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }
    /// <summary>
    /// wablam!
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);

    }
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    private void TriggerEnterOrStay(Collider collider)
    {
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                float nectarReceived = flower.Feed(0.01f);

                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    //reward!
                    float bonus = 0.2f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

                    AddReward(.01f + bonus);
                }

                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            //Collided with boundary. WHIP THEM
            AddReward(-.5f);
        }
    }
    private void Update()
    {
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    private void FixedUpdate()
    {
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class AgentScript : Agent
{
    private enum ACTIONS
    {
        LEFT = 0,
        FORWARD = 1,
        RIGHT = 2,
        BACKWARD = 3
    }

    public float speed = 10f;

    private int stuckSteps = 0;
    private Rigidbody rb;
    private Vector3 lastPos;
    private float previousDistanceToTarget;

    [SerializeField] private EnvironmentManager environmentManager;

    // Kezdeti referencia-beállítások az agenthez és a környezethez.
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (environmentManager == null)
        {
            environmentManager = GetComponentInParent<EnvironmentManager>();
        }

        if (environmentManager == null)
        {
            Debug.LogError("AgentScript: No local EnvironmentManager found.");
        }

        if (rb == null)
        {
            Debug.LogError("AgentScript: No Rigidbody found on agent.");
        }
    }

    // Minden epizód elején újraindítja az agentet és lenullázza a beragadás figyelést.
    public override void OnEpisodeBegin()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (environmentManager != null)
        {
            environmentManager.ResetEnvironment(transform);
        }

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            transform.position += Vector3.up * 0.2f;
        }

        lastPos = transform.position;
        stuckSteps = 0;

        if (environmentManager != null && environmentManager.FireTransform != null)
        {
            previousDistanceToTarget = Vector3.Distance(transform.position, environmentManager.FireTransform.position);
        }
    }

    // Fizikai frissítéskor döntést kér, és figyeli, hogy beragadt-e az agent.
    private void FixedUpdate()
    {
        RequestDecision();

        if (transform.position.y < -5f)
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

        if (Vector3.Distance(transform.position, lastPos) < 0.05f)
            stuckSteps++;
        else
            stuckSteps = 0;

        lastPos = transform.position;

        if (StepCount >= 300)
        {
            AddReward(-0.2f);
            EndEpisode();
            return;
        }
    }

    // Megfigyeléseket ad át a modellnek egyszerűbb formában.
    public override void CollectObservations(VectorSensor sensor)
    {
        if (environmentManager == null || environmentManager.FireTransform == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector3 agentPos = transform.position;
        Vector3 firePos = environmentManager.FireTransform.position;

        Vector3 toFire = firePos - agentPos;

        // Relatív irány a tűz felé
        sensor.AddObservation(toFire.x / 60f);
        sensor.AddObservation(toFire.z / 60f);

        // Az agent aktuális nézeti iránya
        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);
    }

    // A kapott akció alapján mozgatja az agentet és jutalmazza a cél felé haladást.
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (environmentManager == null || environmentManager.FireTransform == null || rb == null)
            return;

        int actionTaken = actions.DiscreteActions[0];

        switch (actionTaken)
        {
            case (int)ACTIONS.FORWARD:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;

            case (int)ACTIONS.LEFT:
                transform.rotation = Quaternion.Euler(0, -90, 0);
                break;

            case (int)ACTIONS.RIGHT:
                transform.rotation = Quaternion.Euler(0, 90, 0);
                break;

            case (int)ACTIONS.BACKWARD:
                transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
        }

        Vector3 move = transform.forward * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Kis büntetés minden lépésért
        AddReward(-0.0005f);


        float currentDistanceToTarget = Vector3.Distance(transform.position, environmentManager.FireTransform.position);
        float progressReward = previousDistanceToTarget - currentDistanceToTarget;
        AddReward(progressReward * 0.02f);
        previousDistanceToTarget = currentDistanceToTarget;

        if (currentDistanceToTarget < 5.0f)
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

    // Billentyűzetről vezérelhetővé teszi az agentet teszteléshez.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = (int)ACTIONS.FORWARD;

        if (Input.GetKey(KeyCode.W))
            discreteActions[0] = (int)ACTIONS.FORWARD;
        else if (Input.GetKey(KeyCode.A))
            discreteActions[0] = (int)ACTIONS.LEFT;
        else if (Input.GetKey(KeyCode.D))
            discreteActions[0] = (int)ACTIONS.RIGHT;
        else if (Input.GetKey(KeyCode.S))
            discreteActions[0] = (int)ACTIONS.BACKWARD;
    }

    // Ütközéskor büntetést ad falnak vagy fának való nekiütközés esetén.
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-1.0f);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Tree1"))
        {
            AddReward(-0.05f);
        }
    }
}
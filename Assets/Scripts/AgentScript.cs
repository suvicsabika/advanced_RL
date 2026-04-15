using System.Collections;
using System.Collections.Generic;
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

    public Transform TargetTransform;
    public float speed = 10f;

    public override void OnEpisodeBegin()
    {
        // Agent reset
        transform.localPosition = new Vector3(0, 0.0f, 0);

        // Target random pozíció
        TargetTransform.localPosition = new Vector3(
            Random.Range(-85f, 85f),
            0.0f,
            Random.Range(-85f, 85f)
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent pozíció
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.z);

        // Target pozíció
        sensor.AddObservation(TargetTransform.localPosition.x);
        sensor.AddObservation(TargetTransform.localPosition.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
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

        // Mozgás előre az aktuális irányba
        transform.Translate(Vector3.forward * speed * Time.fixedDeltaTime);

        // Kis büntetés minden lépésnél
        AddReward(-0.001f);

        // Cél elérése
        float distanceToTarget = Vector3.Distance(transform.localPosition, TargetTransform.localPosition);
        if (distanceToTarget < 2.0f)
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

    private void FixedUpdate()
    {
        RequestDecision();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.W))
            discreteActions[0] = (int)ACTIONS.FORWARD;
        else if (Input.GetKey(KeyCode.A))
            discreteActions[0] = (int)ACTIONS.LEFT;
        else if (Input.GetKey(KeyCode.D))
            discreteActions[0] = (int)ACTIONS.RIGHT;
        else if (Input.GetKey(KeyCode.S))
            discreteActions[0] = (int)ACTIONS.BACKWARD;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
        }
    }
}
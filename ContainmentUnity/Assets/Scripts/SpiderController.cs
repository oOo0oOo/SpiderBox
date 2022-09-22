using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SpiderState { Idle, Walking, TurningLeft, TurningRight };

public class SpiderController : MonoBehaviour
{
    public float _speedHorizontal = 0.05f;
    public float _speedForward = 0.2f;
    public float _speedRot = 20f;
    public float smoothness = 100f;
    public int raysNb = 8;
    public float raysEccentricity = 0.2f;
    public float outerRaysOffset = 0.005f;
    public float innerRaysOffset = 0.05f;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastPosition;
    private Vector3 forward;
    private Vector3 upward;
    private Quaternion lastRot;
    private Vector3[] pn;

    private bool targetToggle = false;

    public Transform targetPosition;

    public SpiderState state = SpiderState.Walking;
    private float stateChangeCountdown = 2.0f;


    Vector3[] GetIcoSphereCoords(int depth)
    {
        Vector3[] res = new Vector3[(int)Mathf.Pow(4, depth) * 12];
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        res[0] = (new Vector3(t, 1, 0));
        res[1] = (new Vector3(-t, -1, 0));
        res[2] = (new Vector3(-1, 0, t));
        res[3] = (new Vector3(0, -t, 1));
        res[4] = (new Vector3(-t, 1, 0));
        res[5] = (new Vector3(1, 0, t));
        res[6] = (new Vector3(-1, 0, -t));
        res[7] = (new Vector3(0, t, -1));
        res[8] = (new Vector3(t, -1, 0));
        res[9] = (new Vector3(1, 0, -t));
        res[10] = (new Vector3(0, t, 1));
        res[11] =(new Vector3(0, -t, -1));

        return res;
    }

    Vector3[] GetClosestPointIco(Vector3 point, Vector3 up, float halfRange)
    {
        Vector3[] res = new Vector3[2] { point, up };

        Vector3[] dirs = GetIcoSphereCoords(0);
        raysNb = dirs.Length;

        float amount = 1f;

        foreach (Vector3 dir in dirs)
        {
            RaycastHit hit;
            Ray ray = new Ray(point + up*0.15f, dir);
            //Debug.DrawRay(ray.origin, ray.direction);
            if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
            {
                res[0] += hit.point;
                res[1] += hit.normal;
                amount += 1;
            }
        }
        res[0] /= amount;
        res[1] /= amount;
        return res;
    }

    static Vector3[] GetClosestPoint(Vector3 point, Vector3 forward, Vector3 up, float halfRange, float eccentricity, float offset1, float offset2, int rayAmount)
    {
        Vector3[] res = new Vector3[2] { point, up };
        Vector3 right = Vector3.Cross(up, forward);
        float normalAmount = 1f;
        float positionAmount = 1f;

        Vector3[] dirs = new Vector3[rayAmount];
        float angularStep = 2f * Mathf.PI / (float)rayAmount;
        float currentAngle = angularStep / 2f;
        for(int i = 0; i < rayAmount; ++i)
        {
            dirs[i] = -up + (right * Mathf.Cos(currentAngle) + forward * Mathf.Sin(currentAngle)) * eccentricity;
            currentAngle += angularStep;
        }

        foreach (Vector3 dir in dirs)
        {
            RaycastHit hit;
            Vector3 largener = Vector3.ProjectOnPlane(dir, up);
            Ray ray = new Ray(point - (dir + largener) * halfRange + largener.normalized * offset1 / 100f, dir);
            Debug.DrawRay(ray.origin, ray.direction);
            if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
            {
                res[0] += hit.point;
                res[1] += hit.normal;
                normalAmount += 1;
                positionAmount += 1;
            }
            ray = new Ray(point - (dir + largener) * halfRange + largener.normalized * offset2 / 100f, dir);
            Debug.DrawRay(ray.origin, ray.direction, Color.green);
            if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
            {
                res[0] += hit.point;
                res[1] += hit.normal;
                normalAmount   += 1;
                positionAmount += 1;
            }
        }
        res[0] /= positionAmount;
        res[1] /= normalAmount;
        return res;
    }

    void Start()
    {
        velocity = new Vector3();
        forward = transform.forward;
        upward = transform.up;
        lastRot = transform.rotation;
    }

    void FixedUpdate()
    {
        velocity = (smoothness * velocity + (transform.position - lastPosition)) / (1f + smoothness);
        if (velocity.magnitude < 0.000025f)
            velocity = lastVelocity;
        lastPosition = transform.position;
        lastVelocity = velocity;

        float dt = Time.fixedDeltaTime;

        // Update the state machine
        stateChangeCountdown -= dt;
        if (stateChangeCountdown <= 0){
            if (state == SpiderState.Idle){
                state = SpiderState.Walking;
                stateChangeCountdown = UnityEngine.Random.Range(4f, 8f);
            } else {
                float r = UnityEngine.Random.Range(0f, 1f);
                if (r < 0.2) {
                    state = SpiderState.TurningRight;
                    stateChangeCountdown = UnityEngine.Random.Range(1f, 2f);
                } else if (r < 0.4) {
                    state = SpiderState.TurningLeft;
                    stateChangeCountdown = UnityEngine.Random.Range(1f, 2f);
                } else if (r < 0.8) {
                    state = SpiderState.Walking;
                    stateChangeCountdown = UnityEngine.Random.Range(4f, 8f);
                } else  {
                    state = SpiderState.Idle;
                    stateChangeCountdown = UnityEngine.Random.Range(1f, 4f);
                }
            }
        }

        if (state == SpiderState.Walking) {
            transform.position += transform.forward * _speedForward * dt;
        } else if (state == SpiderState.TurningRight){
            transform.position += transform.forward * _speedForward * dt * 0.8f;
            transform.position += Vector3.Cross(transform.up, transform.forward) * _speedHorizontal * dt;
        } else if (state == SpiderState.TurningLeft){
            transform.position += transform.forward * _speedForward * dt * 0.8f;
            transform.position += Vector3.Cross(transform.up, transform.forward) * -1 * _speedHorizontal * dt;
        }

        if (state != SpiderState.Idle){
            pn = GetClosestPoint(transform.position, transform.forward, transform.up, 0.1f, 0.1f, 1.9f, -1.9f, 8);
            upward = pn[1];

            Vector3[] pos = GetClosestPoint(transform.position, transform.forward, transform.up, 0.04f, raysEccentricity, innerRaysOffset, outerRaysOffset, raysNb);
            transform.position = Vector3.Lerp(lastPosition, pos[0], 1f / (1f + smoothness));

            forward = velocity.normalized;
            Quaternion q = Quaternion.LookRotation(forward, upward);
            transform.rotation = Quaternion.Lerp(lastRot, q, 1f / (1f + smoothness));
        }

        lastRot = transform.rotation;
    }
}

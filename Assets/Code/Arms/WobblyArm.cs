using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class WobblyArm : MonoBehaviour
{
    [Header("Anchors")]
    public Transform shoulder;
    public Transform hand;

    [Header("Chain")]
    [Min(3)] public int knotCount = 8;

    [Header("Spring")]
    public float stiffness = 120f;      // pull toward the rest position
    public float damping = 8f;          // higher = less wobble
    public float neighborCoupling = 40f; // pulls knots toward their neighbors' average -> wave-like motion
    public float sag = 0.15f;           // how much the arm bows downward
    public float maxDeltaTime = 0.033f;

    SplineContainer container;
    Vector3[] pos;
    Vector3[] vel;
    Vector3[] targets;

    void Awake()
    {
        container = GetComponent<SplineContainer>();

        pos = new Vector3[knotCount];
        vel = new Vector3[knotCount];
        targets = new Vector3[knotCount];

        EnsureKnotCount();

        // start every knot on the line between shoulder and hand
        for (int i = 0; i < knotCount; i++)
        {
            float t = i / (float)(knotCount - 1);
            pos[i] = Vector3.Lerp(shoulder.position, hand.position, t);
        }

        // smooth tangents so the curve doesn't kink
        container.Spline.SetTangentMode(TangentMode.AutoSmooth);
    }

    void EnsureKnotCount()
    {
        var spline = container.Spline;
        while (spline.Count < knotCount) spline.Add(new BezierKnot(float3.zero));
        while (spline.Count > knotCount) spline.RemoveAt(spline.Count - 1);
    }

    void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);
        Vector3 a = shoulder.position;
        Vector3 b = hand.position;
        int last = knotCount - 1;

        // pinned ends
        pos[0] = a;
        pos[last] = b;
        vel[0] = vel[last] = Vector3.zero;

        // rest targets: evenly spaced on the line, with a bow that is
        // strongest in the middle and zero at the ends
        for (int i = 1; i < last; i++)
        {
            float t = i / (float)last;
            float bow = Mathf.Sin(t * Mathf.PI);
            targets[i] = Vector3.Lerp(a, b, t) + Vector3.down * (sag * bow);
        }

        // spring simulation on the inner knots only
        for (int i = 1; i < last; i++)
        {
            Vector3 neighborAvg = (pos[i - 1] + pos[i + 1]) * 0.5f;

            Vector3 force =
                (targets[i] - pos[i]) * stiffness +
                (neighborAvg - pos[i]) * neighborCoupling -
                vel[i] * damping;

            vel[i] += force * dt;
            pos[i] += vel[i] * dt;
        }

        // write back to the spline
        var spline = container.Spline;
        for (int i = 0; i < knotCount; i++)
        {
            spline.SetKnot(i, MakeKnot(pos[i]));
        }
    }

    BezierKnot MakeKnot(Vector3 worldPos)
    {
        Vector3 local = container.transform.InverseTransformPoint(worldPos);
        return new BezierKnot((float3)local);
    }
}
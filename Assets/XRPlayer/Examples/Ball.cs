using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Ball : MonoBehaviour
{
    public float defaultBounciness = 0.5f, defaultFriction = 0.5f;
    public float airDensity = 1.225f;
    public float dragCoeff = .1f;
    public float magnusCoeff = .5f;
    public float minImpact = .3f;

    [HideInInspector] public Vector3 storedVelocity, storedAngularVelocity, storedNormal, storedPosition;
    bool _inCol;int _inColTimer;
    Rigidbody body;
    SphereCollider sphereCollider;
    public float radius => transform.lossyScale.x * sphereCollider.radius;
    
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.contactOffset = 0.005f;
    }
    int ffNum = 0;
    private void FixedUpdate()
    {
        ++ffNum;
        if (!_inCol)
        {
            storedVelocity = body.velocity;
            storedAngularVelocity = body.angularVelocity;
        }
        _inCol = false;
        if (!body.isKinematic && !body.IsSleeping())
        {
            float r = radius;
            Vector3 v = body.velocity, w = body.angularVelocity;

            //Magnus Effect http://math.mit.edu/~bush/wordpress/wp-content/uploads/2013/11/Beautiful-Game-2013.pdf
            Vector3 buoyancy = -airDensity * Mathf.PI*4f/3f*r*r*r * Physics.gravity;
            Vector3 drag = -dragCoeff * Mathf.PI * r * r / 2 * airDensity * v.magnitude * v;
            drag = Vector3.ClampMagnitude(drag, v.magnitude * body.mass * Time.fixedDeltaTime);//Prevent Jittering
            Vector3 magnus = magnusCoeff * Mathf.PI * airDensity *r*r*r * Vector3.Cross(w, v);

            body.AddForce(buoyancy+ magnus+drag);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        _inCol = true; _inColTimer = 0;
        storedNormal = collision.GetContact(0).normal;
        storedPosition = collision.GetContact(0).point+storedNormal*radius;
    }
    private void OnCollisionStay(Collision collision)
    {
        _inCol = true; _inColTimer += 1;
        storedNormal = collision.GetContact(0).normal;
        storedPosition = collision.GetContact(0).point + storedNormal * radius;
        DealCollision(collision);
    }
    private void OnCollisionExit(Collision collision)
    {
        DealCollision(collision);
    }
    void DealCollision(Collision collision)
    {

        Vector3 rsv = Vector3.zero, rsw = Vector3.zero, rp = Vector3.zero;
        float bounciness = defaultBounciness;
        float friction = defaultFriction;

        var racket = collision.rigidbody ? collision.rigidbody.GetComponent<Racket>() : collision.collider.GetComponent<Racket>();
        if (collision.collider.sharedMaterial)
        {
            bounciness = collision.collider.sharedMaterial.bounciness;
            friction = collision.collider.sharedMaterial.dynamicFriction;
        }
        if (racket)
        {
            rsv = racket.smoothedVelocity;
            rsw = racket.smoothedAngularVelocity;
            rp = racket.smoothedPosition;
        }
        else if (collision.rigidbody)
        {
            rsv = body.velocity;
            rsw = body.angularVelocity;
            rp = body.position;
        }
        RacketBallCollision(storedPosition, storedVelocity, storedAngularVelocity,
            rp, rsv, rsw,
            storedNormal, radius, bounciness, friction,
            out Vector3 newVelocity, out Vector3 newAngularVelocity, out float newImpact);
        if (newImpact > minImpact)
        {
            //ShowVector(storedPosition, storedNormal * newImpact / 10);

            body.velocity = newVelocity;
            body.angularVelocity = newAngularVelocity;
            /*if(Physics.ComputePenetration(sphereCollider, body.position, body.rotation,
                collision.collider, collision.collider.transform.position, collision.collider.transform.rotation,
                out Vector3 dir, out float dist))
            {
                transform.position += dir * dist;
            }*/
        }
    }
    public static void RacketBallCollision(Vector3 rBall, Vector3 vBall, Vector3 wBall, Vector3 rRacket, Vector3 vRacket, Vector3 wRacket, Vector3 normal, float radius, float bounciness, float friction, out Vector3 vf, out Vector3 wf, out float impact)
    {
        normal = normal.normalized;
        float momentOfInertia = radius * radius * 2 / 3;//moment of inertia with unit mass

        //Transform ball movement into racket frame to set velocity of collision point as zero
        //TODO physics check
        Vector3 rContact = rBall - normal * radius;
        Vector3 vContact = vRacket + Vector3.Cross(wRacket, rContact - rRacket);
        Vector3 v = vBall - vContact;
        Vector3 w = wBall - wRacket;

        //Resolve the impact
        impact = -Vector3.Dot(v, normal);

        if (impact < 0) { vf = vBall; wf = wBall; impact = 0; return; }

        float impulse = impact * (1 + bounciness);
        v += impulse * normal;

        float N = 10;
        Vector3 R = -normal * radius;
        for (int i = 0; i < N; ++i)
        {
            Vector3 slidingVelocity = Vector3.ProjectOnPlane(v, normal) + Vector3.Cross(w, R);

            Vector3 frictionDir = -slidingVelocity.normalized;

            Vector3 dvh = frictionDir + Vector3.Cross(Vector3.Cross(R, frictionDir) / momentOfInertia, R);
            if (dvh.sqrMagnitude > 0)
            {
                float maxI = -Vector3.Dot(slidingVelocity, dvh) / dvh.sqrMagnitude;
                Debug.Assert(maxI >= 0);

                Vector3 frictionImpulse = Mathf.Min(impulse / N * friction, maxI) * frictionDir;

                v += frictionImpulse;
                w += Vector3.Cross(R, frictionImpulse) / momentOfInertia;
            }
        }

        //Convert back to world frame and write to rigidbody
        vf = v + vContact;
        wf = w + wRacket;

    }
    static void ShowVector(Vector3 origin, Vector3 vec, float time=1f)
    {
        var b= GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.GetComponent<BoxCollider>().enabled = false;
        b.transform.position = origin + vec / 2;
        b.transform.localScale = new Vector3(.01f, .01f, vec.magnitude+.01f);
        b.transform.rotation = Quaternion.LookRotation(vec);
        Destroy(b, time);
    }
    private void OnValidate()
    {
        body = GetComponent<Rigidbody>();
        if(body.drag > .0001f)
        {
            Debug.LogWarning(gameObject.name+": Rigidbody's builtin linear drag are disabled.");
            body.drag = .0001f;
        }
        if (body.collisionDetectionMode == CollisionDetectionMode.Discrete)
        {
            Debug.LogWarning(gameObject.name + ": Should use continuous collision detect mode");
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }
}
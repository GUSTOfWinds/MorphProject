using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;

    // F�r hur m�nga targets som highlightas n�r man kollar mot dem.

    public LayerMask targetMask;
    public LayerMask obstacleMask;


    [HideInInspector]
    public List<Transform> visibleTargets = new List<Transform>();

    public float meshResolution;
    public int edgeResolveIterations;

    public MeshFilter viewMeshFilter;
    Mesh viewMesh;


    void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "view Mesh";
        viewMeshFilter.mesh = viewMesh;

        StartCoroutine("FindTargetsWithDelay", .2f);
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    // denna metod r�knas inte med. F�r hur m�nga targets som highlightas n�r man kollar mot dem.

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    void FindVisibleTargets()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                }
            }
        }
    }

    // F�r hur m�nga targets som highlightas n�r man kollar mot dem.


    // Hur m�nga rays vi skickar ut.
    void DrawFieldOfView()
    {
        var targetObject = GameObject.Find("player");
        var targetPos = targetObject.transform.position;
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                if (newViewCast.hitTarget && visibleTargets.Count > 0)
                {
                    //transform.Translate(0, 0, Time.deltaTime / 50);
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime / 50);
                }

                if (oldViewCast.hitObstacle != newViewCast.hitObstacle)
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }

            }


            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        // sv�r formel 
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }


    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (newViewCast.hitObstacle == minViewCast.hitObstacle)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, targetMask))
        {
            return new ViewCastInfo(false, true, hit.point, hit.distance, globalAngle);
        }
        else if (Physics.Raycast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, false, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }



    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    public struct ViewCastInfo
    {
        public bool hitObstacle;
        public bool hitTarget;
        public Vector3 point;
        public float distance;
        public float angle;


        public ViewCastInfo(bool _hitObstacle, bool _hitTarget, Vector3 _point, float _distance, float _angle)
        {
            hitObstacle = _hitObstacle;
            hitTarget = _hitTarget;
            point = _point;
            distance = _distance;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }

}





// �ndra i viewcast l�gg till spelaren. Spelaren och obstacle.

// Fixa ny raycast som alltid pekar mellan fienden och spelaren. En fr�ga om ser spelaren mig just nu. �r n�got iv�gen eller inte t ex en v�gg. Quaternion.Angle Spelarens rotation och fiendens rotation. Ifall fienden ser spelaren oavsett vart den �r roterad s� ska fienden se en.


// Fixa funktionalitet f�r n�r hunden hittar ett transformerat objekt. Den ska h�nvisa till keep your cool. Hunden ska kalla en metod n�r den hittar spelaren men den ska vara tom s� g�r en annan klar den.

// En collider saknas antar jag?



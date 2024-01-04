using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Snap : MonoBehaviour
{
    private NetworkHandler networkHandler;

    public static bool PointSegmentDistanceSquared(Vector2 point, Vector2 lineStart, Vector2 lineEnd, out float distance, out Vector2 intersectPoint)
    {
        const float kMinSegmentLenSquared = 0.000001f; // adjust to suit.  If you use double, you'll probably want something like 0.00000001
        const float kEpsilon = 1E-7f; // adjust to suit.  If you use doubles, you'll probably want something like 1.0E-14
        float dX = lineEnd.x - lineStart.x;
        float dY = lineEnd.y - lineStart.y;
        float dp1X = point.x - lineStart.x;
        float dp1Y = point.y - lineStart.y;
        float segLenSquared = (dX * dX) + (dY * dY);
        float t = 0.0f;

        if (segLenSquared >= -kMinSegmentLenSquared && segLenSquared <= kMinSegmentLenSquared)
        {
            // segment is a point.
            intersectPoint = lineStart;
            t = 0.0f;
            distance = ((dp1X * dp1X) + (dp1Y * dp1Y));
        }
        else
        {
            // Project a line from p to the segment [p1,p2].  By considering the line
            // extending the segment, parameterized as p1 + (t * (p2 - p1)),
            // we find projection of point p onto the line. 
            // It falls where t = [(p - p1) . (p2 - p1)] / |p2 - p1|^2
            t = ((dp1X * dX) + (dp1Y * dY)) / segLenSquared;
            if (t < kEpsilon)
            {
                // intersects at or to the "left" of first segment vertex (lineStart.X, lineStart.Y).  If t is approximately 0.0, then
                // intersection is at p1.  If t is less than that, then there is no intersection (i.e. p is not within
                // the 'bounds' of the segment)
                if (t > -kEpsilon)
                {
                    // intersects at 1st segment vertex
                    t = 0.0f;
                }
                // set our 'intersection' point to p1.
                intersectPoint = lineStart;
                // Note: If you wanted the ACTUAL intersection point of where the projected lines would intersect if
                // we were doing PointLineDistanceSquared, then intersectPoint.X would be (lineStart.X + (t * dx)) and intersectPoint.Y would be (lineStart.Y + (t * dy)).
            }
            else if (t > (1.0 - kEpsilon))
            {
                // intersects at or to the "right" of second segment vertex (lineEnd.X, lineEnd.Y).  If t is approximately 1.0, then
                // intersection is at p2.  If t is greater than that, then there is no intersection (i.e. p is not within
                // the 'bounds' of the segment)
                if (t < (1.0 + kEpsilon))
                {
                    // intersects at 2nd segment vertex
                    t = 1.0f;
                }
                // set our 'intersection' point to p2.
                intersectPoint = lineEnd;
                // Note: If you wanted the ACTUAL intersection point of where the projected lines would intersect if
                // we were doing PointLineDistanceSquared, then intersectPoint.X would be (lineStart.X + (t * dx)) and intersectPoint.Y would be (lineStart.Y + (t * dy)).
            }
            else
            {
                // The projection of the point to the point on the segment that is perpendicular succeeded and the point
                // is 'within' the bounds of the segment.  Set the intersection point as that projected point.
                intersectPoint = new Vector2((float)(lineStart.x + (t * dX)), (float)(lineStart.y + (t * dY)));
            }
            // return the squared distance from p to the intersection point.  Note that we return the squared distance
            // as an optimization because many times you just need to compare relative distances and the squared values
            // works fine for that.  If you want the ACTUAL distance, just take the square root of this value.
            float dpqX = point.x - intersectPoint.x;
            float dpqY = point.y - intersectPoint.y;

            distance = ((dpqX * dpqX) + (dpqY * dpqY));
        }

        return true;
    }

    TraceNode[] traceNodes;

    void Start()
    {
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();

        traceNodes = GameObject.FindObjectsOfType<TraceNode>();

        //knownAngles.Add(30); //test values
        //knownLengths.Add(3);
    }

    // Update is called once per frame
    void Update()
    {
        //GetImaginaryPoints();

        //Debug.Log("---");
        ////Debug.Log("count: " + stringKeyToVector3.Keys.Count);
        ////foreach (string k in stringKeyToVector3.Keys)
        ////{
        ////    Debug.Log(edgesConnectedToPoint[k].Count);
        ////    Debug.Log(k + " : " + edgesConnectedToPoint[k][0].start.myNum);
        ////    Debug.Log(k + " : " + edgesConnectedToPoint[k][0].end.myNum);
        ////    Debug.Log(k + " : " + edgesConnectedToPoint[k][1].start.myNum);
        ////    Debug.Log(k + " : " + edgesConnectedToPoint[k][1].end.myNum);
        ////}
        ////Debug.Log("nearest imaginary: " + getNearestImaginaryPointWithinDistance(new Vector3(0,0,0), 1000f));
        //Debug.Log("---");

        //initialSnap(new Vector3(0, 0, 0), 2);
    }

    public Dictionary<string, Vector3> stringKeyToVector3 = new Dictionary<string, Vector3>();
    public Dictionary<string, List<EdgeLine>> edgesConnectedToPoint = new Dictionary<string, List<EdgeLine>>();

    List<Tuple<string, Ray>> angleSnapLines;

    public List<Tuple<string, Ray>> selfSnapLines = new List<Tuple<string, Ray>>();

    List<Tuple<string, Vector3, float, int>> lengthSnapCirclesPointsOnly;
    List<Tuple<string, Vector3, float>> lengthSnapCircles;

    public void MakeNewSelfSnap(int current)
    {
        selfSnapLines = new List<Tuple<string, Ray>>();
        foreach (int n in networkHandler.graph.graph[current].adjacentNodes)
        {
            if (networkHandler.graph.graph[n].nodeGameObject.isActivated(true)) //removed && networkHandler.graph.graph[n].nodeGameObject.isTouchingPlayer()
            {
                Vector3 savedDirection = Quaternion.Euler(0, 0, 0) * (networkHandler.graph.graph[n].nodeGameObject.transform.position - networkHandler.graph.graph[current].nodeGameObject.transform.position);
                selfSnapLines.Add(new Tuple<string, Ray>("olp" + n + "," + current + "," + savedDirection.x + "," + savedDirection.y, new Ray(networkHandler.graph.graph[n].nodeGameObject.transform.position, savedDirection)));
            }
        }
    }

    public void GetImaginaryPoints(int current, bool whole=true) //might need to add current activated node parm
    {
        if (whole) //circle part at end is optional, because it worked better in intermsnap to exclude it in some cases (was causing fail to snap when moving along a line)
        {

            angleSnapLines = new List<Tuple<string, Ray>>();
            foreach (int n in networkHandler.graph.graph[current].adjacentNodes)
            {
                if (networkHandler.graph.graph[n].nodeGameObject.isActivated(true)) //removed && networkHandler.graph.graph[n].nodeGameObject.isTouchingPlayer()
                {
                    foreach (int p in networkHandler.graph.graph[n].adjacentNodes)
                    {
                        if (p != current)
                        {
                            if (imaginaryEdgeLine == null || (p != imaginaryEdgeLine.Item1 && p != imaginaryEdgeLine.Item2 && n != imaginaryEdgeLine.Item1 && n != imaginaryEdgeLine.Item2))
                            {
                                foreach (float angle in knownAngles)
                                {
                                    if (AngleCalculator.roomForAngle(networkHandler.graph, n, p, current, angle))
                                        angleSnapLines.Add(new Tuple<string, Ray>("ol" + n + "," + p, new Ray(networkHandler.graph.graph[n].nodeGameObject.transform.position, Quaternion.Euler(0, 0, angle) * (networkHandler.graph.graph[n].nodeGameObject.transform.position - networkHandler.graph.graph[p].nodeGameObject.transform.position))));
                                    if (AngleCalculator.roomForAngle(networkHandler.graph, n, p, current, -angle))
                                        angleSnapLines.Add(new Tuple<string, Ray>("ol" + n + "," + p, new Ray(networkHandler.graph.graph[n].nodeGameObject.transform.position, Quaternion.Euler(0, 0, -angle) * (networkHandler.graph.graph[n].nodeGameObject.transform.position - networkHandler.graph.graph[p].nodeGameObject.transform.position))));
                                }
                            }
                        }
                    }
                    //angleSnapLines.Add(new Ray(networkHandler.graph.graph[n].nodeGameObject.transform.position, Vector3.right));
                }
            }

            //if (networkHandler.graph.graph[current].nodeGameObject.isActivated(false) && networkHandler.graph.graph[current].nodeGameObject.isTouchingPlayer)
            //{
            //    foreach (int n in networkHandler.graph.graph[current].adjacentNodes)
            //    {
            //        if (networkHandler.graph.graph[n] != null && networkHandler.graph.graph.ContainsKey(n))
            //        {
            //            if (networkHandler.graph.graph[n].nodeGameObject.isActivated(false))
            //            {
            //                foreach (float length in knownLengths)
            //                {
            //                    lengthSnapCircles.Add(new Tuple<string, Vector3, float>("oc" + n + "," + length, networkHandler.graph.graph[n].nodeGameObject.transform.position, length));
            //                }
            //            }
            //        }
            //    }
            //}

            stringKeyToVector3 = new Dictionary<string, Vector3>();
            edgesConnectedToPoint = new Dictionary<string, List<EdgeLine>>();

            //level trade nodes
            foreach (TraceNode traceNode in traceNodes)
            {
                string id = "trace" + traceNode.nodeId;
                stringKeyToVector3[id] = traceNode.transform.position;
                edgesConnectedToPoint[id] = new List<EdgeLine>(); //dont need to worry about overwriting here
                Debug.Log("tracenodes");
            }

            //edge line X edge line
            foreach (EdgeLine edgeLine1 in networkHandler.graph.edgeLines)
            {
                foreach (EdgeLine edgeLine2 in networkHandler.graph.edgeLines)
                {
                    if (edgeLine1 != edgeLine2 && edgeLine1.start.myNum != current && edgeLine1.end.myNum != current && edgeLine2.start.myNum != current && edgeLine2.end.myNum != current)
                    {
                        Vector3? intersection = lineIntersection(edgeLine1.start.transform.position, edgeLine1.end.transform.position, edgeLine2.start.transform.position, edgeLine2.end.transform.position);

                        if (intersection != null)
                        {
                            if (!isPointOnLineSegment(edgeLine1.start.transform.position, edgeLine1.end.transform.position, intersection.Value) || !isPointOnLineSegment(edgeLine2.start.transform.position, edgeLine2.end.transform.position, intersection.Value))
                                continue;

                            string intersectionString = intersection.Value.x + "," + intersection.Value.y;
                            stringKeyToVector3[intersectionString] = new Vector3(intersection.Value.x, intersection.Value.y, intersection.Value.z);
                            if (!edgesConnectedToPoint.ContainsKey(intersectionString))
                            {
                                edgesConnectedToPoint[intersectionString] = new List<EdgeLine>();
                            }
                            if (!edgesConnectedToPoint[intersectionString].Contains(edgeLine1))
                                edgesConnectedToPoint[intersectionString].Add(edgeLine1);
                            if (!edgesConnectedToPoint[intersectionString].Contains(edgeLine2))
                                edgesConnectedToPoint[intersectionString].Add(edgeLine2);
                        }
                    }
                }
            }

            //edge line x angle snap line
            foreach (EdgeLine edgeLine in networkHandler.graph.edgeLines)
            {
                if (edgeLine.start.myNum != current && edgeLine.end.myNum != current)
                {

                    foreach (Tuple<string, Ray> angleSnapLine in angleSnapLines.Concat(selfSnapLines))
                    {
                        Vector3? intersection = lineIntersection(edgeLine.start.transform.position, edgeLine.end.transform.position, angleSnapLine.Item2.origin, angleSnapLine.Item2.direction + angleSnapLine.Item2.origin);

                        if (intersection != null)
                        {
                            if (isPointOnLineSegment(edgeLine.start.transform.position, edgeLine.end.transform.position, intersection.Value) && isPointOnRay(intersection.Value, angleSnapLine.Item2))
                            {
                                string intersectionString = intersection.Value.x + "," + intersection.Value.y;
                                stringKeyToVector3[intersectionString] = new Vector3(intersection.Value.x, intersection.Value.y, intersection.Value.z);
                                if (!edgesConnectedToPoint.ContainsKey(intersectionString))
                                    edgesConnectedToPoint[intersectionString] = new List<EdgeLine>();
                                if (!edgesConnectedToPoint[intersectionString].Contains(edgeLine))
                                    edgesConnectedToPoint[intersectionString].Add(edgeLine);
                            }
                        }
                    }
                }
            }

        }

        lengthSnapCirclesPointsOnly = new List<Tuple<string, Vector3, float, int>>();
        lengthSnapCircles = new List<Tuple<string, Vector3, float>>();
        if (networkHandler.graph.graph[current].nodeGameObject.isActivated(false)) //removed && networkHandler.graph.graph[current].nodeGameObject.isTouchingPlayer()
        {
            for (int n = 0; n < networkHandler.graph.getNodeCountId(); n++)
            {
                if (networkHandler.graph.graph[n] != null && networkHandler.graph.graph.ContainsKey(n))
                {
                    if (networkHandler.graph.graph[n].nodeGameObject.isActivated(false))
                    {
                        if (networkHandler.graph.graph[n].adjacentNodes.Contains(current))
                        {
                            foreach (float length in knownLengths)
                            {
                                lengthSnapCircles.Add(new Tuple<string, Vector3, float>("ocf" + n + "," + length, networkHandler.graph.graph[n].nodeGameObject.transform.position, length));
                            }
                        }
                        else
                        {
                            foreach (float length in knownLengths)
                            {
                                lengthSnapCirclesPointsOnly.Add(new Tuple<string, Vector3, float, int>("ocp" + n + "," + length + "," + n, networkHandler.graph.graph[n].nodeGameObject.transform.position, length, n));
                            }
                        }
                    }
                }
            }
        }

        //edge line x circle
        foreach (EdgeLine edgeLine in networkHandler.graph.edgeLines)
        {
            if (edgeLine.start.myNum != current && edgeLine.end.myNum != current)
            {
                foreach (Tuple<string, Vector3, float> lengthSnapCircle in lengthSnapCircles)
                {
                    Vector3[] intersections = IntersectionPoint(edgeLine.start.transform.position, edgeLine.end.transform.position, lengthSnapCircle.Item2, lengthSnapCircle.Item3);
                    Debug.Log("number of intersections: " + intersections.Count());

                    foreach (Vector3 intersection in intersections)
                    {
                        if (isPointOnLineSegment(edgeLine.start.transform.position, edgeLine.end.transform.position, intersection))
                        {
                            string intersectionString = intersection.x + "," + intersection.y;
                            stringKeyToVector3[intersectionString] = intersection;
                            if (!edgesConnectedToPoint.ContainsKey(intersectionString))
                                edgesConnectedToPoint[intersectionString] = new List<EdgeLine>();
                            if (!edgesConnectedToPoint[intersectionString].Contains(edgeLine))
                                edgesConnectedToPoint[intersectionString].Add(edgeLine);
                        }
                    }
                }
                foreach (Tuple<string, Vector3, float, int> lengthSnapCirclePointsOnly in lengthSnapCirclesPointsOnly)
                {
                    Vector3[] intersections = IntersectionPoint(edgeLine.start.transform.position, edgeLine.end.transform.position, lengthSnapCirclePointsOnly.Item2, lengthSnapCirclePointsOnly.Item3);
                    Debug.Log("number of intersections: " + intersections.Count());

                    foreach (Vector3 intersection in intersections)
                    {
                        if (isPointOnLineSegment(edgeLine.start.transform.position, edgeLine.end.transform.position, intersection))
                        {
                            string intersectionString = intersection.x + "," + intersection.y;
                            stringKeyToVector3[intersectionString] = intersection;
                            if (!edgesConnectedToPoint.ContainsKey(intersectionString))
                                edgesConnectedToPoint[intersectionString] = new List<EdgeLine>();
                            if (!edgesConnectedToPoint[intersectionString].Contains(edgeLine))
                                edgesConnectedToPoint[intersectionString].Add(edgeLine);
                        }
                    }
                }
            }
        }

        //angle snap line x one circle
        foreach (Tuple<string, Ray> angleSnapLine in angleSnapLines.Concat(selfSnapLines))
        {
            foreach (Tuple<string, Vector3, float> lengthSnapCircle in lengthSnapCircles)
            {
                Vector3[] intersections = IntersectionPoint(angleSnapLine.Item2.origin, angleSnapLine.Item2.direction + angleSnapLine.Item2.origin, lengthSnapCircle.Item2, lengthSnapCircle.Item3);
                Debug.Log("number of intersections: " + intersections.Count());

                foreach (Vector3 intersection in intersections)
                {
                    if (isPointOnRay(intersection, angleSnapLine.Item2))
                    {
                        string intersectionString = intersection.x + "," + intersection.y;
                        stringKeyToVector3[intersectionString] = intersection;
                        if (!edgesConnectedToPoint.ContainsKey(intersectionString))
                            edgesConnectedToPoint[intersectionString] = new List<EdgeLine>();
                    }
                }
            }
        }
    }


    Vector3[] IntersectionPoint(Vector3 p1, Vector3 p2, Vector3 center, float radius)
    {
        //  get the distance between X and Z on the segment
        Vector3 dp = p2 - p1;

        float a = Vector3.Dot(dp, dp);
        float b = 2 * Vector3.Dot(dp, p1 - center);
        float c = Vector3.Dot(center, center) - 2 * Vector3.Dot(center, p1) + Vector3.Dot(p1, p1) - radius * radius;
        float bb4ac = b * b - 4 * a * c;
        if (Mathf.Abs(a) < float.Epsilon || bb4ac < 0)
        {
            //  line does not intersect
            return new Vector3[] { };
        }
        float mu1 = (-b + Mathf.Sqrt(bb4ac)) / (2 * a);
        float mu2 = (-b - Mathf.Sqrt(bb4ac)) / (2 * a);
        Vector3[] sect = new Vector3[2];
        sect[0] = p1 + mu1 * (p2 - p1);
        sect[1] = p1 + mu2 * (p2 - p1);

        return sect;
    }

    public bool isPointOnLineSegment(Vector3 line1, Vector3 line2, Vector3 point)
    {
        float xMin = Mathf.Min(line1.x, line2.x);
        float xMax = Mathf.Max(line1.x, line2.x);

        float yMin = Mathf.Min(line1.y, line2.y);
        float yMax = Mathf.Max(line1.y, line2.y);

        return point.x >= xMin && point.x <= xMax && point.y >= yMin && point.y <= yMax;
    }

    public List<float> knownAngles = new List<float>();

    public List<float> knownLengths = new List<float>();

    public Tuple<string, Vector3> getNearestFakeLine(Vector3 pos)
    {
        Tuple<string, Ray> closestRay = null;
        float closestRayDistance = Mathf.Infinity;
        Vector3 closestRayPoint = new Vector3();
        foreach (Tuple<string, Ray> ray in angleSnapLines.Concat(selfSnapLines))
        {
            float currentDist = Vector3.Cross(ray.Item2.direction, pos - ray.Item2.origin).magnitude;
            Vector3 rayPoint = ray.Item2.origin + ray.Item2.direction * Vector3.Dot(ray.Item2.direction, pos - ray.Item2.origin);
            if (currentDist < closestRayDistance && isPointOnRay(rayPoint, ray.Item2))
            {
                closestRayPoint = rayPoint;
                closestRayDistance = currentDist;
                closestRay = ray;
            }
        }

        Tuple<string, Vector3, float> closestCircle = null;
        float closestCircleDistance = Mathf.Infinity;
        Vector3 closestCirclePoint = new Vector3();
        foreach (Tuple<string, Vector3, float> circle in lengthSnapCircles)
        {
            float currentDist = distancePointToCircle(pos, circle.Item2, circle.Item3);
            if (currentDist < closestCircleDistance)
            {
                closestCirclePoint = closestPointToCircle(pos, circle.Item2, circle.Item3);
                closestCircleDistance = currentDist;
                closestCircle = circle;
            }
        }

        if (closestRayDistance <= NetworkHandler.lineSnapDistance || closestCircleDistance <= NetworkHandler.lineSnapDistance)
        {
            if (closestRayDistance < closestCircleDistance)
                return new Tuple<string, Vector3>(closestRay.Item1, closestRayPoint);
            else
                return new Tuple<string, Vector3>(closestCircle.Item1, closestCirclePoint);
        }
        return null;
    }

    //inefficient using these functions should combine them and do just one call in getNearestFakeLine
    public float distancePointToCircle(Vector3 point, Vector3 circleCenter, float radius)
    {
        Vector3 closestPoint = closestPointToCircle(point, circleCenter, radius);
        return Vector3.Distance(closestPoint, point);
    }
    public Vector3 closestPointToCircle(Vector3 P, Vector3 C, float R)
    {
        Vector3 V = P - C;
        return C + V / Vector3.Magnitude(V) * R;
    }

    public bool isPointOnRay(Vector3 point, Ray ray)
    {
        Vector3 normalized = ray.direction;
        point -= ray.origin;

        bool withinX = false;
        if (normalized.x >= 0 && point.x >= 0)
            withinX = true;
        if (normalized.x <= 0 && point.x <= 0)
            withinX = true;
        bool withinY = false;
        if (normalized.y >= 0 && point.y >= 0)
            withinY = true;
        if (normalized.y <= 0 && point.y <= 0)
            withinY = true;

        return !(withinX && withinY);
    }

    public Tuple<int, int> imaginaryEdgeLine;
    public Tuple<string, Vector3> getNearestRealLine(Vector3 pos, int[] excludeIds)
    {
        Tuple<int, int> closest = null;
        float closestDistance = Mathf.Infinity;
        Vector2? closestIntersectPoint = null;
        foreach (EdgeLine edgeLine in networkHandler.graph.edgeLines)
        {
            if (! excludeIds.Contains<int>(edgeLine.start.myNum) && ! excludeIds.Contains<int>(edgeLine.end.myNum))
            {
                float thisDistance;
                Vector2 thisIntersectPoint;
                PointSegmentDistanceSquared(pos, edgeLine.start.transform.position, edgeLine.end.transform.position, out thisDistance, out thisIntersectPoint);
                if (thisDistance < closestDistance)
                {
                    closest = new Tuple<int, int>(edgeLine.start.myNum, edgeLine.end.myNum);
                    closestDistance = thisDistance;
                    closestIntersectPoint = thisIntersectPoint;
                }
            }
        }
        if (imaginaryEdgeLine != null)
        {
            float imaginaryEdgeLineDistance;
            Vector2 imaginaryEdgeLineIntersectPoint;
            PointSegmentDistanceSquared(pos, networkHandler.graph.graph[imaginaryEdgeLine.Item1].nodeGameObject.transform.position, networkHandler.graph.graph[imaginaryEdgeLine.Item2].nodeGameObject.transform.position, out imaginaryEdgeLineDistance, out imaginaryEdgeLineIntersectPoint);
            if (imaginaryEdgeLineDistance <= closestDistance)
            {
                closest = new Tuple<int, int>(imaginaryEdgeLine.Item1, imaginaryEdgeLine.Item2); //copy
                closestDistance = imaginaryEdgeLineDistance;
                closestIntersectPoint = imaginaryEdgeLineIntersectPoint;
            }
        }
        if (closestDistance <= NetworkHandler.lineSnapDistance)
        {
            return new Tuple<string, Vector3>("l" + closest.Item1 + "," + closest.Item2, closestIntersectPoint.Value);
        } else
        {
            return null;
        }
    }

    //Gets the node with the nearest world position to a given position, within a certain distance
    public string getNearestImaginaryPointWithinDistance(Vector3 pos, float dist)
    {
        Vector3? tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = pos;
        string id = null;
        foreach (string k in stringKeyToVector3.Keys)
        {
            float distance = Vector3.Distance(stringKeyToVector3[k], currentPos);
            if (distance < minDist)
            {
                tMin = stringKeyToVector3[k];
                id = k;
                minDist = distance;
            }
        }

        if (tMin != null && Vector3.Distance(tMin.Value, pos) <= dist)
        {
            return id;
        }
        else
            return null;
    }

    public static Vector3? lineIntersection(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        // Line AB represented as a1x + b1y = c1
        double a1 = B.y - A.y;
        double b1 = A.x - B.x;
        double c1 = a1 * (A.x) + b1 * (A.y);

        // Line CD represented as a2x + b2y = c2
        double a2 = D.y - C.y;
        double b2 = C.x - D.x;
        double c2 = a2 * (C.x) + b2 * (C.y);

        double determinant = a1 * b2 - a2 * b1;

        if (determinant == 0)
        {
            // The lines are parallel. This is simplified
            // by returning a pair of FLT_MAX
            return null;
        }
        else
        {
            double x = (b2 * c1 - b1 * c2) / determinant;
            double y = (a1 * c2 - a2 * c1) / determinant;
            return new Vector3((float)x, (float)y);
        }
    }

    //Should Backup and delete imaginary line between two real points currently being snapped on here using same system
    Dictionary<int, List<int>> adjacencyBackup;
    public Dictionary<int, bool> wasAngleActive = null;
    public Dictionary<int, bool> wasLengthActive = null;
    public int oldNodeId = -1;
    public int justPointSnapped = -1;
    public void BackupAdjacencies(int currentNodeId) //had to backup everything
    {
        adjacencyBackup = new Dictionary<int, List<int>>();
        wasAngleActive = new Dictionary<int, bool>();
        wasLengthActive = new Dictionary<int, bool>();
        //adjacencyBackup[currentNodeId] = new List<int>(networkHandler.graph.graph[currentNodeId].adjacentNodes);
        for (int i = 0; i < networkHandler.graph.getNodeCountId(); i++)
        {
            if (networkHandler.graph.graph[i] != null)
            {
                adjacencyBackup[i] = new List<int>(networkHandler.graph.graph[i].adjacentNodes);
                wasAngleActive[i] = networkHandler.graph.graph[i].nodeGameObject.isActivated(true);
                wasLengthActive[i] = networkHandler.graph.graph[i].nodeGameObject.isActivated(false);
            }
        }
    }

    public List<int> dontDeactivate = null; //serves similar purpose as used by imaginary edge line, in not deactivating nodes forming an intersection when snapped to
    public void RestoreAdjacencies(Vector3 pos, int current) //maybe make a replace edges function that only removes/adds ones that it needs to
    {
        //recreate node
        if (oldNodeId != -1)
        {
            NodeObject nodeObject = Instantiate(networkHandler.nodeObject);
            nodeObject.setPos(pos);
            Debug.Log(pos);
            NetworkHandler.Graph.Node node = new NetworkHandler.Graph.Node(oldNodeId, nodeObject);
            networkHandler.graph.graph[oldNodeId] = node;
            node.nodeGameObject.isSnapping = false;
            if (networkHandler.graph.graph[current].nodeGameObject.isActivated(false))
                networkHandler.graph.activateNode(oldNodeId, false);
            oldNodeId = -1;
        }

        imaginaryEdgeLine = null; //do this here too
        dontDeactivate = null;

        justPointSnapped = -1;

        //very inefficient way to only delete necessary edges
        for (int i = 0; i < networkHandler.graph.getNodeCountId(); i++)
        {
            if (networkHandler.graph.graph.ContainsKey(i) && networkHandler.graph.graph[i] != null)
            {
                for (int j = 0; j < networkHandler.graph.getNodeCountId(); j++)
                {
                    if (networkHandler.graph.graph.ContainsKey(j) && networkHandler.graph.graph[j] != null)
                    {
                        if (adjacencyBackup.ContainsKey(i) && !adjacencyBackup[i].Contains(j))
                            networkHandler.deleteEdge(i, j);
                        else if (adjacencyBackup.ContainsKey(j) && !adjacencyBackup[j].Contains(i))
                            networkHandler.deleteEdge(i, j);
                    }
                }
            }
        }
        //foreach (int n in adjacencyBackup.Keys)
        //{
        //    Debug.Log("adjacencies for " + n + " : " + String.Join(", ", adjacencyBackup[n]));
        //    for (int i = networkHandler.graph.graph[n].adjacentNodes.Count - 1; i >= 0; i--)
        //    {
        //        if (!adjacencyBackup[n].Contains(i) || !adjacencyBackup.ContainsKey(i))
        //        {
        //            networkHandler.deleteEdge(n, i);
        //            Debug.Log("edge deleted: " + n + ", " + i);
        //        }
        //    }
        //    networkHandler.addAllEdges(n, adjacencyBackup[n]);
        //    //networkHandler.deleteAllEdges(n);
        //    //networkHandler.addAllEdges(n, adjacencyBackup[n]);
        //}
        foreach (int n in adjacencyBackup.Keys)
        {
            Debug.Log("backup: " + n + String.Join(" ,", adjacencyBackup[n]));
            networkHandler.addAllEdges(n, adjacencyBackup[n]);
            if (wasAngleActive[n])
                networkHandler.graph.activateNode(n, true);
            //else
            //    networkHandler.graph.deactivateNode(n, true); //added later //may not need, since nodes usually get before this deactivated anyway?
            if (wasLengthActive[n])
                networkHandler.graph.activateNode(n, false);
            //else
            //    networkHandler.graph.deactivateNode(n, false); //added later
        }
        adjacencyBackup = new Dictionary<int, List<int>>();
        wasAngleActive = new Dictionary<int, bool>();
        wasLengthActive = new Dictionary<int, bool>();

    }

    public Tuple<string, Vector3> getNearest(Vector3 mousePos, int[] excludeIds) //exclude id should be same as current node in intial snap but -1 in interm. snap
    {
        //real line snap should be its own category "l" along with fake line "f" for unique handling in snap functions
        //real line snap should check imaginary line backup above (if not null) along with all real lines
        NetworkHandler.Graph.Node nearestRealNode = networkHandler.graph.getNearestNodeWithinDistance(mousePos, NetworkHandler.snapDistance, excludeIds);
        string nearestImaginaryPoint = getNearestImaginaryPointWithinDistance(mousePos, NetworkHandler.snapDistance);

        if (nearestRealNode == null && nearestImaginaryPoint == null) //no nearby points so try lines instead
        {
            Tuple<string, Vector3> nearestFakeLine = getNearestFakeLine(mousePos);
            Tuple<string, Vector3> nearestRealLine = getNearestRealLine(mousePos, excludeIds);
            if (nearestFakeLine == null) return nearestRealLine;
            if (nearestRealLine == null) return nearestFakeLine;
            if (Vector3.Distance(mousePos, nearestRealLine.Item2) <= Vector3.Distance(mousePos, nearestFakeLine.Item2) || nearestFakeLine.Item1.Substring(0,3)=="olp") //added so self snap lines never interfere with real edge lines and cause bug
                return nearestRealLine;
            else
                return nearestFakeLine;
        }

        if (nearestRealNode == null) return new Tuple<string, Vector3>("i" + nearestImaginaryPoint, stringKeyToVector3[nearestImaginaryPoint]);
        if (nearestImaginaryPoint == null) return new Tuple<string, Vector3>("r" + nearestRealNode.myNum, nearestRealNode.nodeGameObject.transform.position);
        if (Vector3.Distance(mousePos, nearestRealNode.nodeGameObject.transform.position) <= Vector3.Distance(mousePos, stringKeyToVector3[nearestImaginaryPoint]))
            return new Tuple<string, Vector3>("r" + nearestRealNode.myNum, nearestRealNode.nodeGameObject.transform.position); //might need to change "r" id to nearestrealnode so that initial and interm snap recieve same ids. or collapse current and replacement into the id string
        else
            return new Tuple<string, Vector3>("i" + nearestImaginaryPoint, stringKeyToVector3[nearestImaginaryPoint]);
    }

    //public NetworkHandler.Graph.Node getSnap(Vector3 pos, int currentNodeId) //legacy
    //{
    //    return networkHandler.graph.getNearestNodeWithinDistance(pos, NetworkHandler.snapDistance, currentNodeId);
    //}

    public Tuple<string, Vector3> initialSnap(GameObject nodeToPlace, Vector3 mousePos, int currentNodeId, int excludeAdditional=-1)
    {
        //GetImaginaryLines here.
        GetImaginaryPoints(currentNodeId);
        Tuple<string, Vector3> nearest;
        if (excludeAdditional == -1)
            nearest = getNearest(mousePos, new int[] { currentNodeId });
        else
            nearest = getNearest(mousePos, new int[] { currentNodeId, excludeAdditional });
        if (nearest == null) return null;
        
        BackupAdjacencies(currentNodeId); //backup before doing anti extrication then do extric, could be done in nodeobject

        nodeToPlace.transform.position = nearest.Item2;

        networkHandler.graph.deactivateNode(currentNodeId, true);
        networkHandler.graph.deactivateNode(currentNodeId, false);

        if (nearest.Item1[0] == 'r')
        {
            //BackupAdjacencies(currentNodeId);

            oldNodeId = Int32.Parse(nearest.Item1.Remove(0, 1));

            //merge nodes
            List<int> myOldAdjacentNodes = new List<int>(networkHandler.graph.graph[currentNodeId].adjacentNodes);
            List<int> otherOldAdjacentNodes = new List<int>(networkHandler.graph.graph[oldNodeId].adjacentNodes);
            networkHandler.graph.deleteNode(oldNodeId, false);

            networkHandler.deleteAllEdges(currentNodeId);
            List<int> combined = new List<int>(myOldAdjacentNodes);
            combined.AddRange(otherOldAdjacentNodes);

            if (networkHandler.graph.graph[oldNodeId] == null)
                combined.RemoveAll(item => item == oldNodeId);
            combined.RemoveAll(item => item == currentNodeId);

            networkHandler.addAllEdges(currentNodeId, combined);

            if (wasLengthActive[oldNodeId])
                networkHandler.graph.activateNode(currentNodeId, false);

            if (wasAngleActive[oldNodeId]) //for when snapping a node onto another node collapsing one line on itself, resulting in a possibly angle activated node ending up with the same exact edge, and shouldnt deactivate
            {
                if (networkHandler.graph.graph[currentNodeId].adjacentNodes.Count == adjacencyBackup[oldNodeId].Count)
                {
                    networkHandler.graph.activateNode(currentNodeId, true);
                    dontDeactivate = new List<int>();
                    dontDeactivate.Add(currentNodeId);
                }
            }

            foreach (int i in networkHandler.graph.graph.Keys)
            {
                if (networkHandler.graph.graph[i] != null && i != currentNodeId) //added i != currentNodeId to fix bug
                {
                    if (wasAngleActive[i])
                        networkHandler.graph.activateNode(i, true);
                }
            }
            justPointSnapped = currentNodeId;

            networkHandler.graph.graph[currentNodeId].nodeGameObject.mouseNeverReleased = true;


        } else //if type 'i'?
        {
            oldNodeId = -1;
        }

        if (nearest.Item1[0] == 'i')
        {
            dontDeactivate = new List<int>();
            foreach (EdgeLine edge in edgesConnectedToPoint[nearest.Item1.Remove(0,1)])
            {
                int node1 = edge.start.myNum;
                int node2 = edge.end.myNum;

                networkHandler.deleteEdge(node1, node2);

                networkHandler.addEge(node1, currentNodeId);
                networkHandler.addEge(node2, currentNodeId);

                //justPointSnapped = currentNodeId;
                if (wasAngleActive[node1])
                {
                    networkHandler.graph.activateNode(node1, true);
                    dontDeactivate.Add(node1);
                }
                if (wasAngleActive[node2])
                {
                    networkHandler.graph.activateNode(node2, true);
                    dontDeactivate.Add(node2);
                }
            }
        }

        if (nearest.Item1[0] == 'l')
        {
            string withoutLabel = nearest.Item1.Remove(0, 1);
            string[] twoInts = withoutLabel.Split(',');
            imaginaryEdgeLine = new Tuple<int, int>(Int32.Parse(twoInts[0]), Int32.Parse(twoInts[1]));

            networkHandler.deleteEdge(imaginaryEdgeLine.Item1, imaginaryEdgeLine.Item2);
            networkHandler.addEge(imaginaryEdgeLine.Item1, currentNodeId);
            networkHandler.addEge(imaginaryEdgeLine.Item2, currentNodeId);
        }

        //this code deals with nodes snapping while having been activated, and deactivating if no longer within distance of the player
        //if (currentNodeId != -1 && networkHandler.isOutsidePlayerColliderRadius(nodeToPlace.transform.position)) //replace with isOutsidePlayerColliderRadius in networkhandler
        //{
        //    networkHandler.graph.deactivateNode(currentNodeId, true);
            
        //    if (wasLengthActive.ContainsKey(oldNodeId) && !wasLengthActive[oldNodeId])
        //    {
        //        networkHandler.graph.deactivateNode(currentNodeId, false);
        //    }
        //}

        foreach (EdgeLine edgeLine in networkHandler.graph.edgeLines) //added to fix bug where edgelines would fail to activate even though both nodes did. (when forming cross and sliding along it to center)
        {
            if (networkHandler.graph.graph[edgeLine.start.myNum].nodeGameObject.isActivated(false) && networkHandler.graph.graph[edgeLine.end.myNum].nodeGameObject.isActivated(false))
            {
                edgeLine.NodeActivated(edgeLine.end.myNum, false);
            }
        }

        return new Tuple<string, Vector3>(nearest.Item1, nearest.Item2);
    }

    public Tuple<string, Vector3> intermSnap(Vector3 mousePos, int currentNodeId, string currentSnapId)
    {
        //GetImaginaryPoints(currentNodeId); //if current node id AND current activated same, dont need to do this? or just set this based on snap id, "o"s work better with imaginarypoints every frame
        if (currentSnapId[0] == 'o')
        {
            GetImaginaryPoints(currentNodeId); //allows for simultaneous angle and length snap
        }
        if (currentSnapId[0] == 'l')
        {
            GetImaginaryPoints(currentNodeId, false); //might be able to remove this entire line (and "whole" case) once dormant points are added
        }

        Tuple<string, Vector3> nearest; //since exclude id is -1, getnearest will include the same nearestrealnode, so def should change the "r" id in getnearest to include nearestrealnode rather than current!
        if (currentSnapId[0] == 'l' || currentSnapId[0] == 'o')
        {
            nearest = getNearest(mousePos, new int[] { currentNodeId});
        } else
        {
            nearest = getNearest(mousePos, new int[] { });
        }

        if (nearest == null) return null;

        return new Tuple<string, Vector3>(nearest.Item1, nearest.Item2);
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Calculates the angles incident on a node, and returns them as a list of value objects in the correct positions
public class AngleCalculator : MonoBehaviour
{

    public static bool RoughlyEqual(float a, float b, float threshold)
    {
        return (Mathf.Abs(a - b) < threshold);
    }

    public static List<Value> GetAngles(NetworkHandler.Graph graph, NetworkHandler.Graph.Node centerNode, Value valueObject) //return List<Value>
    {
        List<Angle> angles = new List<Angle>();

        List<int> adjacentNodes = new List<int>(centerNode.adjacentNodes);

        if (adjacentNodes.Count <= 1)
            return new List<Value>();

        adjacentNodes.Sort((a, b) => lessInt(graph.graph[a].nodeGameObject.transform.position, graph.graph[b].nodeGameObject.transform.position, centerNode.nodeGameObject.transform.position));

        for (int i = 0; i < adjacentNodes.Count; i++)
        {
            int node1 = adjacentNodes[i];
            int node2 = adjacentNodes[(i + 1) % adjacentNodes.Count];

            angles.Add(new Angle(centerNode.nodeGameObject.transform.position, graph.graph[node1].nodeGameObject.transform.position, graph.graph[node2].nodeGameObject.transform.position));
        }

        string result = "List contents: ";
        foreach (var item in adjacentNodes)
        {
            result += item.ToString() + ", ";
        }
        Debug.Log("LIST: " + result);

        //for (int i = 0; i < adjacentNodes.Count; i++)
        //{
        //    for (int j = i + 1; j < adjacentNodes.Count; j++)
        //    {
        //        Vector3 leftNodePos = graph.graph[adjacentNodes[i]].nodeGameObject.transform.position;
        //        Vector3 rightNodePos = graph.graph[adjacentNodes[j]].nodeGameObject.transform.position;

        //        angles.Add(new Angle(centerNode.nodeGameObject.transform.position, leftNodePos, rightNodePos));
        //    }
        //}

        //This method is NOT sufficient yet in getting rid of the right angles all the time. See below comment.
        //angles.Sort(CompareAngles);
        //angles.RemoveRange(adjacentNodes.Count - 1, angles.Count - (adjacentNodes.Count - 1));
        /*
         * Store 2 node IDs in each Angle.
         * For any angle A and C:
         *  See if A to B AND B to C exist (B is the same, some angle)
         *      If so discard AC
         * (Start from bottom of sorted list and work way up)
         */

        //print out all found angles for testing
        //foreach (Angle a in angles)
        //{
        //    Debug.Log(a.angle);
        //}

        Vector3 centerPosition = graph.graph[centerNode.myNum].nodeGameObject.transform.position;

        List<Value> valueObjects = new List<Value>();
        foreach (Angle angle in angles)
        {
            Value newValue = Instantiate(valueObject);
            newValue.gameObject.transform.SetParent(centerNode.nodeGameObject.transform); //added recently
            newValue.Initialize(angle.angle, Value.Unit.Degrees, centerPosition + angle.displacement, centerNode.nodeGameObject.transform);
            valueObjects.Add(newValue);
        }

        return valueObjects;
    }

    //Used to compare which angle is bigger
    public static int CompareAngles(Angle a1, Angle a2)
    {
        return a1.angle.CompareTo(a2.angle);
    }

    //Stores and calculates a single angle and relative position
    public class Angle
    {
        //Actual value of angle
        public float angle;

        //Relative position
        public Vector3 displacement;

        //Calculates and returns the angle from a center point, and two vectors
        public Angle(Vector3 center, Vector3 left, Vector3 right)
        {

            /*
             * For obtuse angles >90, still need to make displacement negative for proper placement
             */

            left -= center;
            right -= center;

            left.z = 0;
            right.z = 0;

            //angle = Vector3.Angle(left, right);
            angle = SignedAngleBetween(left, right, Vector3.forward);

            displacement = Vector3.Normalize(left) + Vector3.Normalize(right);

            if (angle < 0)
            {
                displacement *= -1;
                angle += 360;
            }
            if (RoughlyEqual(angle, 180f, 0.1f))
            {
                displacement = Vector2.Perpendicular(left);
            }

            displacement = Vector3.Normalize(displacement) / 1.5f; //1.5 to make angle slightly closer
            displacement.z = 0; //was -1
        }
    }

    //public static bool less(Vector3 a, Vector3 b, Vector3 center)
    //{
    //    if (a.x - center.x >= 0 && b.x - center.x < 0)
    //        return true;
    //    if (a.x - center.x < 0 && b.x - center.x >= 0)
    //        return false;
    //    if (a.x - center.x == 0 && b.x - center.x == 0)
    //    {
    //        if (a.y - center.y >= 0 || b.y - center.y >= 0)
    //            return a.y > b.y;
    //        return b.y > a.y;
    //    }

    //    // compute the cross product of vectors (center -> a) x (center -> b)
    //    float det = (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y);
    //    if (det < 0)
    //        return true;
    //    if (det > 0)
    //        return false;

    //    // points a and b are on the same line from the center
    //    // check which point is closer to the center
    //    float d1 = (a.x - center.x) * (a.x - center.x) + (a.y - center.y) * (a.y - center.y);
    //    float d2 = (b.x - center.x) * (b.x - center.x) + (b.y - center.y) * (b.y - center.y);
    //    return d1 > d2;
    //}
    public static int lessInt(Vector3 a, Vector3 b, Vector3 center)
    {
        //return less(a, b, center) ? 1 : -1;
        return Mathf.Atan2(a.x - center.x, a.y - center.y) < Mathf.Atan2(b.x - center.x, b.y - center.y) ? 1 : -1;
    }

    public static float SignedAngleBetween(Vector3 a, Vector3 b, Vector3 n)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(a, b)));

        // angle in [-179,180]
        float signed_angle = angle * sign;

        // angle in [0,360] (not used but included here for completeness)
        //float angle360 =  (signed_angle + 180) % 360;

        return signed_angle;
    }

    //used for forming angle snap lines in Snap
    public static bool roomForAngle(NetworkHandler.Graph graph, int centerNode, int adjacentNode, int currentMovingNode, float angle)
    {
        Ray testRay = testRay = new Ray(graph.graph[centerNode].nodeGameObject.transform.position, Quaternion.Euler(0, 0, angle) * (graph.graph[centerNode].nodeGameObject.transform.position - graph.graph[adjacentNode].nodeGameObject.transform.position));
        Debug.LogWarning("this: " + adjacentNode);
        //bool checkAbove = angle > 0;
        List<int> adjacentNodes = new List<int>(graph.graph[centerNode].adjacentNodes);
        adjacentNodes.Add(-1);
        adjacentNodes.Remove(currentMovingNode);
        adjacentNodes.Sort((a, b) => lessInt(
            a != -1 ? graph.graph[a].nodeGameObject.transform.position : testRay.GetPoint(-10),
            b != -1 ? graph.graph[b].nodeGameObject.transform.position : testRay.GetPoint(-10),
            graph.graph[centerNode].nodeGameObject.transform.position));
        int testIndex = adjacentNodes.IndexOf(-1);
        //testIndex = mod((testIndex + 1), adjacentNodes.Count);
        Debug.LogWarning("Bad index: " + testIndex);

        string result = "Stupid list: ";
        foreach (var item in adjacentNodes)
        {
            result += item.ToString() + ", ";
        }
        Debug.LogWarning(result);

        if (adjacentNodes.Count <= 1) return true;
        //return (adjacentNodes[mod((testIndex + 1), adjacentNodes.Count)] == adjacentNode) || (adjacentNodes[mod((testIndex - 1), adjacentNodes.Count)] == adjacentNode);
        if (angle < 0)
            return (adjacentNodes[mod((testIndex + 1), adjacentNodes.Count)] == adjacentNode);
        else
            return (adjacentNodes[mod((testIndex - 1), adjacentNodes.Count)] == adjacentNode);
    }
    public static int mod(int a, int b)
    {
        return (((a % b) + b) % b);
    }
}

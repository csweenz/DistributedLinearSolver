using UnityEngine;
using System.Collections.Generic;

public class NearestNeighborsGraphVisualizer : MonoBehaviour
{
    public GameObject graphDisplay;  // The GameObject to hold the graph visualization
    public GameObject nodePrefab;    // Prefab representing a client node
    public LineRenderer linePrefab;  // Prefab representing connections between nodes

    private List<GameObject> nodes;
    private List<LineRenderer> edges;

    private void Start()
    {
        nodes = new List<GameObject>();
        edges = new List<LineRenderer>();
    }

    public void GenerateGraph(int numberOfClients)
    {
        // Clear existing nodes if any
        foreach (var node in nodes)
        {
            Destroy(node);
        }
        foreach (var edge in edges)
        {
            Destroy(edge.gameObject);
        }
        nodes.Clear();
        edges.Clear();

        // Instantiate nodes
        for (int i = 0; i < numberOfClients; i++)
        {
            Vector3 position = Random.insideUnitCircle * 5f;  // Random position within a circle
            GameObject node = Instantiate(nodePrefab, position, Quaternion.identity, graphDisplay.transform);
            node.name = $"Node_{i}";
            nodes.Add(node);
        }
    }

    public void UpdateGraphConnections(List<int[]> neighborPairs)
    {
        // Clear existing edges
        foreach (var edge in edges)
        {
            Destroy(edge.gameObject);
        }
        edges.Clear();

        // Create new edges based on neighbor pairs
        foreach (var pair in neighborPairs)
        {
            if (pair.Length == 2)
            {
                GameObject nodeA = nodes[pair[0]];
                GameObject nodeB = nodes[pair[1]];
                LineRenderer edge = Instantiate(linePrefab, graphDisplay.transform);
                edge.SetPosition(0, nodeA.transform.position);
                edge.SetPosition(1, nodeB.transform.position);
                edges.Add(edge);
            }
        }
    }
}
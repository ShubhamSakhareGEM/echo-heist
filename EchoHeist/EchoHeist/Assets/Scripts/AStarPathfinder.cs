using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    public static List<NavNode> FindPath(
        NavNode start,
        NavNode goal)
    {
        List<NavNode> emptyPath = new List<NavNode>();

        if (start == null || goal == null)
        {
            return emptyPath;
        }

        List<NavNode> openSet = new List<NavNode>
        {
            start
        };

        HashSet<NavNode> closedSet =
            new HashSet<NavNode>();

        Dictionary<NavNode, NavNode> cameFrom =
            new Dictionary<NavNode, NavNode>();

        Dictionary<NavNode, float> gScore =
            new Dictionary<NavNode, float>();

        Dictionary<NavNode, float> fScore =
            new Dictionary<NavNode, float>();

        gScore[start] = 0f;

        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            NavNode current = GetLowestScoreNode(
                openSet,
                fScore
            );

            if (current == goal)
            {
                return ReconstructPath(
                    cameFrom,
                    current
                );
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (NavNode neighbor in current.Neighbors)
            {
                if (neighbor == null ||
                    closedSet.Contains(neighbor))
                {
                    continue;
                }

                float tentativeGScore =
                    GetScore(gScore, current) +
                    Vector2.Distance(
                        current.Position,
                        neighbor.Position
                    );

                if (tentativeGScore <
                    GetScore(gScore, neighbor))
                {
                    cameFrom[neighbor] = current;

                    gScore[neighbor] =
                        tentativeGScore;

                    fScore[neighbor] =
                        tentativeGScore +
                        Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return emptyPath;
    }

    private static NavNode GetLowestScoreNode(
        List<NavNode> nodes,
        Dictionary<NavNode, float> scores)
    {
        NavNode bestNode = nodes[0];
        float bestScore = GetScore(scores, bestNode);

        foreach (NavNode node in nodes)
        {
            float score = GetScore(scores, node);

            if (score < bestScore)
            {
                bestNode = node;
                bestScore = score;
            }
        }

        return bestNode;
    }

    private static float GetScore(
        Dictionary<NavNode, float> scores,
        NavNode node)
    {
        if (scores.TryGetValue(node, out float score))
        {
            return score;
        }

        return float.PositiveInfinity;
    }

    private static float Heuristic(
        NavNode a,
        NavNode b)
    {
        return Vector2.Distance(
            a.Position,
            b.Position
        );
    }

    private static List<NavNode> ReconstructPath(
        Dictionary<NavNode, NavNode> cameFrom,
        NavNode current)
    {
        List<NavNode> path = new List<NavNode>
        {
            current
        };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];

            path.Add(current);
        }

        path.Reverse();

        return path;
    }
}
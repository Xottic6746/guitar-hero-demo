using UnityEngine;

public class Note : MonoBehaviour
{
    public int lane;
    public float spawnTime;
    public bool wasHit;

    public void Initialize(int laneIndex, float time)
    {
        lane = laneIndex;
        spawnTime = time;
        wasHit = false;
    }

    public void OnHit(bool perfect)
    {
        wasHit = true;
        string result = perfect ? "Perfect!" : "Good!";
        Debug.Log(result + " note hit on lane " + lane);
        Destroy(gameObject, 0.1f);
    }

    public void OnMiss()
    {
        Debug.Log("Missed note on lane " + lane);
        Destroy(gameObject, 0.1f);
    }
}

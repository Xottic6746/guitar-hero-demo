using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Transform noteParent;
    public float laneWidth = 2f;
    public float spawnAheadSeconds = 2f;

    private readonly List<float> beatMap = new();
    private float songLength;
    private float noteSpeed;
    private float startTime;

    private void Start()
    {
        CreateDemoPattern();
    }

    public void StartSpawning(float length, float speed)
    {
        songLength = length;
        noteSpeed = speed;
        startTime = Time.time;
        SpawnPattern();
    }

    private void CreateDemoPattern()
    {
        for (int i = 0; i < 32; i++)
        {
            beatMap.Add(0.75f + i * 0.5f);
        }
    }

    private void SpawnPattern()
    {
        if (notePrefab == null)
        {
            Debug.LogWarning("Note prefab is missing. Assign a note prefab in the inspector.");
            return;
        }

        foreach (var beat in beatMap)
        {
            int lane = Random.Range(0, 4);
            GameObject obj = Instantiate(notePrefab, noteParent != null ? noteParent : transform);
            obj.transform.localPosition = new Vector3((lane - 1.5f) * laneWidth, 0f, 0f);

            Note note = obj.GetComponent<Note>();
            if (note != null)
            {
                note.Initialize(lane, beat);
            }

            // Allow scaling for a note trail effect
            obj.transform.localScale = Vector3.one * 0.7f;
        }
    }
}

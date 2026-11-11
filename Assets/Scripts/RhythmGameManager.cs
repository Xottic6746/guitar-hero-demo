using System.Collections.Generic;
using UnityEngine;

public class RhythmGameManager : MonoBehaviour
{
    [Header("References")]
    public NoteSpawner noteSpawner;
    public Transform noteParent;

    [Header("Settings")]
    public int totalLanes = 4;
    public float songTime;
    public float noteSpeed = 8f;
    public float perfectWindow = 0.12f;
    public float goodWindow = 0.2f;
    public float missWindow = 0.3f;

    private readonly List<Note> activeNotes = new();
    private float timeSinceStart;
    private bool isPlaying;
    private int score;
    private int combo;
    private int maxCombo;
    private int health = 100;

    public int Score => score;
    public int Combo => combo;
    public int MaxCombo => maxCombo;
    public int Health => health;
    public bool IsPlaying => isPlaying;

    private void Update()
    {
        if (!isPlaying) return;

        timeSinceStart += Time.deltaTime;

        if (timeSinceStart >= songTime)
        {
            EndSong();
            return;
        }

        UpdateActiveNotes();
    }

    public void StartSong()
    {
        if (isPlaying) return;

        isPlaying = true;
        timeSinceStart = 0f;
        score = 0;
        combo = 0;
        maxCombo = 0;
        health = 100;

        if (noteSpawner != null)
        {
            noteSpawner.StartSpawning(songTime, noteSpeed);
        }
    }

    public void EndSong()
    {
        isPlaying = false;
        Debug.Log($"Song complete. Final Score: {score}, Max Combo: {maxCombo}, Health: {health}");
    }

    public void RegisterNote(Note note)
    {
        activeNotes.Add(note);
    }

    public void HitNote(int lane, float hitTime)
    {
        Note hit = activeNotes.Find(note => note.lane == lane && !note.wasHit && Mathf.Abs(note.spawnTime - hitTime) < missWindow);

        if (hit == null)
        {
            return;
        }

        float delta = Mathf.Abs(hit.spawnTime - hitTime);

        if (delta <= perfectWindow)
        {
            score += 1000;
            combo += 1;
            maxCombo = Mathf.Max(maxCombo, combo);
            health = Mathf.Min(100, health + 5);
            hit.OnHit(true);
        }
        else if (delta <= goodWindow)
        {
            score += 700;
            combo += 1;
            maxCombo = Mathf.Max(maxCombo, combo);
            hit.OnHit(false);
        }
        else
        {
            MissNote(hit);
        }

        activeNotes.Remove(hit);
    }

    public void MissNote(Note note)
    {
        if (note == null || note.wasHit) return;

        combo = 0;
        health = Mathf.Max(0, health - 15);
        note.OnMiss();
        activeNotes.Remove(note);
    }

    private void UpdateActiveNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            Note note = activeNotes[i];
            float currentTravel = timeSinceStart - note.spawnTime;

            if (currentTravel > missWindow)
            {
                MissNote(note);
            }
        }
    }
}

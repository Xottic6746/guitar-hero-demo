using UnityEngine;

public class GuitarInput : MonoBehaviour
{
    public RhythmGameManager gameManager;
    public KeyCode[] laneKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };

    private void Update()
    {
        if (gameManager == null) return;

        for (int i = 0; i < laneKeys.Length; i++)
        {
            if (Input.GetKeyDown(laneKeys[i]))
            {
                gameManager.HitNote(i, Time.timeSinceLevelLoad);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && !gameManager.IsPlaying)
        {
            gameManager.StartSong();
        }
    }
}

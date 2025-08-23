using UnityEngine;
using UnityEngine.SceneManagement;

public class HubPuzzleRoom : MonoBehaviour, IPuzzleRoom
{
    [SerializeField] private SocketPad[] sockets;
    [SerializeField] private string finalCutsceneScene;
    [SerializeField] private Transform finalTeleportPoint; //optional

    public void Init(Room room, DungeonMapIndex.RoomIndex ri)
    {
        // Positioning is handled by the placer
    }

    void Update()
    {
        if (AllFilled()) OnSolved();
    }

    bool AllFilled()
    {
        if (sockets == null || sockets.Length == 0) return false;
        for (int i = 0; i < sockets.Length; i++)
            if (!sockets[i] || !sockets[i].filled) return false;
        return true;
    }

    bool solvedInvoked = false;
    void OnSolved()
    {
        if (solvedInvoked) return;
        solvedInvoked = true;

        // Option A: load final scene
        if (!string.IsNullOrEmpty(finalCutsceneScene))
        {
            SceneManager.LoadScene(finalCutsceneScene);
            return;
        }

        // Option B: teleport player to a finale point in this scene
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player && finalTeleportPoint)
            player.transform.position = finalTeleportPoint.position;
    }
}

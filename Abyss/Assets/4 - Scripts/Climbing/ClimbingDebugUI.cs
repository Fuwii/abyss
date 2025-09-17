using UnityEngine;

public class ClimbingDebugUI : MonoBehaviour
{
    public FpsPlayerClimbing player;

    private void OnGUI()
    {
        if (player == null) return;

        GUILayout.BeginArea(new Rect(10, 100, 300, 300));
        GUILayout.Label("<b>Climbing System Debug</b>", new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("State: " + player.state.ToString());
        GUILayout.Label("SubState: " + player.climbingState.ToString());
        GUILayout.Label("Can Climb: " + (ClimbDetector.CanClimb(player.transform, Camera.main ? Camera.main.transform : null, player.climbDetectionDistance, player.climbableLayers, 70f, 0.7f)));
        if (player) GUILayout.Label("Velocity: " + (player.GetComponent<Rigidbody>()?.linearVelocity.ToString("F2") ?? "null"));
        if (player.playerStamina != null)
        {
            float staminaPercent = player.playerStamina.GetCurrentStamina() / player.playerStamina.GetCurrentMaxBaseStamina() * 100f;
            GUILayout.Label("Stamina: " + staminaPercent.ToString("F1") + "%");
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

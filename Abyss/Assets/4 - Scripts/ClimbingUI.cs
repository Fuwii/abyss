//using UnityEngine;
//using UnityEngine.UI;

//public class ClimbingUI : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private FpsPlayerClimbing climbingSystem;
//    [SerializeField] private PlayerStamina staminaSystem;
    
//    [Header("UI Elements")]
//    [SerializeField] private Image staminaBar;
//    [SerializeField] private GameObject climbingIndicator;
//    [SerializeField] private GameObject climbablePrompt;
    
//    [Header("Settings")]
//    [SerializeField] private Color normalStaminaColor = Color.green;
//    [SerializeField] private Color lowStaminaColor = Color.red;
//    [SerializeField] private float lowStaminaThreshold = 0.3f;
    
//    private Camera playerCamera;
//    private float climbDetectionDistance = 2f;
//    private LayerMask climbableLayers;
    
//    private void Start()
//    {
//        if (!climbingSystem)
//            climbingSystem = FindObjectOfType<FpsPlayerClimbing>();
            
//        if (!staminaSystem)
//            staminaSystem = FindObjectOfType<PlayerStamina>();
            
//        playerCamera = Camera.main;
        
//        // Get the climbable layers from the climbing system
//        if (climbingSystem)
//            climbDetectionDistance = climbingSystem.GetClimbDetectionDistance();
            
//        // Initialize UI elements
//        if (climbingIndicator)
//            climbingIndicator.SetActive(false);
            
//        if (climbablePrompt)
//            climbablePrompt.SetActive(false);
//    }
    
//    private void Update()
//    {
//        UpdateStaminaBar();
//        UpdateClimbingIndicator();
//        UpdateClimbablePrompt();
//    }
    
//    private void UpdateStaminaBar()
//    {
//        if (staminaBar && staminaSystem)
//        {
//            float staminaPercent = staminaSystem.GetStamina01();
//            staminaBar.fillAmount = staminaPercent;
            
//            // Change color based on stamina level
//            staminaBar.color = staminaPercent <= lowStaminaThreshold ? lowStaminaColor : normalStaminaColor;
//        }
//    }
    
//    private void UpdateClimbingIndicator()
//    {
//        if (climbingIndicator && climbingSystem)
//        {
//            // Show climbing indicator when in climbing state
//            climbingIndicator.SetActive(climbingSystem.state == FpsPlayerClimbing.PlayerState.CLIMBING);
//        }
//    }
    
//    private void UpdateClimbablePrompt()
//    {
//        if (climbablePrompt && playerCamera && climbingSystem)
//        {
//            // Show prompt when looking at a climbable surface but not currently climbing
//            bool showPrompt = false;
            
//            if (climbingSystem.state != FpsPlayerClimbing.PlayerState.CLIMBING)
//            {
//                RaycastHit hit;
//                if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, climbDetectionDistance, climbingSystem.GetClimbableLayers()))
//                {
//                    showPrompt = true;
//                    // Update prompt text to show LMB instead of E key
//                    if (climbablePrompt.GetComponentInChildren<UnityEngine.UI.Text>())
//                    {
//                        climbablePrompt.GetComponentInChildren<UnityEngine.UI.Text>().text = "Left-click to climb";
//                    }
//                }
//            }
            
//            climbablePrompt.SetActive(showPrompt);
//        }
//    }
//}
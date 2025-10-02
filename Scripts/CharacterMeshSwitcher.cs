using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMeshSwitcher : MonoBehaviour
{
    [Header("Character Groups")]
    public GameObject[] characterGroups; // Array of character groups, each group is a GameObject with its own set of characters

    [Header("Input Action Reference")]
    [SerializeField] private InputActionReference characterSwitchButton;

    private int currentGroupIndex = 0; // Track the currently active group
    private bool isIndexButtonTriggered = false;

    void Start()
    {
        if (characterGroups == null || characterGroups.Length == 0)
        {
            Debug.LogError("No character groups assigned to CharacterMeshSwitcher!");
            return;
        }

        if (characterSwitchButton == null || characterSwitchButton.action == null)
        {
            Debug.LogError("InputActionReference for character switch not assigned or invalid!");
            return;
        }

        // Subscribe to input action events
        characterSwitchButton.action.performed += OnSwitchCharacterGroup;
        characterSwitchButton.action.Enable();

        // Initially hide all character groups except the first
        UpdateCharacterGroupVisibility();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (characterSwitchButton != null && characterSwitchButton.action != null)
        {
            characterSwitchButton.action.performed -= OnSwitchCharacterGroup;
        }
    }

    private void OnSwitchCharacterGroup(InputAction.CallbackContext context)
    {
        isIndexButtonTriggered = true; 
    }

    void Update()
    {
        if (isIndexButtonTriggered)
        {
            SwitchCharacterGroup();
            isIndexButtonTriggered = false; 
        }
    }

    private void SwitchCharacterGroup()
    {
        if (characterGroups.Length == 0) return;

        // Deactivate current character group
        SetCharacterGroupVisibility(characterGroups[currentGroupIndex], false);

        // Switch to the next character group
        currentGroupIndex = (currentGroupIndex + 1) % characterGroups.Length;

        SetCharacterGroupVisibility(characterGroups[currentGroupIndex], true);

        Debug.Log($"Switched to character group: {characterGroups[currentGroupIndex].name}");
    }

    private void UpdateCharacterGroupVisibility()
    {
        for (int i = 0; i < characterGroups.Length; i++)
        {
            characterGroups[i].SetActive(true);
            SetCharacterGroupVisibility(characterGroups[i], i == currentGroupIndex);
        }
    }

    private void SetCharacterGroupVisibility(GameObject characterGroup, bool isVisible)
    {
        // Set visibility of all children in the group
        foreach (Transform child in characterGroup.transform)
        {
            SkinnedMeshRenderer[] meshRenderers = child.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (meshRenderers.Length == 0)
            {
                Debug.LogWarning($"Character group {characterGroup.name} has no SkinnedMeshRenderer components!");
            }

            foreach (var renderer in meshRenderers)
            {
                renderer.enabled = isVisible;
            }
        }
    }
}

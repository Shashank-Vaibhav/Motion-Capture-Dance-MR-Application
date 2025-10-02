using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CharacterSpawner : MonoBehaviour
{
    [Header("XR Components")]
    public XRRayInteractor rayInteractor;   
    [SerializeField] private InputActionReference aButtonAction; 

    [Header("Character Prefabs")]
    public List<GameObject> characterPrefabs; 

    private int currentIndex = -1; 
    private GameObject activeCharacter; 

    private void Start()
    {
        if (characterPrefabs == null || characterPrefabs.Count == 0)
        {
            Debug.LogError("No character prefabs assigned to CharacterSpawnerSwitcher!");
            return;
        }

        if (aButtonAction == null || aButtonAction.action == null)
        {
            Debug.LogError("No InputActionReference assigned for A button!");
            return;
        }

        // Subscribe to A button
        aButtonAction.action.performed += OnAButtonPressed;
        aButtonAction.action.Enable();
    }

    private void OnDestroy()
    {
        if (aButtonAction != null && aButtonAction.action != null)
        {
            aButtonAction.action.performed -= OnAButtonPressed;
        }
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Debug.Log("No valid surface detected for spawning.");
            return;
        }

        if (activeCharacter != null)
        {
            Destroy(activeCharacter);
        }

        currentIndex = (currentIndex + 1) % characterPrefabs.Count;

        activeCharacter = Instantiate(characterPrefabs[currentIndex], hit.point, Quaternion.identity);

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 lookAtPosition = new Vector3(cameraPosition.x, activeCharacter.transform.position.y, cameraPosition.z);
        activeCharacter.transform.LookAt(lookAtPosition);

        Debug.Log($"Spawned {characterPrefabs[currentIndex].name} at {hit.point}");
    }
}

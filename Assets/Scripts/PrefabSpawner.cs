using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Plane Manager and Ray Interactor")]
    public ARPlaneManager planeManager;
    public XRRayInteractor rayInteractor;

    [Header("Prefab Settings")]
    public GameObject prefabToBeSpawnedOnTable;
    public GameObject prefabToBeSpawnedOnFloor;

    [Header("Classification Settings")]
    public PlaneClassification classificationFloor = PlaneClassification.Floor;
    public PlaneClassification classificationTable = PlaneClassification.Table;

    [Header("Debug UI")]
    public TextMeshProUGUI debugText;

    [Header("Line Renderer Settings")]
    public LineRenderer rayInteractorLineRenderer;
    public Material rayInteractorLineMaterial;
    public Material defaultLineRendererMaterial;

    private Dictionary<PlaneClassification, List<ARPlane>> classifiedPlanes = new();
    private ObjectPool tableObjectPool;
    private ObjectPool floorObjectPool;
    private GameObject activeTablePrefab;
    private GameObject activeFloorPrefab;

    private void OnEnable()
    {
        planeManager.planesChanged += OnPlanesChanged;
    }

    private void OnDisable()
    {
        planeManager.planesChanged -= OnPlanesChanged;
    }

    private void Start()
    {
        // Initialize dictionary for classified planes
        classifiedPlanes[classificationFloor] = new List<ARPlane>();
        classifiedPlanes[classificationTable] = new List<ARPlane>();

        // Initialize object pools
        tableObjectPool = new ObjectPool(prefabToBeSpawnedOnTable, 3);
        floorObjectPool = new ObjectPool(prefabToBeSpawnedOnFloor, 3);

        // Subscribe to ray interactor events
        rayInteractor.selectEntered.AddListener(OnSelectEntered);
        rayInteractor.selectExited.AddListener(OnSelectExited);
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            if (plane.classification == classificationFloor)
                classifiedPlanes[classificationFloor].Add(plane);
            else if (plane.classification == classificationTable)
                classifiedPlanes[classificationTable].Add(plane);
        }

        debugText.text = $"Floor Planes: {classifiedPlanes[classificationFloor].Count}, Table Planes: {classifiedPlanes[classificationTable].Count}";
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rayHit);

        // Check for table plane interaction
        if (TryTogglePrefab(rayHit, classifiedPlanes[classificationTable], tableObjectPool, ref activeTablePrefab))
        {
            return;
        }

        // Check for floor plane interaction
        TryTogglePrefab(rayHit, classifiedPlanes[classificationFloor], floorObjectPool, ref activeFloorPrefab);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        rayInteractorLineRenderer.material = defaultLineRendererMaterial;
    }

    private bool TryTogglePrefab(RaycastHit rayHit, List<ARPlane> planes, ObjectPool objectPool, ref GameObject activePrefab)
    {
        foreach (var plane in planes)
        {
            if (!plane.TryGetComponent<MeshCollider>(out MeshCollider meshCollider) || rayHit.collider != meshCollider)
                continue;

            rayInteractorLineRenderer.material = rayInteractorLineMaterial;

            // Toggle the prefab state (spawn or despawn)
            if (activePrefab == null)
            {
                // Spawn the prefab
                activePrefab = objectPool.GetObject();
                activePrefab.transform.position = rayHit.point;

                // Make the prefab face the camera
                Vector3 cameraPosition = Camera.main.transform.position;
                Vector3 lookAtPosition = new Vector3(cameraPosition.x, activePrefab.transform.position.y, cameraPosition.z);
                activePrefab.transform.LookAt(lookAtPosition);

                activePrefab.SetActive(true);  // Make sure it's active when spawned

                if (activePrefab.TryGetComponent<XRSimpleInteractable>(out XRSimpleInteractable interactable))
                {
                    // Add listener to handle toggling on click
                    interactable.selectEntered.AddListener(OnPrefabSelected);
                }
            }
            else
            {
                // If prefab is already active, despawn it (deactivate it)
                DeactivatePrefab(objectPool, activePrefab);
                activePrefab = null;  // Reset the activePrefab reference to null
            }

            return true;
        }

        return false;
    }

    private void OnPrefabSelected(SelectEnterEventArgs args)
    {
        // Toggle prefab visibility upon selection
        if (args.interactableObject.transform.TryGetComponent(out GameObject prefab))
        {
            if (prefab == activeTablePrefab || prefab == activeFloorPrefab)
            {
                DeactivatePrefab(GetObjectPoolForPrefab(prefab), prefab);
                activeTablePrefab = null;
                activeFloorPrefab = null;
            }
            else
            {
                // Spawn a new prefab if it's not the current active one
                TryTogglePrefab(new RaycastHit(), classifiedPlanes[classificationTable], tableObjectPool, ref activeTablePrefab);
                TryTogglePrefab(new RaycastHit(), classifiedPlanes[classificationFloor], floorObjectPool, ref activeFloorPrefab);
            }
        }
    }

    private ObjectPool GetObjectPoolForPrefab(GameObject prefab)
    {
        if (prefab == prefabToBeSpawnedOnTable)
        {
            return tableObjectPool;
        }
        else if (prefab == prefabToBeSpawnedOnFloor)
        {
            return floorObjectPool;
        }
        return null;
    }

    private void DeactivatePrefab(ObjectPool objectPool, GameObject prefab)
    {
        if (prefab != null)
        {
            prefab.SetActive(false);  // Deactivate the prefab
            objectPool.ReturnObject(prefab);  // Return it to the object pool
        }
    }
}

public class ObjectPool
{
    private GameObject prefab;
    private Queue<GameObject> poolQueue;
    private Transform poolParent;

    public ObjectPool(GameObject prefab, int initialSize)
    {
        this.prefab = prefab;
        poolQueue = new Queue<GameObject>();
        poolParent = new GameObject($"{prefab.name}_Pool").transform;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNewObject();
            poolQueue.Enqueue(obj);
        }
    }

    private GameObject CreateNewObject()
    {
        var obj = GameObject.Instantiate(prefab);
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        return obj;
    }

    public GameObject GetObject()
    {
        if (poolQueue.Count > 0)
        {
            var obj = poolQueue.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // If pool is empty, create a new object
        return CreateNewObject();
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        poolQueue.Enqueue(obj);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central component on the food truck root.
/// Manages player state (walking / inside / driving).
///
/// Interior room is a child of this truck — it moves with it when driven.
/// Physics.IgnoreCollision prevents the truck's exterior BoxCollider from
/// ejecting the player when they teleport inside.
///
/// Player state machine:
///   WALKING ──E near door──► INTERIOR ──E on driver seat──► DRIVING
///   DRIVING ──E────────────► INTERIOR ──E on exit door───► WALKING
/// </summary>
public class FoodTruck : MonoBehaviour
{
    // ── Public references (set by SceneSetupEditor) ──────────────────────
    [Header("References")]
    public Transform exteriorDoorPoint;     // where player appears on exit
    public Transform interiorSpawnPoint;    // entry spawn inside the interior
    public Transform driverSeatPoint;       // driver seat spawn inside interior
    public Transform cameraTarget;          // camera target when driving

    [Header("Interior Room Root")]
    public GameObject interiorRoomRoot;     // root GO of the interior (child of truck)

    // ── State ─────────────────────────────────────────────────────────────
    public enum State { Walking, InsideTruck, Driving }
    public State CurrentState { get; private set; } = State.Walking;

    // ── Cached references ─────────────────────────────────────────────────
    FoodTruckDriving _driving;
    BoxCollider _bodyCollider;
    ThirdPersonCamera _tpCam;

    GameObject _player;
    CharacterController _playerCC;
    PlayerController _playerController;

    void Awake()
    {
        _driving      = GetComponent<FoodTruckDriving>();
        _bodyCollider = GetComponent<BoxCollider>();
    }

    // ── Entry points called by interactables ──────────────────────────────

    public void EnterInterior(GameObject player)
    {
        if (CurrentState != State.Walking) return;
        CachePlayer(player);

        CurrentState = State.InsideTruck;

        // Let the player pass through the exterior box while inside
        if (_playerCC != null && _bodyCollider != null)
            Physics.IgnoreCollision(_playerCC, _bodyCollider, true);

        TeleportPlayer(interiorSpawnPoint.position, interiorSpawnPoint.rotation);
        Debug.Log("[FoodTruck] Player entered interior.");
    }

    public void ExitInterior()
    {
        if (CurrentState != State.InsideTruck) return;

        CurrentState = State.Walking;
        TeleportPlayer(exteriorDoorPoint.position, exteriorDoorPoint.rotation);
        SetPlayerMovement(true);

        // Restore exterior collision now that the player is outside
        if (_playerCC != null && _bodyCollider != null)
            Physics.IgnoreCollision(_playerCC, _bodyCollider, false);

        Debug.Log("[FoodTruck] Player exited truck.");
    }

    public void EnterDriverSeat()
    {
        if (CurrentState != State.InsideTruck) return;

        CurrentState = State.Driving;
        SetPlayerMovement(false);
        SetPlayerVisible(false);

        if (_tpCam == null) _tpCam = Camera.main?.GetComponent<ThirdPersonCamera>();
        if (_tpCam != null)
        {
            _tpCam.target       = cameraTarget;
            _tpCam.distance     = 10f;
            _tpCam.targetOffset = new Vector3(0f, 1.5f, 0f);
        }

        _driving.SetActive(true);
        Debug.Log("[FoodTruck] Player is driving.");
    }

    public void ExitDriverSeat()
    {
        if (CurrentState != State.Driving) return;

        _driving.SetActive(false);
        CurrentState = State.InsideTruck;
        SetPlayerVisible(true);

        if (_tpCam != null)
        {
            _tpCam.target       = _player.transform;
            _tpCam.distance     = 6f;
            _tpCam.targetOffset = new Vector3(0f, 1.6f, 0f);
        }

        TeleportPlayer(driverSeatPoint.position, driverSeatPoint.rotation);
        SetPlayerMovement(true);
        Debug.Log("[FoodTruck] Player exited driver seat.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void CachePlayer(GameObject player)
    {
        _player           = player;
        _playerCC         = player.GetComponent<CharacterController>();
        _playerController = player.GetComponent<PlayerController>();
        if (_tpCam == null) _tpCam = Camera.main?.GetComponent<ThirdPersonCamera>();
    }

    void TeleportPlayer(Vector3 pos, Quaternion rot)
    {
        if (_playerCC != null) _playerCC.enabled = false;
        _player.transform.SetPositionAndRotation(pos, rot);
        if (_playerCC != null) _playerCC.enabled = true;
    }

    void SetPlayerMovement(bool on)
    {
        if (_playerCC != null)         _playerCC.enabled         = on;
        if (_playerController != null) _playerController.enabled = on;
    }

    void SetPlayerVisible(bool visible)
    {
        foreach (var r in _player.GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    // ── E to exit driving ─────────────────────────────────────────────────
    void Update()
    {
        if (CurrentState == State.Driving
            && Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExitDriverSeat();
        }
    }
}

// ── Interactable: Exterior door → enter interior ─────────────────────────
public class TruckExteriorDoor : MonoBehaviour, IInteractable
{
    public FoodTruck truck;
    // Prompt hidden when already inside so the OverlapSphere from inside
    // can't accidentally re-trigger this.
    public string GetPrompt() =>
        truck.CurrentState == FoodTruck.State.Walking ? "Enter Food Truck" : null;
    public void Interact(GameObject interactor)
    {
        if (truck.CurrentState == FoodTruck.State.Walking)
            truck.EnterInterior(interactor);
    }
}

// ── Interactable: Interior exit door → return to street ──────────────────
public class TruckInteriorExit : MonoBehaviour, IInteractable
{
    public FoodTruck truck;
    public string GetPrompt() =>
        truck.CurrentState == FoodTruck.State.InsideTruck ? "Exit Truck" : null;
    public void Interact(GameObject interactor)
    {
        if (truck.CurrentState == FoodTruck.State.InsideTruck)
            truck.ExitInterior();
    }
}

// ── Interactable: Driver seat → start driving ─────────────────────────────
public class TruckDriverSeat : MonoBehaviour, IInteractable
{
    public FoodTruck truck;
    public string GetPrompt() =>
        truck.CurrentState == FoodTruck.State.InsideTruck ? "Drive Truck" : null;
    public void Interact(GameObject interactor)
    {
        if (truck.CurrentState == FoodTruck.State.InsideTruck)
            truck.EnterDriverSeat();
    }
}

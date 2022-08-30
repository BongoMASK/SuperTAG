using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    #region Variables

    public PhotonView PV { get; private set; }
    GameObject controller;

    [SerializeField] Camera spectatorCam;
    [SerializeField] float specCamDist = 15;
    Transform currentPlayer = null;

    int currentPlayerIndex = 0;

    bool _isSpectating = false;

    public bool isSpectator { 
        get => _isSpectating; 
        private set => _isSpectating = value;
    }

    float desiredX;
    float xRotation;

    #endregion

    private void Awake() {
        PV = GetComponent<PhotonView>();
    }

    private void Start() {
        if (PV.IsMine) {
            if (PhotonNetwork.IsMasterClient)
                CreateController();
            return;
        }
        Destroy(spectatorCam.gameObject);
    }

    private void LateUpdate() {
        if (!PV.IsMine)
            return;

        FollowPlayer();
    }

    #region Controller Functions

    /// <summary>
    /// Instantiates Player into the scene
    /// </summary>
    public void CreateController() {
        if (!PV.IsMine)
            return;
        
        isSpectator = false;
        currentPlayer = null;
        spectatorCam.gameObject.SetActive(false);

        Vector3 spawnPosition;

        spawnPosition = new Vector3(Random.Range(-50, 50), 0f, Random.Range(-20, 20));

        if (FindObjectOfType<SpawnPoints>() != null) {
            Transform[] spawnPositions = FindObjectOfType<SpawnPoints>().spawnPoints;
            for (int i = 0; i < spawnPositions.Length; i++) {
                spawnPosition = spawnPositions[i].position;
            }
        }

        if (controller != null)
            return;

        controller = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "PlayerContainer 1"), spawnPosition, Quaternion.identity, 0, new object[] { PV.ViewID });
    }

    public void Die() {
        PhotonNetwork.Destroy(controller);
        CreateController();
    }

    #endregion

    #region Spectator Functions

    /// <summary>
    /// Instantiates Player as a Spectator
    /// </summary>
    public void CreateSpectator() {
        if (controller != null)
            PhotonNetwork.Destroy(controller);

        Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;

        spectatorCam.gameObject.SetActive(true);
        isSpectator = true;
        ChangePlayerSpectating(10);
    }

    void FollowPlayer() {
        if (currentPlayer == null) {
            if (isSpectator)
                ChangePlayerSpectating(0);
            return;
        }

        MouseInput();

        #region Camera Clipping

        float distanceOffset;
        Vector3 relativePos = spectatorCam.transform.position - currentPlayer.position;
        RaycastHit hit;

        if (Physics.Raycast(currentPlayer.position, relativePos, out hit, specCamDist + 5f)) {
            Debug.DrawLine(currentPlayer.position, hit.point);
            distanceOffset = specCamDist - hit.distance + 0.8f;
            distanceOffset = Mathf.Clamp(distanceOffset, 0, specCamDist);
        }
        else
            distanceOffset = 0;

        #endregion

        spectatorCam.transform.position = spectatorCam.transform.rotation * new Vector3(0.0f, 0.0f, -specCamDist + distanceOffset) + currentPlayer.position;
    }

    void ChangePlayerSpectating(int i) {
        if(GameManager.instance.playerObjectList.Count == 0)
            return;
        currentPlayerIndex = i % GameManager.instance.playerObjectList.Count;
        currentPlayer = GameManager.instance.playerObjectList[currentPlayerIndex];
    }

    void MouseInput() {
        if (GameManager.instance.gameIsPaused)
            return;

        float mouseX = Input.GetAxis("Mouse X") * 50 * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * 50 * Time.fixedDeltaTime;

        //Find current look rotation
        Vector3 rot = spectatorCam.transform.rotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        spectatorCam.transform.rotation = Quaternion.Euler(xRotation, desiredX, 0);

        if (Input.GetKeyDown(GameManager.instance.otherKeys["fire"].key))
            ChangePlayerSpectating(currentPlayerIndex + 1);
    }
    
    #endregion

    // TODO: Handle player networking, Team Setup, etc on Player Manager
}

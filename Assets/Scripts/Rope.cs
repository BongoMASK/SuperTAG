using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    private LineRenderer lr;
    private Vector2 grappelPoint, mousePos;
    public LayerMask WhatIsGrapplePoint;
    public Transform guntip, camera, Player;
    public float maxDistance = 100f;
    private SpringJoint joint;


    // Start is called before the first frame update
    void Awake() {
        lr = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update() {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        DrawRope();

        if (Input.GetMouseButtonDown(1)) {
            StartGrapple();
        }
        else if (Input.GetMouseButtonUp(1)) {
            StopGrapple();
        }

    }

    void StartGrapple() {
        RaycastHit hit;

        if (Physics.Raycast(guntip.position, mousePos, out hit, maxDistance, WhatIsGrapplePoint)) {
            grappelPoint = hit.point;
            joint = Player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grappelPoint;

            float distanceFromPoint = Vector2.Distance(Player.position, grappelPoint);

            //The distance will try to keep from grapple point
            joint.maxDistance = distanceFromPoint = 0.8f;
            joint.maxDistance = distanceFromPoint = 0.25f;

            //need to change these
            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;
        }
    }
    void DrawRope() {
        lr.SetPosition(0, guntip.position);
        lr.SetPosition(1, grappelPoint);
    }

    void StopGrapple() {

    }
}

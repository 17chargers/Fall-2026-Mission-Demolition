using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

public class Slingshot : MonoBehaviour
{
    [Header("Set in Inspector")]
    public GameObject prefabProjectile;
    public float velocityMult = 8f;
    public GameObject projLinePrefab;
    public Transform leftBandPoint;     //Tip of the left arm
    public Transform rightBandPoint;    //Tip of the right arm
    public AudioClip snapSound;         //Played when the band is released

    [Header("Set Dynamically")]
    public GameObject launchPoint;
    public Vector3 launchPos;
    public GameObject projectile;
    public bool aimingMode;
    private Rigidbody projectileRigidbody;
    private LineRenderer band;          //The rubber band
    private AudioSource audioSource;    //The "speaker" that plays sounds

    void Awake()
    {
        Transform launchPointTrans = transform.Find("LaunchPoint");

        launchPoint = launchPointTrans.gameObject;
        launchPoint.SetActive(false);
        launchPos = launchPointTrans.position;

        //The band goes left tip -> middle -> right tip, so it needs 3 points
        band = GetComponent<LineRenderer>();
        band.positionCount = 3;
        DrawBand(launchPos);

        audioSource = GetComponent<AudioSource>();
    }

    //Draws the rubber band from the left tip, through middle, to the right tip
    void DrawBand(Vector3 middle)
    {
        band.SetPosition(0, leftBandPoint.position);
        band.SetPosition(1, middle);
        band.SetPosition(2, rightBandPoint.position);
    }

    void Update()
    {
        if (!aimingMode)
        {
            return;
        }

        Vector3 mousePos2D = Input.mousePosition;
        mousePos2D.z = -Camera.main.transform.position.z;
        Vector3 mousePos3D = Camera.main.ScreenToWorldPoint(mousePos2D);

        Vector3 mouseDelta = mousePos3D - launchPos;

        float maxMagnitude = this.GetComponent<SphereCollider>().radius;

        if (mouseDelta.magnitude > maxMagnitude)
        {
            mouseDelta.Normalize();
            mouseDelta *= maxMagnitude;
        }

        Vector3 projPos = launchPos + mouseDelta;
        projectile.transform.position = projPos;

        //Stretch the band back to the projectile
        DrawBand(projPos);

        if (Input.GetMouseButtonUp(0))
        {
            aimingMode = false;

            //Snap the band back to its resting spot
            DrawBand(launchPos);

            //Play the snap sound once
            audioSource.PlayOneShot(snapSound);

            projectileRigidbody.isKinematic = false;
            //Launch in the opposite direction of the pull (the minus sign flips it)
            projectileRigidbody.velocity = -mouseDelta * velocityMult;

            //Switch to slingshot view immediately before setting POI
            FollowCam.SWITCH_VIEW( FollowCam.eView.slingshot );

            FollowCam.POI = projectile;
            //Add a ProjectileLine to the Projectile
            Instantiate<GameObject>(projLinePrefab, projectile.transform);
            projectile = null;
            MissionDemolition.SHOT_FIRED();
        }

    }

    void OnMouseEnter()
    {
        //print("Slingshot:OnMouseEnter()");
        launchPoint.SetActive(true);
    }

    void OnMouseExit()
    {
        //print("Slingshot: OnMouseExit()");
        launchPoint.SetActive(false);
    }

    void OnMouseDown()
    {
        //Don't allow a shot if out of shots, between levels, or on game over
        if (!MissionDemolition.CAN_SHOOT()) return;

        aimingMode = true;
        projectile = Instantiate(prefabProjectile) as GameObject;
        projectile.transform.position = launchPos;

        projectileRigidbody = projectile.GetComponent<Rigidbody>();
        projectileRigidbody.isKinematic = true;

    }
}

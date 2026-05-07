using UnityEngine;
using UnityEngine.EventSystems;
using ViveSR.anipal.Eye;

public class ViveEyePointer : MonoBehaviour
{
    public float rayDistance = 50f;

    private GameObject currentObject;

    //[SerializeField] private UIFollowCamera uiFollowCamera;

    //void Start()
    //{
    //    rayDistance = uiFollowCamera.distance + 1f;
    //}

    void Update()
    {
        Vector3 origin, direction;

        if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out origin, out direction))
        {
            Vector3 worldOrigin = transform.TransformPoint(origin);
            Vector3 worldDirection = transform.TransformDirection(direction);

            Ray ray = new Ray(worldOrigin, worldDirection);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance))
            {
                GameObject hitObject = hit.collider.gameObject;

                if (hitObject != currentObject)
                {
                    ExitCurrent();

                    currentObject = hitObject;

                    ExecuteEvents.Execute(
                        currentObject,
                        new PointerEventData(EventSystem.current),
                        ExecuteEvents.pointerEnterHandler
                    );
                }
            }
            else
            {
                ExitCurrent();
            }
        }
    }

    void ExitCurrent()
    {
        if (currentObject != null)
        {
            ExecuteEvents.Execute(
                currentObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerExitHandler
            );

            currentObject = null;
        }
    }
}
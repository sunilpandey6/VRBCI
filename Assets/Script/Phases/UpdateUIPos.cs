using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateUIPos : MonoBehaviour
{
    // Start is called before the first frame update
    #region UI Positioning
    public void PositionCanvasFront()
    {
        if (GlobalInput.Instance.cam == null) return;

        Transform cam = GlobalInput.Instance.cam.transform;

        transform.position =
            cam.position +
            cam.right * GlobalInput.Instance.horizontalOffset +
            cam.up * GlobalInput.Instance.verticalOffset +
            cam.forward * GlobalInput.Instance.distance;

        transform.rotation = cam.rotation;
    }
    #endregion
}

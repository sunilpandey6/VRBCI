using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpenAnimation : MonoBehaviour
{
    public bool doorOpen = false;
    public Animator doorAnimator;

    private void Start()
    {
        doorOpen = false;
        doorAnimator.SetBool("isOpen", doorOpen);
    }
    private void OnEnable()
    {
        Close();
    }
#region Open and Close
    public void Open()
    {
        if (!doorAnimator.GetBool("isOpen"))
            doorAnimator.SetBool("isOpen", true);
    }

    public void Close()
    {
        if (doorAnimator.GetBool("isOpen"))
            doorAnimator.SetBool("isOpen", false);
    }
#endregion
    // Start is called before the first frame update
    #region Trigger Enter and Exit
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Open();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Close();
        }
    }
    #endregion
}

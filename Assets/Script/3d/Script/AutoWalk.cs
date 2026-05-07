using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoWalk : MonoBehaviour
{
    public CharacterController characterController;
    public float speed = 1.2f;
    [SerializeField] private Transform target_DoubleDoor;
    [SerializeField] private Transform target_SingleDoor;
    [SerializeField] private float stopDistance = 0.3f;

    public void MoveToTarget(int code)
    {
        Transform target = null;
        if(code == 301)
        {
            target = target_DoubleDoor;
        }
        else if(code == 302)
        {
            target = target_SingleDoor;
        }
        if(target == null)
        {
            Debug.LogError("Target is not assigned!");
            return;
        }
        
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0;
        float distance = directionToTarget.magnitude;
        if(distance <= stopDistance)
        {
            characterController.Move(Vector3.zero);
            return;
        }
        
        transform.LookAt(target.position);
        characterController.Move(directionToTarget.normalized * speed * Time.deltaTime);
    }

}

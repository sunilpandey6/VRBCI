using System;
using UnityEngine;

public class AutoWalk : MonoBehaviour
{
    public float speed = 1.2f;

    [SerializeField] private Transform target_DoubleDoor;
    [SerializeField] private Transform target_SingleDoor;

    [SerializeField] private float stopDistance = 0.3f;

    [SerializeField] private Transform currentTarget;

    [SerializeField] public Action onReachedTarget;

    public void MoveToTarget(int code, Action callback = null)
    {
        onReachedTarget = callback;

        if (code == 301)
        {
            currentTarget = target_DoubleDoor;
        }
        else if (code == 302)
        {
            currentTarget = target_SingleDoor;
        }
    }

    private void Update()
    {
        if (currentTarget == null)
            return;

        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0;

        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            currentTarget = null;

            // Trigger callback
            onReachedTarget?.Invoke();
            onReachedTarget = null;

            return;
        }

        // Rotate toward target
        Vector3 lookPos = currentTarget.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        // Move
        transform.position += direction.normalized * speed * Time.deltaTime;
    }
}
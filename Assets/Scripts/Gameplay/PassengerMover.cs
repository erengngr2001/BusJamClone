using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PassengerMover
{
    public static IEnumerator AnimateMovement(Passenger passenger, List<Vector3> worldPath, float moveSpeed, Action onComplete)
    {
        if (passenger == null)
        {
            Debug.LogWarning("AnimateMovement started with a null passenger.");
            yield break;
        }

        // Move through each point in the provided path
        foreach (var targetPosition in worldPath)
        {
            // Because of concurrency, continuously check if the passenger was destroyed mid-animation (bless it solves the race condition)
            if (passenger == null)
            {
                Debug.LogWarning("Passenger was destroyed mid-path. Halting movement coroutine.");
                yield break;
            }

            // Move towards the next point until we are very close
            while (Vector3.Distance(passenger.transform.position, targetPosition) > 0.01f)
            {
                if (passenger == null) yield break; // Check again inside the loop
                passenger.transform.position = Vector3.MoveTowards(passenger.transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null; // Wait for the next frame
            }
        }

        if (passenger == null)
        {
            Debug.LogWarning("Passenger was destroyed just before reaching the destination.");
            yield break;
        }

        // Snap to the final position to ensure accuracy if the path wasn't empty
        if (worldPath.Count > 0)
        {
            passenger.transform.position = worldPath[worldPath.Count - 1];
        }

        // The animation is done, call the completion callback
        onComplete?.Invoke();
    }
}

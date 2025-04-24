using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> actions = new();

    public static void Enqueue(Action action)
    {
        lock (actions)
        {
            actions.Enqueue(action);
        }
    }

    private void Update()
    {
        while (actions.Count > 0)
        {
            Action a;
            lock (actions) a = actions.Dequeue();
            a?.Invoke();
        }
    }
}

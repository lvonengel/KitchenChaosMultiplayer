using System;
using UnityEngine;

/// <summary>
/// Interface for objects that tracks progress
/// Currently for cooking, burning, and cutting
/// </summary>
public interface IHasProgress {

    /// <summary>
    /// Fired whenever progress for an object changes
    /// </summary>
    public event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs {
        public float progressNormalized;
    }
}

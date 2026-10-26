using UnityEngine;

// A view consumes the motor's existing state and normalized animation sample.
// It does not advance combat, move roots, or own a second simulation clock.
public abstract class TrainingFighterPresentation : MonoBehaviour
{
    public abstract void Present(TrainingFighter fighter, string state, float sample);
}

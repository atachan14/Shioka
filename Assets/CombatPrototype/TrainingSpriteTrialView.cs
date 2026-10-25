using UnityEngine;

public sealed class TrainingSpriteTrialView : TrainingFighterPresentation
{
    public SpriteRenderer image;
    public Sprite idleA, idleB, windup, contact, recover;
    public string DisplayedState { get; private set; }
    public float DisplayedSample { get; private set; }
    public bool Unsupported { get; private set; }
    public int PresentCount { get; private set; }

    public override void Present(TrainingFighter fighter, string state, float sample)
    {
        DisplayedState = state;
        DisplayedSample = sample;
        PresentCount++;
        if (image == null) return;
        // Keep the same world Y/Z combat plane; the image faces the existing +X camera.
        image.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        image.transform.localPosition = Vector3.zero;
        image.flipX = fighter.Facing < 0;
        bool punch = fighter.CurrentPhase == TrainingFighter.Phase.Attack &&
            (fighter.CurrentMove == null || fighter.CurrentMove.command == TrainingCommand.Light);
        Unsupported = !punch && fighter.CurrentPhase != TrainingFighter.Phase.Idle &&
            fighter.CurrentPhase != TrainingFighter.Phase.Move;
        if (punch)
        {
            // The same contact-normalized sample as Humanoid playback; no copied frame values.
            float hit = fighter.CurrentMove == null ? fighter.definition.jabContactNormalized : fighter.CurrentMove.contactNormalized;
            float end = Mathf.Min(1f, hit + .08f);
            image.sprite = sample < hit ? windup : sample <= end ? contact : recover;
        }
        else image.sprite = sample < .5f ? idleA : idleB;
        image.color = Unsupported ? new Color(.9f, .65f, 1f) :
            fighter.StunRemaining > 0f ? (fighter.LastResult == "Hit" ? new Color(1f, .5f, .5f) : new Color(.5f, 1f, 1f)) :
            fighter.IsGuarding ? new Color(.65f, .8f, 1f) : Color.white;
    }
}

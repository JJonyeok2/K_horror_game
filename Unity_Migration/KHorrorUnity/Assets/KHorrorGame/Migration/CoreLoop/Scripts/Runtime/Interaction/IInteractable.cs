namespace KHorrorGame.Migration
{
    public interface IInteractable
    {
        string InteractionLabel { get; }

        bool CanInteract(UnityPlayerController actor);

        void Interact(UnityPlayerController actor);
    }

    public interface IInteractionFailureReason
    {
        string InvalidInteractionReason(UnityPlayerController actor);
    }
}

namespace ROS.Game.Interaction
{
    public interface IInteractable
    {
        string InteractionLabel { get; }
        bool CanInteract(UnityEngine.GameObject interactor);
        void Interact(UnityEngine.GameObject interactor);
    }

    public interface IPrioritizedInteractable
    {
        int InteractionPriority { get; }
    }
}

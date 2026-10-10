namespace BlockCrush
{
    // Presentation only; scoring, placement and mode rules stay in their existing owners.
    public interface IBlockGameFeedback
    {
        void Restarted();
        void Placed(int lines, int combo);
        void InvalidPlacement();
        void Completed();
        void Failed();
    }
}

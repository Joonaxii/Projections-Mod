namespace Projections.Core.Data.Structures
{
    public struct PBundleEntry : IWeighted
    {
        public bool IsValid => stack > 0 && index.IsValidID() && Projections.IsValidIndex(index, type);
        public bool ConditionsMet => conditions.AreMet;

        public float Weight { get => weight; set=> weight = value; }

        public PType type;
        public ProjectionIndex index;
        public int stack;
        public PConditions conditions;
        public float weight;
    }
}

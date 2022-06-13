namespace RPG.AI
{
    public struct Appraisal
    {
        public Appraisal(int tier, float weight)
        {
            Tier = tier;
            Weight = weight;
        }
        public float Weight;
        public int Tier;
    }
}
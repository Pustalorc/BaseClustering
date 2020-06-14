namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BruteforceOptions
    {
        public float InitialRadius;
        public float MaxRadius;
        public byte MaxRadiusRechecks;

        public BruteforceOptions()
        {
        }

        public BruteforceOptions(float initial, float max, byte recheck)
        {
            InitialRadius = initial;
            MaxRadius = max;
            MaxRadiusRechecks = recheck;
        }
    }
}
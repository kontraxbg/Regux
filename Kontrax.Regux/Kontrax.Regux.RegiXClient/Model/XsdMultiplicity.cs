namespace Kontrax.Regux.RegiXClient.Model
{
    public class XsdMultiplicity
    {
        private readonly int _min;
        private readonly int? _max;

        public XsdMultiplicity(int min, int? max)
        {
            _min = min;
            _max = max;
        }

        public int Min { get { return _min; } }

        public int? Max { get { return _max; } }

        public override string ToString()
        {
            bool isUnbounded = !_max.HasValue;
            if (_min > 0)
            {
                if (_min == _max)
                {
                    return $"({_min})";
                }
                if (isUnbounded)
                {
                    return $"({_min}+)";
                }
            }
            else  // _min == 0
            {
                if (_max == 1)
                {
                    return "(?)";
                }
                if (isUnbounded)
                {
                    return "(*)";
                }
            }
            return $"({_min}-{_max})";
        }
    }
}
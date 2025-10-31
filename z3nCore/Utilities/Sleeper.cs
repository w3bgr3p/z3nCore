using System;
using System.Threading;
namespace z3nCore.Utilities
{
    public class Sleeper
    {
        private readonly int _min;
        private readonly int _max;
        private readonly Random _random;
    
       
        /// <param name="min">Min ms</param>
        /// <param name="max">Max ms</param>
        public Sleeper(int min, int max)
        {
            if (min < 0)
                throw new ArgumentException("Min не может быть отрицательным", nameof(min));
        
            if (max < min)
                throw new ArgumentException("Max не может быть меньше Min", nameof(max));
        
            _min = min;
            _max = max;
        

            _random = new Random(Guid.NewGuid().GetHashCode());
        }
        
        
        /// <param name="multiplier">Множитель для задержки (например, 2.0 = в 2 раза дольше)</param>
        public void Sleep(double multiplier = 1.0)
        {
            int delay = _random.Next(_min, _max + 1);
            Thread.Sleep((int)(delay * multiplier));
        }
        
    }
}
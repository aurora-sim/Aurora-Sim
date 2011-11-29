//#define ACURATE

namespace Aurora.Framework
{
    public class AveragingClass
    {
#if ACURATE
        private List<float> m_list;
        private int timeToBeatLastSet = 0;

        private bool haveFilledBeatList = false;
#else

        private float total;
        private readonly int capacity;
        private int count;
#endif

        public AveragingClass(int capacity)
        {
#if ACURATE
            m_list = new List<float> (capacity);
#else
            this.capacity = capacity;
#endif
        }

        public float GetAverage()
        {
#if ACURATE
            float avg = 0;
            foreach (float a in m_list)
                avg += a;
            avg /= m_list.Count;
            return avg;
#else
            return total/count;
#endif
        }

        public void Add(float value)
        {
#if ACURATE
            if (haveFilledBeatList)
                m_list[timeToBeatLastSet] = value;
            else
                m_list.Add (value);
            timeToBeatLastSet++;
            if (timeToBeatLastSet >= m_list.Capacity)
            {
                timeToBeatLastSet = 0;
                haveFilledBeatList = true;
            }
#else
            if (count < capacity)
                count++;
            else
                total -= GetAverage();
            total += value;
#endif
        }
    }
}
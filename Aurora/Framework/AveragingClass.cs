using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public class AveragingClass
    {
        private List<float> m_list;
        private int timeToBeatLastSet = 0;

        private bool haveFilledBeatList = false;

        public AveragingClass (int capacity)
        {
            m_list = new List<float> (capacity);
        }

        public float GetAverage ()
        {
            float avg = 0;
            foreach (float a in m_list)
                avg += a;
            avg /= m_list.Count;
            return avg;
        }

        public void Add (float value)
        {
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
        }
    }
}

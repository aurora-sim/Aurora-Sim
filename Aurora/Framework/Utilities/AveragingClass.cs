/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

//#define ACURATE

namespace Aurora.Framework.Utilities
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
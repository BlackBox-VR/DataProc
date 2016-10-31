using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{
    class CurveSegmenter
    {
        public float vertGridRange = 2.0f; //total elevation up to 2xheight
        public float horzGridRange = 1.0f; //span up to 2xheight (+/- 1xheight)
        public float gridScale = 0.05f; //0.05 meter ~= 2 inches
        private double[,] segmentCurve;
        private double heightNormalizedValue;

        public CurveSegmenter (float[,] data, float userHeight)
        {
            for (int i=0; i <= data.GetLength(0); i++)
            {
                for (int j = 0; j <= data.GetLength(1); j++)
                {

                    if (j == 0 || j == 1 || j == 2 || j == 6 || j == 7 || j == 8 || j == 12 || j == 13 || j == 14)
                    {
                        heightNormalizedValue = data[i, j] / userHeight;
                    }
                    else
                    {
                        heightNormalizedValue = data[i, j];
                    }

                    segmentCurve[i, j] = Math.Floor(heightNormalizedValue / gridScale) * gridScale;
                }
            }
        }

    }
}

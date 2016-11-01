using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{
    //Based on input 6DOF data (each controller or HMD must be fed seperately) plus a time stamp

    class CurveSegmenter
    {
        public float vertGridRange = 2.0f; //total elevation up to 2xheight
        public float horzGridRange = 1.0f; //span up to 2xheight (+/- 1xheight)
        public float gridScale = 0.05f; //0.05 meter ~= 2 inches
        private double[,] snappedCurve;
        private double[,,] curveSegments;
        private double heightNormalizedValue;
        private int pointCount = 0;
        private double previousPoint;
        private int segmentCount = 0;

        public CurveSegmenter(float[,] data, float userHeight)
        {
            for (int i=0; i <= data.GetLength(0); i++)
            {
                for (int j = 0; j <= data.GetLength(1); j++)
                {
                    switch (j)
                    {
                        case 0:
                        case 1:
                        case 2:
                            heightNormalizedValue = data[i, j] / userHeight;
                            break;
                        default:
                            heightNormalizedValue = data[i, j];
                            break;
                    }
                    snappedCurve[i, j] = Math.Floor(heightNormalizedValue / gridScale) * gridScale;

                    if (snappedCurve[i, j] = previousPoint)
                    {
                        ++runCount;
                    }
                    else
                        curveSegments[segmentCount,j] = ;
                }
            }
        }
    }
}

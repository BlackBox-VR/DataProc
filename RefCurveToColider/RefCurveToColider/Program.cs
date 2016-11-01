using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefCurveToColider
{
    class RefCollider
    {
        private int curveIndex { get; set; } //index of this collider in its series
        private float positionX { get; set; }  //position of collider in X
        private float positionY { get; set; }  //position of collider in Y
        private float positionZ { get; set; }  //position of collider in Z
        private float rotationX { get; set; }  //orientation of collider around the X
        private float rotationY { get; set; }  //orientation of collider around the Y
        private float rotationZ { get; set; }  //orientation of collider around Z
        private float thickness { get; set; }  //thickness of collider orientated along path line extruded from center
        private float innerRadius { get; set; }  //inner radius of collider oriented prepedicular to path line
        private float outerRadius { get; set; }  //outer radius of collider our disc oriented perpendicular to path line
        private float meanAllowRotX { get; set; } //mean of allowable X rotation for reference peripheril in collider
        private float rngAllowRotX { get; set; }  //range of allowable X rotation for reference peripheral in collider
        private float meanAllowRotY { get; set; }  //mean of allowable Y rotation for reference peripheral in collider
        private float rngAllowRotY { get; set; }  //range of allowable Y rotation for reference peripheral in collider
        private float meanAllowRotZ { get; set; }  //mean of allowable Z rotation for reference peripheral in collider
        private float rngAllowRotZ { get; set; }  //range of allowable Z rotation for reference peripheral in collider
        private float meanTransTime { get; set; }  //mean of time to leave center of previous collider in series and hit this collider
        private float rngTransTime { get; set; } //range of time to leave center of previous collider in series and hit this collider

        // Function to take input data as 6DOF plus time, a [7,x] array, and populates the collider dictionary 

        public void RefToColider(float[,] inputArray, float userHeight, string exerciseName,
            float colliderSpacing)
        {
            //first initialize Dictionary for colliders

            var colliders = new Dictionary<string, RefCollider>();
            
            //now normalize the input data to user height

            float[,] normalizedInputArray = inputArray;
            for (int i = 0; i < inputArray.GetLength(1); i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    normalizedInputArray[i, j] = inputArray[i, j] / userHeight;
                }

            }

            //now choose number of colliders to generate based on curve length and collider spacing

            double crvLength = curveLength(inputArray);
            double colliderNumberInSeries = Math.Floor(crvLength);
            double crvSubSegment = crvLength / (colliderNumberInSeries * 2);
            double currentSubSegLength = 0;

            double sumX = 0;
            double sumY = 0;
            double sumZ = 0;
            int n = 0;
            double[] finalMeanPosition = { 0, 0, 0};
            double[] initialMeanPosition = { 0, 0, 0 };
            double totalSubSegments = 0;

            for (int i = 0; i <= inputArray.GetLength(1) - 1; i++)
            {
                n++;
                double deltaX = inputArray[0, i + 1] - inputArray[0, i];
                double deltaY = inputArray[1, i + 1] - inputArray[1, i];
                double deltaZ = inputArray[2, i + 1] - inputArray[2, i];
                sumX = sumX + deltaX;
                sumY = sumY + deltaY;
                sumZ = sumZ + deltaZ;
                currentSubSegLength = currentSubSegLength + Math.Pow(
                    Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2) + Math.Pow(deltaZ, 2)
                    , 0.5);

                if (currentSubSegLength >= crvSubSegment)
                {
                    finalMeanPosition[0] = sumX / n;
                    finalMeanPosition[1] = sumY / n;
                    finalMeanPosition[2] = sumZ / n;
                    ++totalSubSegments;
                    n = 0;
                }

                if (Math.Floor(totalSubSegments / 2) == totalSubSegments / 2)
                {
                
                }

                initialMeanPosition = finalMeanPosition;
            }

            //now find positions and orientatinos of colliders



        }

        // Function to take a string of points as the first 3 rows in an array and calculate total length

        public double curveLength(float[,] inputArray)
        {
            double curveLength = 0;
            for (int i = 0; i <= inputArray.GetLength(1)-1; i++)
            {
                curveLength = curveLength + Math.Pow(
                    Math.Pow(inputArray[0, i + 1] - inputArray[0, i], 2) +
                    Math.Pow(inputArray[1, i + 1] - inputArray[1, i], 2) +
                    Math.Pow(inputArray[2, i + 1] - inputArray[2, i], 2)
                    ,0.5);
            }
            return curveLength;
        }

        //Function to locate things along the length of a curve

        public double curvePosition(double curveLength, double numberInSeries)
        {

        }
    }
}

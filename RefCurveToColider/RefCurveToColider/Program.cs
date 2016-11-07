using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefCurveToColider
{
    class ExerciseTracker
    {
        RefCollider[] RefColliders;
    }

    // money!

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


        // Function to take input data for a list of peripherals and normalize it to user height and HMD location
        // Input coordinates are assumed to be in world space
        // User facing is assumed to be the +z direction (because HMD orientation is a function of head facing, not necessarily body facing)\
        // Therefore only position is corrected and rotations remain relative to world space
        // TODO: if feet peripherals become available in the future, that could add insight to facing
        // Input data is a list of lists of x,y,z position lists, assuming the HMD is the first (index 0) element in the outside list from the same workout

        public List<List<List<double>>> userNormalizeInputData(List<List<List<double>>> inputData, double userHeight)
        {
            List<List<List<double>>> normalizedInputData = new List<List<List<double>>>();
            List<double> coordinate = new List<double> { 0, 0, 0};

            for (int i=0; i < inputData.Count; i++)
            {
                for (int j=0; j < inputData[i].Count; j++)
                {
                    for (int k = 0; k < inputData[i][j].Count; k++)
                    {
                        // Shift from world origin to HMD reference origin for non-HMD peripherals
                        if (i != 0)
                        {
                            coordinate[k] = inputData[i][j][k] - inputData[0][j][k];
                        }
                        else
                        {
                            coordinate[k] = inputData[i][j][k];
                        }

                        // Normalize all length data to user height (i.e. if user is 2m tall, 2m from the HMD equals 1 m/m)

                        coordinate[k] = inputData[i][j][k] / userHeight;
                    }

                    // Add the normalized coordinate to the correct curve index in the normalized output list

                    normalizedInputData[i].Add(coordinate);

                }
            }

            return normalizedInputData;
        }

        // This function takes a series of points along a path and normalizes them to a standard unit Displacement step size through linear interpolation
        // This function is intentionally decoupled from time so that exercises of correct curvature but different times line up
        // i.e. this function takes all paths and makes them have the same points per unit length even if one was very fast and the other very slow

        public List<List<List<double>>> spaceNormalizeInputData(List<List<List<double>>> inputData)
        {
            double unitDisplacement = 0.005;  // ~ 0.5 cm is a good small length to limit the effects of phase differences
            List<double> coordinate = new List<double> { 0, 0, 0 };
            List<double> previousCoordinate = inputData[0][0];
            List<List<List<double>>> normalizedInputData = new List<List<List<double>>>();
            normalizedInputData[0].Add(inputData[0][0]);

            for (int i = 1; i < inputData.Count; i++)
            {
                for (int j = 1; j < inputData[i].Count; j++)
                {
                    double deltaX = inputData[i][j][0] - previousCoordinate[0];
                    double deltaY = inputData[i][j][1] - previousCoordinate[1];
                    double deltaZ = inputData[i][j][2] - previousCoordinate[2];
                    double distance = Math.Pow(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2) + Math.Pow(deltaY, 2), 0.5);

                    double unitDisplacementRatio = distance / unitDisplacement;

                    // If the displacement between points has exceeded the unit displacement, store
                    // an interpolated point at the unit distance and set this to new previous point

                    if (unitDisplacementRatio > 1)
                    {
                        coordinate[0] = (deltaX * (1 / unitDisplacementRatio));
                        coordinate[1] = (deltaY * (1 / unitDisplacementRatio));
                        coordinate[2] = (deltaZ * (1 / unitDisplacementRatio));

                        normalizedInputData[i].Add(coordinate);
                        previousCoordinate = coordinate;
                    }
                }
            }

            return normalizedInputData;
        }


        // Function to take input data as 6DOF plus time for many users all together in one set a [7,x] array, and populates the collider dictionary 

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

            double sumX = inputArray[0, 0];
            double sumY = inputArray[1, 0];
            double sumZ = inputArray[2, 0];
            int n = 0;
            double[] finalMeanPosition = { 0, 0, 0};
            double[] initialMeanPosition = { 0, 0, 0 };
            double totalSubSegments = 0;

            for (int i = 0; i <= inputArray.GetLength(1) - 1; i++)
            {
                n++;
                sumX = sumX + inputArray[0, i + 1];
                sumY = sumY + inputArray[1, i + 1];
                sumZ = sumZ + inputArray[2, i + 1];
                currentSubSegLength = currentSubSegLength + Math.Pow(
                    Math.Pow(inputArray[0, i + 1] - inputArray[0, i], 2) +
                    Math.Pow(inputArray[1, i + 1] - inputArray[1, i], 2) +
                    Math.Pow(inputArray[2, i + 1] - inputArray[2, i], 2)
                    , 0.5);

                if (currentSubSegLength >= crvSubSegment)
                {
                    finalMeanPosition[0] = sumX / n;
                    finalMeanPosition[1] = sumY / n;
                    finalMeanPosition[2] = sumZ / n;
                    ++totalSubSegments;
                    n = 0;
                    sumX = 0;
                    sumY = 0;
                    sumZ = 0;

                    if (Math.Floor(totalSubSegments / 2) == totalSubSegments / 2)
                    {
                        double deltaX = finalMeanPosition[0] - initialMeanPosition[0];
                        double deltaY = finalMeanPosition[1] - initialMeanPosition[1];
                        double deltaZ = finalMeanPosition[2] - initialMeanPosition[2];
                    }

                    initialMeanPosition = finalMeanPosition;
                }
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

        // This function takes two 3D scatter plots and aligns them as closely as possible through interative translation and rotation
        // It is intended for use with points through 3D space that trace some path (typically time snapshots of peripherals as they trace curves)
        // It is assumed the scatters are ordinal with the first point on a path the first in the arrays and the last as the last
        // This function does not scale the scatters; they retain constant orientation and placement relative to other members of their set
        // This function works best when the two scatters are normalized (ex. points along a 3D curve for peripherals should be normalized to user height)
        // This function assumes a shared datum (i.e. the same origin)
        // For normalized data the datum should be coupled to the normalization point (ex. user height based on HMD location would have the HMD as the origin)

        public void scatterAlign(double[,] referenceArray, double[,] baseAdjustedArray)
        {
            // Minimize the error between first and last points

            double endPointNetError = 0;
            double previousError = 0;
            double[,] newAdjustedArray = baseAdjustedArray;

            double deltaX = 0;
            double deltaY = 0;
            double deltaZ = 0;

            double[] deltaS = { 0, 0, 0};

            deltaX = referenceArray[0, 0] - baseAdjustedArray[0, 0];
            deltaY = referenceArray[1, 0] - baseAdjustedArray[1, 0];
            deltaZ = referenceArray[2, 0] - baseAdjustedArray[2, 0];

            for (int j=0; j < baseAdjustedArray.GetLength(1); j++)
            {
                newAdjustedArray[i, 0] = baseAdjustedArray[i, 0] - deltaX;
                newAdjustedArray[i, 1] = baseAdjustedArray[i, 1] - deltaY;
                newAdjustedArray[i, 2] = baseAdjustedArray[i, 2] - deltaZ;
            }

            endPointNetError = newAdjustedArray

        }
        
        // This function takes an array of 3D points and approximates a line made of segments through them
        // It is intended for use with points through 3D space that trace some path (typically time snapshots of peripherals as they trace curves)
        // It is intended for use with several sets of 3D points tracing roughly the same curve (but with some systemic and random variation)
        // Each set of 3D points is its own array with rows as 0=x/1=y/2=z for a single point and columns as points ordered along the path
        // The function takes an array of these arrays and increments through them in order
        // It is assumed that the 3D point arrays are spatially similar with starting and ending points in similar locations
        // This assumption may require some form of data trimming, scaling, or other similar processing
        // It is also best to use normalized data because of this assumption (ex. all length values are height normalized)
        // The 3D point arrays do not need to be similar in spacing (thus one could take 10 points to get from A to B and another 20 without issue)
        // The function uses a rolling 3D aspect ratio approach to locate local centroids based on a provided length scale and creates segments between these centroids
        // THe function iterates through mutliple instances of the segmentation process to reach a best approximation
        // The iteration includes outlier trimming based on removing points with abnormally high distance from a given estimated curve segment

        public List<List<double>> ScatterSegmentedLineApproximator(double[,] inputArray)
        {
            double Xmin = Math.Infinity;
            double Xmax = -1*Math.Infinity;
            double Ymin = Math.Inifnity;
            double Ymax = -1*Math.Infinity;
            double Zmin = Math.Infinity;
            double Zmax = -1*Math.Infinity;

            double Xtot = 0;
            double Ytot = 0;
            double Ztot = 0;

            double[] finalMeanPosition = { 0, 0, 0 };
            double[] initialMeanPosition = { inputArray[0, 0], inputArray[1, 0], inputArray[2, 0] };
            double totalAspectScale = 0;
            double AspectScaleIncrement = 0.02; //current version not normalized to userHeight
            int n = 0;
            List<List<double>> segmentedCurve = new List<List<double>>();



            for (int j = 0; j < inputArray.GetLength(1); j++)
            {

                for (int i = 0; i < inputArray.GetLength(0), i++)
                {
                    n++;

                    if (i == 0)
                    {
                        Xtot = Xtot + inputArray[i, j];

                        if (inputArray[i, j] < Xmin)
                        {
                            Xmin = inputArray[i, j];
                        }

                        if (inputArray[i, j] > Xmax)
                        {
                            Xmax = inputArray[i, j];
                        }
                    }

                    if (i == 1)
                    {
                        Ytot = Ytot + inputArray[i, j];

                        if (inputArray[i, j] < Ymin)
                        {
                            Ymin = inputArray[i, j];
                        }
                        if (inputArray[i, j] > Ymax)
                        {
                            Ymax = inputArray[i, j];
                        }
                    }

                    if (i == 2)
                    {
                        Ztot = Ztot + inputArray[i, j];

                        if (inputArray[i, j] < Zmin)
                        {
                            Zmin = inputArray[i, j];
                        }
                        if (inputArray[i, j] > Zmax)
                        {
                            Zmax = inputArray[i, j];
                        }
                    }
                }

                totalAspectScale = Math.Pow(
                    Math.Pow(Xmax - Xmin, 2) + Math.Pow(Ymax - Ymin, 2) +
                    Math.Pow(Zmax - Zmin, 2), 0.5);

                if (totalAspectScale > AspectScaleIncrement)
                {
                    segmentedCurve.Add(new List<double> {Xtot/n,Ytot/n,Ztot,n});
                    n = 0;
                    Xtot = 0;
                    Ytot = 0;
                    Ztot = 0;
                    Xmin = Xmin + 10;
                    Xmax = Xmax - 10;
                    Ymin = Ymin + 10;
                    Ymax = Ymin - 10;
                    Zmin = Zmin + 10;
                    Zmax = Zmax - 10;
                }
            }

            return segmentedCurve;
        }

        //Function to locate things along the length of a curve

        public double curvePosition(double curveLength, double numberInSeries)
        {

        }
    }
}

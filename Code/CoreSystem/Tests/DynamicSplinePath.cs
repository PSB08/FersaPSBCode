using UnityEngine;
using UnityEngine.Splines;

namespace Work.PSB.Code.CoreSystem.Tests
{
    public class DynamicSplinePath : MonoBehaviour
    {
        public SplineContainer splineContainer;
        public Transform playerTransform;
        public Transform targetTransform;

        public void UpdatePath()
        {
            var spline = splineContainer.Spline;
            
            Vector3 localPlayerPos = splineContainer.transform.InverseTransformPoint(playerTransform.position);
            Vector3 localTargetPos = splineContainer.transform.InverseTransformPoint(targetTransform.position);

            localPlayerPos.z = -10f;
            localTargetPos.z = -10f;

            spline.SetKnot(0, new BezierKnot(localPlayerPos));
            spline.SetKnot(1, new BezierKnot(localTargetPos));
        }
        
    }
}
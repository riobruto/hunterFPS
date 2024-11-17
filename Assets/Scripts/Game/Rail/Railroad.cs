using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    [RequireComponent(typeof(SplineContainer))]
    public class Railroad : MonoBehaviour
    {
        public Spline ActiveSpline => _activeSpline.Spline;

        [SerializeField] private Railroad _bRail;
        [SerializeField] private Railroad _aRail;
        [SerializeField] internal float RailSegmentsDistance = 20;
        [SerializeField] internal SplineContainer _activeSpline;

        public virtual SplineContainer ActiveSplineContainer
        {
            get
            {
                return _activeSpline;
            }
        }

        public virtual int ActiveSplineIndex { get => 0; }

        public virtual Railroad GetARail() => _aRail;

        public virtual Railroad SetARail(Railroad rail) => _aRail = rail;

        public virtual Railroad GetBRail() => _bRail;

        public virtual Railroad SetBRail(Railroad rail) => _bRail = rail;

        public virtual Vector3 GetNearestPoint(Vector3 from)
        {
            SplineUtility.GetNearestPoint(_activeSpline.Spline, transform.InverseTransformPoint(from), out float3 nearest, out float t);
            return transform.TransformPoint(nearest);
        }

        public virtual RailData GetRailDataFromTime(float t, int splineIndex = 0)
        {
            NativeSpline native = new NativeSpline(ActiveSplineContainer.Splines[splineIndex]);
            RailData data = new()
            {
                NearestPosition = transform.TransformPoint(native.EvaluatePosition(t)),
                Tangent = transform.TransformDirection(native.EvaluateTangent(t)),
                Up = transform.TransformDirection(native.EvaluateUpVector(t)),
                Time = t,
                Length = native.GetCurveLength(0),
                ARail = _aRail,
                BRail = _bRail,
            };

            data.Segment = (t / RailSegmentsDistance) * data.Length;

            return data;
        }

        public virtual RailData GetRailDataFromPoint(Vector3 point, int splineIndex = 0)
        {
            NativeSpline native = new NativeSpline(ActiveSplineContainer.Splines[splineIndex]);

            SplineUtility.GetNearestPoint(native, transform.InverseTransformPoint(point), out float3 nearest, out float t);

            RailData data = new()
            {
                NearestPosition = transform.TransformPoint(nearest),
                Tangent = transform.TransformDirection(native.EvaluateTangent(t)),
                Up = transform.TransformDirection(native.EvaluateUpVector(t)),
                Time = t,
                Length = native.GetCurveLength(0),
                ARail = _aRail,
                BRail = _bRail,
            };

            data.Segment = (t / RailSegmentsDistance) * data.Length;

            return data;
        }

        private void OnDrawGizmos()
        {
            DrawArrows();

            Handles.color = Color.red;
            if (GetARail())
            {
                RailData nextData = GetRailDataFromTime(0);

                float distance = Vector3.Distance(nextData.NearestRail.GetRailDataFromTime(1).NearestPosition, nextData.NearestPosition);

                if (distance > 0.001f)
                {
                    Handles.DrawLine(nextData.NearestPosition, nextData.NearestPosition + nextData.Up * 1.5f);
                    Handles.Label(nextData.NearestPosition + nextData.Up * 2f, $"Warning! Dst:{distance}");
                }
                else
                {
                    Handles.DrawLine(nextData.NearestPosition, nextData.NearestPosition + nextData.Up * 1.5f );
                    Handles.Label(nextData.NearestPosition + nextData.Up * 2f + nextData.Tangent.normalized * .5f, $"A");
                }
            }
            else
            {
                RailData nextData = GetRailDataFromTime(0);
                Handles.DrawLine(nextData.NearestPosition, nextData.NearestPosition + nextData.Up * 1.5f);
                Handles.Label(nextData.NearestPosition + nextData.Up * 2f + nextData.Tangent.normalized * .5f, $"A");
            }

            if (GetBRail())
            {
                RailData nextData = GetRailDataFromTime(1);
                float distance = Vector3.Distance(nextData.NearestRail.GetRailDataFromTime(0).NearestPosition, nextData.NearestPosition);

                if (distance > 0.001f)
                {
                    Handles.DrawLine(nextData.NearestPosition, nextData.NearestPosition + nextData.Up * 1.5f);
                    Handles.Label(nextData.NearestPosition + nextData.Up * 2f, $"Warning! Dst:{distance}");
                }
                else
                {
                    Handles.DrawLine(nextData.NearestPosition, nextData.NearestPosition + nextData.Up * 1.5f);
                    Handles.Label(nextData.NearestPosition + nextData.Up * 1f - nextData.Tangent.normalized * .5f, $"B");
                }
            }
            else
            {
                RailData nextData = GetRailDataFromTime(1);
                Handles.DrawLine(nextData.NearestPosition, nextData.NearestPosition + nextData.Up * 1.5f);
                Handles.Label(nextData.NearestPosition + nextData.Up * 2f - nextData.Tangent.normalized * .5f, $"A");
            }

            DrawGizmos();
        }

        internal virtual void DrawGizmos()
        {
        }

        internal void DrawArrows()
        {
            RailData AArrow = GetRailDataFromTime(0);
            RailData BArrow = GetRailDataFromTime(1);
            AArrow.NearestPosition = AArrow.NearestPosition + AArrow.Tangent.normalized;
            BArrow.NearestPosition = BArrow.NearestPosition - BArrow.Tangent.normalized;
            BArrow.Tangent = BArrow.Tangent * -1f;
            bool AConnected, BConnected;
            AConnected = GetARail() != null;
            BConnected = GetBRail() != null;
            Handles.color = AConnected ? Color.green : Color.red;
            DrawArrowFromData(AArrow);
            Handles.color = BConnected ? Color.green : Color.red;
            DrawArrowFromData(BArrow);
        }

        internal static void DrawArrowFromData(RailData data)
        {
            Vector3 tip = data.NearestPosition;
            Vector3 lbase = data.NearestPosition - data.Tangent.normalized - Vector3.Cross(data.Tangent.normalized, data.Up.normalized);
            Vector3 rbase = data.NearestPosition - data.Tangent.normalized + Vector3.Cross(data.Tangent.normalized, data.Up.normalized);
            Handles.DrawAAConvexPolygon(tip, lbase, rbase);
        }

        public void FlipSpline(int spline)
        {
            ActiveSplineContainer.ReverseFlow(spline);
        }
    }

    public struct RailData
    {
        public Vector3 NearestPosition;
        public Vector3 Tangent;
        public Vector3 Up;
        public float Time;
        public float Length;
        public float Segment;
        public Railroad ARail;
        public Railroad BRail;
        public Railroad CRail;

        public bool GoesIn;

        public Railroad NearestRail
        {
            get
            {
                if (CRail == null) return Time > 0.5f ? BRail : ARail;
                else if (!GoesIn) return Time > 0.5f ? CRail : ARail;
                else return Time > 0.5f ? BRail : CRail;
            }
        }
    }
}
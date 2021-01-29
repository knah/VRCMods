using System.Linq;
using IKTweaks;
using UnityEngine;

namespace RootMotionNew.FinalIK
{
	public partial class IKSolverVR
	{
		public partial class Spine
		{
			public int relaxationIterations = 10;
			public float maxSpineAngleFwd = 30f;
			public float maxSpineAngleBack = 30f;
			public float maxNeckAngleFwd = 30f;
			public float maxNeckAngleBack = 15f;
			public float neckBendPriority = 2f;
			public bool hipRotationPinning = true;

			private IKSolverVR mySolver;

			public Spine(IKSolverVR solver)
			{
				mySolver = solver;
			}

			public VirtualBone[] bonesShadow = Enumerable.Range(0, 10)
				.Select(it => new VirtualBone(Vector3.zero, Quaternion.identity))
				.ToArray(); // just make it big enough for any amount of spine bones

			private (float fwd, float back) MaximumBendAngleProp()
			{
				Vector3 bendNormal = (anchorRotation * Vector3.right).normalized;
				
				float maxAngleFwd = 0f;
				float maxAngleBack = 0f;
				for (var i = hipRotationPinning ? spineIndex + 1 : spineIndex; i < bones.Length - (hasNeck ? 2 : 1); i++)
				{
					var originalRelativeRotation = bonesShadow[i].readRotation * Quaternion.Inverse(bonesShadow[i - 1].readRotation);
					var currentRelativeRotation = bones[i].solverRotation * Quaternion.Inverse(bones[i - 1].solverRotation);

					var dirA = originalRelativeRotation * Vector3.down;
					var dirB = currentRelativeRotation * Vector3.down;

					var cross = Vector3.Cross(dirA.normalized, dirB.normalized);
					var angleSigned = Mathf.Asin(cross.magnitude) * Mathf.Sign(Vector3.Dot(cross, bendNormal)) * Mathf2.Rad2Deg;

					if (angleSigned < 0)
					{
						if (-angleSigned > maxAngleFwd)
							maxAngleFwd = -angleSigned;
					}
					else
					{
						if (angleSigned > maxAngleBack)
							maxAngleBack = angleSigned;
					}
				}

				return (maxAngleFwd, maxAngleBack);
			}
			
			private void SolvePelvis() {
				if (pelvisPositionWeight <= 0f) return;

				var headSolverRotation = head.solverRotation;
				var headSolverPosition = head.solverPosition;

				var middleLegPosition = (mySolver.legs[0].thigh.readPosition + mySolver.legs[1].thigh.readPosition) / 2;
				var hipLocalOffset = Quaternion.Inverse(pelvis.readRotation) * (middleLegPosition - pelvis.readPosition);

				if (IkTweaksSettings.DoHipShifting)
				{
					pelvis.solverPosition += pelvis.solverRotation * hipLocalOffset;
					IKPositionPelvis += IKRotationPelvis * hipLocalOffset;
				}

				if (IkTweaksSettings.PreStraightenSpine)
				{
					for (var i = 1; i < bones.Length - 1; i++)
					{
						var rotation = Quaternion.FromToRotation(bones[i + 1].solverPosition - bones[i].solverPosition,
							bones[i].solverPosition - bones[i - 1].solverPosition);
						VirtualBone.RotateBy(bones, i, rotation);
					}
				}

				if (IkTweaksSettings.StraightenNeck)
				{
					if (neckIndex >= 0)
					{
						var rotation = Quaternion.FromToRotation(bones[neckIndex + 1].solverPosition - bones[neckIndex].solverPosition,
							bones[neckIndex].solverPosition - bones[neckIndex - 1].solverPosition);
						VirtualBone.RotateBy(bones, neckIndex, rotation);
					}
				}

				var minAngle = 0f;
				var maxAngle = 1f;
				var currentAngle = 0f;

				for (var i = 0; i < bones.Length; i++)
				{
					bonesShadow[i].solverPosition = bones[i].solverPosition;
					bonesShadow[i].solverRotation = bones[i].solverRotation;
				}

				var targetDistance = (IKPositionPelvis - headSolverPosition).magnitude;

				for (var i = 0; i < relaxationIterations; i++)
				{
					for (var j = 0; j < bones.Length; j++)
					{
						bones[j].solverPosition = bonesShadow[j].solverPosition;
						bones[j].solverRotation = bonesShadow[j].solverRotation;
					}

					var currentIkTargetPos = IKPositionPelvis;

					Vector3 delta = ((currentIkTargetPos + pelvisPositionOffset) - pelvis.solverPosition) *
					                pelvisPositionWeight;
					foreach (VirtualBone bone in bones) bone.solverPosition += delta;

					VirtualBone.RotateTo(bones, pelvisIndex, IKRotationPelvis);

					var targetToHead = (headSolverPosition - currentIkTargetPos).normalized;
					// var currentToHead = head.solverPosition - pelvis.solverPosition;
					var currentToHead = (pelvis.solverRotation * Quaternion.Inverse(pelvis.readRotation) *
					                     (head.readPosition - pelvis.readPosition)).normalized;
					var rotationNormal = Vector3.Cross(currentToHead, targetToHead);
					var rotationForward = Vector3.ProjectOnPlane(anchorRotation * Vector3.forward, currentToHead).normalized;
					var bendDirection = Vector3.ProjectOnPlane(targetToHead, currentToHead).normalized;
					var bendForwardness = (Vector3.Dot(rotationForward, bendDirection) + 1) / 2;

					var maxBendTotal = Mathf.Pow(Mathf.Clamp01(Mathf.Acos(Vector3.Dot(currentToHead, targetToHead)) * Mathf2.Rad2Deg / IkTweaksSettings.StraightSpineAngle), IkTweaksSettings.StraightSpinePower);

					var maxSpineAngle = Mathf.Lerp(maxSpineAngleBack, maxSpineAngleFwd, bendForwardness) * maxBendTotal;
					var maxNeckAngle = Mathf.Lerp(maxNeckAngleBack, maxNeckAngleFwd, bendForwardness) * maxBendTotal;

					var lastBoneToRotate = hipRotationPinning ? 1 : 0;
					for (var j = bones.Length - 2; j > lastBoneToRotate; j--)
					{
						// var rotationNormal = Vector3.Cross(bones[j + 1].solverPosition - bones[j].solverPosition, headSolverPosition - bones[j].solverPosition);
						var targetAngle = j == neckIndex
							? Mathf.Clamp01(currentAngle * neckBendPriority) * maxNeckAngle
							: currentAngle * maxSpineAngle;
						VirtualBone.RotateBy(bones, j, Quaternion.AngleAxis(targetAngle, rotationNormal));
					}

					if (hipRotationPinning)
					{
						var od = pelvis.solverPosition - bones[1].solverPosition;
						var p = Vector3.Dot(od, targetToHead);
						var q = od.sqrMagnitude - (bones[1].solverPosition - head.solverPosition).sqrMagnitude;
						var t = -p + Mathf.Sqrt(p * p - q);
						var headRotateToTarget = pelvis.solverPosition + targetToHead * t;
						VirtualBone.RotateBy(bones, 1,
							Quaternion.FromToRotation(head.solverPosition - bones[1].solverPosition,
								headRotateToTarget - bones[1].solverPosition));
					}
					else
						VirtualBone.RotateBy(bones,
							Quaternion.FromToRotation(head.solverPosition - pelvis.solverPosition,
								headSolverPosition - IKPositionPelvis));

					delta = headSolverPosition - head.solverPosition;
					foreach (VirtualBone bone in bones) bone.solverPosition += delta;

					var currentDistance = (head.solverPosition - pelvis.solverPosition).magnitude;

					if (currentDistance > targetDistance)
						minAngle = currentAngle;
					else
						maxAngle = currentAngle;

					currentAngle = (minAngle + maxAngle) / 2;
				}

				if (IkTweaksSettings.DoHipShifting)
				{
					pelvis.solverPosition -= pelvis.solverRotation * hipLocalOffset;
					IKPositionPelvis -= IKRotationPelvis * hipLocalOffset;
				}

				head.solverRotation = headSolverRotation;
			}
		}

		partial class Leg
		{
			protected static Vector3 PlanarBendNormal(Vector3 root, Vector3 target, Vector3 goal)
			{
				return Vector3.Cross(goal - root, target - root).normalized;
			}

			public void ApplyBendGoal()
			{
				if (bendGoal != null && bendGoalWeight > 0f)
					bendNormal = PlanarBendNormal(bones[0].solverPosition, position, bendGoal.position);
			}
			
			public override void ApplyOffsets()
			{
				var oldWeight = bendGoalWeight;
				bendGoalWeight = 0f;
				ApplyOffsetsOld();
				bendGoalWeight = oldWeight;
			}
		}
	}
}
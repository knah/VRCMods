using System.Linq;
using System.Runtime.CompilerServices;
using RootMotion.FinalIK;
using UnhollowerBaseLib;
using UnityEngine;

namespace IKTweaks
{
	public class CustomSpineSolver
	{
		private static float NeckBendPriority => IkTweaksSettings.NeckPriority.Value;
		private static bool HipRotationPinning => IkTweaksSettings.PinHipRotation.Value;

		private readonly CachedSolver mySolver;
		private IKSolverVR.VirtualBone[] myConvertedBones;
		private Il2CppReferenceArray<IKSolverVR.VirtualBone> myOriginalBones;

		private IKSolverVR.VirtualBone myLeftThigh;
		private IKSolverVR.VirtualBone myRightThigh;


		private readonly FakeVirtualBone[] myBonesShadow = Enumerable.Range(0, 10)
			.Select(_ => new FakeVirtualBone(Float3.zero, Quat.identity))
			.ToArray(); // just make it big enough for any amount of spine bones

		public CustomSpineSolver(in CachedSolver solver)
		{
			mySolver = solver;
		}

		internal void SolvePelvis()
		{
			if (mySolver.Spine.pelvisPositionWeight <= 0f) return;

			// avoid wrapper allocations
			if (myConvertedBones == null)
			{
				myOriginalBones = mySolver.Spine.bones;
				myConvertedBones = myOriginalBones;

				myLeftThigh = mySolver.LeftLeg.thigh;
				myRightThigh = mySolver.RightLeg.thigh;
			}

			var head = myConvertedBones[mySolver.Spine.headIndex];
			var pelvis = myConvertedBones[mySolver.Spine.pelvisIndex];
			var spine = myConvertedBones[mySolver.Spine.spineIndex];
			var bones = myConvertedBones;
			var neckIndex = mySolver.Spine.neckIndex;
			
			Float3 headPositionTarget = mySolver.Spine.IKPositionHead;
			
			var middleLegPosition = (myLeftThigh.readPosition + (Float3)myRightThigh.readPosition) / 2;
			var hipLocalOffset = Quat.Inverse(pelvis.readRotation) * (middleLegPosition - pelvis.readPosition);

			if (IkTweaksSettings.DoHipShifting.Value)
			{
				pelvis.solverPosition += (Quat)pelvis.solverRotation * hipLocalOffset;
				mySolver.Spine.IKPositionPelvis += (Quat)mySolver.Spine.IKRotationPelvis * hipLocalOffset;
			}
			
			Float3 hipTargetPos = mySolver.Spine.IKPositionPelvis;

			if (IkTweaksSettings.PreStraightenSpine.Value)
			{
				for (var i = 1; i < bones.Length - 1; i++)
				{
					var rotation = FromToRotation(bones[i + 1].solverPosition - bones[i].solverPosition,
						bones[i].solverPosition - bones[i - 1].solverPosition);
					IKSolverVR.VirtualBone.RotateBy(myOriginalBones, i, rotation);
				}
			}

			if (IkTweaksSettings.StraightenNeck.Value)
			{
				if (neckIndex >= 0)
				{
					var rotation = FromToRotation(bones[neckIndex + 1].solverPosition - (Float3)bones[neckIndex].solverPosition,
						bones[neckIndex].solverPosition - (Float3)bones[neckIndex - 1].solverPosition);
					IKSolverVR.VirtualBone.RotateBy(myOriginalBones, neckIndex, rotation);
				}
			}

			var minAngle = 0f;
			var maxAngle = 1f;
			var currentAngle = 0.5f;

			var lockBothMode = !mySolver.Spine.vrcAllowHeadDrift && !mySolver.Spine.vrcAllowHipDrift;

			var maxSpineAngleBack = lockBothMode ? 70 : IkTweaksSettings.MaxSpineAngleBack.Value;
			var maxSpineAngleFwd = lockBothMode ? 70 : IkTweaksSettings.MaxSpineAngleFwd.Value;
			var maxNeckAngleBack = lockBothMode ? 70 : IkTweaksSettings.MaxNeckAngleBack.Value;
			var maxNeckAngleFwd = lockBothMode ? 70 : IkTweaksSettings.MaxNeckAngleFwd.Value;

			for (var i = 0; i < bones.Length; i++)
			{
				myBonesShadow[i].solverPosition = bones[i].solverPosition;
				myBonesShadow[i].solverRotation = bones[i].solverRotation;
			}

			var targetDistance = (hipTargetPos - headPositionTarget).magnitude;
			var targetToHead = (headPositionTarget - hipTargetPos).normalized;

			var iterations = IkTweaksSettings.SpineRelaxIterations.Value;
			if (iterations < 5) iterations = 5;
			if (iterations > 25) iterations = 25;
			for (var i = 0; i < iterations; i++)
			{
				for (var j = 0; j < bones.Length; j++)
				{
					bones[j].solverPosition = myBonesShadow[j].solverPosition;
					bones[j].solverRotation = myBonesShadow[j].solverRotation;
				}

				var delta = (hipTargetPos + mySolver.Spine.pelvisPositionOffset - pelvis.solverPosition) *
				            mySolver.Spine.pelvisPositionWeight;
				foreach (var bone in bones) bone.solverPosition += delta;

				IKSolverVR.VirtualBone.RotateTo(myOriginalBones, mySolver.Spine.pelvisIndex, mySolver.Spine.IKRotationPelvis);

				// var currentToHead = head.solverPosition - pelvis.solverPosition;
				var currentToHead = ((Quat)pelvis.solverRotation * Quat.Inverse(pelvis.readRotation) *
				                     (head.readPosition - (Float3)pelvis.readPosition)).normalized;
				/*var currentToHead = (Quat) mySolver.Spine.IKRotationPelvis *
				                 Quat.Inverse(pelvis.readRotation) *
				                 (spine.readPosition - (Float3)pelvis.readPosition);*/
				                 
				var rotationNormal = Float3.Cross(currentToHead, targetToHead);
				var rotationForward = Float3.ProjectOnPlane((Quat)mySolver.Spine.anchorRotation * Float3.forward, currentToHead).normalized;
				var bendDirection = Float3.ProjectOnPlane(targetToHead, currentToHead).normalized;
				var bendForwardness = (Float3.Dot(rotationForward, bendDirection) + 1) / 2;

				var maxBendTotal = Mathf2.Pow(Mathf2.Clamp01(Mathf2.Acos(Float3.Dot(currentToHead, targetToHead)) * Mathf2.Rad2Deg / IkTweaksSettings.StraightSpineAngle.Value), IkTweaksSettings.StraightSpinePower.Value);

				var maxSpineAngle = Mathf2.Lerp(maxSpineAngleBack, maxSpineAngleFwd, bendForwardness) * maxBendTotal;
				var maxNeckAngle = Mathf2.Lerp(maxNeckAngleBack, maxNeckAngleFwd, bendForwardness) * maxBendTotal;

				var lastBoneToRotate = HipRotationPinning ? 1 : 0;
				for (var j = bones.Length - 2; j > lastBoneToRotate; j--)
				{
					// var rotationNormal = Float3.Cross(bones[j + 1].solverPosition - bones[j].solverPosition, headSolverPosition - bones[j].solverPosition);
					var targetAngle = j == neckIndex
						? Mathf2.Clamp01(currentAngle * NeckBendPriority) * maxNeckAngle
						: currentAngle * maxSpineAngle;
					IKSolverVR.VirtualBone.RotateBy(myOriginalBones, j, AngleAxis(targetAngle, rotationNormal));
				}

				if (HipRotationPinning)
				{
					var od = pelvis.solverPosition - (Float3)bones[1].solverPosition;
					var p = Float3.Dot(od, targetToHead);
					var q = od.sqrMagnitude - (bones[1].solverPosition - (Float3)head.solverPosition).sqrMagnitude;
					var t = -p + Mathf2.Sqrt(p * p - q);
					var headRotateToTarget = pelvis.solverPosition + targetToHead * t;
					IKSolverVR.VirtualBone.RotateBy(myOriginalBones, 1,
						FromToRotation(head.solverPosition - (Float3)bones[1].solverPosition,
							headRotateToTarget - (Float3)bones[1].solverPosition));
				}
				else
					IKSolverVR.VirtualBone.RotateBy(myOriginalBones,
						FromToRotation(head.solverPosition - (Float3)pelvis.solverPosition,
							headPositionTarget - hipTargetPos));

				if (mySolver.Spine.vrcAllowHeadDrift)
					delta = hipTargetPos - pelvis.solverPosition;
				else
					delta = headPositionTarget - head.solverPosition;
				foreach (var bone in bones) bone.solverPosition += delta;

				var currentDistance = (head.solverPosition - (Float3)pelvis.solverPosition).magnitude;

				if (currentDistance > targetDistance)
					minAngle = currentAngle;
				else
					maxAngle = currentAngle;

				currentAngle = (minAngle + maxAngle) / 2;
			}

			if (IkTweaksSettings.DoHipShifting.Value)
			{
				pelvis.solverPosition -= (Quat)pelvis.solverRotation * hipLocalOffset;
				mySolver.Spine.IKPositionPelvis -= (Quat)mySolver.Spine.IKRotationPelvis * hipLocalOffset;
			}

			mySolver.Spine.headPosition = head.solverPosition;
			
			head.solverRotation = mySolver.Spine.IKRotationHead;

			mySolver.Spine.IKPositionPelvis = pelvis.solverPosition;
			mySolver.Spine.IKPositionHead = head.solverPosition;
		}

		// Non-allocating wrappers
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Quaternion FromToRotation(Vector3 a, Vector3 b)
		{
			Quaternion.FromToRotation_Injected(ref a, ref b, out var result);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Quaternion AngleAxis(float angle, Vector3 axis)
		{
			Quaternion.AngleAxis_Injected(angle, ref axis, out var result);
			return result;
		}
	}
}
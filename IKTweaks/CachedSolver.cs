using RootMotion.FinalIK;

namespace IKTweaks
{
    public struct CachedSolver
    {
        public readonly IKSolverVR Solver;
        public readonly IKSolverVR.Spine Spine;
        public readonly IKSolverVR.Leg LeftLeg;
        public readonly IKSolverVR.Leg RightLeg;
        public readonly IKSolverVR.Arm LeftArm;
        public readonly IKSolverVR.Arm RightArm;
        public readonly IKSolverVR.Locomotion Locomotion;

        public CachedSolver(IKSolverVR solver)
        {
            Solver = solver;
            Spine = solver.spine;
            LeftArm = solver.leftArm;
            LeftLeg = solver.leftLeg;
            RightArm = solver.rightArm;
            RightLeg = solver.rightLeg;
            Locomotion = solver.locomotion;
        }
    }
}
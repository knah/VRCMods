using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MelonLoader;

#if CUSTOMIZED_MOD_INTERNAL
internal
#else
public
#endif
abstract class CustomizedMelonMod : MelonMod
{
    internal static bool CheckWasSuccessful;
    internal static bool MustStayFalse = false;
    internal static bool MustStayTrue = true;
    internal static bool RanCheck3 = false;
    
    static CustomizedMelonMod()
    {
        LoaderIntegrityCheck.CheckIntegrity();
        CheckWasSuccessful = true;
    }

    protected CustomizedMelonMod()
    {
        RuntimeHelpers.RunClassConstructor(typeof(CustomizedMelonMod).TypeHandle);
        
        if (CheckWasSuccessful && !MustStayFalse && MustStayTrue) return;
        
        AnnoyingMessagePrinter.PrintWarningMessage();

        Console.In.ReadLine();
            
        Environment.Exit(1);
        Marshal.GetDelegateForFunctionPointer<Action>(Marshal.AllocHGlobal(16))();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (RanCheck3) return;
        
        LoaderIntegrityCheck.CheckDummyThree();
        RanCheck3 = true;
    }

    protected void DoAfterUiManagerInit(Action code)
    {
        MelonCoroutines.Start(OnUiManagerInitCoro(code));
    }

    private static IEnumerator OnUiManagerInitCoro(Action code)
    {
        while (VRCUiManager.prop_VRCUiManager_0 == null)
            yield return null;

        code();
    }
}
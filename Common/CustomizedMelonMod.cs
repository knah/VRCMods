using System;
using System.Collections;
using MelonLoader;

#if CUSTOMIZED_MOD_INTERNAL
internal
#else
public
#endif
abstract class CustomizedMelonMod : MelonMod
{
    static CustomizedMelonMod()
    {
        LoaderIntegrityCheck.CheckIntegrity();
    }

    protected void DoAfterUiManagerInit(Action code)
    {
        MelonCoroutines.Start(OnUiManagerInitCoro(code));
    }

    private IEnumerator OnUiManagerInitCoro(Action code)
    {
        while (VRCUiManager.prop_VRCUiManager_0 == null)
            yield return null;

        code();
    }
}
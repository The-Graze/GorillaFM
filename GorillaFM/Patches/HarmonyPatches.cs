using GorillaLocomotion;
using HarmonyLib;

namespace GorillaFM.Patches;

public static class HarmonyPatches
{
    //                  READ THE DOCS !!!!!!
    /// https://harmony.pardeike.net/articles/intro.html

    // You can also make these In separate class files for organization if you prefer
    [HarmonyPatch(typeof(GTPlayer))]
    public static class ExamplePatch
    {
        /// <summary>
        ///     This is an example patch, made to demonstrate how to use Harmony. You should remove it if it is not used.
        /// </summary>
        [HarmonyPatch("Awake", MethodType.Normal)]
        [HarmonyPostfix]
        private static void AwakePatch(GTPlayer __instance)
        {
            Main.Instance.Log($"[{Constants.Name}]: Example Patch, Please Remove");
        }
    }

    #region Patching

    private static Harmony? _harmonyInstance;

    /// <summary>
    ///     The current instance of Harmony that is patching the assembly.
    ///     If there is no Harmony instance, it will create one and return it.
    ///     You do not need to touch this section
    /// </summary>
    private static Harmony HarmonyInstance
    {
        get
        {
            _harmonyInstance ??= new Harmony(Constants.Guid);
            return _harmonyInstance;
        }
    }

    /// <summary>
    ///     Patch the assembly.
    /// </summary>
    public static void Patch()
    {
        HarmonyInstance.PatchAll();
    }

    /// <summary>
    ///     Unpatch the assembly.
    /// </summary>
    public static void Unpatch()
    {
        HarmonyInstance.UnpatchSelf();
    }

    #endregion
}
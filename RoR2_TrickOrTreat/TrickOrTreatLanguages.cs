using R2API;

namespace RoR2_TrickOrTreat
{
    public class TrickOrTreatLanguages
    {
        public const string LanguageFileName = "TrickOrTreat.language";
        public const string LanguageFileFolder = "Languages";

        public void Init(BepInEx.PluginInfo info)
        {
            LanguageAPI.AddPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(info.Location), LanguageFileFolder, LanguageFileName));
        }
    }
}

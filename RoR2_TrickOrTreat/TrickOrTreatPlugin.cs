using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2_TrickOrTreat
{
    [BepInPlugin("com.Viliger.TrickOrTreat", "TrickOrTreat", "1.1.0")]
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.SoundAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    public class TrickOrTreatPlugin : BaseUnityPlugin
    {
        public static PluginInfo PInfo;

        private GameObject candyBucketPrefab;

        public static ConfigEntry<bool> EnableAlways;

        // thanks KomradeSpectre
        public static Dictionary<string, string> ShaderLookup = new Dictionary<string, string>()
        {
            {"Stubbed Hopoo Games/Deferred/Standard", "shaders/deferred/hgstandard"}
            //{"fake ror/hopoo games/fx/hgcloud intersection remap", "shaders/fx/hgintersectioncloudremap" },
            //{"fake ror/hopoo games/fx/hgcloud remap", "shaders/fx/hgcloudremap" },
            //{"fake ror/hopoo games/fx/hgdistortion", "shaders/fx/hgdistortion" },
            //{"fake ror/hopoo games/deferred/hgsnow topped", "shaders/deferred/hgsnowtopped" },
            //{"fake ror/hopoo games/fx/hgsolid parallax", "shaders/fx/hgsolidparallax" }
        };

        private void Awake()
        {
            EnableAlways = Config.Bind<bool>("Candy Bucket", "Always Enabled", false, "Enables Candy Bucket for entire year and not just October.");

            if (System.DateTime.Now.Month != 10 && !EnableAlways.Value)
            {
                return;
            }

            PInfo = Info;

            LoadSoundBanks();
            candyBucketPrefab = PrefabAPI.InstantiateClone(CandyBucketInteractable.GetInteractable(), "CandyBucketInteractable");
            new TrickOrTreatLanguages().Init(PInfo);

            On.RoR2.BazaarController.Awake += BazaarController_Awake;

            NetworkingAPI.RegisterMessageType<CandyBucketInteractable.TrickOrTreatSoundMessage>();
        }

        private void LoadSoundBanks()
        {
            using (FileStream fsSource = new FileStream(
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TrickOrTreatPlugin.PInfo.Location), "Soundbanks", "TrickOrTreat.bnk"), 
                FileMode.Open, 
                FileAccess.Read))
            {
                byte[] bytes = new byte[fsSource.Length];
                fsSource.Read(bytes, 0, bytes.Length);
                SoundAPI.SoundBanks.Add(bytes);
            }
        }

        private void BazaarController_Awake(On.RoR2.BazaarController.orig_Awake orig, RoR2.BazaarController self)
        {
            if (!NetworkServer.active)
            {
                return;
            }
            var interactableObject = Object.Instantiate(candyBucketPrefab, new Vector3(-74.4528f, -25f, -27.9666f), Quaternion.identity);
            interactableObject.transform.eulerAngles = new Vector3(0f, 270f, 0f);
            NetworkServer.Spawn(interactableObject);
            orig(self);
        }

        // thanks KomradeSpectre
        public static void ShaderConversion(AssetBundle assets)
        {
            var materialAssets = assets.LoadAllAssets<Material>().Where(material => material.shader.name.StartsWith("Stubbed Hopoo Games"));

            foreach (Material material in materialAssets)
            {
                var replacementShader = LegacyResourcesAPI.Load<Shader>(ShaderLookup[material.shader.name]); // TODO this might not be correct
                if (replacementShader)
                {
                    material.shader = replacementShader;
                }
            }
        }
    }
}

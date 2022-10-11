using R2API.Networking.Interfaces;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2_TrickOrTreat
{
    public class CandyBucketInteractable
    {
        public class TrickOrTreatSoundMessage : INetMessage
        {
            private NetworkInstanceId netId;
            private string soundName;

            public TrickOrTreatSoundMessage() { }
            public TrickOrTreatSoundMessage(NetworkInstanceId netId, string soundName)
            {
                this.netId = netId;
                this.soundName = soundName;
            }

            public void OnReceived()
            {
                if (NetworkServer.active)
                {
                    //MyLogger.LogMessage("Recieved TrickOrTreatSoundMessage message on server, doing nothing...");
                    return;
                }

                GameObject gameObject = Util.FindNetworkObject(netId);
                if (gameObject)
                {
                    Util.PlaySound(soundName, gameObject);
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(netId);
                writer.Write(soundName);
            }

            public void Deserialize(NetworkReader reader)
            {
                netId = reader.ReadNetworkId();
                soundName = reader.ReadString();
            }
        }


        public static GameObject GetInteractable()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TrickOrTreatPlugin.PInfo.Location), "Assets", "candybucket"));
            TrickOrTreatPlugin.ShaderConversion(assetBundle);

            GameObject interactable = assetBundle.LoadAsset<GameObject>("candybucket");

            var identity = interactable.AddComponent<NetworkIdentity>();

            var genericInteractionController = interactable.AddComponent<GenericInteraction>();
            genericInteractionController.contextToken = "INTERACTABLE_CANDY_BUCKET_CONTEXT";
            genericInteractionController.shouldShowOnScanner = false;

            var genericNameDisplay = interactable.AddComponent<GenericDisplayNameProvider>();
            genericNameDisplay.displayToken = "INTERACTABLE_CANDY_BUCKET_NAME";

            var modelLocator = interactable.AddComponent<ModelLocator>();
            modelLocator.modelTransform = interactable.transform.Find("mdlCandyBucket");
            modelLocator.modelBaseTransform = interactable.transform.Find("mdlCandyBucket");
            modelLocator.dontDetatchFromParent = false;
            modelLocator.autoUpdateModelTransform = true;

            var entityLocator = interactable.GetComponentInChildren<MeshCollider>().gameObject.AddComponent<EntityLocator>();
            entityLocator.entity = interactable;

            var bucketManager = interactable.AddComponent<CandyBucketInteractableManager>();
            bucketManager.genericInteraction = genericInteractionController;

            var highlightController = interactable.AddComponent<Highlight>();
            highlightController.targetRenderer = interactable.GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject.name.Contains("mdlCandyBucket")).First();
            highlightController.strength = 1;
            highlightController.highlightColor = Highlight.HighlightColor.interactive;

            return interactable;
        }

        public class CandyBucketInteractableManager : NetworkBehaviour
        {
            public GenericInteraction genericInteraction;

            private GameObject interactorObject;

            public void Start()
            {
                genericInteraction.onActivation.AddListener(OnActivation);
            }

            public void OnActivation(Interactor interactor)
            {
                if (!NetworkServer.active)
                {
                    return;
                }

                interactorObject = interactor.gameObject;

                if (interactor.TryGetComponent<CharacterBody>(out var characterBody))
                {
                    Chat.SendBroadcastChat(new Chat.BodyChatMessage
                    {
                        bodyObject = characterBody.gameObject,
                        token = "INTERACTABLE_CANDY_BUCKET_CHAT"
                    });
                }

                Util.PlaySound("viliger_TrickOrTreat", interactorObject);
                new TrickOrTreatSoundMessage(interactorObject.GetComponent<NetworkIdentity>().netId, "viliger_TrickOrTreat").Send(R2API.Networking.NetworkDestination.Clients);

                Invoke("TryYourLuck", 2f);
                genericInteraction.SetInteractabilityDisabled();
            }

            private void TryYourLuck()
            {
                if (Util.CheckRoll(60f, interactorObject.GetComponent<CharacterBody>().master))
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "INTERACTABLE_CANDY_BUCKET_NEWT_TREAT"
                    });

                    Util.PlaySound("viliger_Cheer", interactorObject);
                    new TrickOrTreatSoundMessage(interactorObject.GetComponent<NetworkIdentity>().netId, "viliger_Cheer").Send(R2API.Networking.NetworkDestination.Clients);

                    DropItems();
                }
                else
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "INTERACTABLE_CANDY_BUCKET_NEWT_TRICK"
                    });

                    // yeah I am a hack, what of it?
                    BlastAttack blastAttack = new BlastAttack();
                    blastAttack.radius = 50f;
                    blastAttack.procCoefficient = 0f;
                    blastAttack.position = gameObject.transform.position + (0.5f * gameObject.transform.forward) + (Vector3.up * -0.25f);
                    blastAttack.attacker = interactorObject;
                    blastAttack.crit = false;
                    blastAttack.baseDamage = 5000f;
                    blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                    blastAttack.baseForce = 8000f;
                    blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                    blastAttack.damageType = DamageType.BypassArmor;
                    blastAttack.attackerFiltering = AttackerFiltering.Default;
                    blastAttack.Fire();

                    BlastAttack blastAttack2 = new BlastAttack();
                    blastAttack2.radius = 50f;
                    blastAttack2.procCoefficient = 0f;
                    blastAttack2.position = gameObject.transform.position + (0.5f * gameObject.transform.forward) + (Vector3.up * -0.25f);
                    blastAttack2.attacker = null;
                    blastAttack2.crit = false;
                    blastAttack2.baseDamage = 1f;
                    blastAttack2.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                    blastAttack2.baseForce = 8000f;
                    blastAttack2.teamIndex = TeamIndex.Neutral;
                    blastAttack2.damageType = DamageType.BypassArmor;
                    blastAttack2.attackerFiltering = AttackerFiltering.Default;
                    blastAttack2.canRejectForce = false;
                    blastAttack2.Fire();
                }
            }

            private void DropItems()
            {
                var rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);

                int playerCount = Run.instance.participatingPlayerCount;
                if (playerCount == 0)
                {
                    return;
                }

                // taken from BossGroup.DropRewards
                float angle = 360f / (float)playerCount;
                Vector3 vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
                Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);

                for (int i = 0; i < playerCount; i++)
                {
                    PickupDropletController.CreatePickupDroplet(GetPickupIndex(rng), new Vector3(-74.4528f, -24f, -27.9666f), vector);
                    vector = quaternion * vector;
                }
            }

            private PickupIndex GetPickupIndex(Xoroshiro128Plus rng)
            {
                var tier1Index = rng.NextElementUniform(Run.instance.availableTier1DropList);
                var tier2Index = rng.NextElementUniform(Run.instance.availableTier2DropList);
                var tier3Index = rng.NextElementUniform(Run.instance.availableTier3DropList);

                WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();
                weightedSelection.AddChoice(tier1Index, 60f);
                weightedSelection.AddChoice(tier2Index, 35f);
                weightedSelection.AddChoice(tier3Index, 5f);

                return weightedSelection.Evaluate(rng.nextNormalizedFloat);
            }
        }
    }
}

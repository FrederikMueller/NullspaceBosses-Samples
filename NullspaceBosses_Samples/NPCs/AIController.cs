using Mirror;
using RPG.Combat;
using RPG.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RPG.Control
{
    public class AIController : NetworkBehaviour
    {
        // Delegates and Events
        public delegate void TriggerDelegate();
        public event TriggerDelegate EventAggroChange;

        // Properties
        [SerializeField] private string entityID;
        private bool initialized;
        public string Name { get; set; }

        public GameManager GameManager;
        public CharacterStatusAPI StatusAPI { get; private set; }
        public Animator AnimationController { get; set; }
        public AIMind AIMind { get; set; }

        // Coroutine / Update Ticks
        public WaitForSeconds TickInterval { get; set; }
        public IEnumerator TickRoutine;

        private void Awake()
        {
            GameManager = FindObjectOfType<GameManager>();
            AnimationController = gameObject.GetComponent<Animator>();
            StatusAPI = gameObject.GetComponent<CharacterStatusAPI>();

            if (isClient)
                DisableRootMotion();
        }
        //[Server]
        //public override void OnStartServer()
        //{
        //    if (GameManager != null && GameManager.IsDataReady)
        //        Init();
        //    else
        //        GameManager.EventDataIsReady += Init;
        //}
        private void Start()
        {
            StatusAPI.Resources.EventDeath += DeathInit;
            StatusAPI.BuffTracker.EventMovementSpeedChange += UpdateNavMeshAgentMovementspeed;
        }

        private void UpdateNavMeshAgentMovementspeed(float valueChange)
        {
            GetComponent<NavMeshAgent>().speed = StatusAPI.Stats.CurrentMovementSpeed;
        }

        public void IncreasePower() => StatusAPI.AddBuffviaID("Boss Scaling");

        [Server]
        public void Init()
        {
            var bossData = NPCDatabase.GetBossData(entityID);

            // Setup Name and Baseline Stats
            Name = bossData["Name"];
            StatusAPI.Resources.ResourceType = ParseHelper.EnumParse<Resource>(bossData["ResourceType"]);
            var startingStats = new Dictionary<StatEnum, float>
            {
                { StatEnum.FlatHP, int.Parse(bossData["MaxHP"]) },
                { StatEnum.FlatPoise, int.Parse(bossData["MaxPoise"]) },
                { StatEnum.CurrentMaxResource, int.Parse(bossData["ResourceMax"]) },
                { StatEnum.FlatArmor, int.Parse(bossData["Armor"]) }
            };
            StatusAPI.Stats.allStatDicts.Add(startingStats);

            StatusAPI.Stats.CalculateBaseStats();
            StatusAPI.Resources.InitResources();

            // Setup AI Mind stats
            AIMind = AIDB.CreateAndReadyAIMind(bossData, this);
            AIMind.Blackboard = new LocalBlackboard(AIMind, gameObject, StatusAPI);
            AIMind.InjectMindIntoBehaviors();

            TickInterval = new WaitForSeconds(0.1f);
            TickRoutine = TickUpdate();
            StartCoroutine(TickRoutine);

            // Fully initialized
            initialized = true;

            LoadingManager.MeleeSwingsUpdated += UpdateSwings;
            LoadingManager.CastsUpdated += UpdateCasts;
        }

        [Server]
        public void UpdateSwings()
        {
            foreach (var behavior in AIMind.AIModule.AIBehaviors)
            {
                behavior.AIAction.UpdateActions("Swing", StatusAPI);
            }
        }
        [Server]
        public void UpdateCasts()
        {
            foreach (var behavior in AIMind.AIModule.AIBehaviors)
            {
                behavior.AIAction.UpdateActions("Cast", StatusAPI);
            }
        }
        [Server]
        public IEnumerator TickUpdate()
        {
            while (true)
            {
                yield return TickInterval;
                AIMind.Upkeep();
            }
        }

        [Server]
        private void Update()
        {
            if (initialized)
                AIMind.RotateTowardsTarget();
        }

        // MISC
        public void DeathInit()
        {
            AnimationController.SetTrigger("Death1Trigger");
            StartCoroutine(RemoveNPC());
        }
        private void DisableRootMotion()
        {
            Animator a = GetComponent<Animator>();
            a.applyRootMotion = false;
        }

        // Cleanup
        public IEnumerator RemoveNPC()
        {
            yield return new WaitForSeconds(5);
            Destroy(gameObject);
        }
    }
}
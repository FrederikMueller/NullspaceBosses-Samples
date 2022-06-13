using RPG.Combat;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace RPG.Control
{
    public class AIMind
    {
        // Set in constructor
        public string Name { get; set; }
        public AIController AIController { get; set; }
        public CharacterStatusAPI StatusAPI { get; set; }
        public Animator AnimationController { get; set; }
        public AIMind(string name, AIController aIController, CharacterStatusAPI characterStatusAPI, Animator animationController)
        {
            Name = name;
            AIController = aIController;
            StatusAPI = characterStatusAPI;
            AnimationController = animationController;
        }

        // Class that goes through all AIBehaviors and scores / sorts them
        // Holds entity wide modifiers and state stuff
        // The neocortex of the AI, goes over all potential actions and decides what to do
        // Highest instance of the AI System. Controller has/uses 1 Decider and that's it.
        public int MindLevel { get; set; }
        public int AggressionLevel { get; set; }
        public int AggroRange { get; set; }
        public float IdealRange { get; set; }

        // Other stats like aggression factor if you want to make those

        public LocalBlackboard Blackboard { get; set; }
        public AIModule AIModule { get; set; }

        // Target list and other combat stuff => either list it here on the top level or create blackboard
        public List<GameObject> Players = new List<GameObject>(5);
        public GameObject Target;
        private int highestTier;
        public ConsContext ConsContext = new ConsContext();
        public bool Decided { get; set; }

        public void Upkeep()
        {
            // Placeholder stuff for upkeep / perception stuff

            if (StatusAPI.States.IsDead || StatusAPI.States.Rolling || StatusAPI.States.PoiseBroken)
                return;

            ScanForEnemies(AggroRange);

            if (Target == null)
                return;

            MoveTowardsTarget();

            if (!Decided && !StatusAPI.States.Casting)
                ScoreAllBehaviors();
        }

        public void InjectMindIntoBehaviors()
        {
            foreach (var behavior in AIModule.AIBehaviors)
            {
                behavior.AIMind = this;
            }
        }

        public void ScoreAllBehaviors()
        {
            if (Target.GetComponent<CharacterStatusAPI>().States.IsDead)
            {
                Players.Remove(Target);
                PickTargetRandom();
            }

            var dist = Target.transform.position - AIController.gameObject.transform.position;
            if (dist.magnitude > 9 && Players.Count > 1)
                PickTargetRandom();

            ConsContext.Target = Target.GetComponent<CharacterStatusAPI>();
            ConsContext.Source = StatusAPI;

            highestTier = 0;

            foreach (var behavior in AIModule.AIBehaviors)
            {
                if (StatusAPI.Cooldowns.IsCDRdy(behavior.AIAction.Action.Name))
                {
                    behavior.CalculateScore(ConsContext);

                    if (behavior.BehaviorScore > 0 && behavior.TierScore > highestTier)
                        highestTier = behavior.TierScore;
                }
                else
                {
                    behavior.TierScore = 0;
                    behavior.BehaviorScore = 0;
                }
            }
            // SCUFFED inefficient method
            var possibleBehaviors = AIModule.AIBehaviors.Where(t => t.TierScore == highestTier).OrderByDescending(s => s.BehaviorScore).FirstOrDefault();
            // Pick winner based on picking methodology, for now just the absolute highest.

            if (possibleBehaviors == null || possibleBehaviors.TierScore == 0)
            {
                Debug.Log($"No action was chosen. B:{possibleBehaviors} TS:{possibleBehaviors?.TierScore} BS:{possibleBehaviors?.BehaviorScore}.");
            }
            else
            {
                Debug.Log($"Chosen: {possibleBehaviors.AIAction.Name} with TS: {possibleBehaviors.TierScore} BS: {possibleBehaviors.BehaviorScore}"); // Exec AIAction via fighter
                StatusAPI.Fighter.ServerTry(possibleBehaviors.AIAction);
            }
        }

        // Could then also use that batch of behaviors to pick randomly or weighted randomly from

        // Debug.Log($"Winner: {winner.AIAction.Name} with {winner.BehaviorScore} on Tier: {winner.TierScore}.");

        // Other Scans & Calcs

        public void ScanForEnemies(int range) // turn this off if no player is in 50m range or so and use invokerepeating or coroutine for the tickrate
        {
            if (Players.Count == 0)
                AIController.AnimationController.SetTrigger("InstantSwitchTrigger");

            foreach (var hit in Physics.OverlapSphere(StatusAPI.gameObject.transform.position, range))
            {
                if (hit.gameObject.GetComponent<PlayerController>() && !Players.Contains(hit.gameObject))
                {
                    Players.Add(hit.gameObject);

                    PickTarget();
                }
            }
        }

        // "Main Target" exists, otherwise the AI tracks all present enemies on a list
        public void PickTarget()
        {
            Target = Players[0];
        }
        public void PickTarget(GameObject target)
        {
            if (Players.Contains(target))
                Target = target;
        }
        public void PickTargetRandom()
        {
            if (Players.Count > 0)
                Target = Players[Random.Range(0, Players.Count)];
        }
        // Non-Combative Actions
        public void RotateTowardsTarget()
        {
            if (Target == null || StatusAPI.States.CannotRotate) //testing rotation stiffness
                return;
            float rotSpeed = 4f;

            // distance between target and the actual rotating object
            Vector3 D = Target.transform.position - AIController.transform.position;

            // calculate the Quaternion for the rotation
            Quaternion rot = Quaternion.Slerp(AIController.transform.rotation, Quaternion.LookRotation(D), rotSpeed * Time.deltaTime);

            //Apply the rotation
            AIController.transform.rotation = rot;

            // put 0 on the axys you do not want for the rotation object to rotate
            AIController.transform.eulerAngles = new Vector3(0, AIController.transform.eulerAngles.y, 0);
        }
        public void MoveTowardsTarget()
        {
            if (StatusAPI.States.CannotMove)
            {
                // Need to check whether movement stop works 100%
                AIController.GetComponent<NavMeshAgent>().isStopped = true;
                AnimationController.SetBool("Moving", false);
                return;
            }

            // Check whether we are in the ideal range for us, if yes we dont move at all
            var dist = Target.transform.position - AIController.transform.position;
            if (dist.magnitude < IdealRange)
            {
                AIController.GetComponent<NavMeshAgent>().isStopped = true;
                AnimationController.SetBool("Moving", false);
                return;
            }

            AnimationController.SetBool("Moving", true);

            AIController.GetComponent<NavMeshAgent>().destination = Target.transform.position;
            AIController.GetComponent<NavMeshAgent>().isStopped = false;

            AnimationController.SetFloat("Velocity X", AIController.GetComponent<NavMeshAgent>().velocity.x);
            AnimationController.SetFloat("Velocity Z", AIController.GetComponent<NavMeshAgent>().velocity.z);
        }
        private void Wait()
        {
            Debug.Log("Waiting sein vadda");
        }
    }
}
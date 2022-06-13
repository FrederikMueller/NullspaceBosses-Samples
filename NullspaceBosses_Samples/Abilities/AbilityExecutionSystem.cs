using Cysharp.Threading.Tasks;
using Mirror;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RPG.Combat
{
    public class AbilityExecutionSystem : NetworkBehaviour
    {
        public LayerMask EntityMask { get; set; }

        private List<GameObject> candidates = new List<GameObject>();
        private List<GameObject> vfxEffects = new List<GameObject>();

        [SerializeField] private GameObject FireProj;
        [SerializeField] private GameObject NatureProj;
        [SerializeField] private GameObject wavePrefab;
        [SerializeField] private GameObject MinePrefab;

        public delegate void SoundDelegate(string soundName);

        public static event SoundDelegate EventSoundBlast;

        private void Start()
        {
            EntityMask = LayerMask.GetMask("Enemies", "Players");
        }

        [Server]
        public static bool MeleeHitArcCheck(GameObject source, GameObject target, int arc)
        {
            if (Vector3.Angle(source.gameObject.transform.forward.normalized, target.gameObject.transform.position - source.gameObject.transform.position) < arc / 2)
                return true;
            else return false;
        }

        [Server]
        public GameObject ProjectileSpawner(string id)
        {
            if (id == "NatureProj" || (id == "NatureProj2"))
            {
                GameObject projectile = Instantiate(NatureProj, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(projectile);
                return projectile;
            }
            else
            {
                GameObject projectile = Instantiate(FireProj, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(projectile);
                return projectile;
            }
        }
        [Server]
        public GameObject WaveSpawner()
        {
            GameObject wave = Instantiate(wavePrefab, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(wave);
            return wave;
        }

        [Server]
        public GameObject MineSpawner()
        {
            GameObject mine = Instantiate(MinePrefab);
            NetworkServer.Spawn(mine);
            return mine;
        }

        [ClientRpc] public void RpcBroadcastSound(string soundName) => EventSoundBlast(soundName);

        public GameObject PickRandomEnemyInCircle(Vector3 center, float radius, CharacterStatusAPI owner)
        {
            candidates.Clear();

            foreach (var hit in Physics.OverlapSphere(center, radius, EntityMask))
            {
                if (!owner.gameObject.CompareTag(hit.gameObject.tag))
                {
                    candidates.Add(hit.gameObject);
                }
            }
            var pickedIndex = Random.Range(0, candidates.Count);
            return candidates[pickedIndex];
        }

        [ClientRpc]
        public void RpcSpawnVFX(string vfxID, Vector3 pos, string soundID)
        {
            if (!vfxID.IsNullOrWhitespace())
            {
                var vfx = Instantiate(Resources.Load(vfxID)) as GameObject;
                vfx.transform.position = pos;
                vfx.GetComponent<ParticleSystem>().Play(isClientOnly);
                if (soundID != null)
                    FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Testing/{soundID}", vfx);
            }
        }
        [ClientRpc]
        public void RpcSpawnVFXTimed(string vfxID, Vector3 pos, string soundID, float duration)
        {
            if (!vfxID.IsNullOrWhitespace())
            {
                var vfx = Instantiate(Resources.Load(vfxID)) as GameObject;
                vfx.transform.position = pos;
                vfx.GetComponent<ParticleSystem>().Play(isClientOnly);
                if (soundID != null)
                    FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Testing/{soundID}", vfx);

                DestroyEffect(vfx, duration).Forget();
            }
        }
        [ClientRpc]
        public void RpcSpawnVFXTimedWithRef(string vfxID, Vector3 pos, string soundID, float duration)
        {
            if (!vfxID.IsNullOrWhitespace())
            {
                var vfx = Instantiate(Resources.Load(vfxID)) as GameObject;
                vfx.transform.position = pos;
                vfxEffects.Add(vfx);
                vfx.GetComponent<ParticleSystem>().Play(isClientOnly);
                if (soundID != null)
                    FMODUnity.RuntimeManager.PlayOneShotAttached($"event:/Testing/{soundID}", vfx);

                DestroyEffect(vfx, duration).Forget();
            }
        }
        [ClientRpc]
        public void RpcDestroyVFXByID()
        {
            foreach (var vfx in vfxEffects)
            {
                // LAL brotkind
                Destroy(vfx);
            }
            vfxEffects.Clear();
        }

        public async UniTaskVoid DestroyEffect(GameObject gameObject, float duration)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: false);
            // return to pool
            if (vfxEffects.Contains(gameObject))
                vfxEffects.Remove(gameObject);
            Destroy(gameObject);
        }

        public Vector3 PickRandomPointInRange(Vector3 center, float radius)
        {
            var r = radius * Mathf.Sqrt(Random.Range(0, 1f));
            var theta = Random.Range(0, 1f) * 2 * Mathf.PI;

            float x = r * Mathf.Cos(theta);
            float z = r * Mathf.Sin(theta);

            return new Vector3(center.x + x, 1.3f, center.z + z);
        }

        public Vector3 FindAveragePosOfEnemies(Vector3 center, float radius, CharacterStatusAPI owner)
        {
            float x = 0;
            float z = 0;
            int count = 0;

            foreach (var hit in Physics.OverlapSphere(center, radius, EntityMask))
            {
                if (!owner.gameObject.CompareTag(hit.gameObject.tag))
                {
                    x += hit.gameObject.transform.position.x;
                    z += hit.gameObject.transform.position.z;
                    count++;
                }
            }

            return new Vector3(x / count, 0, z / count);
        }

        public List<GameObject> PickAllEnemiesInRange(Vector3 center, float radius, CharacterStatusAPI owner)
        {
            var viableTargets = new List<GameObject>();

            foreach (var hit in Physics.OverlapSphere(center, radius, EntityMask))
            {
                if (!owner.gameObject.CompareTag(hit.gameObject.tag))
                    viableTargets.Add(hit.gameObject);
            }
            return viableTargets;
        }
        public List<GameObject> PickAllAlliesInRange(Vector3 center, float radius, CharacterStatusAPI owner)
        {
            var viableTargets = new List<GameObject>();

            foreach (var hit in Physics.OverlapSphere(center, radius, EntityMask))
            {
                if (owner.gameObject.CompareTag(hit.gameObject.tag))
                    viableTargets.Add(hit.gameObject);
            }
            return viableTargets;
        }
        public List<GameObject> PickAllInRange(Vector3 center, float radius, CharacterStatusAPI owner)
        {
            var viableTargets = new List<GameObject>();

            foreach (var hit in Physics.OverlapSphere(center, radius, EntityMask))
            {
                viableTargets.Add(hit.gameObject);
            }
            return viableTargets;
        }

        public GameObject PickClosestEnemy(Vector3 targetPoint, CharacterStatusAPI source)
        {
            GameObject target = null;
            float distance = 99;

            foreach (var hit in Physics.OverlapSphere(targetPoint, 5, EntityMask))
            {
                if (!source.gameObject.CompareTag(hit.gameObject.tag))
                {
                    Vector3 d = hit.gameObject.transform.position - targetPoint;
                    d.y = 0;
                    if (d.magnitude < distance)
                    {
                        target = hit.gameObject;
                        distance = d.magnitude;
                    }
                }
            }
            return target;
        }
        public GameObject PickClosestAlly(Vector3 targetPoint, CharacterStatusAPI source)
        {
            GameObject target = new GameObject();
            float distance = 99;

            foreach (var hit in Physics.OverlapSphere(targetPoint, 5, EntityMask))
            {
                if (source.gameObject.CompareTag(hit.gameObject.tag))
                {
                    Vector3 d = hit.gameObject.transform.position - targetPoint;
                    d.y = 0;
                    if (d.magnitude < distance)
                    {
                        target = hit.gameObject;
                        distance = d.magnitude;
                    }
                }
            }
            return target;
        }
        public GameObject PickClosestAll(Vector3 targetPoint)
        {
            GameObject target = new GameObject();
            float distance = 99;

            foreach (var hit in Physics.OverlapSphere(targetPoint, 5, EntityMask))
            {
                Vector3 d = hit.gameObject.transform.position - targetPoint;
                d.y = 0;
                if (d.magnitude < distance)
                {
                    target = hit.gameObject;
                    distance = d.magnitude;
                }
            }
            return target;
        }

        public IEnumerator SimpleDelay(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }
    }
}
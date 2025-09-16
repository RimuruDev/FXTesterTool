using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AbyssMoth.FXTesterTool
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class FXTester : MonoBehaviour
    {
        #region Data

        [Tooltip("Тег объекта игрока, по которому срабатывают триггеры")]
        public string PlayerTag = "Player";

        [Tooltip("Автосбор всех ParticleSystem из детей")]
        public bool AutoCollectChildren = true;

        [Tooltip("Запустить эффект при старте сцены")]
        public bool PlayOnStart;

        [Tooltip("Запускать при входе игрока в триггер")]
        public bool PlayOnTriggerEnter;

        [Tooltip("Останавливать при выходе игрока из триггера")]
        public bool StopOnTriggerExit;

        [Tooltip("Играть в цикле")] public bool Loop;

        [Tooltip("Повторный триггер перезапускает эффект")]
        public bool RestartOnRetrigger = true;

        [Tooltip("Возвращать исходные значения loop у партиклов при остановке")]
        public bool RestoreOriginalLoopOnStop = true;

        // TODO: Для новой системы ввода нужно добавить варнинг в FXTesterEditor для поля ManualKey, что бы не вводить в заблуждение худа.
        [Tooltip("Горячая клавиша одноразового запуска")]
        public KeyCode ManualKey = KeyCode.None;

        [Tooltip("Задержка перед запуском, сек")]
        public float Delay;

        [Tooltip("Кулдаун между запусками, сек")]
        public float Cooldown;

        [Tooltip("Максимум запусков (0 — без лимита)")]
        public int MaxPlays;

        [Tooltip("Список управляемых ParticleSystem; можно оставить пустым и включить автосбор")] [SerializeField]
        private List<ParticleSystem> particles = new();

        private readonly List<bool> originalLoops = new();
        private Coroutine pending;
        private float nextAllowedTime;
        private int playsCount;
        private Collider col3D;
        private Collider2D col2D;

        #endregion

        #region Unity API

        private void Awake()
        {
            col3D = GetComponent<Collider>();
            col2D = GetComponent<Collider2D>();

            if (col3D != null)
                col3D.isTrigger = true;

            if (col2D != null)
                col2D.isTrigger = true;

            if (AutoCollectChildren)
                CollectChildren();

            CacheOriginalLoops();

            if (PlayOnStart)
                TriggerPlay();
        }

        private void OnValidate()
        {
            col3D = GetComponent<Collider>();
            col2D = GetComponent<Collider2D>();

            if (col3D != null)
                col3D.isTrigger = true;

            if (col2D != null)
                col2D.isTrigger = true;

            if (AutoCollectChildren)
                CollectChildren();

            CacheOriginalLoops();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (ManualKey != KeyCode.None && Input.GetKeyDown(ManualKey)) 
                TriggerPlay();
#endif
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayOnTriggerEnter)
                return;

            if (!other.CompareTag(PlayerTag))
                return;

            TriggerPlay();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!StopOnTriggerExit)
                return;

            if (!other.CompareTag(PlayerTag))
                return;

            StopAll(clear: true, restoreLoops: true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayOnTriggerEnter)
                return;

            if (!other.CompareTag(PlayerTag))
                return;

            TriggerPlay();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!StopOnTriggerExit)
                return;

            if (!other.CompareTag(PlayerTag))
                return;

            StopAll(clear: true, restoreLoops: true);
        }

        #endregion

        #region Public API

        [ContextMenu("Play Once")]
        public void PlayOnce()
        {
            Loop = false;
            TriggerPlay();
        }

        [ContextMenu("Play Loop")]
        public void PlayLoop()
        {
            Loop = true;
            TriggerPlay();
        }

        [ContextMenu("Stop")]
        public void StopAllContext() =>
            StopAll(clear: true, restoreLoops: true);

        public void SetParticles(IEnumerable<ParticleSystem> set)
        {
            particles.Clear();
            originalLoops.Clear();

            foreach (var particle in set)
                particles.Add(particle);

            CacheOriginalLoops();
        }

        #endregion

        #region Private API

        private void TriggerPlay()
        {
            if (MaxPlays > 0 && playsCount >= MaxPlays)
                return;

            if (Time.time < nextAllowedTime)
                return;

            if (pending != null)
            {
                if (!RestartOnRetrigger)
                    return;

                StopCoroutine(pending);

                pending = null;
            }

            pending = StartCoroutine(StartRoutine());
        }

        private IEnumerator StartRoutine()
        {
            nextAllowedTime = Time.time + Mathf.Max(Cooldown, 0f);

            if (Delay > 0f)
                yield return new WaitForSeconds(Delay);

            ApplyLoopOverride(Loop);

            if (RestartOnRetrigger)
                StopAll(clear: false, restoreLoops: false);

            PlayAll();

            playsCount++;
            pending = null;
        }

        private void PlayAll()
        {
            for (var i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (p == null)
                    continue;

                p.Play(withChildren: true);
            }
        }

        private void StopAll(bool clear, bool restoreLoops)
        {
            for (var i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                if (particle == null)
                    continue;

                particle.Stop(withChildren: true,
                    clear
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting);
            }

            if (restoreLoops && RestoreOriginalLoopOnStop)
                RestoreOriginalLoops();
        }

        private void ApplyLoopOverride(bool loop)
        {
            for (var i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (p == null)
                    continue;

                var main = p.main;
                main.loop = loop;
            }
        }

        private void CollectChildren()
        {
            particles.Clear();
            GetComponentsInChildren(includeInactive: true, particles);
        }

        private void CacheOriginalLoops()
        {
            originalLoops.Clear();

            for (var i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                if (p == null)
                {
                    originalLoops.Add(false);
                    continue;
                }

                var main = p.main;

                originalLoops.Add(main.loop);
            }
        }

        private void RestoreOriginalLoops()
        {
            for (var i = 0; i < particles.Count; i++)
            {
                var p = particles[i];

                if (p == null)
                    continue;

                var main = p.main;

                var value = i < originalLoops.Count
                    ? originalLoops[i]
                    : main.loop;

                main.loop = value;
            }
        }

        #endregion
    }
}